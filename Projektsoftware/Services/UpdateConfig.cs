using System;
using System.IO;
using System.Text.Json;

namespace Projektsoftware.Services
{
    public class UpdateConfig
    {
        public string ManifestUrl { get; set; } = "";
        public bool AutoCheckOnStartup { get; set; } = true;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ManifestUrl);

        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware",
            "update-config.json"
        );

        public static UpdateConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    return JsonSerializer.Deserialize<UpdateConfig>(json) ?? new UpdateConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Update-Konfiguration: {ex.Message}");
            }
            return new UpdateConfig();
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
                throw new Exception($"Fehler beim Speichern der Update-Konfiguration: {ex.Message}", ex);
            }
        }
    }
}
