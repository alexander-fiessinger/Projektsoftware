using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Projektsoftware.Models;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Ordnet Kontoeingänge offenen Easybill-Rechnungen zu. Da Kunden die Rechnungsnummer
    /// im Verwendungszweck uneinheitlich oder gar nicht angeben, erfolgt die Zuordnung primär
    /// über den Kundennamen (Auftraggeber der Überweisung ↔ Rechnungskunde) und den Betrag.
    /// Ein Treffer gilt nur dann als eindeutig (automatisch buchbar), wenn genau eine offene
    /// Rechnung des erkannten Kunden existiert und der Zahlungseingang exakt dem offenen Betrag
    /// entspricht (Vollzahlung). Teilzahlungen und mehrdeutige Fälle werden zur manuellen
    /// Bestätigung markiert.
    /// </summary>
    public class PaymentReconciliationService
    {
        /// <summary>Mindestlänge einer normalisierten Rechnungsnummer, um Zufallstreffer zu vermeiden.</summary>
        private const int MinInvoiceNumberLength = 4;

        /// <summary>Mindestlänge eines Namens-Tokens, damit es für den Kundenabgleich zählt.</summary>
        private const int MinTokenLength = 3;

        /// <summary>Rechtsform-/Füllwörter, die beim Namensvergleich ignoriert werden.</summary>
        private static readonly HashSet<string> NameStopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "GMBH", "AG", "KG", "OHG", "GBR", "UG", "EK", "EG", "MBH", "CO", "COKG",
            "GMBHCOKG", "GMBHCO", "LTD", "INC", "LLC", "SE", "PARTG", "PARTGMBB",
            "UND", "AND", "DIE", "DER", "DAS", "FIRMA", "FAMILIE", "HERR", "HERRN", "FRAU"
        };

        /// <summary>
        /// Gleicht die übergebenen (eingehenden) Kontobewegungen mit den offenen Rechnungen ab.
        /// </summary>
        public List<ReconciliationMatch> Match(
            IEnumerable<BankTransaction> transactions,
            IEnumerable<EasybillDocument> documents)
        {
            var invoiceIndex = documents
                .Where(d => d.Type == "INVOICE"
                            && !d.IsDraft
                            && d.Id.HasValue
                            && d.TotalGross.HasValue
                            && !IsClosed(d))
                .Select(d =>
                {
                    var name = ExtractCustomerName(d);
                    return new InvoiceKey(d, OpenAmountCents(d), Normalize(d.Number), name, InvoiceNameTokens(d));
                })
                .ToList();

            var usedInvoiceIds = new HashSet<long>();
            var results = new List<ReconciliationMatch>();

            foreach (var tx in transactions.OrderBy(t => t.ValueDate))
            {
                results.Add(MatchSingle(tx, invoiceIndex, usedInvoiceIds));
            }

            return results;
        }

        private static ReconciliationMatch MatchSingle(
            BankTransaction tx, List<InvoiceKey> invoiceIndex, HashSet<long> usedInvoiceIds)
        {
            var match = new ReconciliationMatch { Transaction = tx };

            var available = invoiceIndex
                .Where(x => !usedInvoiceIds.Contains(x.Document.Id!.Value))
                .ToList();

            // 1) Kandidaten anhand des Kundennamens (Auftraggeber der Überweisung ↔ Rechnungskunde)
            var payerTokens = NameTokens(tx.PartnerName);
            var nameCandidates = available
                .Where(x => x.NameTokens.Count > 0 && NameMatches(payerTokens, x.NameTokens))
                .ToList();

            // 1b) Falls die Rechnungsnummer doch im Verwendungszweck steht, als zusätzliche Kandidatenquelle nutzen
            var normText = Normalize(tx.SearchText);
            var numberCandidates = available
                .Where(x => x.Normalized.Length >= MinInvoiceNumberLength && normText.Contains(x.Normalized))
                .ToList();

            // Namens- und Nummernkandidaten zusammenführen (Nummer bestätigt einen Namenstreffer besonders stark)
            var candidates = nameCandidates
                .Union(numberCandidates)
                .ToList();

            if (candidates.Count == 0)
            {
                match.Status = ReconciliationMatchStatus.NoMatch;
                match.Reason = string.IsNullOrWhiteSpace(tx.PartnerName)
                    ? "Kein Auftraggebername vorhanden – keine Zuordnung über Kundenname möglich."
                    : $"Keine offene Rechnung zum Auftraggeber „{tx.PartnerName}“ gefunden.";
                return match;
            }

            // Exakter Betrag (Vollzahlung) unter den Kandidaten?
            var exactAmount = candidates.Where(x => x.AmountCents == tx.AmountCents).ToList();

            // Teilzahlung: Eingang kleiner als offener Betrag der Rechnung.
            var partialCandidates = candidates.Where(x => tx.AmountCents < x.AmountCents).ToList();

            InvoiceKey chosen;
            bool numberConfirms;

            if (exactAmount.Count == 1)
            {
                chosen = exactAmount[0];
                numberConfirms = numberCandidates.Any(n => n.Document.Id == chosen.Document.Id);

                match.Invoice = chosen.Document;
                match.MatchedInvoiceNumber = chosen.Document.Number;
                match.MatchedCustomerName = chosen.CustomerName;
                match.OpenAmount = chosen.AmountCents / 100m;
                match.AmountMatches = true;
                match.IsPartialPayment = false;

                // Automatisch nur, wenn der Kunde eindeutig genau eine passende Rechnung hat.
                bool uniqueForCustomer = candidates.Count(c => c.AmountCents == tx.AmountCents) == 1
                                         && nameCandidates.Count(c => c.AmountCents == tx.AmountCents) <= 1;

                match.IsUniqueCustomerMatch = uniqueForCustomer;

                if (uniqueForCustomer)
                {
                    match.Status = ReconciliationMatchStatus.Automatic;
                    match.Reason = numberConfirms
                        ? $"Kunde „{chosen.CustomerName}“ und Betrag stimmen; Rechnungsnummer zusätzlich im Verwendungszweck gefunden."
                        : $"Kunde „{chosen.CustomerName}“ und offener Betrag stimmen exakt überein.";
                }
                else
                {
                    match.Status = ReconciliationMatchStatus.NeedsConfirmation;
                    match.Reason = $"Betrag passt, aber mehrere offene Rechnungen kommen in Frage – bitte bestätigen.";
                }

                return match;
            }

            // Keine exakte Vollzahlung -> Teilzahlung prüfen (immer nur zur Bestätigung, nie automatisch).
            if (partialCandidates.Count > 0)
            {
                // Bevorzugt einen eindeutigen Namenstreffer, sonst den kleinsten passenden offenen Betrag.
                chosen = partialCandidates
                    .OrderByDescending(x => nameCandidates.Any(n => n.Document.Id == x.Document.Id))
                    .ThenBy(x => x.AmountCents)
                    .First();

                // Eindeutig, wenn dem Kunden genau eine offene Rechnung per Namenstreffer zugeordnet ist.
                bool uniquePartialForCustomer =
                    nameCandidates.Count == 1 &&
                    nameCandidates[0].Document.Id == chosen.Document.Id;

                match.Invoice = chosen.Document;
                match.MatchedInvoiceNumber = chosen.Document.Number;
                match.MatchedCustomerName = chosen.CustomerName;
                match.OpenAmount = chosen.AmountCents / 100m;
                match.AmountMatches = false;
                match.IsPartialPayment = true;
                match.IsUniqueCustomerMatch = uniquePartialForCustomer;
                match.Status = ReconciliationMatchStatus.NeedsConfirmation;
                match.Reason = partialCandidates.Count > 1
                    ? $"Teilzahlung für Kunde „{chosen.CustomerName}“ – mehrere offene Rechnungen möglich, bitte prüfen."
                    : $"Teilzahlung: Eingang {FormatEuro(tx.AmountCents)} auf offenen Betrag {FormatEuro(chosen.AmountCents)} von Kunde „{chosen.CustomerName}“.";
                return match;
            }

            // Nur Überzahlung/abweichender Betrag übrig -> Bestätigung nötig.
            chosen = candidates
                .OrderByDescending(x => nameCandidates.Any(n => n.Document.Id == x.Document.Id))
                .ThenBy(x => Math.Abs(x.AmountCents - tx.AmountCents))
                .First();

            match.Invoice = chosen.Document;
            match.MatchedInvoiceNumber = chosen.Document.Number;
            match.MatchedCustomerName = chosen.CustomerName;
            match.OpenAmount = chosen.AmountCents / 100m;
            match.AmountMatches = false;
            match.IsPartialPayment = false;
            match.Status = ReconciliationMatchStatus.NeedsConfirmation;
            match.Reason = $"Kunde „{chosen.CustomerName}“ erkannt, aber Betrag weicht ab " +
                           $"(offen {FormatEuro(chosen.AmountCents)} / Eingang {FormatEuro(tx.AmountCents)}).";
            return match;
        }

        /// <summary>
        /// Prüft, ob eine Rechnung bereits abgeschlossen (vollständig bezahlt/storniert) ist.
        /// Wichtig: Das Easybill-Feld <c>paid_at</c> ist KEIN zuverlässiges Signal – es kann gesetzt
        /// sein, obwohl <c>paid_amount = 0</c> ist. Daher wird ausschließlich der tatsächlich
        /// gezahlte Betrag (bzw. ein expliziter Status) herangezogen.
        /// </summary>
        private static bool IsClosed(EasybillDocument d)
        {
            if (d.Status is "PAID" or "CANCELLED" or "INVOICE_CANCELLATION")
                return true;

            // Vollständig gezahlt (paid_amount deckt den Bruttobetrag) => geschlossen.
            if (d.PaidAmount.HasValue && d.TotalGross.HasValue
                && ToCents(d.TotalGross.Value) > 0
                && ToCents(d.PaidAmount.Value) >= ToCents(d.TotalGross.Value))
                return true;

            return false;
        }

        private static long ToCents(decimal euro) =>
            (long)Math.Round(euro * 100m, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Noch offener Betrag in Cent: Bruttobetrag abzüglich bereits gezahlter (Teil-)Beträge.
        /// So werden auch Restzahlungen nach einer Anzahlung korrekt zugeordnet. Negative
        /// gezahlte Beträge (z. B. Gutschriften/Korrekturen) werden ignoriert.
        /// </summary>
        private static long OpenAmountCents(EasybillDocument d)
        {
            var gross = ToCents(d.TotalGross ?? 0m);
            var paid = ToCents(d.PaidAmount ?? 0m);
            if (paid < 0) paid = 0;
            var open = gross - paid;
            return open > 0 ? open : gross;
        }

        private static string FormatEuro(long cents) => (cents / 100m).ToString("N2") + " €";

        /// <summary>Normalisiert Text für den Nummernvergleich: nur Buchstaben/Ziffern, Großschreibung.</summary>
        private static string Normalize(string? s) =>
            string.IsNullOrEmpty(s) ? "" : Regex.Replace(s, "[^A-Za-z0-9]", "").ToUpperInvariant();

        /// <summary>
        /// Ermittelt den Kundennamen einer Rechnung aus dem CustomerSnapshot:
        /// bevorzugt den Firmennamen, sonst Vor-/Nachname.
        /// </summary>
        private static string ExtractCustomerName(EasybillDocument d)
        {
            var snap = d.CustomerSnapshot;
            if (snap == null) return "";

            if (!string.IsNullOrWhiteSpace(snap.CompanyName))
                return snap.CompanyName!;

            var name = $"{snap.FirstName} {snap.LastName}".Trim();
            return name;
        }

        /// <summary>
        /// Erzeugt alle vergleichbaren Namens-Tokens einer Rechnung. Berücksichtigt wird
        /// nicht nur der Firmenname, sondern auch der Nachname des hinterlegten Ansprechpartners,
        /// da Kunden häufig persönlich statt unter dem Firmennamen überweisen
        /// (z. B. Firma „Usetronic“ ↔ Auftraggeber „Schweckendiek, Peter“).
        /// Vornamen werden bewusst nur bei reinen Privatpersonen (ohne Firmenname/Nachname)
        /// herangezogen, da generische Vornamen sonst zu Fehlzuordnungen führen können.
        /// </summary>
        private static HashSet<string> InvoiceNameTokens(EasybillDocument d)
        {
            var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var snap = d.CustomerSnapshot;
            if (snap == null) return tokens;

            tokens.UnionWith(NameTokens(snap.CompanyName));

            if (!string.IsNullOrWhiteSpace(snap.LastName))
                tokens.UnionWith(NameTokens(snap.LastName));
            else if (tokens.Count == 0)
                tokens.UnionWith(NameTokens(snap.FirstName));

            return tokens;
        }

        /// <summary>
        /// Zerlegt einen Namen in vergleichbare Tokens: Umlaute/ß aufgelöst, Sonderzeichen entfernt,
        /// Rechtsform-/Füllwörter und zu kurze Tokens verworfen.
        /// </summary>
        private static HashSet<string> NameTokens(string? name)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(name)) return result;

            var expanded = name
                .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue")
                .Replace("Ä", "AE").Replace("Ö", "OE").Replace("Ü", "UE")
                .Replace("ß", "ss");

            var tokens = Regex.Split(expanded, "[^A-Za-z0-9]+");
            foreach (var t in tokens)
            {
                if (t.Length < MinTokenLength) continue;
                var upper = t.ToUpperInvariant();
                if (NameStopWords.Contains(upper)) continue;
                result.Add(upper);
            }
            return result;
        }

        /// <summary>
        /// Prüft, ob der Auftraggebername der Überweisung zum Rechnungskunden passt.
        /// Ein Treffer liegt vor, wenn mindestens ein aussagekräftiges Namens-Token übereinstimmt.
        /// </summary>
        private static bool NameMatches(HashSet<string> payerTokens, HashSet<string> invoiceTokens)
        {
            if (payerTokens.Count == 0 || invoiceTokens.Count == 0) return false;
            return payerTokens.Overlaps(invoiceTokens);
        }

        private readonly record struct InvoiceKey(
            EasybillDocument Document,
            long AmountCents,
            string Normalized,
            string CustomerName,
            HashSet<string> NameTokens);
    }
}
