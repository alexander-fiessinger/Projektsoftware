using Projektsoftware.Models;
using System;
using System.Collections.Generic;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Steuerrechtliche Szenarien gemäß deutschem UStG und EU-Recht
    /// </summary>
    public enum VatScenario
    {
        /// <summary>Inland (DE → DE): Standard 19% oder ermäßigt 7%</summary>
        Inland,

        /// <summary>Innergemeinschaftliche Lieferung B2B (DE → EU mit gültiger USt-IdNr.): 0% Reverse Charge</summary>
        InnergemeinschaftlichB2B,

        /// <summary>Innergemeinschaftliche Lieferung B2C (DE → EU ohne USt-IdNr.): Bestimmungslandprinzip</summary>
        InnergemeinschaftlichB2C,

        /// <summary>Drittland-Export (DE → Nicht-EU): 0% steuerfrei</summary>
        DrittlandExport,

        /// <summary>Kleinunternehmerregelung §19 UStG: 0%</summary>
        Kleinunternehmer
    }

    /// <summary>
    /// Ergebnis der Steuerermittlung
    /// </summary>
    public class VatResult
    {
        /// <summary>Ermitteltes Szenario</summary>
        public VatScenario Scenario { get; set; }

        /// <summary>Standard-MwSt-Satz in Prozent (0, 7 oder 19)</summary>
        public int VatPercent { get; set; }

        /// <summary>Kurzbeschreibung für die UI</summary>
        public string DisplayText { get; set; } = "";

        /// <summary>Ausführliche Beschreibung mit Rechtsgrundlage</summary>
        public string LegalNotice { get; set; } = "";

        /// <summary>Text für den Dokument-Schlusstext (text_suffix) in Easybill</summary>
        public string DocumentSuffix { get; set; } = "";

        /// <summary>Farbe für die UI-Anzeige (Hex)</summary>
        public string InfoColor { get; set; } = "#2196F3";

        /// <summary>Ob MwSt auf 0% gesetzt werden muss (Reverse Charge / Export)</summary>
        public bool IsTaxFree => VatPercent == 0;

        /// <summary>Land des Kunden (ISO 2-Letter)</summary>
        public string? CustomerCountry { get; set; }

        /// <summary>USt-IdNr. des Kunden</summary>
        public string? CustomerVatId { get; set; }
    }

    /// <summary>
    /// Service zur Ermittlung der korrekten Umsatzsteuer gemäß deutschem Steuerrecht
    /// und EU-Regelungen (Innergemeinschaftliche Lieferung, Reverse Charge, Drittland-Export)
    /// </summary>
    public static class VatService
    {
        /// <summary>
        /// EU-Mitgliedstaaten (ISO 3166-1 Alpha-2) - Stand 2024
        /// </summary>
        private static readonly HashSet<string> EuCountries = new(StringComparer.OrdinalIgnoreCase)
        {
            "AT", // Österreich
            "BE", // Belgien
            "BG", // Bulgarien
            "HR", // Kroatien
            "CY", // Zypern
            "CZ", // Tschechien
            "DK", // Dänemark
            "EE", // Estland
            "FI", // Finnland
            "FR", // Frankreich
            "DE", // Deutschland
            "GR", // Griechenland
            "HU", // Ungarn
            "IE", // Irland
            "IT", // Italien
            "LV", // Lettland
            "LT", // Litauen
            "LU", // Luxemburg
            "MT", // Malta
            "NL", // Niederlande
            "PL", // Polen
            "PT", // Portugal
            "RO", // Rumänien
            "SK", // Slowakei
            "SI", // Slowenien
            "ES", // Spanien
            "SE"  // Schweden
        };

        /// <summary>
        /// Ländernamen für Anzeige (Deutsch)
        /// </summary>
        private static readonly Dictionary<string, string> CountryNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AT"] = "Österreich", ["BE"] = "Belgien", ["BG"] = "Bulgarien",
            ["HR"] = "Kroatien", ["CY"] = "Zypern", ["CZ"] = "Tschechien",
            ["DK"] = "Dänemark", ["EE"] = "Estland", ["FI"] = "Finnland",
            ["FR"] = "Frankreich", ["DE"] = "Deutschland", ["GR"] = "Griechenland",
            ["HU"] = "Ungarn", ["IE"] = "Irland", ["IT"] = "Italien",
            ["LV"] = "Lettland", ["LT"] = "Litauen", ["LU"] = "Luxemburg",
            ["MT"] = "Malta", ["NL"] = "Niederlande", ["PL"] = "Polen",
            ["PT"] = "Portugal", ["RO"] = "Rumänien", ["SK"] = "Slowakei",
            ["SI"] = "Slowenien", ["ES"] = "Spanien", ["SE"] = "Schweden",
            ["CH"] = "Schweiz", ["GB"] = "Großbritannien", ["US"] = "USA",
            ["NO"] = "Norwegen", ["IS"] = "Island", ["LI"] = "Liechtenstein",
            ["TR"] = "Türkei", ["CN"] = "China", ["JP"] = "Japan",
            ["AU"] = "Australien", ["CA"] = "Kanada", ["BR"] = "Brasilien"
        };

        /// <summary>
        /// Ermittelt das korrekte Steuer-Szenario basierend auf Kundenland und USt-IdNr.
        /// </summary>
        public static VatResult DetermineVat(EasybillCustomer? customer)
        {
            if (customer == null)
            {
                return CreateInlandResult();
            }

            return DetermineVat(customer.Country, customer.VatId);
        }

        /// <summary>
        /// Ermittelt das korrekte Steuer-Szenario basierend auf Land und USt-IdNr.
        /// </summary>
        public static VatResult DetermineVat(string? country, string? vatId)
        {
            var countryCode = NormalizeCountryCode(country);
            var hasVatId = !string.IsNullOrWhiteSpace(vatId);

            // 1. Kein Land angegeben → Inland (Standard)
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return CreateInlandResult();
            }

            // 2. Deutschland → Inland
            if (countryCode.Equals("DE", StringComparison.OrdinalIgnoreCase))
            {
                return CreateInlandResult();
            }

            // 3. EU-Mitgliedstaat
            if (IsEuCountry(countryCode))
            {
                if (hasVatId)
                {
                    // EU B2B: Innergemeinschaftliche Lieferung / Reverse Charge
                    return CreateEuB2BResult(countryCode, vatId!);
                }
                else
                {
                    // EU B2C: Bestimmungslandprinzip (vereinfacht: deutsche MwSt)
                    return CreateEuB2CResult(countryCode);
                }
            }

            // 4. Drittland (nicht EU)
            return CreateDrittlandResult(countryCode);
        }

        /// <summary>
        /// Prüft ob ein Land ein EU-Mitgliedstaat ist
        /// </summary>
        public static bool IsEuCountry(string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode)) return false;
            return EuCountries.Contains(countryCode.Trim());
        }

        /// <summary>
        /// Gibt den deutschen Ländernamen zurück
        /// </summary>
        public static string GetCountryName(string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode)) return "Unbekannt";
            return CountryNames.TryGetValue(countryCode.Trim(), out var name) ? name : countryCode.Trim();
        }

        /// <summary>
        /// Normalisiert den Ländercode (z.B. "Deutschland" → "DE", "germany" → "DE")
        /// </summary>
        private static string? NormalizeCountryCode(string? country)
        {
            if (string.IsNullOrWhiteSpace(country))
                return null;

            var trimmed = country.Trim();

            // Bereits ein 2-Letter-Code
            if (trimmed.Length == 2)
                return trimmed.ToUpperInvariant();

            // Bekannte Ländernamen auf Code mappen
            var nameMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Deutschland"] = "DE", ["Germany"] = "DE",
                ["Österreich"] = "AT", ["Austria"] = "AT",
                ["Schweiz"] = "CH", ["Switzerland"] = "CH",
                ["Frankreich"] = "FR", ["France"] = "FR",
                ["Italien"] = "IT", ["Italy"] = "IT",
                ["Spanien"] = "ES", ["Spain"] = "ES",
                ["Niederlande"] = "NL", ["Netherlands"] = "NL", ["Holland"] = "NL",
                ["Belgien"] = "BE", ["Belgium"] = "BE",
                ["Luxemburg"] = "LU", ["Luxembourg"] = "LU",
                ["Polen"] = "PL", ["Poland"] = "PL",
                ["Tschechien"] = "CZ", ["Czech Republic"] = "CZ", ["Czechia"] = "CZ",
                ["Ungarn"] = "HU", ["Hungary"] = "HU",
                ["Dänemark"] = "DK", ["Denmark"] = "DK",
                ["Schweden"] = "SE", ["Sweden"] = "SE",
                ["Finnland"] = "FI", ["Finland"] = "FI",
                ["Portugal"] = "PT",
                ["Griechenland"] = "GR", ["Greece"] = "GR",
                ["Irland"] = "IE", ["Ireland"] = "IE",
                ["Rumänien"] = "RO", ["Romania"] = "RO",
                ["Bulgarien"] = "BG", ["Bulgaria"] = "BG",
                ["Kroatien"] = "HR", ["Croatia"] = "HR",
                ["Slowakei"] = "SK", ["Slovakia"] = "SK",
                ["Slowenien"] = "SI", ["Slovenia"] = "SI",
                ["Estland"] = "EE", ["Estonia"] = "EE",
                ["Lettland"] = "LV", ["Latvia"] = "LV",
                ["Litauen"] = "LT", ["Lithuania"] = "LT",
                ["Malta"] = "MT",
                ["Zypern"] = "CY", ["Cyprus"] = "CY",
                ["Großbritannien"] = "GB", ["United Kingdom"] = "GB", ["UK"] = "GB", ["England"] = "GB",
                ["Norwegen"] = "NO", ["Norway"] = "NO",
                ["Island"] = "IS", ["Iceland"] = "IS",
                ["Liechtenstein"] = "LI",
                ["Türkei"] = "TR", ["Turkey"] = "TR", ["Türkiye"] = "TR",
                ["USA"] = "US", ["United States"] = "US", ["Vereinigte Staaten"] = "US",
                ["Kanada"] = "CA", ["Canada"] = "CA",
                ["China"] = "CN",
                ["Japan"] = "JP",
                ["Australien"] = "AU", ["Australia"] = "AU",
                ["Brasilien"] = "BR", ["Brazil"] = "BR"
            };

            return nameMappings.TryGetValue(trimmed, out var code) ? code : trimmed.ToUpperInvariant();
        }

        #region Result Factory Methods

        private static VatResult CreateInlandResult()
        {
            return new VatResult
            {
                Scenario = VatScenario.Inland,
                VatPercent = 19,
                CustomerCountry = "DE",
                DisplayText = "Inland (Deutschland) - 19% MwSt",
                LegalNotice = "Standardbesteuerung gemäß §1 Abs. 1 Nr. 1 UStG",
                DocumentSuffix = "",
                InfoColor = "#4CAF50"
            };
        }

        private static VatResult CreateEuB2BResult(string countryCode, string vatId)
        {
            var countryName = GetCountryName(countryCode);
            return new VatResult
            {
                Scenario = VatScenario.InnergemeinschaftlichB2B,
                VatPercent = 0,
                CustomerCountry = countryCode,
                CustomerVatId = vatId,
                DisplayText = $"Innergemeinschaftliche Lieferung ({countryName}) - Reverse Charge 0%",
                LegalNotice = $"Steuerfreie innergemeinschaftliche Lieferung gemäß §4 Nr. 1b i.V.m. §6a UStG.\n" +
                              $"USt-IdNr. Kunde: {vatId}\n" +
                              $"Die Steuerschuldnerschaft geht auf den Leistungsempfänger über (Reverse Charge).",
                DocumentSuffix = $"Steuerschuldnerschaft des Leistungsempfängers (Reverse Charge).\n" +
                                 $"Steuerfreie innergemeinschaftliche Lieferung gemäß §4 Nr. 1b i.V.m. §6a UStG.\n" +
                                 $"USt-IdNr. des Leistungsempfängers: {vatId}",
                InfoColor = "#FF9800"
            };
        }

        private static VatResult CreateEuB2CResult(string countryCode)
        {
            var countryName = GetCountryName(countryCode);
            return new VatResult
            {
                Scenario = VatScenario.InnergemeinschaftlichB2C,
                VatPercent = 19,
                CustomerCountry = countryCode,
                DisplayText = $"EU-Kunde B2C ({countryName}) - 19% MwSt (ohne USt-IdNr.)",
                LegalNotice = $"Leistung an Privatkunden in {countryName} (EU).\n" +
                              $"Keine USt-IdNr. vorhanden - deutsche Umsatzsteuer wird berechnet.\n" +
                              $"Hinweis: Ab bestimmten Schwellenwerten gilt das Bestimmungslandprinzip (OSS-Verfahren).",
                DocumentSuffix = "",
                InfoColor = "#2196F3"
            };
        }

        private static VatResult CreateDrittlandResult(string countryCode)
        {
            var countryName = GetCountryName(countryCode);
            return new VatResult
            {
                Scenario = VatScenario.DrittlandExport,
                VatPercent = 0,
                CustomerCountry = countryCode,
                DisplayText = $"Drittland-Export ({countryName}) - 0% steuerfrei",
                LegalNotice = $"Steuerfreie Ausfuhrlieferung in Drittland ({countryName}) gemäß §4 Nr. 1a i.V.m. §6 UStG.\n" +
                              $"Ausfuhrnachweis erforderlich.",
                DocumentSuffix = $"Steuerfreie Ausfuhrlieferung gemäß §4 Nr. 1a i.V.m. §6 UStG.",
                InfoColor = "#9C27B0"
            };
        }

        /// <summary>
        /// Erstellt ein Kleinunternehmer-Ergebnis (für spätere Verwendung)
        /// </summary>
        public static VatResult CreateKleinunternehmerResult()
        {
            return new VatResult
            {
                Scenario = VatScenario.Kleinunternehmer,
                VatPercent = 0,
                DisplayText = "Kleinunternehmerregelung - 0% MwSt",
                LegalNotice = "Gemäß §19 UStG wird keine Umsatzsteuer berechnet.",
                DocumentSuffix = "Gemäß §19 UStG wird keine Umsatzsteuer berechnet.",
                InfoColor = "#607D8B"
            };
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validiert eine USt-IdNr. (Formatprüfung)
        /// </summary>
        public static bool IsValidVatIdFormat(string? vatId)
        {
            if (string.IsNullOrWhiteSpace(vatId))
                return false;

            var cleaned = vatId.Replace(" ", "").Replace("-", "").Replace(".", "").Trim();

            // Mindestens 2 Buchstaben (Länderkennung) + Ziffern
            if (cleaned.Length < 4)
                return false;

            // Erste 2 Zeichen müssen Buchstaben sein (Ländercode)
            if (!char.IsLetter(cleaned[0]) || !char.IsLetter(cleaned[1]))
                return false;

            return true;
        }

        /// <summary>
        /// Extrahiert den Ländercode aus einer USt-IdNr.
        /// </summary>
        public static string? GetCountryFromVatId(string? vatId)
        {
            if (string.IsNullOrWhiteSpace(vatId) || vatId.Length < 2)
                return null;

            var prefix = vatId[..2].ToUpperInvariant();

            // Griechenland hat Sonderfall: EL statt GR
            if (prefix == "EL") return "GR";

            return prefix;
        }

        #endregion
    }
}
