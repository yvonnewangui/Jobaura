using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JobMatching.Domain.Entities;
using JobMatching.Application.Services;

[Route("api/job-matching")]
[ApiController]
[Authorize(Roles = "JobSeeker")]  // 🔥 Only Job Seekers Can Get Matched Jobs
public class JobMatchingController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly JobMatchingService _jobMatchingService;

    public JobMatchingController(UserManager<User> userManager, JobMatchingService jobMatchingService)
    {
        _userManager = userManager;
        _jobMatchingService = jobMatchingService;
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetJobRecommendations()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var jobSeeker = await _userManager.FindByIdAsync(userId);

        if (jobSeeker == null) return Unauthorized();

        var recommendedJobs = await _jobMatchingService.GetRecommendedJobsAsync(jobSeeker);
        return Ok(recommendedJobs);
    }
}
