using JobMatching.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

[Route("api/user")]
[ApiController]
[Authorize]
public class UserProfileController : ControllerBase
{
    private readonly UserProfileService _userProfileService;

    public UserProfileController(UserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User ID is null");
        var user = await _userProfileService.GetUserProfileAsync(userId);
        if (user == null) return NotFound("User not found");

        return Ok(user);
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateProfile([FromBody] User updatedUser)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest("User ID is null");
        var success = await _userProfileService.UpdateUserProfileAsync(userId, updatedUser);
        if (!success) return BadRequest("Profile update failed");

        return Ok("Profile updated successfully");
    }
}
