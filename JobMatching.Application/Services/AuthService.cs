using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using JobMatching.Domain.Entities;
using System.Net.Http.Headers;
using System.Text.Json;

namespace JobMatching.Application.Services;

public class AuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
     private readonly HttpClient _httpClient;

    public AuthService(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration, HttpClient httpClient)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<string> RegisterUserAsync(string email, string password, string role)
    {
        var user = new User { Email = email, UserName = email, Role = role };
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
            return string.Join(", ", result.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, role);
        return GenerateJwtToken(user);
    }

    public async Task<string?> LoginUserAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return null;

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
        if (!result.Succeeded) return null;

        return GenerateJwtToken(user);
    }

    public async Task<string> ExternalLoginAsync(string provider, string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new User { Email = email, UserName = email, Role = "JobSeeker" };
            await _userManager.CreateAsync(user);
        }

        return GenerateJwtToken(user);
    }
    
    public string GenerateJwtToken(User user)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
            claims: claims);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public async Task<string?> FetchLinkedInProfileDataAsync(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync("https://api.linkedin.com/v2/me");
        if (!response.IsSuccessStatusCode) return null;

        var profileJson = await response.Content.ReadAsStringAsync();
        var profileData = JsonSerializer.Deserialize<LinkedInProfileDto>(profileJson);

        return profileData?.ProfilePictureUrl;
    }
}

public class LinkedInProfileDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string ProfilePictureUrl { get; set; }
}
