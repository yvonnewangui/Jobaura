using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class InterviewPrepService
{
    private readonly HttpClient _httpClient;
    private readonly string _openAiApiKey;
    private static readonly ConcurrentDictionary<string, InterviewResponse> _cache = new();

    public InterviewPrepService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "OpenAI API Key is required");

        // ✅ API Key is now correctly set in request headers
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
    }

    // 🔥 1️⃣ Generate AI-Based Interview Questions
    public async Task<InterviewResponse> GenerateInterviewQuestionsAsync(string jobTitle, List<string> skills, int questionCount)
    {
        if (string.IsNullOrWhiteSpace(jobTitle) || skills == null || skills.Count == 0)
            throw new ArgumentException("Job title and skills cannot be empty.");

        var cacheKey = $"{jobTitle}-{string.Join("-", skills)}-{questionCount}";

        // 🔹 Check if we already cached questions for this request
        if (_cache.TryGetValue(cacheKey, out var cachedQuestions))
            return cachedQuestions;

        var prompt = GeneratePrompt(jobTitle, skills, questionCount);

        var requestBody = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "You are an AI interview coach. Generate structured interview questions with answers." },
                new { role = "user", content = prompt }
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

        var questions = ParseInterviewResponse(result);

        // 🔹 Cache the response for this job title & skills combination
        _cache[cacheKey] = questions;

        return questions;
    }

    // 🔥 2️⃣ Generate AI Prompt for Interview Questions
    private static string GeneratePrompt(string jobTitle, List<string> skills, int questionCount)
    {
        return $$"""
            Generate {{questionCount}} interview questions for a {{jobTitle}} role.
            The candidate has skills in: {{string.Join(", ", skills)}}.
            Provide a mix of technical and behavioral questions.
            Each question should have a sample answer.

            Respond in JSON format:
            {
                "questions": [
                    { "question": "What is dependency injection in C#?", "answer": "Dependency Injection (DI) is a design pattern..." },
                    { "question": "Explain SOLID principles.", "answer": "SOLID principles are a set of five design principles in OOP..." }
                ]
            }
            """;
    }

    // 🔥 3️⃣ Convert AI Response JSON into Interview Questions List
    private InterviewResponse ParseInterviewResponse(OpenAiResponse? response)
    {
        if (response == null || response.Choices.Length == 0 || string.IsNullOrEmpty(response.Choices[0].Message.Content))
        {
            throw new Exception("Invalid response from OpenAI");
        }

        try
        {
            var jsonText = response.Choices[0].Message.Content.Trim();
            var parsedData = JsonSerializer.Deserialize<InterviewResponse>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parsedData ?? new InterviewResponse();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing OpenAI response: {ex.Message}");
        }
    }
}

// 🔹 Data Model for AI-Generated Interview Questions
public class InterviewResponse
{
    public List<InterviewQuestion> Questions { get; set; } = new();
}

public class InterviewQuestion
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}
