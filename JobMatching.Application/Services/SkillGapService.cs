using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JobMatching.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class SkillGapService
{
    private readonly HttpClient _httpClient;
    private readonly string _openAiApiKey;
    private readonly ILogger<SkillGapService> _logger;

    public SkillGapService(IConfiguration configuration, ILogger<SkillGapService> logger)
    {
        _httpClient = new HttpClient();
        _openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "OpenAI API Key is required");
        _logger = logger;
    }

    public async Task<SkillGapAnalysisResult> AnalyzeSkillGapsAsync(User user, Job job)
    {
        var prompt = $$"""
            Identify skill gaps for {{user.FullName}} applying for {{job.Title}} at {{job.Company}}.
            - Candidate's Skills: {{string.Join(", ", user.Skills)}}
            - Job Requirements: {{string.Join(", ", job.SkillsRequired)}}
            
            List missing skills and recommend relevant courses from Udemy, Coursera, and LinkedIn Learning.

            Respond in JSON format:
            {
                "missingSkills": ["AWS Cloud", "Microservices", "CI/CD"],
                "recommendedCourses": [
                    {
                        "platform": "Udemy",
                        "courseTitle": "AWS Certified Solutions Architect",
                        "link": "https://www.udemy.com/course/aws-solutions-architect/"
                    },
                    {
                        "platform": "Coursera",
                        "courseTitle": "Microservices with Spring Boot",
                        "link": "https://www.coursera.org/learn/microservices"
                    }
                ]
            }
        """;

        var requestBody = new
        {
            model = "gpt-4-turbo",
            messages = new[] { new { role = "user", content = prompt } },
            max_tokens = 500,
            temperature = 0.5
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);

        try
        {
            var response = await _httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API call failed: {StatusCode}", response.StatusCode);
                return new SkillGapAnalysisResult { MissingSkills = new List<string>(), RecommendedCourses = new List<OnlineCourse>() };
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
            return JsonSerializer.Deserialize<SkillGapAnalysisResult>(result?.Choices?[0]?.Message?.Content ?? "{}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error analyzing skill gaps: {Message}", ex.Message);
            return new SkillGapAnalysisResult { MissingSkills = new List<string>(), RecommendedCourses = new List<OnlineCourse>() };
        }
    }
}

public class SkillGapAnalysisResult
{
    public List<string> MissingSkills { get; set; } = new();
    public List<OnlineCourse> RecommendedCourses { get; set; } = new();
}

public class OnlineCourse
{
    public string Platform { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
}