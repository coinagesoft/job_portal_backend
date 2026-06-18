//// ============================================================
////  JobPortal.Services/Implement/Candidate/AffindaService.cs
////
////  Flow:
////    1. POST /v3/documents  → upload file, get identifier
////    2. GET  /v3/documents/{identifier}  → poll until ready:true
////    3. Deserialize data{} → AffindaParseResult
//// ============================================================

//using JobPortal.Application.DTOs.AI;
//using JobPortal.Services.IImplement.ICandidate;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using System.Net.Http.Headers;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace JobPortal.Services.Implement.Candidate;

//public class AffindaService : IAffindaService
//{
//    private readonly HttpClient _httpClient;
//    private readonly IConfiguration _configuration;
//    private readonly ILogger<AffindaService> _logger;

//    private static readonly JsonSerializerOptions _jsonOpts = new()
//    {
//        PropertyNameCaseInsensitive = true,
//        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
//    };

//    // Affinda workspace identifier (required for v3)
//    // Set "Affinda:WorkspaceId" in appsettings.json
//    private string WorkspaceId => _configuration["Affinda:WorkspaceId"]
//        ?? throw new InvalidOperationException("Affinda:WorkspaceId is not configured.");

//    private string ApiKey => _configuration["Affinda:ApiKey"]
//        ?? throw new InvalidOperationException("Affinda:ApiKey is not configured.");

//    public AffindaService(
//        HttpClient httpClient,
//        IConfiguration configuration,
//        ILogger<AffindaService> logger)
//    {
//        _httpClient = httpClient;
//        _configuration = configuration;
//        _logger = logger;
//    }

//    // ══════════════════════════════════════════════════════════
//    // PUBLIC — ParseResumeAsync
//    // ══════════════════════════════════════════════════════════
//    public async Task<AffindaParseResult> ParseResumeAsync(IFormFile file)
//    {
//        try
//        {
//            SetAuthHeader();

//            // ── STEP 1: Upload document ───────────────────────────────
//            var identifier = await UploadDocumentAsync(file);
//            if (identifier == null)
//                return Fail("Affinda upload did not return a document identifier.");

//            _logger.LogInformation("Affinda document uploaded. Identifier: {Id}", identifier);

//            // ── STEP 2: Poll until ready ──────────────────────────────
//            var document = await PollUntilReadyAsync(identifier);
//            if (document == null)
//                return Fail("Affinda document did not become ready within the timeout.");

//            if (document.Meta?.Failed == true)
//                return Fail($"Affinda parsing failed: {document.Error?.ErrorDetail}");

//            // ── STEP 3: Map to AffindaParseResult ────────────────────
//            return MapToParseResult(document, identifier);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "AffindaService.ParseResumeAsync failed");
//            return Fail(ex.Message);
//        }
//    }

//    // ══════════════════════════════════════════════════════════
//    // STEP 1 — Upload to Affinda
//    // ══════════════════════════════════════════════════════════
//    private async Task<string?> UploadDocumentAsync(IFormFile file)
//    {
//        using var form = new MultipartFormDataContent();

//        // File
//        using var stream = file.OpenReadStream();
//        var fileContent = new StreamContent(stream);
//        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
//        form.Add(fileContent, "file", file.FileName);

//        // Workspace — required for Affinda v3
//        form.Add(new StringContent(WorkspaceId), "workspace");

//        // Wait = false — upload async, then poll
//        form.Add(new StringContent("false"), "wait");

//        var response = await _httpClient.PostAsync("https://api.affinda.com/v3/documents", form);
//        var body = await response.Content.ReadAsStringAsync();

//        _logger.LogDebug("Affinda upload response {Status}: {Body}", response.StatusCode, body);

//        if (!response.IsSuccessStatusCode)
//        {
//            _logger.LogError("Affinda upload failed {Status}: {Body}", response.StatusCode, body);
//            return null;
//        }

//        // Upload response is a single document object (not array)
//        var doc = JsonSerializer.Deserialize<AffindaSingleDocumentResponse>(body, _jsonOpts);
//        return doc?.Meta?.Identifier;
//    }

//    // ══════════════════════════════════════════════════════════
//    // STEP 2 — Poll GET /v3/documents/{identifier}
//    // ══════════════════════════════════════════════════════════
//    private async Task<AffindaSingleDocumentResponse?> PollUntilReadyAsync(string identifier)
//    {
//        const int maxAttempts = 20;
//        const int delaySeconds = 3;

//        for (int attempt = 1; attempt <= maxAttempts; attempt++)
//        {
//            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

//            SetAuthHeader();   // refresh header each call

//            var response = await _httpClient.GetAsync(
//                $"https://api.affinda.com/v3/documents/{identifier}");

//            var body = await response.Content.ReadAsStringAsync();

//            if (!response.IsSuccessStatusCode)
//            {
//                _logger.LogWarning("Affinda poll attempt {Attempt} failed: {Status}", attempt, response.StatusCode);
//                continue;
//            }

//            var doc = JsonSerializer.Deserialize<AffindaSingleDocumentResponse>(body, _jsonOpts);

//            if (doc?.Meta?.Ready == true || doc?.Meta?.Failed == true)
//            {
//                _logger.LogInformation(
//                    "Affinda document {Id} ready after {Attempt} attempt(s). Failed={Failed}",
//                    identifier, attempt, doc.Meta.Failed);
//                return doc;
//            }

//            _logger.LogDebug("Affinda document {Id} not ready yet (attempt {Attempt}/{Max})",
//                identifier, attempt, maxAttempts);
//        }

//        _logger.LogError("Affinda document {Id} did not become ready after {Max} attempts",
//            identifier, maxAttempts);
//        return null;
//    }

//    // ══════════════════════════════════════════════════════════
//    // STEP 3 — Map Affinda response → AffindaParseResult
//    // ══════════════════════════════════════════════════════════
//    private static AffindaParseResult MapToParseResult(
//        AffindaSingleDocumentResponse document,
//        string identifier)
//    {
//        var d = document.Data;

//        // ── Full name ─────────────────────────────────────────
//        var firstName = d?.CandidateName?.FirstName?.Trim();
//        var familyName = d?.CandidateName?.FamilyName?.Trim();
//        var fullName = string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(familyName)
//            ? null
//            : $"{firstName} {familyName}".Trim();

//        // ── Phone ─────────────────────────────────────────────
//        var phone = d?.PhoneNumber?.FirstOrDefault()?.FormattedNumber
//                 ?? d?.PhoneNumber?.FirstOrDefault()?.RawText;

//        // ── Email ─────────────────────────────────────────────
//        var email = d?.Email?.FirstOrDefault();

//        // ── Primary trade = first work experience title ───────
//        var primaryTrade = d?.WorkExperience?.FirstOrDefault()?.JobTitle;

//        // ── Experience years (round to nearest int) ───────────
//        int? experienceYrs = d?.TotalYearsExperience.HasValue == true
//            ? (int)Math.Round(d.TotalYearsExperience.Value)
//            : null;

//        // ── Skills — deduplicate by name ──────────────────────
//        var skills = (d?.Skill ?? new())
//            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
//            .Select(s => s.Name!)
//            .Distinct(StringComparer.OrdinalIgnoreCase)
//            .ToList();

//        // ── OCR confidence ────────────────────────────────────
//        decimal? confidence = document.Meta?.OcrConfidence;

//        // ── Store raw JSON for future re-parsing ──────────────
//        var rawJson = JsonSerializer.Serialize(document, new JsonSerializerOptions
//        {
//            WriteIndented = false,
//            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
//        });

//        return new AffindaParseResult
//        {
//            Success = true,
//            AffindaDocId = identifier,
//            ParsedName = fullName,
//            ParsedPhone = phone,
//            ParsedEmail = email,
//            ParsedTrade = primaryTrade,
//            ParsedExperienceYrs = experienceYrs,
//            ParsedSkills = skills,
//            AiConfidenceScore = confidence,
//            City = d?.Location?.City,
//            State = d?.Location?.State,
//            Country = d?.Location?.Country,
//            WorkExperiences = d?.WorkExperience ?? new(),
//            Educations = d?.Education ?? new(),
//            Languages = d?.Language ?? new(),
//            RawAffindaJson = rawJson
//        };
//    }

//    // ══════════════════════════════════════════════════════════
//    // HELPERS
//    // ══════════════════════════════════════════════════════════
//    private void SetAuthHeader()
//    {
//        _httpClient.DefaultRequestHeaders.Authorization =
//            new AuthenticationHeaderValue("Bearer", ApiKey);
//    }

//    private static AffindaParseResult Fail(string message) =>
//        new() { Success = false, ErrorMessage = message };
//}
