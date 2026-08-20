using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SupportPanel.Data;
using SupportPanel.Interfaces;

namespace SupportPanel.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly AppDbContext _dbContext;
        private readonly IDataProtector _protector;
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(
            AppDbContext dbContext,
            IDataProtectionProvider dataProtectionProvider,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _protector = dataProtectionProvider.CreateProtector("SupportPanel.EmailSettings.Password.v1");
            _configuration = configuration;
        }

        public async Task SendAsync(
            string recipientEmail,
            string recipientName,
            string subject,
            string message)
        {
            var saved = await _dbContext.EmailSettings.AsNoTracking().FirstOrDefaultAsync();
            var enabled = saved?.Enabled ?? _configuration.GetValue<bool>("Email:Smtp:Enabled");
            if (!enabled)
            {
                return;
            }

            var host = saved?.Host ?? _configuration["Email:Smtp:Host"];
            var username = saved?.Username ?? _configuration["Email:Smtp:Username"];
            var password = saved?.ProtectedPassword is { Length: > 0 }
                ? _protector.Unprotect(saved.ProtectedPassword)
                : _configuration["Email:Smtp:Password"];
            var fromAddress = saved?.FromAddress ?? _configuration["Email:Smtp:FromAddress"];
            var fromName = saved?.FromName ?? _configuration["Email:Smtp:FromName"] ?? "SupportPanel";
            var port = saved?.Port ?? _configuration.GetValue("Email:Smtp:Port", 587);
            var enableSsl = saved?.EnableSsl ?? _configuration.GetValue("Email:Smtp:EnableSsl", true);

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(fromAddress) ||
                string.IsNullOrWhiteSpace(recipientEmail))
            {
                throw new InvalidOperationException("SMTP sunucu ve gönderici ayarları eksik.");
            }

            using var mail = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body = $"Merhaba {recipientName},\r\n\r\n{message}\r\n\r\nSupportPanel",
                IsBodyHtml = false
            };
            mail.To.Add(new MailAddress(recipientEmail, recipientName));

            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = string.IsNullOrWhiteSpace(username),
                Credentials = string.IsNullOrWhiteSpace(username)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(username, password)
            };

            await smtp.SendMailAsync(mail);
        }
    }
}
