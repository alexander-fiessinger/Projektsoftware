using System;
using System.IO;
using System.Text.Json;

namespace Projektsoftware.Services
{
    public class TicketSmtpConfig
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 465;
        public bool UseSsl { get; set; } = true;
        public string SenderName { get; set; } = string.Empty;
        public bool AcceptInvalidCertificates { get; set; } = false;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Email) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(SmtpServer);

        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware",
            "ticket-smtp-config.json"
        );

        public static TicketSmtpConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    return JsonSerializer.Deserialize<TicketSmtpConfig>(json) ?? new TicketSmtpConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Ticket-SMTP-Konfiguration: {ex.Message}");
            }
            return new TicketSmtpConfig();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigFilePath);
                if (directory != null && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Speichern der Ticket-SMTP-Konfiguration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Konvertiert diese Konfiguration in ein ExchangeConfig-Objekt für ExchangeEmailService.
        /// </summary>
        public ExchangeConfig ToExchangeConfig() => new ExchangeConfig
        {
            Email = Email,
            Password = Password,
            SmtpServer = SmtpServer,
            SmtpPort = SmtpPort,
            UseSsl = UseSsl,
            SenderName = string.IsNullOrWhiteSpace(SenderName) ? Email : SenderName,
            AcceptInvalidCertificates = AcceptInvalidCertificates
        };
    }
}
