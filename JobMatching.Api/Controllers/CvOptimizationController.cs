using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JobMatching.Domain.Entities;

[Route("api/cv")]
[ApiController]
[Authorize(Roles = "JobSeeker")]  // 🔥 Only job seekers can optimize CVs
public class CvOptimizationController : ControllerBase
{
    private readonly CvOptimizationService _cvOptimizationService;
    private readonly UserManager<User> _userManager;

    public CvOptimizationController(CvOptimizationService cvOptimizationService, UserManager<User> userManager)
    {
        _cvOptimizationService = cvOptimizationService;
        _userManager = userManager;
    }

    // 🔥 Job Seeker Submits Resume for AI Optimization
    [HttpPost("optimize")]
    public async Task<IActionResult> OptimizeResume([FromBody] ResumeOptimizationRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var optimizedResume = await _cvOptimizationService.OptimizeResumeAsync(request);

        return Ok(new
        {
            optimizedResume.OptimizedResume,
            optimizedResume.MissingSkills
        });
    }
}
