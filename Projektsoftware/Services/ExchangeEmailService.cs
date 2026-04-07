using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    public class ExchangeEmailService
    {
        private readonly ExchangeConfig config;

        public ExchangeEmailService()
        {
            config = ExchangeConfig.Load();
        }

        public ExchangeEmailService(ExchangeConfig config)
        {
            this.config = config;
        }

        public bool IsConfigured => config.IsConfigured;

        public async Task SendEmailAsync(string to, string subject, string body, string? cc = null, string? bcc = null, string? pdfFileName = null, byte[]? pdfBytes = null)
        {
            var message = new MimeMessage();
            var senderName = string.IsNullOrWhiteSpace(config.SenderName) ? config.Email : config.SenderName;
            message.From.Add(new MailboxAddress(senderName, config.Email));
            foreach (var addr in to.Split(';', ',').Select(a => a.Trim()).Where(a => a.Length > 0))
                message.To.Add(MailboxAddress.Parse(addr));
            if (!string.IsNullOrWhiteSpace(cc))
                foreach (var addr in cc.Split(';', ',').Select(a => a.Trim()).Where(a => a.Length > 0))
                    message.Cc.Add(MailboxAddress.Parse(addr));
            if (!string.IsNullOrWhiteSpace(bcc))
                foreach (var addr in bcc.Split(';', ',').Select(a => a.Trim()).Where(a => a.Length > 0))
                    message.Bcc.Add(MailboxAddress.Parse(addr));
            message.Subject = subject;
            var builder = new BodyBuilder { TextBody = body };
            if (pdfBytes != null && pdfBytes.Length > 0 && !string.IsNullOrWhiteSpace(pdfFileName))
                builder.Attachments.Add(pdfFileName, pdfBytes, new ContentType("application", "pdf"));
            message.Body = builder.ToMessageBody();
            using var smtp = new SmtpClient();
            if (config.AcceptInvalidCertificates)
            {
                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
                smtp.CheckCertificateRevocation = false;
            }
            var socketOptions = config.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await smtp.ConnectAsync(config.SmtpServer, config.SmtpPort, socketOptions);
            await smtp.AuthenticateAsync(config.Email, config.Password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        public async Task<(bool Success, string Error)> TestConnectionAsync()
        {
            try
            {
                using var smtp = new SmtpClient();
                if (config.AcceptInvalidCertificates)
                {
                    smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    smtp.CheckCertificateRevocation = false;
                }
                var socketOptions = config.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                await smtp.ConnectAsync(config.SmtpServer, config.SmtpPort, socketOptions);
                await smtp.AuthenticateAsync(config.Email, config.Password);
                await smtp.DisconnectAsync(true);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException is not null ? $"{ex.Message} → {ex.InnerException.Message}" : ex.Message;
                return (false, msg);
            }
        }
    }
}
