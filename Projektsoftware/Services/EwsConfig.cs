using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

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

        private const string SalesDbKey = "sales_ews_config";

        private static readonly string configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware",
            "ews-config.json"
        );

        public static EwsConfig Load() => LoadFromFile(configFilePath);

        // Cache: wird einmalig beim ersten LoadSales() befüllt
        private static EwsConfig? _salesCache;

        /// <summary>
        /// Invalidiert den Sales-Cache (z.B. nach SaveSalesAsync).
        /// </summary>
        public static void InvalidateSalesCache() => _salesCache = null;

        /// <summary>
        /// Lädt die Sales-EWS-Konfiguration. Ergebnis wird gecacht, damit
        /// kein synchroner DB-Aufruf auf dem UI-Thread nötig ist.
        /// </summary>
        public static EwsConfig LoadSales()
        {
            if (_salesCache != null) return _salesCache;

            try
            {
                var db   = new DatabaseService();
                var json = Task.Run(() => db.GetAppSettingAsync(SalesDbKey)).GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var sales = JsonSerializer.Deserialize<EwsConfig>(json) ?? new EwsConfig();
                    sales.EwsEmail = "sales@af-software-engineering.de";
                    _salesCache = sales;
                    return _salesCache;
                }

                // Nichts in DB → lokale Datei als Migrationsquelle versuchen
                var migrated = TryLoadAndMigrateLocalFile(db);
                if (migrated != null)
                {
                    _salesCache = migrated;
                    return _salesCache;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sales-EWS aus DB laden fehlgeschlagen: {ex.Message}");

                // DB nicht erreichbar → lokale Datei als Fallback
                var local = TryLoadLocalSalesFile();
                if (local != null) return local;
            }
            return new EwsConfig { EwsEmail = "sales@af-software-engineering.de" };
        }

        /// <summary>
        /// Lädt die Sales-EWS-Konfiguration asynchron aus der Datenbank.
        /// </summary>
        public static async Task<EwsConfig> LoadSalesAsync()
        {
            try
            {
                var db   = new DatabaseService();
                var json = await db.GetAppSettingAsync(SalesDbKey);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var sales = JsonSerializer.Deserialize<EwsConfig>(json) ?? new EwsConfig();
                    sales.EwsEmail = "sales@af-software-engineering.de";
                    return sales;
                }

                // Nichts in DB → lokale Datei als Migrationsquelle versuchen
                var migrated = TryLoadAndMigrateLocalFile(db);
                if (migrated != null) return migrated;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sales-EWS aus DB laden fehlgeschlagen: {ex.Message}");

                var local = TryLoadLocalSalesFile();
                if (local != null) return local;
            }
            return new EwsConfig { EwsEmail = "sales@af-software-engineering.de" };
        }

        private static EwsConfig? TryLoadAndMigrateLocalFile(DatabaseService db)
        {
            // Alte Pfade prüfen (AppData und ProgramData)
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),       "Projektsoftware", "sales-ews-config.json"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Projektsoftware", "sales-ews-config.json"),
            };

            foreach (var path in candidates)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var fileJson = File.ReadAllText(path);
                    var cfg = JsonSerializer.Deserialize<EwsConfig>(fileJson);
                    if (cfg == null || !cfg.IsConfigured) continue;

                    cfg.EwsEmail = "sales@af-software-engineering.de";

                    // Einmalig in DB migrieren
                    var migrateJson = JsonSerializer.Serialize(cfg);
                    Task.Run(() => db.SetAppSettingAsync(SalesDbKey, migrateJson)).GetAwaiter().GetResult();
                    System.Diagnostics.Debug.WriteLine($"✅ Sales-EWS-Config aus lokaler Datei in DB migriert: {path}");

                    return cfg;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Migration Sales-EWS fehlgeschlagen ({path}): {ex.Message}");
                }
            }
            return null;
        }

        private static EwsConfig? TryLoadLocalSalesFile()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),       "Projektsoftware", "sales-ews-config.json"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Projektsoftware", "sales-ews-config.json"),
            };
            foreach (var path in candidates)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var cfg = JsonSerializer.Deserialize<EwsConfig>(File.ReadAllText(path));
                    if (cfg?.IsConfigured == true)
                    {
                        cfg.EwsEmail = "sales@af-software-engineering.de";
                        return cfg;
                    }
                }
                catch { }
            }
            return null;
        }

        /// <summary>
        /// Speichert die Sales-EWS-Konfiguration in der Datenbank (für alle Rechner sichtbar).
        /// </summary>
        public async Task SaveSalesAsync()
        {
            EwsEmail = "sales@af-software-engineering.de";
            var db   = new DatabaseService();
            var json = JsonSerializer.Serialize(this);
            await db.SetAppSettingAsync(SalesDbKey, json);
            // Cache invalidieren damit alle nachfolgenden LoadSales()-Aufrufe die neue Config holen
            InvalidateSalesCache();
        }

        private static EwsConfig LoadFromFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<EwsConfig>(json) ?? new EwsConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der EWS-Konfiguration: {ex.Message}");
            }
            return new EwsConfig();
        }

        public void Save() => SaveToFile(configFilePath);

        private void SaveToFile(string path)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (directory != null && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Speichern der EWS-Konfiguration: {ex.Message}", ex);
            }
        }
    }
}
