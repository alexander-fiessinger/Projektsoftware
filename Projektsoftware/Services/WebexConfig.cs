using System;
using System.IO;
using System.Text.Json;

namespace Projektsoftware.Services
{
    public class WebexConfig
    {
        public string AccessToken { get; set; } = "";
        public string BotName { get; set; } = "Projektierungssoftware";
        public bool SendInviteEmails { get; set; } = true;
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime TokenExpiry { get; set; } = DateTime.MinValue;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(AccessToken);
        public bool HasOAuthCredentials => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
        public bool CanRefresh => HasOAuthCredentials && !string.IsNullOrWhiteSpace(RefreshToken);
        public bool IsTokenExpired => TokenExpiry != DateTime.MinValue && DateTime.UtcNow >= TokenExpiry.AddMinutes(-5);

        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware",
            "webex-config.json"
        );

        public static WebexConfig Load()
        {
            WebexConfig config;
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    config = JsonSerializer.Deserialize<WebexConfig>(json) ?? new WebexConfig();
                }
                else
                {
                    config = new WebexConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Webex-Konfiguration: {ex.Message}");
                config = new WebexConfig();
            }

            // Benutzerspezifischer Anzeigename aus Cache überschreiben (falls vorhanden und nicht leer)
            var userBotName = UserCredentialService.Get(UserCredentialService.WebexBotName);
            if (!string.IsNullOrEmpty(userBotName)) config.BotName = userBotName;

            return config;
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigFilePath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Speichern der Webex-Konfiguration: {ex.Message}", ex);
            }
        }
    }
}
