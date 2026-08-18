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

    public async Task<GeminiCompanyDocumentParseResponse> ParseDocumentAsync(
        IFormFile document)
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
        var model =
            _configuration["Gemini:Model"]
            ?? "gemini-2.5-flash-lite";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new GeminiCompanyDocumentParseResponse
            {
                Success = false,
                Message = "Gemini API key is not configured."
            };
        }

        // ===========================================================
        // READ FILE
        // ===========================================================

        using var memoryStream = new MemoryStream();

        await document.CopyToAsync(memoryStream);

        var bytes = memoryStream.ToArray();

        var base64 =
            Convert.ToBase64String(bytes);

        var mimeType =
            document.ContentType;


        // ===========================================================
        // PROMPT
        // ===========================================================
        //
        // Existing parsing logic is preserved.
        //
        // Only added instruction:
        // return fields with null when the field is not available.
        //
        // This allows our application to calculate extraction
        // completeness correctly.
        //
        // ===========================================================

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
8. For important fields that are not available or cannot be extracted,
   return the field with a null value instead of completely omitting it.

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


        // ===========================================================
        // REQUEST BODY
        // ===========================================================

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
                temperature = 0.1,
                topP = 0.95,
                maxOutputTokens = 8192,
                responseMimeType = "application/json"
            }
        };


        var json =
            JsonSerializer.Serialize(requestBody);

        using var content =
            new StringContent(
                json,
                Encoding.UTF8,
                "application/json");


        var endpoint =
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";


        HttpResponseMessage response;

        try
        {
            response =
                await _httpClient.PostAsync(
                    endpoint,
                    content);
        }
        catch (Exception ex)
        {
            return new GeminiCompanyDocumentParseResponse
            {
                Success = false,
                Message =
                    $"Network error contacting Gemini: {ex.Message}"
            };
        }


        var responseBody =
            await response.Content.ReadAsStringAsync();


        // ===========================================================
        // GEMINI ERROR
        // ===========================================================

        if (!response.IsSuccessStatusCode)
        {
            return new GeminiCompanyDocumentParseResponse
            {
                Success = false,
                Message =
                    $"Gemini API error ({(int)response.StatusCode}): {responseBody}",

                RawResponse =
                    responseBody
            };
        }


        // ===========================================================
        // PARSE GEMINI ENVELOPE
        // ===========================================================

        using var jsonDocument =
            JsonDocument.Parse(responseBody);

        var root =
            jsonDocument.RootElement;


        if (!root.TryGetProperty(
                "candidates",
                out var candidates) ||
            candidates.GetArrayLength() == 0)
        {
            return new GeminiCompanyDocumentParseResponse
            {
                Success = false,

                Message =
                    "Gemini returned no candidates.",

                RawResponse =
                    responseBody
            };
        }


        var jsonText =
            candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();


        if (string.IsNullOrWhiteSpace(jsonText))
        {
            return new GeminiCompanyDocumentParseResponse
            {
                Success = false,

                Message =
                    "Gemini returned an empty response.",

                RawResponse =
                    responseBody
            };
        }


        // ===========================================================
        // CLEAN JSON
        // ===========================================================

        jsonText = jsonText
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();


        JsonDocument parsedDocument;

        try
        {
            parsedDocument =
                JsonDocument.Parse(jsonText);
        }
        catch (JsonException ex)
        {
            return new GeminiCompanyDocumentParseResponse
            {
                Success = false,

                Message =
                    $"Unable to parse Gemini JSON: {ex.Message}",

                RawResponse =
                    jsonText
            };
        }


        using (parsedDocument)
        {
            var parsedRoot =
                parsedDocument.RootElement;


            string? documentType = null;

            JsonElement? fields = null;

            // =======================================================
            // IMPORTANT:
            // This will now be calculated from extracted fields.
            // =======================================================

            decimal? aiConfidence = null;


            string? documentNumber = null;

            string? issuingAuthority = null;

            DateOnly? issueDate = null;

            DateOnly? expiryDate = null;


            // =======================================================
            // DOCUMENT TYPE
            // =======================================================

            if (parsedRoot.TryGetProperty(
                    "documentType",
                    out var typeElement))
            {
                documentType =
                    typeElement.GetString();
            }


            // =======================================================
            // FIELDS
            // =======================================================

            if (parsedRoot.TryGetProperty(
                    "fields",
                    out var fieldElement))
            {
                fields =
                    fieldElement.Clone();


                // ===================================================
                // DOCUMENT NUMBER
                // ===================================================

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
                    if (fieldElement.TryGetProperty(
                            key,
                            out var value))
                    {
                        if (value.ValueKind ==
                            JsonValueKind.String)
                        {
                            documentNumber =
                                value.GetString();
                        }

                        if (!string.IsNullOrWhiteSpace(
                                documentNumber))
                        {
                            break;
                        }
                    }
                }


                // ===================================================
                // ISSUING AUTHORITY
                // ===================================================

                string[] authorityKeys =
                {
                    "issuingAuthority",
                    "issuingOffice",
                    "issuedBy",
                    "authority"
                };


                foreach (var key in authorityKeys)
                {
                    if (fieldElement.TryGetProperty(
                            key,
                            out var value))
                    {
                        if (value.ValueKind ==
                            JsonValueKind.String)
                        {
                            issuingAuthority =
                                value.GetString();
                        }

                        if (!string.IsNullOrWhiteSpace(
                                issuingAuthority))
                        {
                            break;
                        }
                    }
                }


                // ===================================================
                // ISSUE DATE
                // ===================================================

                string[] issueDateKeys =
                {
                    "issueDate",
                    "dateOfIssue",
                    "issuedOn",
                    "validFrom"
                };


                foreach (var key in issueDateKeys)
                {
                    if (fieldElement.TryGetProperty(
                            key,
                            out var value))
                    {
                        if (value.ValueKind ==
                            JsonValueKind.String &&
                            DateOnly.TryParse(
                                value.GetString(),
                                out var parsed))
                        {
                            issueDate =
                                parsed;

                            break;
                        }
                    }
                }


                // ===================================================
                // EXPIRY DATE
                // ===================================================

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
                    if (fieldElement.TryGetProperty(
                            key,
                            out var value))
                    {
                        if (value.ValueKind ==
                            JsonValueKind.String &&
                            DateOnly.TryParse(
                                value.GetString(),
                                out var parsed))
                        {
                            expiryDate =
                                parsed;

                            break;
                        }
                    }
                }
            }


            // =======================================================
            // DOCUMENT-WISE AI EXTRACTION SCORE
            // =======================================================
            //
            // IMPORTANT:
            //
            // We DO NOT use Gemini's confidence.overall anymore.
            //
            // Score:
            //
            // Non-empty extracted fields
            // --------------------------------
            // Total fields returned by Gemini
            //
            // Example:
            //
            // 4 extracted / 5 fields = 0.80
            //
            // Database:
            //
            // AiConfidenceScore = 0.80
            //
            // GET API:
            //
            // aiExtractionPercentage = 80
            //
            // =======================================================

            if (fields.HasValue &&
                fields.Value.ValueKind ==
                    JsonValueKind.Object)
            {
                aiConfidence =
                    CalculateExtractionScore(
                        fields.Value);
            }
            else
            {
                aiConfidence = 0m;
            }


            // =======================================================
            // RETURN
            // =======================================================

            return new GeminiCompanyDocumentParseResponse
            {
                Success = true,

                Message =
                    "Company document parsed successfully.",

                DocumentType =
                    documentType,

                // OUR calculated score
                AiConfidenceScore =
                    aiConfidence,

                ParsedData =
                    fields,

                RawResponse =
                    jsonText,

                DocumentNumber =
                    documentNumber,

                IssuingAuthority =
                    issuingAuthority,

                IssueDate =
                    issueDate,

                ExpiryDate =
                    expiryDate
            };
        }
    }


    // ===========================================================
    // CALCULATE DOCUMENT-WISE EXTRACTION SCORE
    // ===========================================================
    //
    // This method ONLY calculates the AI extraction score.
    //
    // No existing document parsing logic is changed.
    //
    // Result is stored between 0 and 1.
    //
    // 0.98 = 98%
    // 0.75 = 75%
    // 0.50 = 50%
    //
    // ===========================================================

    private decimal CalculateExtractionScore(
        JsonElement fields)
    {
        if (fields.ValueKind !=
            JsonValueKind.Object)
        {
            return 0m;
        }


        var properties =
            fields.EnumerateObject()
                .ToList();


        if (properties.Count == 0)
        {
            return 0m;
        }


        var totalFields =
            properties.Count;


        var extractedFields = 0;


        foreach (var property in properties)
        {
            if (HasExtractedValue(
                    property.Value))
            {
                extractedFields++;
            }
        }


        var score =
            (decimal)extractedFields /
            totalFields;


        return Math.Round(
            Math.Clamp(
                score,
                0m,
                1m),
            4);
    }


    // ===========================================================
    // CHECK WHETHER GEMINI ACTUALLY EXTRACTED A VALUE
    // ===========================================================

    private bool HasExtractedValue(
        JsonElement value)
    {
        // -------------------------------------------------------
        // NULL
        // -------------------------------------------------------

        if (value.ValueKind ==
                JsonValueKind.Null ||
            value.ValueKind ==
                JsonValueKind.Undefined)
        {
            return false;
        }


        // -------------------------------------------------------
        // STRING
        // -------------------------------------------------------

        if (value.ValueKind ==
            JsonValueKind.String)
        {
            return !string.IsNullOrWhiteSpace(
                value.GetString());
        }


        // -------------------------------------------------------
        // ARRAY
        // -------------------------------------------------------

        if (value.ValueKind ==
            JsonValueKind.Array)
        {
            return value
                .EnumerateArray()
                .Any(HasExtractedValue);
        }


        // -------------------------------------------------------
        // OBJECT
        // -------------------------------------------------------

        if (value.ValueKind ==
            JsonValueKind.Object)
        {
            return value
                .EnumerateObject()
                .Any(x =>
                    HasExtractedValue(
                        x.Value));
        }


        // -------------------------------------------------------
        // NUMBER / BOOLEAN
        // -------------------------------------------------------

        if (value.ValueKind ==
                JsonValueKind.Number ||
            value.ValueKind ==
                JsonValueKind.True ||
            value.ValueKind ==
                JsonValueKind.False)
        {
            return true;
        }


        return false;
    }
}