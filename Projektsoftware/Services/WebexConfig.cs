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

        public bool IsConfigured => !string.IsNullOrWhiteSpace(AccessToken);

        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware",
            "webex-config.json"
        );

        public static WebexConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    return JsonSerializer.Deserialize<WebexConfig>(json) ?? new WebexConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Webex-Konfiguration: {ex.Message}");
            }
            return new WebexConfig();
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
