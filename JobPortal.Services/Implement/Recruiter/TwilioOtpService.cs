using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Verify.V2.Service;

namespace JobPortal.Services.Implement.Recruiter
{
    public class TwilioOtpService : ITwilioOtpService
    {
        private readonly IConfiguration _config;

        public TwilioOtpService(
            IConfiguration config)
        {
            _config = config;

            TwilioClient.Init(
                _config["Twilio:AccountSid"],
                _config["Twilio:AuthToken"]);
        }

        public async Task<bool> SendOtpAsync(
            string phoneNumber)
        {
            var verification =
                await VerificationResource.CreateAsync(
                    to: phoneNumber,
                    channel: "sms",
                    pathServiceSid:
                        _config["Twilio:VerifyServiceSid"]);

            return verification.Status == "pending";
        }

        public async Task<bool> VerifyOtpAsync(
            string phoneNumber,
            string otpCode)
        {
            var verificationCheck =
                await VerificationCheckResource
                    .CreateAsync(
                        to: phoneNumber,
                        code: otpCode,
                        pathServiceSid:
                            _config["Twilio:VerifyServiceSid"]);

            return verificationCheck.Status ==
                   "approved";
        }
    }
}