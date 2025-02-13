using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JobMatching.Domain.Entities;
using JobMatching.Domain.Interfaces;

[Route("api/payment")]
[ApiController]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly UserManager<User> _userManager;

    public PaymentController(IPaymentRepository paymentRepository, UserManager<User> userManager)
    {
        _paymentRepository = paymentRepository;
        _userManager = userManager;
    }

    /// <summary>
    /// 🔥 Job Seeker Views Payment History
    /// </summary>
    [HttpGet("history")]
    [Authorize(Roles = "JobSeeker")]
    public async Task<IActionResult> GetPaymentHistory()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var payments = await _paymentRepository.GetPaymentsByUserAsync(userId);
        return Ok(payments);
    }

    /// <summary>
    /// 🔥 Confirm Payment & Upgrade Subscription
    /// </summary>
    [HttpPost("confirm-payment/{transactionId}")]
    [Authorize(Roles = "JobSeeker")]
    public async Task<IActionResult> ConfirmPayment(string transactionId)
    {
        var updated = await _paymentRepository.UpdatePaymentStatusAsync(transactionId, "Completed");
        if (!updated) return BadRequest("Payment not found or already processed.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized("User not found.");

        // Upgrade Subscription Based on Payment Amount
        var payment = await _paymentRepository.GetPaymentByTransactionId(transactionId);

        if (payment == null)
        {
            return BadRequest("Payment not found.");
        }

        if (payment.Amount >= 50) // Example: $50 or 5000 KES
        {
            user.SubscriptionTier = "Pro";
            user.SubscriptionExpiry = DateTime.UtcNow.AddMonths(3); // 3-Month Subscription
        }
        else
        {
            user.SubscriptionTier = "Premium";
            user.SubscriptionExpiry = DateTime.UtcNow.AddMonths(1); // 1-Month Subscription
        }

        await _userManager.UpdateAsync(user);
        return Ok(new { Message = "Subscription updated successfully!", SubscriptionTier = user.SubscriptionTier });
    }

    /// <summary>
    /// 🔥 Admin Views All Payments
    /// </summary>
    [HttpGet("all-payments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllPayments()
    {
        var payments = await _paymentRepository.GetAllPaymentsAsync();
        return Ok(payments);
    }
}

public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string? PhoneNumber { get; set; } // Optional (Required only for M-Pesa)
}
