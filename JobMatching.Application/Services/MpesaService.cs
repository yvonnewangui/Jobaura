using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JobMatching.Domain.Entities;
using JobMatching.Domain.Interfaces;

public class MpesaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<MpesaService> _logger;

    public MpesaService(IConfiguration configuration, IPaymentRepository paymentRepository, ILogger<MpesaService> logger)
    {
        _httpClient = new HttpClient();
        _configuration = configuration;
        _paymentRepository = paymentRepository;
        _logger = logger;
    }
    
    // 🔥 1️⃣ Initiate Payment Request to M-Pesa
    public async Task<string> InitiatePaymentAsync(string phoneNumber, decimal amount, string userId)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Failed to get M-Pesa access token.");
                return "Failed to get access token";
            }

            var transactionId = Guid.NewGuid().ToString(); // Generate Unique Transaction ID

            // 🔥 Store Pending Payment in DB
            var payment = new PaymentRecord
            {
                UserId = userId, // ✅ Only store UserId instead of creating a new User object
                PaymentProvider = "Mpesa",
                Amount = amount,
                Status = "Pending",
                TransactionId = transactionId
            };

            await _paymentRepository.SavePaymentAsync(payment);

            var request = new
            {
                BusinessShortCode = _configuration["Mpesa:BusinessShortCode"],
                Password = GeneratePassword(),
                Timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                TransactionType = "CustomerPayBillOnline",
                Amount = amount,
                PartyA = phoneNumber,
                PartyB = _configuration["Mpesa:BusinessShortCode"],
                PhoneNumber = phoneNumber,
                CallBackURL = _configuration["Mpesa:CallBackURL"],
                AccountReference = "JobMatching Subscription",
                TransactionDesc = "Job Matching Subscription Payment"
            };

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.PostAsync(
                "https://api.safaricom.co.ke/mpesa/stkpush/v1/processrequest",
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"M-Pesa payment initiation failed: {response.StatusCode}");
                return "M-Pesa payment request failed";
            }

            return transactionId;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error initiating M-Pesa payment: {ex.Message}");
            return "Error processing payment";
        }
    }

    // 🔥 2️⃣ Confirm Payment & Update Subscription
    public async Task<bool> ConfirmPaymentAsync(string transactionId)
    {
        if (string.IsNullOrEmpty(transactionId))
        {
            _logger.LogError("Invalid transaction ID provided for confirmation.");
            return false;
        }

        var updated = await _paymentRepository.UpdatePaymentStatusAsync(transactionId, "Completed");

        if (updated)
        {
            _logger.LogInformation($"Payment {transactionId} marked as completed.");
        }
        else
        {
            _logger.LogWarning($"Failed to update payment status for {transactionId}.");
        }

        return updated;
    }

    // 🔥 3️⃣ Get M-Pesa Access Token
    private async Task<string> GetAccessTokenAsync()
    {
        try
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_configuration["Mpesa:ConsumerKey"]}:{_configuration["Mpesa:ConsumerSecret"]}"));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            var response = await _httpClient.GetStringAsync("https://api.safaricom.co.ke/oauth/v1/generate?grant_type=client_credentials");

            using var doc = JsonDocument.Parse(response);
            return doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving M-Pesa access token: {ex.Message}");
            return string.Empty;
        }
    }

    // 🔥 4️⃣ Generate M-Pesa Password
    private string GeneratePassword()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var passwordBytes = Encoding.UTF8.GetBytes($"{_configuration["Mpesa:BusinessShortCode"]}{_configuration["Mpesa:PassKey"]}{timestamp}");
        return Convert.ToBase64String(passwordBytes);
    }

    // 🔥 5️⃣ Webhook: Receive M-Pesa Payment Confirmation
    public async Task<bool> ProcessMpesaWebhookAsync(HttpRequest request)
    {
        try
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();
            _logger.LogInformation($"Received M-Pesa Webhook: {body}");

            var json = JsonDocument.Parse(body);

            if (!json.RootElement.TryGetProperty("Body", out var bodyElement) ||
                !bodyElement.TryGetProperty("stkCallback", out var callbackElement) ||
                !callbackElement.TryGetProperty("CheckoutRequestID", out var transactionIdElement))
            {
                _logger.LogError("Invalid M-Pesa webhook payload.");
                return false;
            }

            var transactionId = transactionIdElement.GetString();
            var resultCode = callbackElement.GetProperty("ResultCode").GetInt32();

            if (string.IsNullOrEmpty(transactionId))
            {
                _logger.LogError("M-Pesa webhook does not contain a valid transaction ID.");
                return false;
            }

            var status = resultCode == 0 ? "Completed" : "Failed";
            var updated = await _paymentRepository.UpdatePaymentStatusAsync(transactionId, status);

            if (updated)
            {
                _logger.LogInformation($"M-Pesa payment {transactionId} marked as {status}.");
            }
            else
            {
                _logger.LogWarning($"Failed to update payment status for {transactionId}.");
            }

            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing M-Pesa webhook: {ex.Message}");
            return false;
        }
    }
}
