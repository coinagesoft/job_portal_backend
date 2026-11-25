using System.Net;
using System.Text;

namespace JobPortal.Services.Implement;

/// <summary>
/// Calls Gemini's generateContent endpoint with retries.
/// Gemini often answers 503 ("high demand") or 429 for a few seconds at a
/// time; one failed call should not fail a candidate's upload. This retries
/// the primary model with a short back-off and then tries a fallback model.
/// </summary>
public static class GeminiRequestHelper
{
    public const string DefaultFallbackModel = "gemini-2.5-flash";

    public const string BusyMessage =
        "Our document reader is very busy right now. " +
        "Please try uploading again in a minute.";

    public sealed record GeminiCallResult(
        bool Success,
        int StatusCode,
        string Body,
        string? NetworkError,
        string ModelUsed)
    {
        // True when the failure is Gemini being overloaded / rate limited /
        // unreachable (as opposed to a bad request or bad API key).
        public bool IsTransient =>
            NetworkError != null || IsTransientStatus(StatusCode);

        // Text that is safe to show to a candidate / recruiter.
        public string UserMessage =>
            Success ? string.Empty
            : IsTransient ? BusyMessage
            : "We could not read this document right now. " +
              "Please try again or upload a clearer copy.";
    }

    public static bool IsTransientStatus(int status) =>
        status == (int)HttpStatusCode.TooManyRequests ||
        status == 500 || status == 502 || status == 503 || status == 504;

    public static async Task<GeminiCallResult> PostWithRetryAsync(
        HttpClient http,
        string apiKey,
        string primaryModel,
        string? fallbackModel,
        string json,
        CancellationToken ct = default)
    {
        // (model, delay before attempt)
        var plan = new List<(string Model, TimeSpan Delay)>
        {
            (primaryModel, TimeSpan.Zero),
            (primaryModel, TimeSpan.FromSeconds(1)),
            (primaryModel, TimeSpan.FromSeconds(2)),
        };

        if (!string.IsNullOrWhiteSpace(fallbackModel) &&
            !string.Equals(fallbackModel, primaryModel,
                StringComparison.OrdinalIgnoreCase))
        {
            plan.Add((fallbackModel!, TimeSpan.FromSeconds(1)));
            plan.Add((fallbackModel!, TimeSpan.FromSeconds(2)));
        }

        GeminiCallResult last =
            new(false, 0, string.Empty, "No attempt made.", primaryModel);

        foreach (var (model, delay) in plan)
        {
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, ct);

            var endpoint =
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            try
            {
                // New content per attempt: a request body can't be re-sent
                // after it has been consumed.
                using var content =
                    new StringContent(json, Encoding.UTF8, "application/json");

                using var response =
                    await http.PostAsync(endpoint, content, ct);

                var body = await response.Content.ReadAsStringAsync(ct);

                last = new GeminiCallResult(
                    response.IsSuccessStatusCode,
                    (int)response.StatusCode,
                    body,
                    null,
                    model);

                if (last.Success) return last;

                // Bad request / invalid key / etc: retrying won't help.
                if (!IsTransientStatus(last.StatusCode)) return last;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (
                ex is HttpRequestException || ex is TaskCanceledException)
            {
                // Network error or HttpClient timeout: treat as transient.
                last = new GeminiCallResult(
                    false, 0, string.Empty, ex.Message, model);
            }
        }

        return last;
    }
}