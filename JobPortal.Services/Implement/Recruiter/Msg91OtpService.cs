
using System.Text;
using System.Text.Json;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter
{
    public class Msg91OtpService : ITwilioOtpService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<Msg91OtpService> _logger;

        private const string VerifyAccessTokenUrl =
            "https://control.msg91.com/api/v5/widget/verifyAccessToken";

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

        // ---------------------------------------------------------
        // NEW MSG91 WIDGET METHOD
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

                var requestBody = new Dictionary<string, string>
                {
                    ["authkey"] = authKey,
                    ["access-token"] = accessToken
                };

                var json = JsonSerializer.Serialize(requestBody);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                var client = _httpClientFactory.CreateClient();

                var response = await client.PostAsync(
                    VerifyAccessTokenUrl,
                    content);

                var body = await response.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "MSG91 VERIFY ACCESS TOKEN - StatusCode:{StatusCode} Body:{Body}",
                    response.StatusCode,
                    body);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                using var doc = JsonDocument.Parse(body);

                var root = doc.RootElement;

                // MSG91 success/failure response can vary,
                // so first inspect the response type/message.
                if (root.TryGetProperty("type", out var typeProperty))
                {
                    var type = typeProperty.GetString();

                    if (string.Equals(
                        type,
                        "success",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Some responses may use "message" instead.
                if (root.TryGetProperty("message", out var messageProperty))
                {
                    var message = messageProperty.GetString();

                    if (!string.IsNullOrWhiteSpace(message) &&
                        message.Contains(
                            "success",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "MSG91 VERIFY ACCESS TOKEN FAILED.");

                return false;
            }
        }


        // ---------------------------------------------------------
        // OLD METHODS
        // ---------------------------------------------------------
        // Keep these temporarily because your existing application
        // may still reference ITwilioOtpService.
        //
        // We will remove/replace them after checking your controller
        // and frontend OTP flow.
        // ---------------------------------------------------------

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

        private static string NormalizeForMsg91(string phoneNumber)
        {
            return phoneNumber.TrimStart('+');
        }
    }
}



//using System.Text.Json;
//using JobPortal.Services.IImplement.IRecruiter;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;

//namespace JobPortal.Services.Implement.Recruiter
//{
//    // Place this file at:
//    // JobPortal.Services/Implement/Recruiter/Msg91OtpService.cs
//    public class Msg91OtpService : ITwilioOtpService
//    {
//        private readonly IConfiguration _config;
//        private readonly IHttpClientFactory _httpClientFactory;
//        private readonly ILogger<Msg91OtpService> _logger;

//        private const string SendUrl = "https://control.msg91.com/api/v5/otp";
//        private const string VerifyUrl = "https://control.msg91.com/api/v5/otp/verify";

//        public Msg91OtpService(
//            IConfiguration config,
//            IHttpClientFactory httpClientFactory,
//            ILogger<Msg91OtpService> logger)
//        {
//            _config = config;
//            _httpClientFactory = httpClientFactory;
//            _logger = logger;
//        }

//        public async Task<bool> SendOtpAsync(string phoneNumber)
//        {
//            try
//            {
//                var mobile = NormalizeForMsg91(phoneNumber);
//                var authKey = _config["Msg91:AuthKey"];
//                var templateId = _config["Msg91:TemplateId"];

//                var url =
//                    $"{SendUrl}?template_id={templateId}&mobile={mobile}&authkey={authKey}";

//                var client = _httpClientFactory.CreateClient();
//                var response = await client.PostAsync(url, content: null);
//                var body = await response.Content.ReadAsStringAsync();

//                _logger.LogInformation(
//                    "MSG91 SEND OTP - Mobile:{Mobile} StatusCode:{StatusCode} Body:{Body}",
//                    mobile, response.StatusCode, body);

//                if (!response.IsSuccessStatusCode)
//                    return false;

//                using var doc = JsonDocument.Parse(body);
//                var type = doc.RootElement.TryGetProperty("type", out var typeProp)
//                    ? typeProp.GetString()
//                    : null;

//                return type == "success";
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "MSG91 SEND OTP FAILED - Phone:{Phone}", phoneNumber);
//                return false;
//            }
//        }

//        public async Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode)
//        {
//            try
//            {
//                var mobile = NormalizeForMsg91(phoneNumber);
//                var authKey = _config["Msg91:AuthKey"];

//                var url =
//                    $"{VerifyUrl}?mobile={mobile}&otp={otpCode}&authkey={authKey}";

//                var client = _httpClientFactory.CreateClient();
//                var response = await client.GetAsync(url);
//                var body = await response.Content.ReadAsStringAsync();

//                _logger.LogInformation(
//                    "MSG91 VERIFY OTP - Mobile:{Mobile} StatusCode:{StatusCode} Body:{Body}",
//                    mobile, response.StatusCode, body);

//                if (!response.IsSuccessStatusCode)
//                    return false;

//                using var doc = JsonDocument.Parse(body);
//                var type = doc.RootElement.TryGetProperty("type", out var typeProp)
//                    ? typeProp.GetString()
//                    : null;

//                return type == "success";
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "MSG91 VERIFY OTP FAILED - Phone:{Phone}", phoneNumber);
//                return false;
//            }
//        }

//        // MSG91 wants the number WITHOUT a leading "+", e.g. 919876543210
//        // Twilio wants it WITH a leading "+", e.g. +919876543210
//        // The router always calls us with the Twilio-style "+91..." format,
//        // so we strip the "+" here.
//        private static string NormalizeForMsg91(string phoneNumber)
//        {
//            return phoneNumber.TrimStart('+');
//        }
//    }
//}