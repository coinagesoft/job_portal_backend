using JobPortal.Application.DTOs.Admin.CompanyDocuments;
using JobPortal.Application.DTOs.Recruiter.CompanyDocuments;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace JobPortal.Services.Implement.Recruiter;

public class GeminiCompanyDocumentParserService
    : IGeminiCompanyDocumentParserService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiCompanyDocumentParserService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<GeminiCompanyDocumentParseResponse> ParseDocumentAsync(IFormFile document)
    {
        if (document == null || document.Length == 0)
        {
            return new GeminiCompanyDocumentParseResponse
            {
                Success = false,
                Message = "Document not found."
            };
        }

        var apiKey = _configuration["Gemini:ApiKey"];
        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash-lite";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new GeminiCompanyDocumentParseResponse
            {
                Success = false,
                Message = "Gemini API key is not configured."
            };
        }

        // ── Read File ─────────────────────────────────────────────────────
        using var memoryStream = new MemoryStream();
        await document.CopyToAsync(memoryStream);

        var bytes = memoryStream.ToArray();
        var base64 = Convert.ToBase64String(bytes);
        var mimeType = document.ContentType;

        // ── Prompt ────────────────────────────────────────────────────────
        var prompt = """
You are an OCR and business document extraction engine.

The uploaded file may be ANY company or business related document.

Examples include but are not limited to:

- Company Registration Certificate
- GST Certificate
- PAN Card
- ISO Certificate
- MSME Certificate
- Startup India Certificate
- Import Export Code (IEC)
- Factory License
- Trade License
- Shop & Establishment License
- Pollution Certificate
- Drug License
- Labour License
- RPSL License
- POE License
- Any Government License
- Any Company Compliance Certificate
- Any Company Related Document

Tasks:

1. Detect the document type.
2. Extract every visible field from the document.
3. Identify important values like:
   - Document Number
   - Company Name
   - Registration Number
   - GST Number
   - PAN Number
   - CIN
   - Issue Date
   - Expiry Date
   - Issuing Authority
   - Any other useful fields
4. Return confidence score.
5. Return ONLY valid JSON.
6. Do NOT return markdown.
7. Do NOT explain anything.

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

        // ── Request Body ──────────────────────────────────────────────────
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
                            data = base64
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

        using var content =
            new StringContent(json, Encoding.UTF8, "application/json");

        var endpoint =
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsync(endpoint, content);
        }
        catch (Exception ex)
        {
            return new GeminiCompanyDocumentParseResponse
            {
                Success = false,
                Message = $"Network error contacting Gemini: {ex.Message}"
            };
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new GeminiCompanyDocumentParseResponse
            {
                Success = false,
                Message = $"Gemini API error ({(int)response.StatusCode}): {responseBody}",
                RawResponse = responseBody
            };
        }

        // ── Parse Gemini Envelope ─────────────────────────────────────────
        using var jsonDocument = JsonDocument.Parse(responseBody);

        var root = jsonDocument.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) ||
            candidates.GetArrayLength() == 0)
        {
            return new GeminiCompanyDocumentParseResponse
            {
                Success = false,
                Message = "Gemini returned no candidates.",
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
            return new GeminiCompanyDocumentParseResponse
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

        JsonDocument parsedDocument;

        try
        {
            parsedDocument = JsonDocument.Parse(jsonText);
        }
        catch (JsonException ex)
        {
            return new GeminiCompanyDocumentParseResponse
            {
                Success = false,
                Message = $"Unable to parse Gemini JSON: {ex.Message}",
                RawResponse = jsonText
            };
        }
        using (parsedDocument)
        {
            var parsedRoot = parsedDocument.RootElement;

            string? documentType = null;
            JsonElement? fields = null;
            decimal? aiConfidence = null;

            string? documentNumber = null;
            string? issuingAuthority = null;
            DateOnly? issueDate = null;
            DateOnly? expiryDate = null;

            if (parsedRoot.TryGetProperty("documentType", out var typeElement))
                documentType = typeElement.GetString();

            if (parsedRoot.TryGetProperty("fields", out var fieldElement))
            {
                fields = fieldElement.Clone();

                // -------- Document Number --------
                string[] documentNumberKeys =
                {
            "documentNumber",
            "certificateNumber",
            "registrationNumber",
            "licenseNumber",
            "consentNumber",
            "gstNumber",
            "panNumber",
            "cin",
            "iecNumber"
        };

                foreach (var key in documentNumberKeys)
                {
                    if (fieldElement.TryGetProperty(key, out var value))
                    {
                        documentNumber = value.GetString();

                        if (!string.IsNullOrWhiteSpace(documentNumber))
                            break;
                    }
                }

                // -------- Issuing Authority --------
                string[] authorityKeys =
                {
            "issuingAuthority",
            "issuingOffice",
            "issuedBy",
            "authority"
        };

                foreach (var key in authorityKeys)
                {
                    if (fieldElement.TryGetProperty(key, out var value))
                    {
                        issuingAuthority = value.GetString();

                        if (!string.IsNullOrWhiteSpace(issuingAuthority))
                            break;
                    }
                }

                // -------- Issue Date --------
                string[] issueDateKeys =
                {
            "issueDate",
            "dateOfIssue",
            "issuedOn",
            "validFrom"
        };

                foreach (var key in issueDateKeys)
                {
                    if (fieldElement.TryGetProperty(key, out var value))
                    {
                        if (DateOnly.TryParse(value.GetString(), out var parsed))
                        {
                            issueDate = parsed;
                            break;
                        }
                    }
                }

                // -------- Expiry Date --------
                string[] expiryDateKeys =
                {
            "expiryDate",
            "expiry",
            "validTill",
            "validTo",
            "expiresOn"
        };

                foreach (var key in expiryDateKeys)
                {
                    if (fieldElement.TryGetProperty(key, out var value))
                    {
                        if (DateOnly.TryParse(value.GetString(), out var parsed))
                        {
                            expiryDate = parsed;
                            break;
                        }
                    }
                }
            }

            if (parsedRoot.TryGetProperty("confidence", out var confidenceElement) &&
                confidenceElement.ValueKind == JsonValueKind.Object &&
                confidenceElement.TryGetProperty("overall", out var overall))
            {
                if (overall.ValueKind == JsonValueKind.Number)
                {
                    aiConfidence = overall.GetDecimal();
                }
                else if (decimal.TryParse(overall.GetString(), out var score))
                {
                    aiConfidence = score;
                }
            }

            return new GeminiCompanyDocumentParseResponse
            {
                Success = true,
                Message = "Company document parsed successfully.",

                DocumentType = documentType,
                AiConfidenceScore = aiConfidence,
                ParsedData = fields,
                RawResponse = jsonText,

                DocumentNumber = documentNumber,
                IssuingAuthority = issuingAuthority,
                IssueDate = issueDate,
                ExpiryDate = expiryDate
            };
        }
    }
}