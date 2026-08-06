using JobPortal.Services.IImplement.IRecruiter;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody)
    {
        try
        {
            Console.WriteLine($"Sending email to: {toEmail}");

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    "Job Portal",
                    _configuration["EmailSettings:FromEmail"]));

            message.To.Add(MailboxAddress.Parse(toEmail));

            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = htmlBody
            };

            using var client = new MailKit.Net.Smtp.SmtpClient();

            await client.ConnectAsync(
                _configuration["EmailSettings:SmtpHost"],
                int.Parse(_configuration["EmailSettings:SmtpPort"]),
                SecureSocketOptions.StartTls);

            Console.WriteLine("Connected");

            await client.AuthenticateAsync(
                _configuration["EmailSettings:Username"],
                _configuration["EmailSettings:Password"]);

            Console.WriteLine("Authenticated");

            await client.SendAsync(message);

            Console.WriteLine("Email Sent");

            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }


    }


    public async Task SendEmailWithAttachmentAsync(
        string toEmail,
        string subject,
        string htmlBody,
        byte[] attachmentBytes,
        string attachmentFileName,
        string attachmentContentType = "application/pdf")
    {
        try
        {
            Console.WriteLine($"Sending email with attachment to: {toEmail}");

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    "Job Portal",
                    _configuration["EmailSettings:FromEmail"]));

            message.To.Add(MailboxAddress.Parse(toEmail));

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            var contentType = ContentType.Parse(attachmentContentType);
            bodyBuilder.Attachments.Add(
                attachmentFileName,
                attachmentBytes,
                contentType);

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new MailKit.Net.Smtp.SmtpClient();

            await client.ConnectAsync(
                _configuration["EmailSettings:SmtpHost"],
                int.Parse(_configuration["EmailSettings:SmtpPort"]),
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                _configuration["EmailSettings:Username"],
                _configuration["EmailSettings:Password"]);

            await client.SendAsync(message);

            Console.WriteLine("Email with attachment sent");

            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }


    public async Task SendOtpEmailAsync(string email, string otp)
    {
        await SendEmailAsync(
            email,
            "Job Portal Login OTP",
            $@"
        <h2>Your OTP is: {otp}</h2>
        <p>Valid for 10 minutes.</p>
        <p>Do not share this OTP.</p>");
    }

    public async Task SendAdminOtpEmailAsync(
      string email,
      string otp)
    {
        var html = $@"
<!DOCTYPE html>

<html>

<head>
    <meta charset='UTF-8'>
</head>

<body style='font-family:Arial;background:#f5f5f5;padding:30px;'>

<div style='max-width:600px;
background:#fff;
padding:30px;
margin:auto;
border-radius:8px;
box-shadow:0 2px 10px rgba(0,0,0,0.08);'>

<h2>Hello,</h2>

<p>
Your One-Time Password (OTP) for <strong>Job Portal Admin Login</strong> is:
</p>

<div style='
background:#f3f3f3;
padding:20px;
font-size:34px;
font-weight:bold;
letter-spacing:8px;
text-align:center;
margin:25px 0;
border-radius:6px;'>

{otp}

</div>

<p>
This OTP is valid for <strong>5 minutes</strong>.
</p>

<p>
For your security, please do not share this OTP with anyone.
</p>

<p>
If you did not request this login, you can safely ignore this email.
</p>

<hr/>

<p style='font-size:12px;color:#888;margin-top:20px;'>
Regards,<br/>
<strong>Job Portal Admin Team</strong>
</p>

</div>

</body>

</html>";

        await SendEmailAsync(
            email,
            "Job Portal Admin Login OTP",
            html);
    }
}