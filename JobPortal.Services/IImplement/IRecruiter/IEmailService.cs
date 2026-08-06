using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface IEmailService
    {
        Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody);

        // Same as SendEmailAsync but with a single file attachment — used
        // for emailing invoice PDFs, generated CVs, etc. Kept as a separate
        // method rather than an optional param so every existing caller of
        // SendEmailAsync (OTPs, sub-user invites, ...) is unaffected.
        Task SendEmailWithAttachmentAsync(
            string toEmail,
            string subject,
            string htmlBody,
            byte[] attachmentBytes,
            string attachmentFileName,
            string attachmentContentType = "application/pdf");

        Task SendOtpEmailAsync(
            string email,
            string otp);

        Task SendAdminOtpEmailAsync(
    string email,
    string otp);
    }
}