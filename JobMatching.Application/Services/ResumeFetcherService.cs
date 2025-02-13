using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

public class ResumeFetcherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResumeFetcherService> _logger;

    public ResumeFetcherService(HttpClient httpClient, ILogger<ResumeFetcherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// 🔥 Fetches a resume from an external URL and extracts text.
    /// </summary>
    public async Task<string> FetchAndExtractResumeTextAsync(string resumeUrl)
    {
        if (string.IsNullOrEmpty(resumeUrl))
        {
            _logger.LogError("Resume URL is empty.");
            return string.Empty;
        }

        try
        {
            var response = await _httpClient.GetAsync(resumeUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch resume from URL: {Url}", resumeUrl);
                return string.Empty;
            }

            var contentStream = await response.Content.ReadAsStreamAsync();
            return ExtractTextFromPdf(contentStream);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching resume: {Message}", ex.Message);
            return string.Empty;
        }
    }

    /// <summary>
    /// 🔥 Extracts text from a PDF resume.
    /// </summary>
    private string ExtractTextFromPdf(Stream pdfStream)
    {
        try
        {
            using var reader = new PdfReader(pdfStream);
            var text = string.Empty;

            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                text += PdfTextExtractor.GetTextFromPage(reader, i);
            }

            return text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Error extracting text from PDF: {Message}", ex.Message);
            return string.Empty;
        }
    }
}
