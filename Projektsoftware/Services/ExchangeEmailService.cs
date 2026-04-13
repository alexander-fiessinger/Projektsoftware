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
        private readonly ExchangeConfig _config;

        public ExchangeEmailService()
        {
            _config = ExchangeConfig.Load();
        }

        public ExchangeEmailService(ExchangeConfig config)
        {
            _config = config;
        }

        public bool IsConfigured => _config.IsConfigured;

        public async Task SendEmailAsync(
            string to,
            string subject,
            string body,
            string? cc = null,
            string? bcc = null,
            string? pdfFileName = null,
            byte[]? pdfBytes = null)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config.SenderName ?? _config.Email, _config.Email));
            message.To.Add(MailboxAddress.Parse(to));

            if (!string.IsNullOrWhiteSpace(cc))
                foreach (var addr in cc.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    message.Cc.Add(MailboxAddress.Parse(addr.Trim()));

            if (!string.IsNullOrWhiteSpace(bcc))
                foreach (var addr in bcc.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    message.Bcc.Add(MailboxAddress.Parse(addr.Trim()));

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };

            if (pdfBytes != null && pdfFileName != null)
                bodyBuilder.Attachments.Add(pdfFileName, pdfBytes, new ContentType("application", "pdf"));

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            if (_config.AcceptInvalidCertificates)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await ConnectWithAutoDetectAsync(client);
            await client.AuthenticateAsync(_config.Email, _config.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            try
            {
                using var client = new SmtpClient();

                if (_config.AcceptInvalidCertificates)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await ConnectWithAutoDetectAsync(client);
                await client.AuthenticateAsync(_config.Email, _config.Password);
                await client.DisconnectAsync(true);

                return (true, "Verbindung erfolgreich.");
            }
            catch (Exception ex)
            {
                return (false, $"Fehler: {ex.Message}");
            }
        }

        /// <summary>
        /// Verbindet zum SMTP-Server mit automatischer Erkennung der richtigen
        /// Verschlüsselungsmethode basierend auf Port und UseSsl-Flag.
        /// Bei Fehlschlag wird die alternative Methode versucht.
        /// </summary>
        private async Task ConnectWithAutoDetectAsync(SmtpClient client)
        {
            var primaryOption = GetSocketOptions(_config.SmtpPort, _config.UseSsl);

            try
            {
                await client.ConnectAsync(_config.SmtpServer, _config.SmtpPort, primaryOption);
            }
            catch
            {
                // Fallback: alternative Verschlüsselungsmethode versuchen
                var fallbackOption = primaryOption == SecureSocketOptions.SslOnConnect
                    ? SecureSocketOptions.StartTlsWhenAvailable
                    : SecureSocketOptions.SslOnConnect;

                await client.ConnectAsync(_config.SmtpServer, _config.SmtpPort, fallbackOption);
            }
        }

        private static SecureSocketOptions GetSocketOptions(int port, bool useSsl) => port switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            25 => SecureSocketOptions.StartTlsWhenAvailable,
            587 => SecureSocketOptions.StartTlsWhenAvailable,
            _ => useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable
        };
    }
}
