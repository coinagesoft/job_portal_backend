using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Recruiter;

public class SubUserEmailService : ISubUserEmailService
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public SubUserEmailService(
        IEmailService emailService,
        IConfiguration configuration)
    {
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task SendSubUserInviteAsync(
       string email,
       string name,
       string companyName,
       string role,
       string inviteLink,
       DateTime expiresAt)
    {
        var subject = $"Invitation to join {companyName}";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
</head>

<body style='margin:0;padding:30px;background:#f5f5f5;font-family:Arial,Helvetica,sans-serif;'>

<table width='100%' cellpadding='0' cellspacing='0'>
<tr>
<td align='center'>

<table width='600' cellpadding='0' cellspacing='0'
style='background:#ffffff;border-radius:8px;padding:40px;'>

<tr>
<td>

<h2 style='margin-top:0;color:#333333;'>
You're Invited!
</h2>

<p>Hello <strong>{name}</strong>,</p>

<p>
You have been invited to join
<strong>{companyName}</strong>
as a
<strong>{role}</strong>.
</p>

<p>
Click the button below to accept your invitation.
</p>

<p style='margin:35px 0;'>

<a href='{inviteLink}'
style='
background:#0d6efd;
color:#ffffff;
text-decoration:none;
padding:12px 28px;
border-radius:6px;
display:inline-block;
font-weight:bold;'>

Accept Invitation

</a>

</p>

<p>
<b>Invitation Expiry:</b><br/>
{expiresAt:dd MMM yyyy hh:mm tt} UTC
</p>

<hr style='margin:30px 0;'>

<p>
If the button doesn't work, copy and paste this link into your browser:
</p>

<p style='word-break:break-all;'>

<a href='{inviteLink}'>{inviteLink}</a>

</p>

<br/>

<p>
Regards,<br/>
<strong>Job Portal Team</strong>
</p>

</td>
</tr>

</table>

</td>
</tr>
</table>

</body>
</html>";

        await _emailService.SendEmailAsync(
            email,
            subject,
            body);
    }

    public async Task SendSubUserDeactivatedAsync(
        string email,
        string name,
        string companyName)
    {
        var subject = $"Access Deactivated - {companyName}";

        var body = $@"
        <html>
        <body>
            <h2>Account Access Deactivated</h2>

            <p>Hello <strong>{name}</strong>,</p>

            <p>
                Your sub-user access for
                <strong>{companyName}</strong>
                has been deactivated.
            </p>

            <p>
                You can no longer access employer resources
                until your account is reactivated.
            </p>

            <p>
                If you believe this is incorrect,
                please contact your employer administrator.
            </p>

            <br/>

            <p>Regards,</p>
            <p><strong>Job Portal Team</strong></p>
        </body>
        </html>";

        await _emailService.SendEmailAsync(
            email,
            subject,
            body);
    }

    public async Task SendSubUserReactivatedAsync(
        string email,
        string name,
        string companyName)
    {
        var subject = $"Access Restored - {companyName}";

        var body = $@"
        <html>
        <body>
            <h2>Account Reactivated</h2>

            <p>Hello <strong>{name}</strong>,</p>

            <p>
                Your sub-user access for
                <strong>{companyName}</strong>
                has been restored.
            </p>

            <p>
                You may now log in and continue using
                the employer dashboard.
            </p>

            <br/>

            <p>Regards,</p>
            <p><strong>Job Portal Team</strong></p>
        </body>
        </html>";

        await _emailService.SendEmailAsync(
            email,
            subject,
            body);
    }

    public async Task SendOtpEmailAsync(string email, string otp)
    {
        try
        {
            using var client = new SmtpClient("smtp.gmail.com", 587);

            client.Credentials = new NetworkCredential(
                _configuration["EmailSettings:Username"],
                _configuration["EmailSettings:Password"]);

            client.EnableSsl = true;

            var mail = new MailMessage(
                _configuration["EmailSettings:FromEmail"],
                email);

            mail.Subject = "Job Portal Login OTP";

            mail.Body = $"Your OTP is: {otp}";

            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }
}
