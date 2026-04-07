using System;
using System.IO;
using System.Text.Json;

namespace Projektsoftware.Services
{
    public class ExchangeConfig
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool UseSsl { get; set; } = false;
        public string SenderName { get; set; } = string.Empty;
        public bool AcceptInvalidCertificates { get; set; } = false;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Email) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(SmtpServer);

        private static readonly string configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware",
            "exchange-config.json"
        );

        public static ExchangeConfig Load()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    return JsonSerializer.Deserialize<ExchangeConfig>(json) ?? new ExchangeConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Exchange-Konfiguration: {ex.Message}");
            }
            return new ExchangeConfig();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(configFilePath);
                if (directory != null && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Speichern der Exchange-Konfiguration: {ex.Message}", ex);
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(configFilePath))
                    File.Delete(configFilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Loeschen der Exchange-Konfiguration: {ex.Message}");
            }
        }
    }
}
