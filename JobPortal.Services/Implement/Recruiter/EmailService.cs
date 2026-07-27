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
}