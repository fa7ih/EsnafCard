using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using SecureCardSystem.Models;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace SecureCardSystem.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailConfiguration _config;

        public EmailSender(EmailConfiguration config)
        {
            _config = config;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return Execute(email, subject, htmlMessage);
        }

        public async Task Execute(string email, string subject, string message)
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress("Market Kartım", _config.From));
            mimeMessage.To.Add(new MailboxAddress(email, email));
            mimeMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = message
            };
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using (var smtpClient = new SmtpClient())
            {
                await smtpClient.ConnectAsync(_config.SmtpServer, _config.Port, SecureSocketOptions.SslOnConnect);
                await smtpClient.AuthenticateAsync(_config.Username, _config.Password);
                await smtpClient.SendAsync(mimeMessage);
                await smtpClient.DisconnectAsync(true);
            }
        }
    }
}