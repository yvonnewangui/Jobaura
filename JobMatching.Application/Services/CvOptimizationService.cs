using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class CvOptimizationService
{
    private readonly HttpClient _httpClient;
    private readonly string _openAiApiKey;

    public CvOptimizationService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "OpenAI API Key is required");

        // ✅ Now setting API Key in Headers
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
    }

    // 🔥 1️⃣ Optimize Resume with OpenAI API
    public async Task<OptimizedCvResponse> OptimizeResumeAsync(ResumeOptimizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ResumeText))
        {
            throw new ArgumentException("Resume text cannot be empty", nameof(request.ResumeText));
        }

        var requestBody = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "You are an AI career coach that optimizes resumes for ATS and professional readability." },
                new { role = "user", content = GeneratePrompt(request) }
            },
            max_tokens = 800,
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
            throw new Exception($"OpenAI API call failed: {response.StatusCode}. Error: {errorMessage}");
        }

        var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();

        return ParseOptimizedCv(result);
    }

    // 🔥 2️⃣ Generate Dynamic Prompt for OpenAI
    private static string GeneratePrompt(ResumeOptimizationRequest request)
    {
        return $$"""
            Optimize the following resume for a {{request.SeniorityLevel}} position in {{request.Industry}}.
            The user prefers a {{request.OptimizationStyle}} style.
            Improve clarity, grammar, and structure while making it ATS-friendly.
            Additionally, identify any missing skills relevant to {{request.JobTitle}}.
            
            Respond in JSON format:
            {
                "optimizedResume": "Updated resume text...",
                "missingSkills": ["Skill 1", "Skill 2"]
            }

            Original Resume:
            {{request.ResumeText}}
            """;
    }

    // 🔥 3️⃣ Convert OpenAI JSON Response into C# Object
    private OptimizedCvResponse ParseOptimizedCv(OpenAiResponse? response)
    {
        if (response == null || response.Choices.Length == 0 || string.IsNullOrEmpty(response.Choices[0].Message.Content))
        {
            throw new Exception("Invalid response from OpenAI");
        }

        try
        {
            var jsonText = response.Choices[0].Message.Content.Trim();
            var parsedData = JsonSerializer.Deserialize<OptimizedCvResponse>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parsedData ?? new OptimizedCvResponse();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error parsing OpenAI response: {ex.Message}");
        }
    }
}

// 🔹 Data Model for Optimized CV Response
public class OptimizedCvResponse
{
    public string OptimizedResume { get; set; } = string.Empty;
    public List<string> MissingSkills { get; set; } = new();
}

// 🔹 Data Model for Resume Optimization Request
public class ResumeOptimizationRequest
{
    public string ResumeText { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Industry { get; set; } = "General";  // Default to General
    public string SeniorityLevel { get; set; } = "Mid-Level";  // Entry, Mid, Senior
    public string OptimizationStyle { get; set; } = "Professional";  // Concise, Professional, Keyword-Optimized
}
