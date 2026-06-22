
﻿//using JobPortal.Services.IImplement.ICandidate;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Configuration;
//using System.Net.Http.Headers;

//public class AffindaService : IAffindaService
//{
//    private readonly HttpClient _httpClient;
//    private readonly IConfiguration _configuration;

//    public AffindaService(
//        HttpClient httpClient,
//        IConfiguration configuration)
//    {
//        _httpClient = httpClient;
//        _configuration = configuration;
//    }

//    public async Task<string> ParseResumeAsync(IFormFile file)
//    {
//        try
//        {
//            var apiKey = _configuration["Affinda:ApiKey"];

//            Console.WriteLine($"Affinda Key Found: {!string.IsNullOrEmpty(apiKey)}");

//            _httpClient.DefaultRequestHeaders.Authorization =
//                new AuthenticationHeaderValue("Bearer", apiKey);

//            using var form = new MultipartFormDataContent();

//            using var stream = file.OpenReadStream();

//            form.Add(
//                new StreamContent(stream),
//                "file",
//                file.FileName);

//            var response =
//                await _httpClient.PostAsync(
//                    "https://api.affinda.com/v3/documents",
//                    form);

//            var responseBody =
//                await response.Content.ReadAsStringAsync();

//            Console.WriteLine("STATUS: " + response.StatusCode);
//            Console.WriteLine("BODY: " + responseBody);

//            return responseBody;
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine("AFFINDA ERROR:");
//            Console.WriteLine(ex.ToString());

//            throw;
//        }
//    }
//}

﻿// ============================================================
//  JobPortal.Services/Implement/Candidate/AffindaService.cs
//
//  REPLACES the old AffindaService.cs (no namespace, returned string)
//  DELETE: AffindaServices.cs, IAffindaServices.cs (stale duplicates)
//
//  Flow:
//    1. POST /v3/documents  → upload file, get identifier
//    2. GET  /v3/documents/{identifier}  → poll until ready:true
//    3. Deserialize data{} → AffindaParseResult
// ============================================================

using JobPortal.Application.DTOs.AI;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobPortal.Services.Implement.Candidate;

public class AffindaService : IAffindaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AffindaService> _logger;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private string WorkspaceId => _configuration["Affinda:WorkspaceId"]
        ?? throw new InvalidOperationException("Affinda:WorkspaceId is not configured.");

    private string ApiKey => _configuration["Affinda:ApiKey"]
        ?? throw new InvalidOperationException("Affinda:ApiKey is not configured.");

    // Affinda is regional (api.affinda.com / api.us1.affinda.com / api.eu1.affinda.com / ...).
    // A workspace only exists on the regional instance it was created on, and an API key
    // only works against that same instance — using the wrong one returns a misleading
    // "Object with identifier=... does not exist" error for an otherwise-valid workspace ID.
    // Previously this was hardcoded to https://api.affinda.com everywhere, ignoring this setting.
    private string BaseUrl => (_configuration["Affinda:BaseUrl"]
        ?? throw new InvalidOperationException("Affinda:BaseUrl is not configured.")).TrimEnd('/');

    public AffindaService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AffindaService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    // ══════════════════════════════════════════════════════════
    // PUBLIC
    // ══════════════════════════════════════════════════════════
    public async Task<AffindaParseResult> ParseResumeAsync(IFormFile file)
    {
        try
        {
            SetAuthHeader();

            var identifier = await UploadDocumentAsync(file);
            if (identifier == null)
                return Fail("Affinda upload did not return a document identifier.");

            _logger.LogInformation("Affinda document uploaded. Identifier: {Id}", identifier);

            var document = await PollUntilReadyAsync(identifier);
            if (document == null)
                return Fail("Affinda document did not become ready within the timeout.");

            if (document.Meta?.Failed == true)
                return Fail($"Affinda parsing failed: {document.Error?.ErrorDetail}");

            return MapToParseResult(document, identifier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AffindaService.ParseResumeAsync failed");
            return Fail(ex.Message);
        }
    }

    // ── Step 1: Upload ────────────────────────────────────────
    private async Task<string?> UploadDocumentAsync(IFormFile file)
    {
        using var form = new MultipartFormDataContent();

        using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(fileContent, "file", file.FileName);
        form.Add(new StringContent(WorkspaceId), "workspace");
        form.Add(new StringContent("false"), "wait");

        var response = await _httpClient.PostAsync($"{BaseUrl}/v3/documents", form);
        var body = await response.Content.ReadAsStringAsync();

        _logger.LogDebug("Affinda upload {Status}: {Body}", response.StatusCode, body);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Affinda upload failed {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException(ExtractAffindaErrorDetail(body, response.StatusCode));
        }

        var doc = JsonSerializer.Deserialize<AffindaSingleDocumentResponse>(body, _jsonOpts);
        return doc?.Meta?.Identifier;
    }

    // ── Step 2: Poll ─────────────────────────────────────────
    private async Task<AffindaSingleDocumentResponse?> PollUntilReadyAsync(string identifier)
    {
        const int maxAttempts = 20;
        const int delaySeconds = 3;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            SetAuthHeader();

            var response = await _httpClient.GetAsync(
                $"{BaseUrl}/v3/documents/{identifier}");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Affinda poll attempt {Attempt} failed: {Status}", attempt, response.StatusCode);
                continue;
            }

            var doc = JsonSerializer.Deserialize<AffindaSingleDocumentResponse>(body, _jsonOpts);

            if (doc?.Meta?.Ready == true || doc?.Meta?.Failed == true)
            {
                _logger.LogInformation(
                    "Affinda document {Id} ready after {Attempt} attempt(s). Failed={Failed}",
                    identifier, attempt, doc.Meta.Failed);
                return doc;
            }

            _logger.LogDebug("Affinda document {Id} not ready yet (attempt {Attempt}/{Max})",
                identifier, attempt, maxAttempts);
        }

        _logger.LogError("Affinda document {Id} did not become ready after {Max} attempts",
            identifier, maxAttempts);
        return null;
    }

    // ── Step 3: Map ──────────────────────────────────────────
    private static AffindaParseResult MapToParseResult(
        AffindaSingleDocumentResponse document, string identifier)
    {
        var d = document.Data;

        var firstName = d?.CandidateName?.FirstName?.Trim();
        var familyName = d?.CandidateName?.FamilyName?.Trim();
        var fullName = string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(familyName)
            ? null
            : $"{firstName} {familyName}".Trim();

        var phone = d?.PhoneNumber?.FirstOrDefault()?.FormattedNumber
                 ?? d?.PhoneNumber?.FirstOrDefault()?.RawText;

        var email = d?.Email?.FirstOrDefault();

        var primaryTrade = d?.WorkExperience?.FirstOrDefault()?.JobTitle;

        int? experienceYrs = d?.TotalYearsExperience.HasValue == true
            ? (int)Math.Round(d.TotalYearsExperience.Value)
            : null;

        var skills = (d?.Skill ?? new())
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .Select(s => s.Name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rawJson = JsonSerializer.Serialize(document, new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        Console.WriteLine("=================================");
        Console.WriteLine($"Name: {fullName}");
        Console.WriteLine($"Email: {email}");
        Console.WriteLine($"Phone: {phone}");
        Console.WriteLine($"Trade: {primaryTrade}");
        Console.WriteLine($"Skills Count: {skills.Count}");
        Console.WriteLine("=================================");
        return new AffindaParseResult
        {
            Success = true,
            AffindaDocId = identifier,
            ParsedName = fullName,
            ParsedPhone = phone,
            ParsedEmail = email,
            ParsedTrade = primaryTrade,
            ParsedExperienceYrs = experienceYrs,
            ParsedSkills = skills,
            AiConfidenceScore = document.Meta?.OcrConfidence,
            City = d?.Location?.City,
            State = d?.Location?.State,
            Country = d?.Location?.Country,
            WorkExperiences = d?.WorkExperience ?? new(),
            Educations = d?.Education ?? new(),
            Languages = d?.Language ?? new(),
            RawAffindaJson = rawJson
        };
    }

    private static string ExtractAffindaErrorDetail(string body, System.Net.HttpStatusCode status)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
            {
                var first = errors[0];
                var detail = first.TryGetProperty("detail", out var d) ? d.GetString() : null;
                var attr = first.TryGetProperty("attr", out var a) ? a.GetString() : null;
                if (!string.IsNullOrWhiteSpace(detail))
                    return string.IsNullOrWhiteSpace(attr)
                        ? $"Affinda upload failed ({(int)status}): {detail}"
                        : $"Affinda upload failed ({(int)status}): {detail} [{attr}]";
            }
        }
        catch (JsonException)
        {
            // body wasn't the expected validation_error shape — fall through to raw body below
        }

        return $"Affinda upload failed ({(int)status}): {body}";
    }

    private void SetAuthHeader() =>
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiKey);

    private static AffindaParseResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}

