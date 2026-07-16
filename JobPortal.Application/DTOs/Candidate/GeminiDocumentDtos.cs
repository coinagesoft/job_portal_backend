using System.Text.Json;

namespace JobPortal.Application.DTOs.Candidate;

public class GeminiDocumentParseResponse
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public string? DocumentType { get; set; }

    // Dynamic JSON returned by Gemini
    public JsonElement? ParsedData { get; set; }

    // Raw response from Gemini (useful for debugging)
    public string? RawResponse { get; set; }
    // ADD THIS
    public decimal? AiConfidenceScore { get; set; }
}

public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-2.5-flash";
}