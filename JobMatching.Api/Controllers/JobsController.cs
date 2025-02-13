using JobMatching.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using JobMatching.Application.Services;
using Microsoft.AspNetCore.Authorization;

[Route("api/jobs")]
[ApiController]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly JobService _jobService;
    public JobsController(JobService jobService) => _jobService = jobService;

    [HttpGet]
    public async Task<IActionResult> GetJobs() => Ok(await _jobService.GetJobsAsync());

    [HttpPost]
    [Authorize(Roles = "Recruiter")]
    public async Task<IActionResult> PostJob([FromBody] Job job)
    {
        await _jobService.AddJobAsync(job);
        return CreatedAtAction(nameof(GetJobs), new { id = job.Id }, job);
    }
}
