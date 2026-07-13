using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Allgemeiner Importservice, der Artikel/Produkte aus einer CSV-Datei
    /// einliest und mit Easybill synchronisiert.
    ///
    /// Erwartetes CSV-Format (erste Zeile = Header, Trennzeichen ; oder ,):
    ///   Number;Description;SalePrice;PurchasePrice;Unit;VatPercent;Type;Note
    ///
    /// Pflichtfelder: Number, Description, SalePrice
    /// Optionale Felder: PurchasePrice, Unit (Standard "Stk"), VatPercent (Standard 19),
    ///                    Type (PRODUCT|SERVICE, Standard PRODUCT), Note
    ///
    /// Existierende Artikel (gleiche Nummer) werden aktualisiert,
    /// neue Artikel werden in Easybill angelegt.
    /// </summary>
    public class CsvProductImportService
    {
        private readonly EasybillService easybillService;

        public CsvProductImportService(EasybillService easybillService)
        {
            this.easybillService = easybillService ?? throw new ArgumentNullException(nameof(easybillService));
        }

        public class ImportResult
        {
            public int Created { get; set; }
            public int Updated { get; set; }
            public int Failed { get; set; }
            public List<string> Errors { get; } = new List<string>();
            public int Total => Created + Updated + Failed;
        }

        public class CsvProductRow
        {
            public string Number { get; set; }
            public string Description { get; set; }
            public decimal SalePrice { get; set; }
            public decimal? PurchasePrice { get; set; }
            public string Unit { get; set; } = "Stk";
            public int VatPercent { get; set; } = 19;
            public string Type { get; set; } = "PRODUCT";
            public string Note { get; set; }
        }

        /// <summary>
        /// Mindestabstand zwischen zwei API-Aufrufen in Millisekunden,
        /// um das Easybill-Rate-Limit (Default ~2 Requests/Sek.) einzuhalten.
        /// </summary>
        public int RequestDelayMs { get; set; } = 700;

        /// <summary>
        /// Anzahl Wiederholungen bei HTTP 429 (Too Many Requests) bevor abgebrochen wird.
        /// </summary>
        public int MaxRetriesOnRateLimit { get; set; } = 5;

        /// <summary>
        /// Importiert Artikel aus der angegebenen CSV-Datei und synchronisiert sie mit Easybill.
        /// </summary>
        public async Task<ImportResult> ImportAsync(string csvFilePath, IProgress<string> progress = null)
        {
            if (string.IsNullOrWhiteSpace(csvFilePath))
                throw new ArgumentException("Es wurde kein CSV-Pfad angegeben.", nameof(csvFilePath));
            if (!File.Exists(csvFilePath))
                throw new FileNotFoundException("CSV-Datei wurde nicht gefunden.", csvFilePath);

            var result = new ImportResult();

            progress?.Report("Lese CSV-Datei...");
            var rows = ParseCsv(csvFilePath);
            if (rows.Count == 0)
            {
                progress?.Report("Keine Datenzeilen in der CSV-Datei gefunden.");
                return result;
            }

            progress?.Report("Lade bestehende Easybill-Artikel...");
            var existing = await easybillService.GetAllProductsAsync();
            var byNumber = existing
                .Where(p => !string.IsNullOrWhiteSpace(p.Number))
                .GroupBy(p => p.Number, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            progress?.Report($"Importiere {rows.Count} Artikel...");

            int processed = 0;
            foreach (var row in rows)
            {
                processed++;
                try
                {
                    if (byNumber.TryGetValue(row.Number, out var existingProduct))
                    {
                        existingProduct.Type = row.Type;
                        existingProduct.Description = row.Description;
                        existingProduct.SalePrice = row.SalePrice;
                        existingProduct.VatPercent = row.VatPercent;
                        existingProduct.Unit = row.Unit;
                        if (row.PurchasePrice.HasValue)
                        {
                            existingProduct.PurchasePrice = row.PurchasePrice;
                            if (string.IsNullOrWhiteSpace(existingProduct.PurchasePriceNetGross))
                                existingProduct.PurchasePriceNetGross = "NET";
                        }
                        if (!string.IsNullOrWhiteSpace(row.Note))
                            existingProduct.Note = row.Note;

                        progress?.Report($"[{processed}/{rows.Count}] Aktualisiere {row.Number} – {row.Description}");
                        await ExecuteWithRateLimitAsync(
                            () => easybillService.UpdateProductAsync(existingProduct.Id.Value, existingProduct),
                            row.Number,
                            progress);
                        result.Updated++;
                    }
                    else
                    {
                        var product = new EasybillProduct
                        {
                            Type = row.Type,
                            Number = row.Number,
                            Description = row.Description,
                            SalePrice = row.SalePrice,
                            PurchasePrice = row.PurchasePrice,
                            PurchasePriceNetGross = row.PurchasePrice.HasValue ? "NET" : null,
                            VatPercent = row.VatPercent,
                            Unit = row.Unit,
                            Note = row.Note
                        };

                        progress?.Report($"[{processed}/{rows.Count}] Erstelle {row.Number} – {row.Description}");
                        await ExecuteWithRateLimitAsync(
                            () => easybillService.CreateProductAsync(product),
                            row.Number,
                            progress);
                        result.Created++;
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add($"{row.Number}: {ex.Message}");
                }

                if (RequestDelayMs > 0)
                    await Task.Delay(RequestDelayMs);
            }

            progress?.Report($"Fertig. Erstellt: {result.Created}, Aktualisiert: {result.Updated}, Fehler: {result.Failed}");
            return result;
        }

        /// <summary>
        /// Führt einen API-Aufruf aus und wiederholt ihn bei HTTP 429 (Too Many Requests)
        /// mit exponentiellem Backoff.
        /// </summary>
        private async Task<T> ExecuteWithRateLimitAsync<T>(Func<Task<T>> action, string articleNumber, IProgress<string> progress)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex) when (IsRateLimitError(ex) && attempt < MaxRetriesOnRateLimit)
                {
                    attempt++;
                    int waitMs = (int)Math.Min(30000, 2000 * Math.Pow(2, attempt - 1));
                    progress?.Report($"Rate-Limit erreicht bei {articleNumber} – warte {waitMs / 1000}s (Versuch {attempt}/{MaxRetriesOnRateLimit})...");
                    await Task.Delay(waitMs);
                }
            }
        }

        private static bool IsRateLimitError(Exception ex)
        {
            if (ex == null) return false;
            var msg = ex.Message ?? string.Empty;
            return msg.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("429")
                || msg.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("21000"); // Easybill rate-limit error code
        }

        /// <summary>
        /// Liest die CSV-Datei und liefert die geparsten Datenzeilen.
        /// Trennzeichen werden anhand der Header-Zeile automatisch erkannt (; , oder Tab).
        /// Erkennt verschiedene Header-Varianten (z.B. number/article_code/artikelnummer,
        /// saleprice/price_plus_25pct_eur/verkaufspreis, purchaseprice/net_price_eur/nettopreis).
        /// </summary>
        public static List<CsvProductRow> ParseCsv(string csvFilePath)
        {
            var rows = new List<CsvProductRow>();
            var lines = File.ReadAllLines(csvFilePath, DetectEncoding(csvFilePath));
            if (lines.Length == 0)
                return rows;

            var headerLine = lines[0];
            char separator = DetectSeparator(headerLine);

            var headers = SplitCsvLine(headerLine, separator)
                .Select(h => NormalizeHeader(h))
                .ToList();

            int idxNumber = FindHeader(headers, "number", "articlecode", "article_code", "artikelnummer", "artikelnr", "nummer", "nr", "sku");
            int idxDescription = FindHeader(headers, "description", "beschreibung", "bezeichnung", "name");
            int idxSalePrice = FindHeader(headers, "saleprice", "sale_price", "price_plus_25pct_eur", "priceplus25pcteur", "verkaufspreis", "vkpreis", "vk", "bruttopreis", "preis");
            int idxPurchasePrice = FindHeader(headers, "purchaseprice", "purchase_price", "net_price_eur", "netpriceeur", "nettopreis", "einkaufspreis", "ekpreis", "ek");
            int idxUnit = FindHeader(headers, "unit", "einheit");
            int idxVat = FindHeader(headers, "vatpercent", "vat_percent", "vat", "mwst", "ust", "steuer");
            int idxType = FindHeader(headers, "type", "typ", "art");
            int idxNote = FindHeader(headers, "note", "notiz", "bemerkung", "anmerkung");

            // Zusatzspalten zum optionalen Anreichern der Beschreibung
            int idxSeries = FindHeader(headers, "series", "baureihe", "serie");
            int idxCompatibility = FindHeader(headers, "compatibility", "controller", "ausfuehrung", "ausführung");

            if (idxNumber < 0 || idxDescription < 0 || idxSalePrice < 0)
            {
                throw new InvalidDataException(
                    "Die CSV-Datei muss mindestens Spalten für Artikelnummer, Beschreibung und Verkaufspreis enthalten.\n" +
                    "Unterstützte Header (Beispiele):\n" +
                    "  • Artikelnummer: Number, article_code, Artikelnummer, SKU\n" +
                    "  • Beschreibung: Description, Beschreibung, Bezeichnung\n" +
                    "  • Verkaufspreis: SalePrice, price_plus_25pct_eur, Verkaufspreis, Preis\n" +
                    "  • Optional: PurchasePrice/net_price_eur, Unit, VatPercent, Type, Note\n\n" +
                    "Gefundene Header: " + string.Join(", ", headers));
            }

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var fields = SplitCsvLine(line, separator);

                string Get(int idx) =>
                    idx >= 0 && idx < fields.Count ? fields[idx]?.Trim() : null;

                var numberValue = Get(idxNumber);
                var descriptionValue = Get(idxDescription);
                var salePriceValue = Get(idxSalePrice);

                // Titel-/Trennzeilen ohne Artikelnummer oder Preis überspringen
                if (string.IsNullOrWhiteSpace(numberValue) || string.IsNullOrWhiteSpace(salePriceValue))
                    continue;

                if (string.IsNullOrWhiteSpace(descriptionValue))
                    descriptionValue = numberValue;

                // Beschreibung optional mit Baureihe/Kompatibilität anreichern
                var seriesValue = Get(idxSeries);
                var compatibilityValue = Get(idxCompatibility);
                var fullDescription = descriptionValue;
                if (!string.IsNullOrWhiteSpace(seriesValue) &&
                    !descriptionValue.Contains(seriesValue, StringComparison.OrdinalIgnoreCase))
                {
                    fullDescription = $"{seriesValue}: {fullDescription}";
                }
                if (!string.IsNullOrWhiteSpace(compatibilityValue) &&
                    !fullDescription.Contains(compatibilityValue, StringComparison.OrdinalIgnoreCase))
                {
                    fullDescription = $"{fullDescription} ({compatibilityValue})";
                }

                if (!TryParseDecimal(salePriceValue, out var salePrice))
                    throw new InvalidDataException($"Zeile {i + 1}: Ungültiger Verkaufspreis '{salePriceValue}'.");

                decimal? purchasePrice = null;
                var purchaseRaw = Get(idxPurchasePrice);
                if (!string.IsNullOrWhiteSpace(purchaseRaw))
                {
                    if (!TryParseDecimal(purchaseRaw, out var pp))
                        throw new InvalidDataException($"Zeile {i + 1}: Ungültiger Einkaufs-/Nettopreis '{purchaseRaw}'.");
                    purchasePrice = pp;
                }

                int vat = 19;
                var vatRaw = Get(idxVat);
                if (!string.IsNullOrWhiteSpace(vatRaw))
                {
                    var vatClean = vatRaw.Replace("%", string.Empty).Trim();
                    if (!int.TryParse(vatClean, NumberStyles.Integer, CultureInfo.InvariantCulture, out vat))
                        throw new InvalidDataException($"Zeile {i + 1}: Ungültiger MwSt.-Wert '{vatRaw}'.");
                }

                var typeRaw = Get(idxType);
                var type = string.IsNullOrWhiteSpace(typeRaw) ? "PRODUCT" : typeRaw.ToUpperInvariant();
                if (type != "PRODUCT" && type != "SERVICE")
                {
                    // Tolerant: Deutsche Begriffe zuordnen
                    if (type.StartsWith("DIENST") || type.StartsWith("SERV"))
                        type = "SERVICE";
                    else if (type.StartsWith("PROD") || type.StartsWith("ART") || type.StartsWith("WARE"))
                        type = "PRODUCT";
                    else
                        throw new InvalidDataException($"Zeile {i + 1}: Type muss 'PRODUCT' oder 'SERVICE' sein (war '{typeRaw}').");
                }

                var unit = Get(idxUnit);
                if (string.IsNullOrWhiteSpace(unit))
                    unit = "Stk";

                var note = Get(idxNote);
                // Bewusst KEINE automatische Notiz mit dem Einkaufspreis erzeugen:
                // Der Einkaufspreis wird separat im Easybill-Feld PurchasePrice gespeichert
                // und darf niemals in der (für Kunden sichtbaren) Beschreibung landen.

                rows.Add(new CsvProductRow
                {
                    Number = numberValue,
                    Description = fullDescription,
                    SalePrice = salePrice,
                    PurchasePrice = purchasePrice,
                    Unit = unit,
                    VatPercent = vat,
                    Type = type,
                    Note = note
                });
            }

            return rows;
        }

        private static char DetectSeparator(string headerLine)
        {
            if (headerLine.Contains(';')) return ';';
            if (headerLine.Contains('\t')) return '\t';
            return ',';
        }

        private static string NormalizeHeader(string header)
        {
            if (string.IsNullOrEmpty(header)) return string.Empty;
            var sb = new StringBuilder(header.Length);
            foreach (var c in header.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private static int FindHeader(List<string> headers, params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                var normalized = NormalizeHeader(candidate);
                int idx = headers.IndexOf(normalized);
                if (idx >= 0) return idx;
            }
            // Fallback: ohne Unterstriche vergleichen
            foreach (var candidate in candidates)
            {
                var normalized = NormalizeHeader(candidate).Replace("_", string.Empty);
                int idx = headers.FindIndex(h => h.Replace("_", string.Empty) == normalized);
                if (idx >= 0) return idx;
            }
            return -1;
        }

        private static bool TryParseDecimal(string value, out decimal result)
        {
            value = (value ?? string.Empty).Trim();
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return true;
            return decimal.TryParse(value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        private static Encoding DetectEncoding(string path)
        {
            using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            _ = reader.Peek();
            return reader.CurrentEncoding;
        }

        /// <summary>
        /// Einfacher CSV-Zeilen-Parser, der Anführungszeichen ("...") und
        /// doppelte Anführungszeichen ("") als Escaping unterstützt.
        /// </summary>
        private static List<string> SplitCsvLine(string line, char separator)
        {
            var result = new List<string>();
            if (line == null)
                return result;

            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == separator)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            result.Add(sb.ToString());
            return result;
        }
    }
}
