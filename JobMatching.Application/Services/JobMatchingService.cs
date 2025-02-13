using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using JobMatching.Domain.Entities;
using JobMatching.Domain.Interfaces;
using System.Collections.Concurrent;

public class JobMatchingService
{
    private readonly IJobRepository _jobRepository;
    private readonly HttpClient _httpClient;
    private readonly string _openAiApiKey;
    private static readonly ConcurrentDictionary<string, List<Job>> _jobCache = new();

    public JobMatchingService(IJobRepository jobRepository, IConfiguration configuration)
    {
        _jobRepository = jobRepository;
        _httpClient = new HttpClient();
        _openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "OpenAI API Key is required");

        // ✅ API Key is now correctly set in request headers
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
    }

    // 🔥 1️⃣ Get Recommended Jobs for a Job Seeker
    public async Task<List<Job>> GetRecommendedJobsAsync(User jobSeeker)
    {
        if (string.IsNullOrEmpty(jobSeeker.Id))
            throw new ArgumentException("User ID cannot be null", nameof(jobSeeker));

        // 🔹 Check if we already cached jobs for this user
        if (_jobCache.TryGetValue(jobSeeker.Id, out var cachedJobs))
            return cachedJobs;

        var allJobs = await _jobRepository.GetAllAsync();
        var prompt = GenerateMatchingPrompt(jobSeeker, allJobs);

        var requestBody = new
        {
            model = "gpt-4-turbo",
            messages = new[]
            {
                new { role = "system", content = "You are an AI job-matching assistant that finds the best job matches based on skills, experience, and behavioral data." },
                new { role = "user", content = prompt }
            },
            max_tokens = 300,
            temperature = 0.7
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);

        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"OpenAI API call failed: {response.StatusCode}. Error: {errorMessage}");
        }

        var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();

        var recommendedJobs = ParseRecommendedJobs(result, allJobs);

        // 🔹 Cache results for this user to reduce redundant calls
        _jobCache[jobSeeker.Id] = recommendedJobs;

        return recommendedJobs;
    }

    // 🔥 2️⃣ Generate AI Prompt for Job Matching
    private string GenerateMatchingPrompt(User jobSeeker, IEnumerable<Job> jobs)
    {
        return $"""
        Match {jobSeeker.FullName} to the best jobs based on:
        - Skills: {string.Join(", ", jobSeeker.Skills)}
        - Experience: {jobSeeker.Experience}
        - Job Preferences: {jobSeeker.JobPreferences}

        Available Jobs:
        {string.Join("\n", jobs.Select(j => $"- {j.Title} at {j.Company} (Required Skills: {string.Join(", ", j.SkillsRequired)})"))}

        Respond in JSON format with job titles, match scores, and reasons for ranking.
    """;
    }

    // 🔥 3️⃣ Convert AI Response JSON into Job List
    private List<Job> ParseRecommendedJobs(OpenAiResponse? response, IEnumerable<Job> allJobs)
    {
        if (response == null || response.Choices.Length == 0 || string.IsNullOrEmpty(response.Choices[0].Message.Content))
        {
            throw new InvalidOperationException("Invalid response from OpenAI");
        }

        try
        {
            var jsonText = response.Choices[0].Message.Content.Trim();
            var parsedData = JsonSerializer.Deserialize<JobRecommendationResponse>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsedData?.Jobs == null || !parsedData.Jobs.Any())
                return new List<Job>();

            return allJobs.Where(job => parsedData.Jobs.Contains(job.Title)).Take(5).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing OpenAI response: {ex.Message}");
        }
    }
}

// 🔹 Data Model for AI-Recommended Jobs
public class JobRecommendationResponse
{
    public List<string> Jobs { get; set; } = new();
}
