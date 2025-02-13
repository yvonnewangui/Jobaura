using Microsoft.AspNetCore.Mvc;
using JobMatching.Application.Services;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text.Json;
using JobMatching.Domain.Entities;
using Microsoft.AspNetCore.Identity;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UserManager<User> _userManager;

    public AuthController(AuthService authService, UserManager<User> userManager)
    {
        _authService = authService;
        _userManager = userManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var token = await _authService.RegisterUserAsync(model.Email, model.Password, model.Role);
        if (string.IsNullOrEmpty(token)) return BadRequest("Registration failed.");
        return Ok(new { Token = token });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var token = await _authService.LoginUserAsync(model.Email, model.Password);
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        return Ok(new { Token = token });
    }
    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { provider = "Google" });
        return Challenge(new AuthenticationProperties { RedirectUri = redirectUrl }, "Google");
    }

    [HttpGet("linkedin")]
    public IActionResult LinkedInLogin()
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { provider = "LinkedIn" });
        return Challenge(new AuthenticationProperties { RedirectUri = redirectUrl }, "LinkedIn");
    }

    [HttpGet("external-login-callback")]
    public async Task<IActionResult> ExternalLoginCallback()
    {
        var info = await HttpContext.AuthenticateAsync();
        if (!info.Succeeded) return Unauthorized();

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var provider = info.Principal.Identity.AuthenticationType;

        if (email == null) return BadRequest("Email not found");

        var token = await _authService.ExternalLoginAsync(provider, email);
        return Ok(new { Token = token });
    }

    [HttpGet("linkedin-callback")]
    public async Task<IActionResult> LinkedInCallback()
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrEmpty(accessToken)) return Unauthorized();

        var profilePictureUrl = await _authService.FetchLinkedInProfileDataAsync(accessToken);

        var email = User.FindFirstValue(ClaimTypes.Email);
        if (email == null) return BadRequest("Email not found");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return Unauthorized();

        if (profilePictureUrl != null)
        {
            user.ProfilePictureUrl = profilePictureUrl;
        }
        await _userManager.UpdateAsync(user);

        var token = _authService.GenerateJwtToken(user);
        return Ok(new { Token = token, ProfilePicture = profilePictureUrl });
    }
}

public class RegisterModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "JobSeeker"; // Default role
}

public class LoginModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LinkedInProfileDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string ProfilePictureUrl { get; set; }
}
