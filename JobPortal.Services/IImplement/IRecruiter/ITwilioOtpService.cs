using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface ITwilioOtpService
    {
        Task<bool> SendOtpAsync(string phoneNumber);

        Task<bool> VerifyOtpAsync(
            string phoneNumber,
            string otp);
    }
}