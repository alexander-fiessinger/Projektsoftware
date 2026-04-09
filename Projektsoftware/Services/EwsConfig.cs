using System;
using System.IO;
using System.Text.Json;

namespace Projektsoftware.Services
{
    public class EwsConfig
    {
        public string EwsUrl { get; set; } = string.Empty;
        public string EwsEmail { get; set; } = string.Empty;
        public string EwsPassword { get; set; } = string.Empty;
        public bool UseWindowsAuth { get; set; } = false;
        public bool EnableEwsFetch { get; set; } = false;
        public string EwsUsername { get; set; } = string.Empty;
        public string EwsDomain { get; set; } = string.Empty;
        public bool AcceptInvalidCertificates { get; set; } = false;
        public bool UseBasicAuth { get; set; } = false;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(EwsUrl) &&
            (UseWindowsAuth || (!string.IsNullOrWhiteSpace(EwsEmail) && !string.IsNullOrWhiteSpace(EwsPassword)));

        private static readonly string configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware",
            "ews-config.json"
        );

        public static EwsConfig Load()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    return JsonSerializer.Deserialize<EwsConfig>(json) ?? new EwsConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der EWS-Konfiguration: {ex.Message}");
            }
            return new EwsConfig();
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
                throw new Exception($"Fehler beim Speichern der EWS-Konfiguration: {ex.Message}", ex);
            }
        }
    }
}
