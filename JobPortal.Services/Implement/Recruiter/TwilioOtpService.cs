using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Verify.V2.Service;

namespace JobPortal.Services.Implement.Recruiter
{
    public class TwilioOtpService : ITwilioOtpService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TwilioOtpService> _logger;

        public TwilioOtpService(
            IConfiguration config,
            ILogger<TwilioOtpService> logger)
        {
            _config = config;
            _logger = logger;

            TwilioClient.Init(
                _config["Twilio:AccountSid"],
                _config["Twilio:AuthToken"]);
        }

        public async Task<bool> SendOtpAsync(
            string phoneNumber)
        {
            try
            {
                var serviceSid =
                    _config["Twilio:VerifyServiceSid"];

                _logger.LogInformation(
                    "Sending Twilio Verify OTP to {Phone}. ServiceSid:{ServiceSid}",
                    phoneNumber,
                    serviceSid);

                var verification =
                    await VerificationResource.CreateAsync(
                        to: phoneNumber,
                        channel: "sms",
                        pathServiceSid: serviceSid);

                _logger.LogInformation(
                    "Twilio Verify response. Status:{Status}",
                    verification.Status);

                return verification.Status == "pending";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Twilio OTP sending failed");

                throw;
            }
        }

        public async Task<bool> VerifyOtpAsync(
            string phoneNumber,
            string otpCode)
        {
            try
            {
                var serviceSid =
                    _config["Twilio:VerifyServiceSid"];

                var verificationCheck =
                    await VerificationCheckResource
                        .CreateAsync(
                            to: phoneNumber,
                            code: otpCode,
                            pathServiceSid: serviceSid);

                _logger.LogInformation(
                    "Twilio Verify Check response. Status:{Status}",
                    verificationCheck.Status);

                return verificationCheck.Status ==
                       "approved";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Twilio OTP verification failed");

                throw;
            }
        }
    }
}