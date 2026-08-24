using System.Text.Json;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter
{
    // Place this file at:
    // JobPortal.Services/Implement/Recruiter/Msg91OtpService.cs
    public class Msg91OtpService : ITwilioOtpService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<Msg91OtpService> _logger;

        private const string SendUrl = "https://control.msg91.com/api/v5/otp";
        private const string VerifyUrl = "https://control.msg91.com/api/v5/otp/verify";

        public Msg91OtpService(
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<Msg91OtpService> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> SendOtpAsync(string phoneNumber)
        {
            try
            {
                var mobile = NormalizeForMsg91(phoneNumber);
                var authKey = _config["Msg91:AuthKey"];
                var templateId = _config["Msg91:TemplateId"];

                var url =
                    $"{SendUrl}?template_id={templateId}&mobile={mobile}&authkey={authKey}";

                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync(url, content: null);
                var body = await response.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "MSG91 SEND OTP - Mobile:{Mobile} StatusCode:{StatusCode} Body:{Body}",
                    mobile, response.StatusCode, body);

                if (!response.IsSuccessStatusCode)
                    return false;

                using var doc = JsonDocument.Parse(body);
                var type = doc.RootElement.TryGetProperty("type", out var typeProp)
                    ? typeProp.GetString()
                    : null;

                return type == "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MSG91 SEND OTP FAILED - Phone:{Phone}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode)
        {
            try
            {
                var mobile = NormalizeForMsg91(phoneNumber);
                var authKey = _config["Msg91:AuthKey"];

                var url =
                    $"{VerifyUrl}?mobile={mobile}&otp={otpCode}&authkey={authKey}";

                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "MSG91 VERIFY OTP - Mobile:{Mobile} StatusCode:{StatusCode} Body:{Body}",
                    mobile, response.StatusCode, body);

                if (!response.IsSuccessStatusCode)
                    return false;

                using var doc = JsonDocument.Parse(body);
                var type = doc.RootElement.TryGetProperty("type", out var typeProp)
                    ? typeProp.GetString()
                    : null;

                return type == "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MSG91 VERIFY OTP FAILED - Phone:{Phone}", phoneNumber);
                return false;
            }
        }

        // MSG91 wants the number WITHOUT a leading "+", e.g. 919876543210
        // Twilio wants it WITH a leading "+", e.g. +919876543210
        // The router always calls us with the Twilio-style "+91..." format,
        // so we strip the "+" here.
        private static string NormalizeForMsg91(string phoneNumber)
        {
            return phoneNumber.TrimStart('+');
        }
    }
}