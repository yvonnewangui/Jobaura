

using JobMatching.Domain.Entities;

namespace JobMatching.Api.Controllers
{
    using System.Security.Claims;
    using System.Threading.Tasks;
    using JobMatching.Domain.Entities;
    using JobMatching.Application.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using JobMatching.Domain.Interfaces;

    [Route("api/auto-apply")]
    [ApiController]
    [Authorize(Roles = "JobSeeker")]
    public class AutoApplyController : ControllerBase
    {
        private readonly AutoApplyService _autoApplyService;
        private readonly IUserRepository _userRepository;

        public AutoApplyController(AutoApplyService autoApplyService, IUserRepository userRepository)
        {
            _autoApplyService = autoApplyService;
            _userRepository = userRepository;
        }

        /// <summary>
        /// 🔥 Auto-applies to multiple jobs, generating custom resumes & cover letters.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AutoApply([FromBody] AutoApplyRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized("User not found.");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound("User profile not found.");

            if (string.IsNullOrEmpty(user.ResumeUrl))
                return BadRequest("Please upload a resume before applying for jobs.");

            if (request.SelectedJobs == null || request.SelectedJobs.Count == 0)
                return BadRequest("No jobs provided for auto-apply.");

            // Convert JobRequest to Job Entity
            var jobsToApply = request.SelectedJobs.Select(job => new Job
            {
                Id = job.Id,
                Title = job.Title,
                Company = job.Company,
                Description = job.Description,
                SkillsRequired = job.SkillsRequired,
                //TODO: Add Recruiter to Job Entity
                Recruiter = job.Recruiter
            }).ToList();

            var result = await _autoApplyService.AutoApplyAsync(user, jobsToApply);
            return Ok(result);
        }
    }


}

public class AutoApplyRequest
{
    public List<JobRequest> SelectedJobs { get; set; } = new();
}

/// <summary>
/// Represents a job that a user wants to auto-apply to.
/// </summary>
public class JobRequest
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public User Recruiter { get; set; } = new();
    public List<string> SkillsRequired { get; set; } = new();
}
