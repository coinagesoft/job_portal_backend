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
        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash-lite";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new GeminiDocumentParseResponse
            {
                Success = false,
                Message = "Gemini API key is not configured."
            };
        }

        // ── Read file ──────────────────────────────────────────────────
        using var memoryStream = new MemoryStream();
        await document.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();
        var base64 = Convert.ToBase64String(bytes);
        var mimeType = document.ContentType;

        // ── Prompt ────────────────────────────────────────────────────
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
3. Return ONLY a valid JSON object — no markdown, no explanation.

Return format:
{
  "documentType": "",
 "confidence": {
  "overall": 0,
  "documentType": 0,
  "ocr": 0
},
  "fields": {}
}
""";

        // ── Request body ──────────────────────────────────────────────
        var requestBody = new
        {
            contents = new object[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data      = base64
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                topP = 0.95,
                maxOutputTokens = 8192,
                responseMimeType = "application/json"
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var endpoint =
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        // ── Call Gemini ────────────────────────────────────────────────
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(endpoint, content);
        }
        catch (Exception ex)
        {
            return new GeminiDocumentParseResponse
            {
                Success = false,
                Message = $"Network error contacting Gemini: {ex.Message}"
            };
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new GeminiDocumentParseResponse
            {
                Success = false,
                Message = $"Gemini API error ({(int)response.StatusCode}): {responseBody}",
                RawResponse = responseBody
            };
        }

        // ── Parse Gemini envelope ─────────────────────────────────────
        using var jsonDocument = JsonDocument.Parse(responseBody);
        var root = jsonDocument.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) ||
            candidates.GetArrayLength() == 0)
        {
            return new GeminiDocumentParseResponse
            {
                Success = false,
                Message = "Gemini returned no candidates.",
                RawResponse = responseBody
            };
        }

        // When responseMimeType = "application/json" Gemini returns JSON text
        // directly in parts[0].text — extract and re-parse it.
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
                Message = "Gemini returned an empty text.",
                RawResponse = responseBody
            };
        }

        // Strip any accidental markdown fences (safety net)
        jsonText = jsonText
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();

        // ── Parse the OCR payload ──────────────────────────────────────
        JsonDocument parsedDocument;
        try
        {
            parsedDocument = JsonDocument.Parse(jsonText);
        }
        catch (JsonException jex)
        {
            return new GeminiDocumentParseResponse
            {
                Success = false,
                Message = $"Could not parse Gemini JSON output: {jex.Message}",
                RawResponse = jsonText
            };
        }

        using (parsedDocument)
        {
            var parsedRoot = parsedDocument.RootElement;
            string? documentType = null;
            JsonElement? fields = null;
            decimal? aiConfidence = null;

            if (parsedRoot.TryGetProperty("documentType", out var docType))
                documentType = docType.GetString();

            if (parsedRoot.TryGetProperty("fields", out var fieldsElement))
                fields = fieldsElement.Clone();

            if (parsedRoot.TryGetProperty("confidence", out var confidenceElement))
            {
                if (confidenceElement.ValueKind == JsonValueKind.Object &&
                    confidenceElement.TryGetProperty("overall", out var overall))
                {
                    if (overall.TryGetDecimal(out var score))
                        aiConfidence = score;
                }
            }

            return new GeminiDocumentParseResponse
            {
                Success = true,
                Message = "Document parsed successfully.",
                DocumentType = documentType,
                AiConfidenceScore = aiConfidence,
                ParsedData = fields,
                RawResponse = jsonText
            };
        }
    }
}