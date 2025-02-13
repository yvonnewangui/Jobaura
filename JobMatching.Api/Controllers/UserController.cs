using System.Security.Claims;
using JobMatching.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[Route("api/user")]
[ApiController]
[Authorize(Roles = "JobSeeker")]
public class UserController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    public UserController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    // 🔥 Get Subscription Status
    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscriptionStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized("User ID not found.");
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized("User not found.");

        return Ok(new
        {
            SubscriptionTier = user.SubscriptionTier,
            ExpiryDate = user.SubscriptionExpiry
        });
    }
}
