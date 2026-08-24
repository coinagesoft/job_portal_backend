using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter
{
    // Place this file at:
    // JobPortal.Services/Implement/Recruiter/OtpProviderRouter.cs
    //
    // This is the ONLY class registered against ITwilioOtpService in Program.cs.
    // Every existing caller (RecruiterAuthService, CandidateLoginServices,
    // RecruiterRegistrationService, CandidateAuthService, Admin AuthService, etc.)
    // keeps injecting ITwilioOtpService exactly as before — nothing else changes.
    public class OtpProviderRouter : ITwilioOtpService
    {
        private readonly TwilioOtpService _twilio;
        private readonly Msg91OtpService _msg91;
        private readonly ILogger<OtpProviderRouter> _logger;

        public OtpProviderRouter(
            TwilioOtpService twilio,
            Msg91OtpService msg91,
            ILogger<OtpProviderRouter> logger)
        {
            _twilio = twilio;
            _msg91 = msg91;
            _logger = logger;
        }

        public Task<bool> SendOtpAsync(string phoneNumber)
        {
            var useIndia = IsIndianNumber(phoneNumber);

            _logger.LogInformation(
                "OTP ROUTER - Phone:{Phone} Provider:{Provider}",
                phoneNumber, useIndia ? "MSG91" : "Twilio");

            return useIndia
                ? _msg91.SendOtpAsync(phoneNumber)
                : _twilio.SendOtpAsync(phoneNumber);
        }

        public Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode)
        {
            var useIndia = IsIndianNumber(phoneNumber);

            _logger.LogInformation(
                "OTP ROUTER VERIFY - Phone:{Phone} Provider:{Provider}",
                phoneNumber, useIndia ? "MSG91" : "Twilio");

            return useIndia
                ? _msg91.VerifyOtpAsync(phoneNumber, otpCode)
                : _twilio.VerifyOtpAsync(phoneNumber, otpCode);
        }

        // phoneNumber always arrives here as "+<countrycode><number>"
        // e.g. "+919876543210" (India) or "+14155552671" (US).
        // Indian mobile numbers are always +91 followed by exactly 10 digits.
        private static bool IsIndianNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            var digits = phoneNumber.TrimStart('+');

            return digits.StartsWith("91") && digits.Length == 12;
        }
    }
}