using JobPortal.Application.DTOs.Recruiter.AIJobDescription;
using JobPortal.Services.IImplement.AI;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace JobPortal.Services.Implement.AI;

/// <summary>
/// Two behaviours:
/// 1. AUTO-GENERATE  — full JD the moment employer fills all meta fields.
/// 2. INLINE SUGGEST — 3 short continuations while employer is typing.
/// Both use gpt-4o-mini via OpenAI Chat Completions.
/// </summary>
public class AiJobDescriptionService : IAiJobDescriptionService
{
    private readonly HttpClient _http;
    private const string Model = "gpt-4o-mini";

    public AiJobDescriptionService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        var apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey missing.");

        _http = httpClientFactory.CreateClient("OpenAI");
        _http.BaseAddress = new Uri("https://api.openai.com/");
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    // ══════════════════════════════════════════════════════════
    // 1. AUTO-GENERATE
    //    Triggered when all meta-fields are filled.
    //    Returns one polished JD + suggested skills.
    // ══════════════════════════════════════════════════════════

    public async Task<AutoGenerateJdResponseDto> AutoGenerateAsync(
        AutoGenerateJdRequestDto req)
    {
        try
        {
            var systemPrompt =
                """
                You are an expert HR copywriter for maritime, offshore, and skilled-trades
                recruitment in India and the Middle East.

                Produce a concise, professional job description split into clear parts.
                Keep it brief and skimmable. Use these rules:
                  - summary: 2-3 sentence role overview.
                  - responsibilities: 5 short bullet strings, each starting with an action verb.
                  - requirements: 4-5 short bullet strings (experience, certifications, education).
                  - benefits: 2-3 short bullet strings (what the company offers).
                  - suggestedSkills: 8-12 short skill strings relevant to the role.

                Return ONLY valid JSON - no markdown, no preamble:
                {
                  "summary": "...",
                  "responsibilities": ["...", "..."],
                  "requirements": ["...", "..."],
                  "benefits": ["...", "..."],
                  "suggestedSkills": ["skill1", "skill2"]
                }
                """;

            var userPrompt = BuildAutoGeneratePrompt(req);
            var raw = await CallChatAsync(systemPrompt, userPrompt, maxTokens: 900);
            raw = StripFences(raw);

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            string GetStr(string name) =>
                root.TryGetProperty(name, out var v) ? v.GetString() ?? string.Empty : string.Empty;

            List<string> GetArr(string name) =>
                root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array
                    ? v.EnumerateArray()
                        .Select(x => x.GetString() ?? string.Empty)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList()
                    : new List<string>();

            var summary = GetStr("summary");
            var responsibilities = GetArr("responsibilities");
            var requirements = GetArr("requirements");
            var benefits = GetArr("benefits");
            var skills = GetArr("suggestedSkills");

            if (string.IsNullOrWhiteSpace(summary) && responsibilities.Count == 0)
                return AutoGenFailure("AI returned an empty description.");

            // Main JD (Step 1): summary + key responsibilities.
            var main = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(summary))
                main.AppendLine(summary).AppendLine();
            if (responsibilities.Count > 0)
            {
                main.AppendLine("Key Responsibilities:");
                foreach (var r in responsibilities) main.AppendLine($"• {r}");
            }

            // Additional JD (Step 3): requirements + what we offer.
            var extra = new System.Text.StringBuilder();
            if (requirements.Count > 0)
            {
                extra.AppendLine("Requirements:");
                foreach (var r in requirements) extra.AppendLine($"• {r}");
            }
            if (benefits.Count > 0)
            {
                if (extra.Length > 0) extra.AppendLine();
                extra.AppendLine("What We Offer:");
                foreach (var b in benefits) extra.AppendLine($"• {b}");
            }

            return new AutoGenerateJdResponseDto
            {
                Success = true,
                Message = "Job description generated successfully.",
                GeneratedDescription = main.ToString().Trim(),
                AdditionalDescription = extra.ToString().Trim(),
                Summary = summary,
                Responsibilities = responsibilities,
                Requirements = requirements,
                Benefits = benefits,
                SuggestedSkills = skills
            };
        }
        catch (Exception ex)
        {
            return AutoGenFailure($"AI generation failed: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════
    // 2. INLINE SUGGESTIONS
    //    Called on debounced keyup (~600 ms) while employer types.
    //    Returns 3 short completions / improvement phrases.
    // ══════════════════════════════════════════════════════════

    public async Task<JdInlineSuggestionResponseDto> GetInlineSuggestionsAsync(
        JdInlineSuggestionRequestDto req)
    {
        try
        {
            // Don't burn API calls on very short input
            if (string.IsNullOrWhiteSpace(req.CurrentText) ||
                req.CurrentText.Trim().Length < 20)
            {
                return new JdInlineSuggestionResponseDto
                {
                    Success = true,
                    Suggestions = new()
                };
            }

            var systemPrompt =
                """
                You are an AI writing assistant embedded in a job-posting form for a maritime /
                offshore / skilled-trades job portal.
                
                The employer is typing a job description. Your job is to suggest 3 SHORT next
                sentences or phrase continuations that fit naturally after what they have written.
                
                Rules:
                - Each suggestion must be 1 sentence (15–35 words).
                - Stay on-topic with the role context provided.
                - Do NOT repeat what is already written.
                - Return ONLY a JSON array of 3 strings — no markdown, no explanation:
                  ["suggestion 1", "suggestion 2", "suggestion 3"]
                """;

            var userPrompt = BuildInlineSuggestionPrompt(req);
            var raw = await CallChatAsync(systemPrompt, userPrompt, maxTokens: 250);
            raw = StripFences(raw);

            var suggestions = JsonSerializer.Deserialize<List<string>>(raw)
                ?? new List<string>();

            return new JdInlineSuggestionResponseDto
            {
                Success = true,
                Suggestions = suggestions
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Take(3)
                    .ToList()
            };
        }
        catch
        {
            // Inline suggestions failing silently is acceptable —
            // the employer can still type freely.
            return new JdInlineSuggestionResponseDto
            {
                Success = false,
                Suggestions = new()
            };
        }
    }

    // ══════════════════════════════════════════════════════════
    // 3. SKILL SUGGESTIONS  (Step 3)
    // ══════════════════════════════════════════════════════════

    public async Task<AiSkillSuggestionResponseDto> SuggestSkillsAsync(
        AiSkillSuggestionRequestDto request)
    {
        try
        {
            var systemPrompt =
                "You are a job portal expert for the maritime, offshore, and skilled-trades industry. " +
                "Return ONLY a JSON array of skill strings — no extra text, no markdown, no explanation.";

            var userPrompt =
                $"Suggest the top 10–15 key skills for a {request.JobTitle} " +
                $"in the {request.TradeCategory} trade." +
                (string.IsNullOrWhiteSpace(request.JobDescription)
                    ? ""
                    : $"\n\nJob description context:\n" +
                      $"{request.JobDescription[..Math.Min(500, request.JobDescription.Length)]}");

            var raw = await CallChatAsync(systemPrompt, userPrompt, maxTokens: 400);
            raw = StripFences(raw);

            var skills = JsonSerializer.Deserialize<List<string>>(raw) ?? new();

            return new AiSkillSuggestionResponseDto { Success = true, Skills = skills };
        }
        catch
        {
            return new AiSkillSuggestionResponseDto { Success = false, Skills = new() };
        }
    }

    // ══════════════════════════════════════════════════════════
    // Private helpers
    // ══════════════════════════════════════════════════════════

    private static string BuildAutoGeneratePrompt(AutoGenerateJdRequestDto req)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Job Title: {req.JobTitle}");
        if (!string.IsNullOrWhiteSpace(req.Role))
            sb.AppendLine($"Specific Role: {req.Role}");
        sb.AppendLine($"Trade Category: {req.TradeCategory}");
        sb.AppendLine($"Experience Required: {req.ExperienceYears} year(s)");
        if (!string.IsNullOrWhiteSpace(req.JobType))
            sb.AppendLine($"Job Type: {req.JobType}");
        if (!string.IsNullOrWhiteSpace(req.EmploymentType))
            sb.AppendLine($"Employment Type: {req.EmploymentType}");
        return sb.ToString();
    }

    private static string BuildInlineSuggestionPrompt(JdInlineSuggestionRequestDto req)
    {
        var sb = new StringBuilder();

        // Provide role context if available — helps AI stay on-topic
        if (!string.IsNullOrWhiteSpace(req.JobTitle))
            sb.AppendLine($"Role: {req.JobTitle}" +
                (string.IsNullOrWhiteSpace(req.Role) ? "" : $" — {req.Role}"));
        if (!string.IsNullOrWhiteSpace(req.TradeCategory))
            sb.AppendLine($"Trade: {req.TradeCategory}");
        if (req.ExperienceYears.HasValue && req.ExperienceYears > 0)
            sb.AppendLine($"Experience: {req.ExperienceYears} year(s)");
        if (!string.IsNullOrWhiteSpace(req.JobType))
            sb.AppendLine($"Job Type: {req.JobType}");

        sb.AppendLine();
        sb.AppendLine("Text written so far:");

        // Send the last 600 chars — enough context without wasting tokens
        var text = req.CurrentText.Trim();
        sb.AppendLine(text.Length > 600 ? "..." + text[^600..] : text);

        return sb.ToString();
    }

    private async Task<string> CallChatAsync(
        string systemPrompt,
        string userPrompt,
        int maxTokens)
    {
        var payload = new
        {
            model = Model,
            max_tokens = maxTokens,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userPrompt   }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await _http.PostAsync("v1/chat/completions", content);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    private static string StripFences(string raw)
    {
        raw = raw.Trim();
        if (!raw.StartsWith("```")) return raw;
        var first = raw.IndexOf('\n');
        var last = raw.LastIndexOf("```");
        return (first > 0 && last > first)
            ? raw[(first + 1)..last].Trim()
            : raw;
    }

    private static AutoGenerateJdResponseDto AutoGenFailure(string msg) =>
        new() { Success = false, Message = msg };
}