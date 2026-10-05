using System.Text;
using System.Text.Json;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter
{
    /// <summary>
    /// MSG91 SMS OTP provider (used for Indian +91 mobile numbers).
    ///
    /// Required configuration (appsettings.json / user-secrets / env vars):
    ///   "Msg91": {
    ///     "AuthKey":          "<MSG91 dashboard -> AuthKey>",
    ///     "TemplateId":       "<MSG91 dashboard -> OTP -> Templates -> Template ID>",
    ///     "OtpExpiryMinutes": 10          // optional, default 10
    ///   }
    ///
    /// MSG91 generates, stores and checks the OTP itself, so no OTP value is
    /// kept in our database (same model as Twilio Verify).
    /// </summary>
    public class Msg91OtpService : ITwilioOtpService
    {
        private const string SendUrl = "https://control.msg91.com/api/v5/otp";
        private const string VerifyUrl = "https://control.msg91.com/api/v5/otp/verify";
        private const string VerifyAccessTokenUrl =
            "https://control.msg91.com/api/v5/widget/verifyAccessToken";

        private const int DefaultOtpExpiryMinutes = 10;
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<Msg91OtpService> _logger;

        public Msg91OtpService(
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<Msg91OtpService> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // ---------------------------------------------------------
        // SEND OTP  (POST /api/v5/otp)
        // ---------------------------------------------------------
        public async Task<bool> SendOtpAsync(string phoneNumber)
        {
            var masked = Mask(phoneNumber);

            try
            {
                var authKey = _config["Msg91:AuthKey"];
                var templateId = _config["Msg91:TemplateId"];

                if (string.IsNullOrWhiteSpace(authKey) ||
                    string.IsNullOrWhiteSpace(templateId))
                {
                    _logger.LogError(
                        "MSG91 SEND OTP FAILED - Msg91:AuthKey or Msg91:TemplateId is not configured.");
                    return false;
                }

                var mobile = ToMsg91Mobile(phoneNumber);

                if (mobile == null)
                {
                    _logger.LogWarning(
                        "MSG91 SEND OTP REJECTED - '{Phone}' is not a valid Indian mobile number.",
                        masked);
                    return false;
                }

                var expiry = GetOtpExpiryMinutes();

                var url =
                    $"{SendUrl}?template_id={Uri.EscapeDataString(templateId)}" +
                    $"&mobile={mobile}&otp_expiry={expiry}";

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };

                // AuthKey goes in a header (not the URL) so it never ends up in
                // HttpClient / proxy request logs.
                request.Headers.Add("authkey", authKey);

                var (httpOk, body) = await SendAsync(request);

                _logger.LogInformation(
                    "MSG91 SEND OTP - Mobile:{Mobile} HttpOk:{HttpOk} Body:{Body}",
                    masked, httpOk, body);

                return httpOk && IsSuccess(body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MSG91 SEND OTP FAILED - Phone:{Phone}", masked);
                return false;
            }
        }

        // ---------------------------------------------------------
        // VERIFY OTP  (GET /api/v5/otp/verify)
        // ---------------------------------------------------------
        public async Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode)
        {
            var masked = Mask(phoneNumber);

            try
            {
                var authKey = _config["Msg91:AuthKey"];

                if (string.IsNullOrWhiteSpace(authKey))
                {
                    _logger.LogError(
                        "MSG91 VERIFY OTP FAILED - Msg91:AuthKey is not configured.");
                    return false;
                }

                var mobile = ToMsg91Mobile(phoneNumber);
                var otp = otpCode?.Trim();

                if (mobile == null || string.IsNullOrEmpty(otp))
                {
                    return false;
                }

                var url =
                    $"{VerifyUrl}?mobile={mobile}&otp={Uri.EscapeDataString(otp)}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("authkey", authKey);

                var (httpOk, body) = await SendAsync(request);

                _logger.LogInformation(
                    "MSG91 VERIFY OTP - Mobile:{Mobile} HttpOk:{HttpOk} Body:{Body}",
                    masked, httpOk, body);

                return httpOk && IsSuccess(body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MSG91 VERIFY OTP FAILED - Phone:{Phone}", masked);
                return false;
            }
        }

        // ---------------------------------------------------------
        // MSG91 OTP WIDGET (front-end widget flow) - optional
        // ---------------------------------------------------------
        public async Task<bool> VerifyAccessTokenAsync(string accessToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    _logger.LogWarning(
                        "MSG91 ACCESS TOKEN VERIFICATION FAILED - Token is empty.");
                    return false;
                }

                var authKey = _config["Msg91:AuthKey"];

                if (string.IsNullOrWhiteSpace(authKey))
                {
                    _logger.LogError(
                        "MSG91 ACCESS TOKEN VERIFICATION FAILED - AuthKey is missing.");
                    return false;
                }

                var json = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["authkey"] = authKey,
                    ["access-token"] = accessToken
                });

                using var request = new HttpRequestMessage(HttpMethod.Post, VerifyAccessTokenUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                var (httpOk, body) = await SendAsync(request);

                _logger.LogInformation(
                    "MSG91 VERIFY ACCESS TOKEN - HttpOk:{HttpOk} Body:{Body}",
                    httpOk, body);

                return httpOk && IsSuccess(body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MSG91 VERIFY ACCESS TOKEN FAILED.");
                return false;
            }
        }

        // ---------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------
        private async Task<(bool HttpOk, string Body)> SendAsync(HttpRequestMessage request)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = RequestTimeout;

            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            return (response.IsSuccessStatusCode, body);
        }

        /// <summary>
        /// MSG91 answers HTTP 200 even for logical failures (wrong OTP, bad
        /// template, IP blocked...), so the JSON "type" field is what decides.
        /// Success looks like {"type":"success", ...}; failures like
        /// {"type":"error","message":"OTP not match"}.
        /// </summary>
        private static bool IsSuccess(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return false;

            try
            {
                using var doc = JsonDocument.Parse(body);

                return doc.RootElement.ValueKind == JsonValueKind.Object &&
                       doc.RootElement.TryGetProperty("type", out var type) &&
                       type.ValueKind == JsonValueKind.String &&
                       string.Equals(type.GetString(), "success",
                           StringComparison.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private int GetOtpExpiryMinutes()
        {
            return int.TryParse(_config["Msg91:OtpExpiryMinutes"], out var minutes) &&
                   minutes > 0
                ? minutes
                : DefaultOtpExpiryMinutes;
        }

        /// <summary>
        /// MSG91 wants digits only with the country code and no "+", e.g.
        /// 919876543210. Returns null if this is not a valid Indian mobile
        /// number (91 + 10 digits, first digit 6-9).
        /// </summary>
        internal static string? ToMsg91Mobile(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;

            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // tolerate "+91 0XXXXXXXXXX" (trunk zero typed by the user)
            if (digits.StartsWith("910") && digits.Length == 13)
                digits = "91" + digits.Substring(3);

            return digits.Length == 12 &&
                   digits.StartsWith("91") &&
                   digits[2] >= '6' && digits[2] <= '9'
                ? digits
                : null;
        }

        private static string Mask(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 6)
                return "***";

            return phone.Substring(0, 3) + new string('*', phone.Length - 5) +
                   phone.Substring(phone.Length - 2);
        }
    }
}