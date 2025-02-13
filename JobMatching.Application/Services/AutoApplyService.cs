using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JobMatching.Domain.Entities;
using JobMatching.Domain.Interfaces;

public class AutoApplyService
{
    private readonly HttpClient _httpClient;
    private readonly string _openAiApiKey;
    private readonly IJobApplicationRepository _jobApplicationRepository;
    private readonly ResumeFetcherService _resumeFetcherService;
    private readonly ILogger<AutoApplyService> _logger;

    public AutoApplyService(IConfiguration configuration, IJobApplicationRepository jobApplicationRepository, ResumeFetcherService resumeFetcherService, ILogger<AutoApplyService> logger)
    {
        _httpClient = new HttpClient();
        _openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI API Key is required");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
        _jobApplicationRepository = jobApplicationRepository;
        _resumeFetcherService = resumeFetcherService;
        _logger = logger;
    }

    /// <summary>
    /// 🔥 Auto-applies to multiple jobs, generating custom resumes & cover letters.
    /// </summary>
    public async Task<List<JobApplicationResult>> AutoApplyAsync(User user, List<Job> selectedJobs)
    {
        var applicationResults = new List<JobApplicationResult>();

        foreach (var job in selectedJobs)
        {
            var originalResume = await _resumeFetcherService.FetchAndExtractResumeTextAsync(user.ResumeUrl);
            var coverLetter = await GenerateCoverLetterAsync(user, job, originalResume);
            var resume = await OptimizeResumeForJobAsync(user, job, originalResume);

            var application = new JobApplication
            {
                UserId = user.Id,
                JobId = job.Id,
                ResumeText = resume,
                CoverLetter = coverLetter,
                Status = "Pending"
            };

            await _jobApplicationRepository.SaveApplicationAsync(application);

            applicationResults.Add(new JobApplicationResult
            {
                JobTitle = job.Title,
                Company = job.Company,
                CoverLetter = coverLetter,
                Resume = resume,
                Status = "Applied"
            });

            _logger.LogInformation("✅ Auto-applied to {JobTitle} at {Company} for {UserId}", job.Title, job.Company, user.Id);
        }

        return applicationResults;
    }

    /// <summary>
    /// 🔥 Generates an AI-personalized cover letter for a job application.
    /// </summary>
    private async Task<string> GenerateCoverLetterAsync(User user, Job job, string originalResume)
    {
        var prompt = $"""
            Write a professional, customized cover letter for {user.FullName} applying for {job.Title} at {job.Company}.
            - The user's resume:
              {originalResume}
            - Job description:
              {job.Description}
            
            Follow this structure:
            - Introduction
            - Why I’m a great fit
            - Key skills that match the job
            - Closing statement

            Format it as a well-written business letter.
        """;

        return await CallOpenAiApiAsync(prompt);
    }

    /// <summary>
    /// 🔥 Optimizes the user's resume based on job requirements.
    /// </summary>
    private async Task<string> OptimizeResumeForJobAsync(User user, Job job, string originalResume)
    {
        var prompt = $"""
            Optimize {user.FullName}'s resume for {job.Title} at {job.Company}.
            - Highlight relevant experience & skills.
            - Make it ATS-friendly.
            - Improve formatting & clarity.

            Original Resume:
            {originalResume}

            Return only the optimized resume.
        """;

        return await CallOpenAiApiAsync(prompt);
    }

    /// <summary>
    /// 🔥 Calls OpenAI API to generate AI-powered content.
    /// </summary>
    private async Task<string> CallOpenAiApiAsync(string prompt)
    {
        var requestBody = new
        {
            model = "gpt-4-turbo",
            messages = new[] { new { role = "user", content = prompt } },
            max_tokens = 600,
            temperature = 0.7
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
                _logger.LogError("OpenAI API call failed: {StatusCode} {Reason}", response.StatusCode, response.ReasonPhrase);
                return "Error generating content.";
            }

            var resultContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OpenAiResponse>(resultContent);

            return result?.Choices?[0]?.Message?.Content ?? "Error processing response.";
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in OpenAI request: {Message}", ex.Message);
            return "Error processing AI response.";
        }
    }
}

// 🔹 Data Model for Job Application Results
public class JobApplicationResult
{
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string CoverLetter { get; set; } = string.Empty;
    public string Resume { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}
