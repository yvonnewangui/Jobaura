using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class CvParsingService
{
    private readonly HttpClient _httpClient;
    private readonly string _openAiApiKey;

    public CvParsingService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "OpenAI API Key is required");

        // ✅ Now setting API Key in Headers
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
    }

    // 🔥 1️⃣ Extract CV Details Using OpenAI API
    public async Task<ParsedCvData> ExtractCvDetailsFromText(string cvText)
    {
        if (string.IsNullOrWhiteSpace(cvText))
        {
            throw new ArgumentException("CV text cannot be empty", nameof(cvText));
        }

        var requestBody = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "You are an AI CV parser. Extract relevant job details from resumes and return structured JSON." },
                new { role = "user", content = $"Extract structured details from the following resume:\n\n{cvText}\n\nRespond with JSON only:" }
            },
            max_tokens = 500,
            temperature = 0.3
        };

        // ✅ Explicitly passing API Key in request headers
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"OpenAI API call failed: {response.StatusCode}. Error: {errorMessage}");
        }

        var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();

        return ParseOpenAiResponse(result);
    }

    // 🔥 2️⃣ Convert AI JSON Response into C# Object
    private ParsedCvData ParseOpenAiResponse(OpenAiResponse? response)
    {
        if (response == null || response.Choices.Length == 0 || string.IsNullOrEmpty(response.Choices[0].Message.Content))
        {
            throw new InvalidOperationException("Invalid response from OpenAI");
        }

        try
        {
            var jsonText = response.Choices[0].Message.Content.Trim();
            var parsedData = JsonSerializer.Deserialize<ParsedCvData>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parsedData ?? new ParsedCvData();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing OpenAI response: {ex.Message}", ex);
        }
    }
}

// 🔹 Data Model for AI-Parsed CV Data
public class ParsedCvData
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public string Experience { get; set; } = string.Empty;
    public string Education { get; set; } = string.Empty;
    public List<string> Certifications { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

// 🔹 Data Model for OpenAI Response
public class OpenAiResponse
{
    public OpenAiChoice[] Choices { get; set; } = Array.Empty<OpenAiChoice>();
}

public class OpenAiChoice
{
    public OpenAiMessage Message { get; set; } = new();
}

public class OpenAiMessage
{
    public string Content { get; set; } = string.Empty;
}
