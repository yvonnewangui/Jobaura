using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JobMatching.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class PredictiveHiringService
{
    private readonly HttpClient _httpClient;
    private readonly string _openAiApiKey;
    private readonly ILogger<PredictiveHiringService> _logger;

    public PredictiveHiringService(IConfiguration configuration, ILogger<PredictiveHiringService> logger)
    {
        _httpClient = new HttpClient();
        _openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "OpenAI API Key is required");
        _logger = logger;
    }

    public async Task<HiringScoreResult> CalculateHiringScoreAsync(User user, Job job)
    {
        var prompt = $$"""
            Predict the hiring score for {{user.FullName}} applying for {{job.Title}} at {{job.Company}}.
            - Candidate's Skills: {{string.Join(", ", user.Skills)}}
            - Experience: {{user.Experience}}
            - Job Requirements: {{string.Join(", ", job.SkillsRequired)}}

            Provide a hiring score (1-100) and improvement suggestions in JSON format:
            {
                "score": 85,
                "reason": "Candidate matches 85% of required skills but lacks experience with cloud platforms.",
                "improvements": ["AWS Certification", "More experience in Microservices"]
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
                return new HiringScoreResult { Score = 0, Reason = "API call failed" };
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
            return JsonSerializer.Deserialize<HiringScoreResult>(result?.Choices?[0]?.Message?.Content ?? "{}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error calculating hiring score: {Message}", ex.Message);
            return new HiringScoreResult { Score = 0, Reason = "Error calculating hiring score" };
        }
    }
}


// 🔹 Data Model for AI-Powered Hiring Score Result
public class HiringScoreResult
{
    public int Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> Improvements { get; set; } = new();
}
