using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JobMatching.Domain.Entities;
using JobMatching.Domain.Interfaces;
using Stripe;
using Stripe.Checkout;

public class StripeService
{
    private readonly IConfiguration _configuration;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<StripeService> _logger;
    const string CheckoutSessionCompleted = "checkout.session.completed";
    const string PaymentIntentSucceeded = "payment_intent.succeeded";
    const string PaymentIntentFailed = "payment_intent.payment_failed";


    public StripeService(IConfiguration configuration, IPaymentRepository paymentRepository, ILogger<StripeService> logger)
    {
        _configuration = configuration;
        _paymentRepository = paymentRepository;
        _logger = logger;

        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    // 🔥 1️⃣ Create Stripe Checkout Session
    public async Task<string?> CreateCheckoutSessionAsync(decimal amount, string userId, string successUrl, string cancelUrl)
    {
        if (amount <= 0)
        {
            _logger.LogError("Invalid payment amount: {Amount}", amount);
            return null;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogError("User ID cannot be null or empty.");
            return null;
        }

        try
        {
            var sessionService = new SessionService();
            var session = await sessionService.CreateAsync(new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)(amount * 100),  // Convert to cents
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Job Matching Premium Subscription"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            });

            if (session == null || string.IsNullOrEmpty(session.Id))
            {
                _logger.LogError("Stripe session creation failed. No session ID returned.");
                return null;
            }

            // 🔥 Store Pending Payment in DB
            var payment = new PaymentRecord
            {
                UserId = userId,
                PaymentProvider = "Stripe",
                Amount = amount,
                Status = "Pending",
                TransactionId = session.Id
            };

            await _paymentRepository.SavePaymentAsync(payment);

            _logger.LogInformation("Stripe checkout session created successfully: {SessionId}", session.Id);

            return session.Url;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating Stripe checkout session: {Message}", ex.Message);
            return null;
        }
    }

    // 🔥 2️⃣ Confirm Stripe Payment & Upgrade Subscription
    public async Task<bool> ConfirmPaymentAsync(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            _logger.LogError("Invalid transaction ID provided for confirmation.");
            return false;
        }

        try
        {
            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(transactionId);

            if (session == null)
            {
                _logger.LogError("Stripe session not found for transaction ID: {TransactionId}", transactionId);
                return false;
            }

            if (session.PaymentStatus == "paid")
            {
                var updated = await _paymentRepository.UpdatePaymentStatusAsync(transactionId, "Completed");

                if (updated)
                {
                    _logger.LogInformation("Payment {TransactionId} marked as completed.", transactionId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to update payment status for {TransactionId}.", transactionId);
                }
            }
            else
            {
                _logger.LogWarning("Payment {TransactionId} is not completed yet. Status: {PaymentStatus}", transactionId, session.PaymentStatus);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error confirming Stripe payment: {Message}", ex.Message);
            return false;
        }
    }

    // 🔥 3️⃣ Webhook: Process Stripe Payment Events
    public async Task<bool> ProcessStripeWebhookAsync(string jsonPayload, string stripeSignature)
    {
        try
        {
            var endpointSecret = _configuration["Stripe:WebhookSecret"];

            if (string.IsNullOrEmpty(endpointSecret))
            {
                _logger.LogError("Stripe webhook secret is not configured.");
                return false;
            }

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(jsonPayload, stripeSignature, endpointSecret);
            }
            catch (StripeException e)
            {
                _logger.LogError("Invalid Stripe webhook signature: {Message}", e.Message);
                return false;
            }

            switch (stripeEvent.Type)
            {
                case CheckoutSessionCompleted:
                    var session = stripeEvent.Data.Object as Session;
                    if (session?.PaymentIntentId != null)
                    {
                        _logger.LogInformation("Checkout session completed for transaction: {TransactionId}", session.Id);
                        return await ConfirmPaymentAsync(session.Id);
                    }
                    break;

                case PaymentIntentSucceeded:
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        _logger.LogInformation("Payment succeeded for intent: {PaymentIntentId}", paymentIntent.Id);
                        return await ConfirmPaymentAsync(paymentIntent.Id);
                    }
                    break;

                case PaymentIntentFailed:
                    var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (failedIntent != null)
                    {
                        _logger.LogWarning("Payment failed for intent: {PaymentIntentId}", failedIntent.Id);
                        await _paymentRepository.UpdatePaymentStatusAsync(failedIntent.Id, "Failed");
                    }
                    break;

                default:
                    _logger.LogWarning("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error processing Stripe webhook: {Message}", ex.Message);
            return false;
        }
    }
}