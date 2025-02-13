using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/interview-prep")]
[ApiController]
[Authorize(Roles = "JobSeeker")]  //Only job seekers can access this
public class InterviewPrepController : ControllerBase
{
    private readonly InterviewPrepService _interviewPrepService;

    public InterviewPrepController(InterviewPrepService interviewPrepService)
    {
        _interviewPrepService = interviewPrepService;
    }

    // Generate AI-Powered Interview Questions
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateInterviewQuestions([FromBody] InterviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JobTitle) || request.Skills == null || request.Skills.Count == 0)
            return BadRequest("Job title and skills are required.");

        if (request.QuestionCount < 1 || request.QuestionCount > 15)
            return BadRequest("Question count must be between 1 and 15.");

        var questions = await _interviewPrepService.GenerateInterviewQuestionsAsync(request.JobTitle, request.Skills, request.QuestionCount);
        return Ok(questions);
    }
}

public class InterviewRequest
{
    public string JobTitle { get; set; } = string.Empty;
    public int QuestionCount { get; set; } = 5;
    public List<string> Skills { get; set; } = new();
}
