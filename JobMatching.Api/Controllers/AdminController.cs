using JobMatching.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[Microsoft.AspNetCore.Mvc.Route("api/admin")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    public AdminController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    // 🔥 Get List of All Users & Subscriptions
    [HttpGet("subscriptions")]
    public IActionResult GetAllSubscriptions()
    {
        var users = _userManager.Users.Select(user => new
        {
            user.Id,
            user.Email,
            user.SubscriptionTier,
            user.SubscriptionExpiry
        }).ToList();

        return Ok(users);
    }
}
