using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobMatching.Domain.Interfaces;
using JobMatching.Domain.Entities;

[Route("api/payment/mpesa")]
[ApiController]
[Authorize(Roles = "JobSeeker")]
public class MpesaController : ControllerBase
{
    private readonly MpesaService _mpesaService;

    public MpesaController(MpesaService mpesaService)
    {
        _mpesaService = mpesaService;
    }

    /// <summary>
    /// 🔥 Job Seeker Initiates M-Pesa Payment
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PayWithMpesa([FromBody] PaymentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized("User not found.");

        if (string.IsNullOrEmpty(request.PhoneNumber)) return BadRequest("Phone number is required.");
        var transactionId = await _mpesaService.InitiatePaymentAsync(request.PhoneNumber, request.Amount, userId);
        return Ok(new { TransactionId = transactionId });
    }

    /// <summary>
    /// 🔥 M-Pesa Webhook: Processes M-Pesa Payment Updates
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]  // Webhooks must be accessible to external services
    public async Task<IActionResult> MpesaWebhook()
    {
        var success = await _mpesaService.ProcessMpesaWebhookAsync(Request);
        if (!success) return BadRequest("Webhook processing failed");

        return Ok("M-Pesa payment processed successfully.");
    }
}
