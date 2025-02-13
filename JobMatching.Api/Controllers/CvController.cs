using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JobMatching.Domain.Entities;

[Route("api/cv")]
[ApiController]
[Authorize(Roles = "JobSeeker")]  // 🔥 Only job seekers can upload CVs
public class CvController : ControllerBase
{
    private readonly CvUploadService _cvUploadService;
    private readonly CvParsingService _cvParsingService;
    private readonly UserManager<User> _userManager;

    public CvController(CvUploadService cvUploadService, CvParsingService cvParsingService, UserManager<User> userManager)
    {
        _cvUploadService = cvUploadService;
        _cvParsingService = cvParsingService;
        _userManager = userManager;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadCv([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Invalid file.");

        // Get authenticated user
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        // Upload file
        var filePath = await _cvUploadService.UploadCvAsync(file);
        if (filePath == null) return BadRequest("Invalid file format.");

        // Extract CV text and parse skills
        var cvText = await ExtractTextFromFileAsync(file);
        var extractedData = await _cvParsingService.ExtractCvDetailsFromText(cvText);

        // Update user profile with CV data
        user.ResumeUrl = filePath;
        user.Skills = extractedData.Skills;
        user.Experience = extractedData.Experience;
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            Message = "CV uploaded and parsed successfully",
            FilePath = filePath,
            ExtractedSkills = user.Skills,
            ExtractedExperience = user.Experience
        });
    }

    private static async Task<string> ExtractTextFromFileAsync(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        return await reader.ReadToEndAsync();
    }
}
