using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement;

/// <summary>Resolves the visitor's country from the public client IP.
/// Forwarded headers must only be accepted from the trusted Nginx proxy.</summary>
public sealed class VisitorCountryService
{
    private static readonly HashSet<string> SupportedRegions = new(StringComparer.OrdinalIgnoreCase)
    {
        "in", "us", "gb", "au", "ae", "sa", "qa", "kw", "bh", "om", "eg", "jo", "lb", "tr"
    };

    private static readonly Dictionary<string, string> RegionCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["in"] = "INR",
        ["us"] = "USD",
        ["gb"] = "GBP",
        ["au"] = "AUD",
        ["ae"] = "AED",
        ["sa"] = "SAR",
        ["qa"] = "QAR",
        ["kw"] = "KWD",
        ["bh"] = "BHD",
        ["om"] = "OMR",
        ["eg"] = "EGP",
        ["jo"] = "JOD",
        ["lb"] = "LBP",
        ["tr"] = "TRY"
    };

    // Currencies PayPal can actually settle. AED/SAR/QAR/KWD/BHD/OMR/EGP/JOD/LBP/TRY are not
    // on this list, so visitors from those regions are charged in the USD ("us") plans instead.
    private static readonly HashSet<string> PayPalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "GBP", "AUD", "CAD", "EUR", "SGD", "NZD", "CHF", "SEK", "NOK", "DKK", "PLN", "CZK", "HUF", "HKD", "JPY"
    };

    public static string CurrencyForRegion(string region) =>
        RegionCurrencies.TryGetValue(region ?? string.Empty, out var c) ? c : "USD";

    public static bool IsPayPalCurrency(string currency) => PayPalCurrencies.Contains(currency ?? string.Empty);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VisitorCountryService> _logger;

    public VisitorCountryService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<VisitorCountryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<VisitorCountryResult?> ResolveAsync(IPAddress? address, CancellationToken cancellationToken = default)
    {
        // Local testing only: GeoLocation:DevCountryOverride=IN|US|... is honoured ONLY when
        // ASPNETCORE_ENVIRONMENT=Development, so it can never affect production.
        var devOverride = _configuration["GeoLocation:DevCountryOverride"];
        if (!string.IsNullOrWhiteSpace(devOverride) &&
            string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
        {
            return BuildResult(devOverride.Trim().ToUpperInvariant(), devOverride.Trim().ToUpperInvariant());
        }

        if (address is null || IPAddress.IsLoopback(address) || IsPrivate(address))
            return null;

        var ip = address.ToString();
        var baseUrl = (_configuration["GeoLocation:BaseUrl"] ?? "https://ipwho.is").TrimEnd('/');
        var url = $"{baseUrl}/{Uri.EscapeDataString(ip)}";

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(VisitorCountryService));
            client.Timeout = TimeSpan.FromSeconds(4);
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("IP geolocation provider returned HTTP {StatusCode}", (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = json.RootElement;
            if (root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False)
                return null;

            var countryCode = GetString(root, "country_code")?.Trim().ToUpperInvariant();
            var countryName = GetString(root, "country");
            if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2 || string.IsNullOrWhiteSpace(countryName))
                return null;

            return BuildResult(countryCode, countryName);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("IP geolocation request timed out");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to resolve visitor country");
            return null;
        }
    }

    private static VisitorCountryResult BuildResult(string countryCode, string countryName)
    {
        var isIndia = countryCode == "IN";
        var supported = SupportedRegions.Contains(countryCode.ToLowerInvariant());
        var region = supported ? countryCode.ToLowerInvariant() : "us"; // fallback pricing region
        var currency = RegionCurrencies[region];
        var fallback = !supported;

        // PayPal can't settle every regional currency — fall back to the USD plans.
        if (!isIndia && !IsPayPalCurrency(currency))
        {
            region = "us";
            currency = "USD";
            fallback = true;
        }

        return new VisitorCountryResult
        {
            CountryCode = countryCode,
            CountryName = countryName,
            Region = region,
            Currency = currency,
            PaymentGateway = isIndia ? "Razorpay" : "PayPal",
            IsFallbackRegion = fallback
        };
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool IsPrivate(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10 || bytes[0] == 127 ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 169 && bytes[1] == 254);
        }
        return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || IPAddress.IsLoopback(address) ||
               address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6None);
    }
}

public sealed class VisitorCountryResult
{
    public string CountryCode { get; init; } = string.Empty;
    public string CountryName { get; init; } = string.Empty;
    public string Region { get; init; } = "us";
    public string Currency { get; init; } = "USD";
    public string PaymentGateway { get; init; } = "PayPal";
    public bool IsFallbackRegion { get; init; }
}