using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter
{
    // Place this file at:
    // JobPortal.Services/Implement/Recruiter/OtpProviderRouter.cs
    //
    // This is the ONLY class registered against ITwilioOtpService in Program.cs.
    // Every caller (RecruiterAuthService, CandidateLoginServices,
    // RecruiterRegistrationService, CandidateAuthService, RecruiterSettingsService...)
    // keeps injecting ITwilioOtpService exactly as before.
    //
    // ALL phone OTPs (India and every other country) are sent and verified
    // through Twilio Verify. There is no static/bypass OTP here - a real SMS
    // is sent and the code the user types is checked by Twilio.
    //
    // (MSG91 code is left in the project but is not used by this router.)
    public class OtpProviderRouter : ITwilioOtpService
    {
        private readonly TwilioOtpService _twilio;
        private readonly ILogger<OtpProviderRouter> _logger;

        public OtpProviderRouter(
            TwilioOtpService twilio,
            ILogger<OtpProviderRouter> logger)
        {
            _twilio = twilio;
            _logger = logger;
        }

        public Task<bool> SendOtpAsync(string phoneNumber)
        {
            var phone = NormalizeE164(phoneNumber);

            if (phone.Length == 0)
            {
                _logger.LogWarning("OTP ROUTER SEND - empty phone number.");
                return Task.FromResult(false);
            }

            _logger.LogInformation(
                "OTP ROUTER SEND - Phone:{Phone} Provider:Twilio", phone);

            return _twilio.SendOtpAsync(phone);
        }

        public Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode)
        {
            var phone = NormalizeE164(phoneNumber);

            if (phone.Length == 0 || string.IsNullOrWhiteSpace(otpCode))
                return Task.FromResult(false);

            _logger.LogInformation(
                "OTP ROUTER VERIFY - Phone:{Phone} Provider:Twilio", phone);

            return _twilio.VerifyOtpAsync(phone, otpCode.Trim());
        }

        // Callers build the number as CountryCode + MobileNumber. Twilio needs
        // E.164 ("+919876543210"), so strip spaces/dashes, make sure there is a
        // leading "+", and drop a typed trunk zero for India ("+91 0XXXXXXXXXX").
        // Send and Verify both use this, so they always target the same number.
        internal static string NormalizeE164(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

            if (digits.Length == 0)
                return string.Empty;

            if (digits.StartsWith("910") && digits.Length == 13)
                digits = "91" + digits.Substring(3);

            return "+" + digits;
        }
    }
}