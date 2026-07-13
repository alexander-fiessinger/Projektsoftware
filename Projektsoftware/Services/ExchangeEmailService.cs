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
            byte[]? pdfBytes = null,
            string? attachmentFileName = null,
            byte[]? attachmentBytes = null,
            string attachmentMimeType = "application/octet-stream")
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

            if (attachmentBytes != null && attachmentFileName != null)
            {
                var parts = attachmentMimeType.Split('/');
                var ct = parts.Length == 2 ? new ContentType(parts[0], parts[1]) : new ContentType("application", "octet-stream");
                bodyBuilder.Attachments.Add(attachmentFileName, attachmentBytes, ct);
            }

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

        /// <summary>
        /// Sendet eine Kalendereinladung (ICS) per E-Mail.
        /// In Outlook erscheinen damit die Annehmen / Ablehnen / Tentativ Buttons.
        /// </summary>
        public async Task SendCalendarInviteAsync(
            string to,
            string subject,
            string location,
            string description,
            DateTime startUtc,
            DateTime endUtc,
            string uid)
        {
            var ics = BuildIcs(subject, location, description, startUtc, endUtc, uid,
                               _config.Email!, _config.SenderName ?? _config.Email!, to);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config.SenderName ?? _config.Email, _config.Email));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var icsBytes = System.Text.Encoding.UTF8.GetBytes(ics);

            // HTML-Body
            var htmlPart = new TextPart("html")
            {
                Text = $"<p>Sie wurden zu folgendem Meeting eingeladen:</p><p><b>{subject}</b><br/>{startUtc.ToLocalTime():dd.MM.yyyy HH:mm} – {endUtc.ToLocalTime():HH:mm}</p><p>{description}</p>"
            };

            // ICS-Part: Content-Type text/calendar; method=REQUEST (Outlook erkennt RSVP-Buttons)
            var calendarPart = new TextPart("calendar")
            {
                Text = ics,
                ContentTransferEncoding = ContentEncoding.Base64
            };
            calendarPart.ContentType.Parameters.Add("method", "REQUEST");
            calendarPart.ContentType.Parameters.Add("charset", "utf-8");

            // ICS auch als Attachment damit es in jedem Client funktioniert
            var attachment = new MimePart("application", "ics")
            {
                Content = new MimeContent(new System.IO.MemoryStream(icsBytes)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = "invite.ics"
            };

            // Struktur: multipart/mixed > multipart/alternative (html + calendar) + attachment
            var alternative = new MultipartAlternative();
            alternative.Add(htmlPart);
            alternative.Add(calendarPart);

            var mixed = new Multipart("mixed");
            mixed.Add(alternative);
            mixed.Add(attachment);

            message.Body = mixed;

            using var client = new SmtpClient();
            if (_config.AcceptInvalidCertificates)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            await ConnectWithAutoDetectAsync(client);
            await client.AuthenticateAsync(_config.Email, _config.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private static string BuildIcs(string summary, string location, string description,
            DateTime startUtc, DateTime endUtc, string uid, string organizerEmail, string organizerName,
            string attendeeEmail)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//Projektsoftware//Meeting//DE");
            sb.AppendLine("METHOD:REQUEST");
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{uid}");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTSTART:{startUtc:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTEND:{endUtc:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"SUMMARY:{summary}");
            sb.AppendLine($"LOCATION:{location}");
            sb.AppendLine($"DESCRIPTION:{description.Replace("\n", "\\n")}");
            sb.AppendLine($"ORGANIZER;CN=\"{organizerName}\":mailto:{organizerEmail}");
            // ATTENDEE mit RSVP=TRUE ist Pflicht damit Outlook die Annehmen/Ablehnen Buttons zeigt
            sb.AppendLine($"ATTENDEE;CUTYPE=INDIVIDUAL;ROLE=REQ-PARTICIPANT;RSVP=TRUE;CN=\"{attendeeEmail}\":mailto:{attendeeEmail}");
            sb.AppendLine("STATUS:CONFIRMED");
            sb.AppendLine("SEQUENCE:0");
            sb.AppendLine("BEGIN:VALARM");
            sb.AppendLine("TRIGGER:-PT15M");
            sb.AppendLine("ACTION:DISPLAY");
            sb.AppendLine("DESCRIPTION:Erinnerung");
            sb.AppendLine("END:VALARM");
            sb.AppendLine("END:VEVENT");
            sb.AppendLine("END:VCALENDAR");
            return sb.ToString();
        }
    }
}
