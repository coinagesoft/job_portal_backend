using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface ISubUserEmailService
    {
        Task SendSubUserInviteAsync(
            string email,
            string name,
            string companyName,
            string role,
            string inviteLink,
            DateTime expiresAt);

        Task SendSubUserDeactivatedAsync(
            string email,
            string name,
            string companyName);

        Task SendSubUserReactivatedAsync(
            string email,
            string name,
            string companyName);

        Task SendOtpEmailAsync(string email, string otp);
    }
}
