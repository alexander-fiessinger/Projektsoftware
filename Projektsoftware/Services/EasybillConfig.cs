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
            try
            {
                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    return JsonSerializer.Deserialize<EasybillConfig>(json) ?? new EasybillConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Easybill-Konfiguration: {ex.Message}");
            }

            return new EasybillConfig();
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
