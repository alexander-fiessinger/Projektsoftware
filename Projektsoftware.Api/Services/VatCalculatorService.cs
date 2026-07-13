using Projektsoftware.Api.Models;

namespace Projektsoftware.Api.Services;

/// <summary>
/// Ermittlung der korrekten Umsatzsteuer gemäß deutschem UStG und EU-Recht
/// (Inland, innergemeinschaftliche Lieferung B2B/B2C, Drittland-Export, Kleinunternehmer).
/// Portiert aus dem WPF-VatService.
/// </summary>
public class VatCalculatorService
{
    private static readonly HashSet<string> EuCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "AT","BE","BG","HR","CY","CZ","DK","EE","FI","FR","DE","GR","HU","IE","IT",
        "LV","LT","LU","MT","NL","PL","PT","RO","SK","SI","ES","SE"
    };

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

    public VatResultDto DetermineVat(string? country, string? vatId)
    {
        var countryCode = NormalizeCountryCode(country);
        var hasVatId = !string.IsNullOrWhiteSpace(vatId);

        if (string.IsNullOrWhiteSpace(countryCode))
            return CreateInlandResult();

        if (countryCode.Equals("DE", StringComparison.OrdinalIgnoreCase))
            return CreateInlandResult();

        if (IsEuCountry(countryCode))
            return hasVatId ? CreateEuB2BResult(countryCode, vatId!) : CreateEuB2CResult(countryCode);

        return CreateDrittlandResult(countryCode);
    }

    public static bool IsEuCountry(string? countryCode)
        => !string.IsNullOrWhiteSpace(countryCode) && EuCountries.Contains(countryCode.Trim());

    public static string GetCountryName(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return "Unbekannt";
        return CountryNames.TryGetValue(countryCode.Trim(), out var name) ? name : countryCode.Trim();
    }

    public IEnumerable<KeyValuePair<string, string>> GetSelectableCountries()
        => CountryNames.OrderBy(kv => kv.Value);

    private static string? NormalizeCountryCode(string? country)
    {
        if (string.IsNullOrWhiteSpace(country)) return null;
        var trimmed = country.Trim();
        if (trimmed.Length == 2) return trimmed.ToUpperInvariant();

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

    private static VatResultDto CreateInlandResult() => new()
    {
        Scenario = "Inland",
        VatPercent = 19,
        CustomerCountry = "DE",
        DisplayText = "Inland (Deutschland) - 19% MwSt",
        LegalNotice = "Standardbesteuerung gemäß §1 Abs. 1 Nr. 1 UStG",
        DocumentSuffix = "",
        InfoColor = "#4CAF50"
    };

    private static VatResultDto CreateEuB2BResult(string countryCode, string vatId)
    {
        var countryName = GetCountryName(countryCode);
        return new VatResultDto
        {
            Scenario = "InnergemeinschaftlichB2B",
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

    private static VatResultDto CreateEuB2CResult(string countryCode)
    {
        var countryName = GetCountryName(countryCode);
        return new VatResultDto
        {
            Scenario = "InnergemeinschaftlichB2C",
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

    private static VatResultDto CreateDrittlandResult(string countryCode)
    {
        var countryName = GetCountryName(countryCode);
        return new VatResultDto
        {
            Scenario = "DrittlandExport",
            VatPercent = 0,
            CustomerCountry = countryCode,
            DisplayText = $"Drittland-Export ({countryName}) - 0% steuerfrei",
            LegalNotice = $"Steuerfreie Ausfuhrlieferung in Drittland ({countryName}) gemäß §4 Nr. 1a i.V.m. §6 UStG.\n" +
                          $"Ausfuhrnachweis erforderlich.",
            DocumentSuffix = $"Steuerfreie Ausfuhrlieferung gemäß §4 Nr. 1a i.V.m. §6 UStG.",
            InfoColor = "#9C27B0"
        };
    }

    public VatResultDto CreateKleinunternehmerResult() => new()
    {
        Scenario = "Kleinunternehmer",
        VatPercent = 0,
        DisplayText = "Kleinunternehmerregelung - 0% MwSt",
        LegalNotice = "Gemäß §19 UStG wird keine Umsatzsteuer berechnet.",
        DocumentSuffix = "Gemäß §19 UStG wird keine Umsatzsteuer berechnet.",
        InfoColor = "#607D8B"
    };

    public static bool IsValidVatIdFormat(string? vatId)
    {
        if (string.IsNullOrWhiteSpace(vatId)) return false;
        var cleaned = vatId.Replace(" ", "").Replace("-", "").Replace(".", "").Trim();
        if (cleaned.Length < 4) return false;
        if (!char.IsLetter(cleaned[0]) || !char.IsLetter(cleaned[1])) return false;
        return true;
    }
}
