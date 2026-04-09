using System;
using System.IO;
using System.Text.Json;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Persistente Konfiguration der eigenen Firmendaten (Auftragnehmer)
    /// für den Vertragsgenerator.
    /// </summary>
    public class ContractorConfig
    {
        public string Company { get; set; } = "Alexander Fiessinger Software Engineering";
        public string Name { get; set; } = "Alexander Fiessinger";
        public string Street { get; set; } = "D\u00f6rflaser Hauptstr. 36";
        public string ZipCity { get; set; } = "95615 Marktredwitz";
        public string Email { get; set; } = "anfrage@af-software-engineering.de";
        public string Phone { get; set; } = string.Empty;
        public string VatId { get; set; } = "DE317729383";
        public string TaxNumber { get; set; } = "25821680754";

        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware",
            "contractor.config.json"
        );

        public static ContractorConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    return JsonSerializer.Deserialize<ContractorConfig>(json) ?? new ContractorConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Auftragnehmer-Konfiguration: {ex.Message}");
            }

            return new ContractorConfig();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigFilePath)!;
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern der Auftragnehmer-Konfiguration: {ex.Message}");
            }
        }

        public bool HasData =>
            !string.IsNullOrWhiteSpace(Company) ||
            !string.IsNullOrWhiteSpace(Name);
    }
}
