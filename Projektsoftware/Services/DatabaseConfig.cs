using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Projektsoftware.Services
{
    public class DatabaseConfig
    {
        public string Server { get; set; } = "localhost";
        public string Port { get; set; } = "3306";
        public string Database { get; set; } = "projektsoftware";
        public string User { get; set; } = "root";
        public string Password { get; set; } = "";
        public string SslMode { get; set; } = "Preferred";

        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware",
            "database.config.json"
        );

        public static DatabaseConfig Load()
        {
            Debug.WriteLine($"[DatabaseConfig] Lade Konfiguration von: {ConfigFilePath}");

            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    Debug.WriteLine("[DatabaseConfig] Datei existiert, lese Inhalt...");
                    string json = File.ReadAllText(ConfigFilePath);
                    Debug.WriteLine($"[DatabaseConfig] JSON-Inhalt: {json}");

                    var config = JsonSerializer.Deserialize<DatabaseConfig>(json);

                    if (config != null)
                    {
                        Debug.WriteLine($"[DatabaseConfig] Konfiguration geladen: Server={config.Server}, Database={config.Database}, User={config.User}");
                        Debug.WriteLine($"[DatabaseConfig] IsConfigured() = {config.IsConfigured()}");
                        return config;
                    }
                    else
                    {
                        Debug.WriteLine("[DatabaseConfig] Deserialisierung ergab null");
                    }
                }
                else
                {
                    Debug.WriteLine("[DatabaseConfig] Datei existiert nicht");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseConfig] Fehler beim Laden: {ex.Message}");
                Debug.WriteLine($"[DatabaseConfig] Stack Trace: {ex.StackTrace}");
            }

            Debug.WriteLine("[DatabaseConfig] Gebe neue leere Konfiguration zurück");
            return new DatabaseConfig();
        }

        public void Save()
        {
            Debug.WriteLine($"[DatabaseConfig] Speichere Konfiguration nach: {ConfigFilePath}");
            Debug.WriteLine($"[DatabaseConfig] Server={Server}, Database={Database}, User={User}, Password={(string.IsNullOrEmpty(Password) ? "(leer)" : "***")}");

            try
            {
                string directory = Path.GetDirectoryName(ConfigFilePath);
                if (!Directory.Exists(directory))
                {
                    Debug.WriteLine($"[DatabaseConfig] Erstelle Verzeichnis: {directory}");
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                Debug.WriteLine($"[DatabaseConfig] JSON-Ausgabe: {json}");

                File.WriteAllText(ConfigFilePath, json);
                Debug.WriteLine("[DatabaseConfig] Datei erfolgreich gespeichert");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseConfig] Fehler beim Speichern: {ex.Message}");
                throw new Exception($"Fehler beim Speichern der Konfiguration: {ex.Message}", ex);
            }
        }

        public static string GetConfigFilePath()
        {
            return ConfigFilePath;
        }

        public bool IsConfigured()
        {
            // Prüfe ob mindestens Server und Database gesetzt sind
            bool hasBasicConfig = !string.IsNullOrWhiteSpace(Server) && 
                                  !string.IsNullOrWhiteSpace(Database) &&
                                  !string.IsNullOrWhiteSpace(User);

            Debug.WriteLine($"[DatabaseConfig.IsConfigured] hasBasicConfig={hasBasicConfig} (Server={Server}, Database={Database}, User={User})");

            // Für localhost/XAMPP ist leeres Passwort OK
            bool isLocalhost = Server == "localhost" || Server == "127.0.0.1";
            Debug.WriteLine($"[DatabaseConfig.IsConfigured] isLocalhost={isLocalhost}");

            if (isLocalhost && hasBasicConfig)
            {
                Debug.WriteLine("[DatabaseConfig.IsConfigured] Localhost mit Basic-Config -> TRUE");
                return true; // Bei localhost ist leeres Passwort erlaubt
            }

            // Für Remote-Server muss ein Passwort gesetzt sein
            // Wir prüfen nur noch, ob überhaupt ein Passwort vorhanden ist
            bool hasValidPassword = !string.IsNullOrWhiteSpace(Password);

            Debug.WriteLine($"[DatabaseConfig.IsConfigured] hasValidPassword={hasValidPassword}");
            bool result = hasBasicConfig && hasValidPassword;
            Debug.WriteLine($"[DatabaseConfig.IsConfigured] Final Result={result}");

            return result;
        }

        public string GetConnectionString()
        {
            return $"Server={Server};Port={Port};Database={Database};Uid={User};Pwd={Password};SslMode={SslMode};CharSet=utf8mb4;AllowPublicKeyRetrieval=True;";
        }
    }
}
