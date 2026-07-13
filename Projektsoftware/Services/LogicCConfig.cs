using System;
using System.IO;
using System.Text.Json;

namespace Projektsoftware.Services
{
    public class LogicCConfig
    {
        public string ApiKey { get; set; }
        public string ApiUrl { get; set; } = "https://api.logicc.io/v1";
        public string Model { get; set; } = "gpt-4o";
        public int MaxTokens { get; set; } = 2000;
        public double Temperature { get; set; } = 0.7;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

        private static readonly string configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware",
            "logicc-config.json"
        );

        public static LogicCConfig Load()
        {
            LogicCConfig config;
            try
            {
                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    config = JsonSerializer.Deserialize<LogicCConfig>(json) ?? new LogicCConfig();
                }
                else
                {
                    config = new LogicCConfig();
                }
            }
            catch
            {
                config = new LogicCConfig();
            }

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
                throw new Exception($"Fehler beim Speichern der LogicC-Konfiguration: {ex.Message}");
            }
        }

        public void Clear()
        {
            ApiKey = string.Empty;
            ApiUrl = "https://api.logicc.io/v1";
            Model = "gpt-4o";
            MaxTokens = 2000;
            Temperature = 0.7;
            Save();
        }
    }
}
