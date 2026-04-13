using System;
using System.IO;
using System.Text.Json;

namespace Projektsoftware.Services
{
    public class EasybillConfig
    {
        public string Email { get; set; }
        public string ApiKey { get; set; }
        public string ApiUrl { get; set; } = "https://api.easybill.de/rest/v1";
        public bool IsConfigured => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(ApiKey);

        private static readonly string configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware",
            "easybill-config.json"
        );

        public static EasybillConfig Load()
        {
            EasybillConfig config;
            try
            {
                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    config = JsonSerializer.Deserialize<EasybillConfig>(json) ?? new EasybillConfig();
                }
                else
                {
                    config = new EasybillConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Easybill-Konfiguration: {ex.Message}");
                config = new EasybillConfig();
            }

            // Benutzerspezifische Werte aus Cache überschreiben (falls vorhanden und nicht leer)
            var userEmail = UserCredentialService.Get(UserCredentialService.EasybillEmail);
            if (!string.IsNullOrEmpty(userEmail)) config.Email = userEmail;

            var userApiKey = UserCredentialService.Get(UserCredentialService.EasybillApiKey);
            if (!string.IsNullOrEmpty(userApiKey)) config.ApiKey = userApiKey;

            return config;
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(configFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Speichern der Easybill-Konfiguration: {ex.Message}", ex);
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    File.Delete(configFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Löschen der Easybill-Konfiguration: {ex.Message}");
            }
        }
    }
}
