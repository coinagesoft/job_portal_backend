
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace JobPortal.Services.Implement.Public;

/// <summary>
/// Server-side PayPal Orders v2 client.
/// Does not activate memberships or credits by itself.
/// </summary>
public sealed class PayPalPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayPalPaymentService> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    public PayPalPaymentService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<PayPalPaymentService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PayPalOrderResult> CreateOrderAsync(
        decimal amount,
        string currency,
        string referenceId,
        CancellationToken cancellationToken = default)
    {
        ValidateAmountAndCurrency(amount, currency);

        if (string.IsNullOrWhiteSpace(referenceId))
            throw new ArgumentException(
                "A transaction reference is required.",
                nameof(referenceId));

        var token = await GetAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "v2/checkout/orders");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Headers.Add(
            "PayPal-Request-Id",
            Guid.NewGuid().ToString("N"));

        request.Content = JsonContent.Create(new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = referenceId,
                    custom_id = referenceId,
                    amount = new
                    {
                        currency_code = currency.ToUpperInvariant(),
                        value = amount.ToString(
                            "0.00",
                            CultureInfo.InvariantCulture)
                    }
                }
            },
            application_context = new
            {
                user_action = "PAY_NOW",
                shipping_preference = "NO_SHIPPING"
            }
        });

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "PayPal create order failed with HTTP {StatusCode}",
                (int)response.StatusCode);

            throw new InvalidOperationException(
                "PayPal could not create the payment order.");
        }

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        var orderId = ReadString(root, "id");
        var status = ReadString(root, "status");

        var approvalUrl = root.TryGetProperty("links", out var links)
            ? links.EnumerateArray()
                .Where(link => ReadString(link, "rel") == "approve")
                .Select(link => ReadString(link, "href"))
                .FirstOrDefault()
            : null;

        if (string.IsNullOrWhiteSpace(orderId) ||
            string.IsNullOrWhiteSpace(approvalUrl))
        {
            throw new InvalidOperationException(
                "PayPal returned an incomplete order response.");
        }

        return new PayPalOrderResult(
            orderId,
            approvalUrl,
            status ?? "CREATED",
            amount,
            currency.ToUpperInvariant());
    }

    public async Task<PayPalCaptureResult> CaptureOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException(
                "PayPal order ID is required.",
                nameof(orderId));

        var token = await GetAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Headers.Add(
            "PayPal-Request-Id",
            Guid.NewGuid().ToString("N"));

        request.Content = JsonContent.Create(new { });

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "PayPal capture failed with HTTP {StatusCode}",
                (int)response.StatusCode);

            throw new InvalidOperationException(
                "PayPal could not capture the payment.");
        }

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        var status = ReadString(root, "status") ?? string.Empty;

        string? captureId = null;
        decimal? capturedAmount = null;
        string? capturedCurrency = null;

        if (root.TryGetProperty("purchase_units", out var units) &&
            units.ValueKind == JsonValueKind.Array)
        {
            foreach (var unit in units.EnumerateArray())
            {
                if (!unit.TryGetProperty("payments", out var payments) ||
                    !payments.TryGetProperty("captures", out var captures) ||
                    captures.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var capture in captures.EnumerateArray())
                {
                    captureId ??= ReadString(capture, "id");

                    if (capture.TryGetProperty(
                        "amount", out var amountElement))
                    {
                        capturedCurrency ??=
                            ReadString(amountElement, "currency_code");

                        var value = ReadString(amountElement, "value");

                        if (decimal.TryParse(
                            value,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out var parsed))
                        {
                            capturedAmount ??= parsed;
                        }
                    }
                }
            }
        }

        return new PayPalCaptureResult(
            orderId,
            captureId,
            status,
            capturedAmount,
            capturedCurrency);
    }

    private async Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) &&
            _tokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _accessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);

        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) &&
                _tokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return _accessToken;
            }

            var clientId = _configuration["PayPal:ClientId"];
            var clientSecret = _configuration["PayPal:ClientSecret"];

            if (string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new InvalidOperationException(
                    "PayPal ClientId and ClientSecret are not configured.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "v1/oauth2/token");

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            request.Content = new StringContent(
                "grant_type=client_credentials",
                Encoding.UTF8,
                "application/x-www-form-urlencoded");

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "PayPal OAuth request failed with HTTP {StatusCode}",
                    (int)response.StatusCode);

                throw new InvalidOperationException(
                    "PayPal authentication failed. Check backend credentials and environment.");
            }

            using var json = JsonDocument.Parse(body);

            _accessToken = ReadString(
                json.RootElement,
                "access_token");

            var expiresIn =
                json.RootElement.TryGetProperty("expires_in", out var expiry) &&
                expiry.TryGetInt32(out var seconds)
                    ? seconds
                    : 300;

            if (string.IsNullOrWhiteSpace(_accessToken))
            {
                throw new InvalidOperationException(
                    "PayPal did not return an access token.");
            }

            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(
                Math.Max(60, expiresIn));

            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private static void ValidateAmountAndCurrency(
        decimal amount,
        string currency)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(currency) ||
            currency.Length != 3 ||
            !currency.All(char.IsAsciiLetter))
        {
            throw new ArgumentException(
                "Currency must be a three-letter ISO currency code.",
                nameof(currency));
        }
    }

    private static string? ReadString(
        JsonElement element,
        string property)
    {
        return element.TryGetProperty(property, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}

public sealed record PayPalOrderResult(
    string OrderId,
    string ApprovalUrl,
    string Status,
    decimal Amount,
    string Currency);

public sealed record PayPalCaptureResult(
    string OrderId,
    string? CaptureId,
    string Status,
    decimal? CapturedAmount,
    string? CapturedCurrency)
{
    public bool IsCompleted =>
        string.Equals(
            Status,
            "COMPLETED",
            StringComparison.OrdinalIgnoreCase);
}
