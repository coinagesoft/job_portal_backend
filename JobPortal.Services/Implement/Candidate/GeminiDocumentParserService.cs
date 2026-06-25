using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JobPortal.Application.DTOs.Candidate;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace JobPortal.Services.Implement.Candidate;

public class GeminiDocumentParserService : IGeminiDocumentParserService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiDocumentParserService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<GeminiDocumentParseResponse> ParseDocumentAsync(IFormFile document)
    {
        if (document == null || document.Length == 0)
        {
            return new GeminiDocumentParseResponse
            {
                Success = false,
                Message = "Document not found."
            };
        }

        var apiKey = _configuration["Gemini:ApiKey"];
        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

        using var memoryStream = new MemoryStream();
        await document.CopyToAsync(memoryStream);

        var bytes = memoryStream.ToArray();
        var base64 = Convert.ToBase64String(bytes);

        var mimeType = document.ContentType;

        var prompt = """
You are an OCR and document extraction engine.

The uploaded file can be:

- Aadhaar Card
- PAN Card
- Passport
- Driving Licence
- Voter ID
- ITI Certificate
- 10th Marksheet
- 12th Marksheet
- Any Government Document

Tasks:

1. Detect document type.
2. Extract every visible field.
3. Return ONLY JSON.
4. Never explain.
5. Never wrap response inside markdown.

Return format:

{
  "documentType":"",
  "fields":{

  }
}
""";
        var requestBody = new
        {
            contents = new object[]
    {
                new
                {
                    parts = new object[]
                    {
                        new
                        {
                            text = prompt
                        },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = base64
                            }
                        }
                    }
                }
    },
            generationConfig = new
            {
                temperature = 0.2,
                topP = 0.95,
                maxOutputTokens = 8192,
                responseMimeType = "application/json"
            }
        };

        var json = JsonSerializer.Serialize(requestBody);

        using var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        var endpoint =
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var response = await _httpClient.PostAsync(endpoint, content);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new GeminiDocumentParseResponse
            {
                Success = false,
                Message = responseBody
            };
        }
        using var jsonDocument = JsonDocument.Parse(responseBody);

        var root = jsonDocument.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates))
        {
            return new GeminiDocumentParseResponse
            {
                Success = false,
                Message = "Gemini returned an invalid response.",
                RawResponse = responseBody
            };
        }

        var jsonText = candidates[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            return new GeminiDocumentParseResponse
            {
                Success = false,
                Message = "Gemini returned an empty response.",
                RawResponse = responseBody
            };
        }

        jsonText = jsonText
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();

        using var parsedDocument = JsonDocument.Parse(jsonText);

        var parsedRoot = parsedDocument.RootElement;

        string? documentType = null;

        if (parsedRoot.TryGetProperty("documentType", out var docType))
        {
            documentType = docType.GetString();
        }

        JsonElement? fields = null;

        if (parsedRoot.TryGetProperty("fields", out var fieldsElement))
        {
            fields = fieldsElement.Clone();
        }

        return new GeminiDocumentParseResponse
        {
            Success = true,
            Message = "Document parsed successfully.",
            DocumentType = documentType,
            ParsedData = fields,
            RawResponse = jsonText
        };
    }
}