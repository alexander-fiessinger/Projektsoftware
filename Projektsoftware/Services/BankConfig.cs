using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projektsoftware.Services
{
    /// <summary>
    /// BANKSapi-Zugangsdaten (BANKS/Connect) für den automatischen Kontoumsatz-Abruf.
    /// Wird analog zu <see cref="EasybillConfig"/> als JSON unter
    /// %AppData%\Projektsoftware\ gespeichert.
    /// </summary>
    public class BankConfig
    {
        // Vorbelegung mit den BANKSapi-Zugangsdaten (BANKS/Connect).
        // Die eigentliche Bankanbindung (Bank-Login + starke Kundenauthentifizierung/SCA)
        // erfolgt über die BANKSapi-WebForm im Browser (REG/Protect), nicht in dieser Anwendung.

        /// <summary>Basis-URL der BANKSapi (BANKS/Connect).</summary>
        public string ApiBaseUrl { get; set; } = "https://banksapi.io";

        /// <summary>BANKSapi-Mandant (Tenant).</summary>
        public string Tenant { get; set; } = "fiessingertest";

        /// <summary>BANKSapi-Client-ID.</summary>
        public string ClientId { get; set; } = "fiessingertestClient";

        /// <summary>BANKSapi-Client-Secret.</summary>
        public string ClientSecret { get; set; } = "jEQYCwDTNc93zSuj3q8avmuDNsUfjceYxfWjPeaqPdgJYF9h";

        /// <summary>
        /// Technischer BANKSapi-User (wird beim Einrichten automatisch angelegt).
        /// Über diesen User werden Bankzugänge und Kontoumsätze abgerufen.
        /// </summary>
        public string BanksApiUsername { get; set; } = "";

        /// <summary>Passwort des technischen BANKSapi-Users.</summary>
        public string BanksApiPassword { get; set; } = "";

        /// <summary>
        /// ID des in der WebForm eingerichteten Bankzugangs (selbst vergebene UUID).
        /// Wird nach erfolgreicher Einrichtung gespeichert.
        /// </summary>
        public string AccessId { get; set; } = "";

        /// <summary>
        /// Produkt-/Konto-ID (z. B. IBAN) innerhalb des Bankzugangs,
        /// dessen Umsätze abgeglichen werden sollen.
        /// </summary>
        public string ProductId { get; set; } = "";

        /// <summary>Kontoinhaber (optional, für Anzeige).</summary>
        public string AccountHolder { get; set; } = "";

        /// <summary>
        /// Callback-URL, zu der der Browser nach Abschluss der BANKSapi-WebForm
        /// zurückgeleitet wird. Für nicht-regulierte Mandanten (REG/Protect) ist sie
        /// zwingend erforderlich – ohne sie zeigt die WebForm "No Callback URL /
        /// Callback-URL must not be blank". Der Seiteninhalt ist unkritisch; nach dem
        /// Redirect wechselt der Nutzer zurück in die Anwendung und klickt auf
        /// "Einrichtung abgeschlossen".
        /// </summary>
        public string CallbackUrl { get; set; } = "https://banksapi.io";

        /// <summary>Basic-Auth-Benutzer für den Token-Endpunkt: "{Tenant}/{ClientId}".</summary>
        [JsonIgnore]
        public string BasicAuthUser => $"{Tenant}/{ClientId}";

        [JsonIgnore]
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ApiBaseUrl) &&
            !string.IsNullOrWhiteSpace(Tenant) &&
            !string.IsNullOrWhiteSpace(ClientId) &&
            !string.IsNullOrWhiteSpace(ClientSecret);

        /// <summary>True, wenn bereits ein Bankzugang eingerichtet wurde (AccessId vorhanden).</summary>
        [JsonIgnore]
        public bool HasBankAccess => !string.IsNullOrWhiteSpace(AccessId);

        private static readonly string configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware",
            "bank-config.json"
        );

        public static BankConfig Load()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    return JsonSerializer.Deserialize<BankConfig>(json) ?? new BankConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Bank-Konfiguration: {ex.Message}");
            }

            return new BankConfig();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(configFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Speichern der Bank-Konfiguration: {ex.Message}", ex);
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
                System.Diagnostics.Debug.WriteLine($"Fehler beim Löschen der Bank-Konfiguration: {ex.Message}");
            }
        }
    }
}
