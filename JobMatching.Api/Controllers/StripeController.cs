using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using JobMatching.Domain.Interfaces;
using JobMatching.Domain.Entities;

[Route("api/payment/stripe")]
[ApiController]
[Authorize(Roles = "JobSeeker")]
public class StripeController : ControllerBase
{
    private readonly StripeService _stripeService;
    private readonly ILogger<StripeController> _logger;

    public StripeController(StripeService stripeService, ILogger<StripeController> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    /// <summary>
    /// 🔥 Job Seeker Initiates Stripe Payment
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PayWithStripe([FromBody] PaymentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized("User not found.");

        var sessionUrl = await _stripeService.CreateCheckoutSessionAsync(
            request.Amount, userId,
            "https://yourfrontend.com/payment-success",
            "https://yourfrontend.com/payment-failed"
        );

        return Ok(new { CheckoutUrl = sessionUrl });
    }

    /// <summary>
    /// 🔥 Stripe Webhook: Processes Stripe Payment Events
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook([FromHeader(Name = "Stripe-Signature")] string stripeSignature)
    {
        _logger.LogInformation("🔔 Received Stripe webhook request.");

        try
        {
            using var reader = new StreamReader(Request.Body);
            var jsonPayload = await reader.ReadToEndAsync();

            var success = await _stripeService.ProcessStripeWebhookAsync(jsonPayload, stripeSignature);

            return success ? Ok() : BadRequest("Webhook processing failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook: {Message}", ex.Message);
            return BadRequest("Webhook processing error.");
        }
    }
}
