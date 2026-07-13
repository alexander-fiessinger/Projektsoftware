using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Projektsoftware.Models;

namespace Projektsoftware.Services
{
    /// <summary>
    /// UI-unabhängiger, vollautomatischer Zahlungsabgleich für den Hintergrundbetrieb.
    /// Ruft Kontoumsätze per BANKSapi ab, gleicht sie mit offenen Easybill-Rechnungen ab
    /// und bucht ausschließlich eindeutige Treffer (eindeutige Vollzahlungen sowie eindeutige
    /// Teilzahlungen) automatisch. Mehrdeutige Fälle bleiben zur manuellen Bestätigung im
    /// <see cref="Views.PaymentReconciliationDialog"/> stehen.
    /// Der Doppelbuchungsschutz erfolgt über den <see cref="ReconciliationLogStore"/>.
    /// </summary>
    public class AutoReconciliationService
    {
        /// <summary>Standard-Zeitraum für den automatischen Kontoabruf (Tage rückwirkend).</summary>
        public const int DefaultLookbackDays = 180;

        /// <summary>Ergebnis eines automatischen Abgleichlaufs.</summary>
        public class AutoReconciliationResult
        {
            /// <summary>True, wenn der Lauf technisch durchlief (auch ohne Buchungen).</summary>
            public bool Success { get; set; }

            /// <summary>True, wenn der Lauf mangels Konfiguration übersprungen wurde.</summary>
            public bool Skipped { get; set; }

            /// <summary>Menschliche Kurzbeschreibung des Laufs.</summary>
            public string Message { get; set; } = "";

            /// <summary>Anzahl abgerufener Zahlungseingänge.</summary>
            public int TransactionCount { get; set; }

            /// <summary>Anzahl automatisch gebuchter (Voll-/Teil-)Zahlungen.</summary>
            public int BookedCount { get; set; }

            /// <summary>Anzahl der Buchungen, die fehlschlugen.</summary>
            public int FailedCount { get; set; }

            /// <summary>Anzahl mehrdeutiger Treffer, die eine manuelle Bestätigung erfordern.</summary>
            public int NeedsConfirmationCount { get; set; }

            /// <summary>Kurzbeschreibungen der automatisch gebuchten Zahlungen (für Benachrichtigungen).</summary>
            public List<string> BookedDetails { get; set; } = new();

            /// <summary>Zeitpunkt des Laufs.</summary>
            public DateTime RunAt { get; set; } = DateTime.Now;
        }

        /// <summary>
        /// Führt einen vollständigen automatischen Abgleich- und Buchungslauf aus.
        /// Wirft keine Ausnahmen nach außen – Fehler werden im Ergebnis gemeldet, damit der
        /// Hintergrundbetrieb die Anwendung niemals stört.
        /// </summary>
        /// <param name="lookbackDays">Zeitraum des Kontoabrufs in Tagen (Standard: 180).</param>
        public async Task<AutoReconciliationResult> RunAsync(int lookbackDays = DefaultLookbackDays)
        {
            var result = new AutoReconciliationResult();

            try
            {
                var bankConfig = BankConfig.Load();
                var easybillService = new EasybillService();

                // Ohne vollständige Konfiguration wird der Lauf still übersprungen.
                if (!bankConfig.IsConfigured || !bankConfig.HasBankAccess || !easybillService.IsConfigured)
                {
                    result.Skipped = true;
                    result.Message = "Automatischer Abgleich übersprungen (Bank- oder Easybill-Konfiguration fehlt).";
                    return result;
                }

                var to = DateTime.Today;
                var from = to.AddDays(-Math.Abs(lookbackDays));

                var banksApi = new BanksApiService(bankConfig);
                var txResult = await banksApi.GetIncomingTransactionsAsync(from, to);

                if (!txResult.Success)
                {
                    result.Success = false;
                    result.Message = $"Kontoabruf fehlgeschlagen: {txResult.Message}";
                    return result;
                }

                result.TransactionCount = txResult.Transactions.Count;

                var invoices = await easybillService.GetAllDocumentsAsync("INVOICE");
                var reconciliation = new PaymentReconciliationService();
                var matches = reconciliation.Match(txResult.Transactions, invoices);

                var logStore = new ReconciliationLogStore();

                foreach (var match in matches)
                {
                    match.AlreadyBooked = logStore.IsAlreadyBooked(match.Transaction.TransactionHash);
                }

                result.NeedsConfirmationCount = matches.Count(m =>
                    !m.AlreadyBooked && !m.IsAutoBookable &&
                    m.Status == ReconciliationMatchStatus.NeedsConfirmation);

                var autoBookable = matches.Where(m => m.IsAutoBookable).ToList();

                foreach (var match in autoBookable)
                {
                    if (match.Invoice?.Id == null) continue;

                    try
                    {
                        var paidAt = match.Transaction.ValueDate.ToString("yyyy-MM-dd");

                        // Teilzahlungen werden erfasst, ohne die Rechnung vollständig als bezahlt zu markieren.
                        await easybillService.MarkDocumentAsPaidAsync(
                            match.Invoice.Id.Value,
                            paidAt,
                            match.Transaction.Amount,
                            markAsPaid: !match.IsPartialPayment);

                        match.Booked = true;
                        match.BookingError = null;
                        logStore.Add(match);
                        result.BookedCount++;

                        var customer = match.MatchedCustomerName
                            ?? match.Invoice?.CustomerDisplay
                            ?? "Unbekannter Kunde";
                        var kind = match.IsPartialPayment ? "Teilzahlung" : "Zahlung";
                        result.BookedDetails.Add(
                            $"{kind} {match.Transaction.Amount:N2} € · Rg. {match.InvoiceNumberDisplay} · {customer}");
                    }
                    catch (Exception ex)
                    {
                        match.BookingError = ex.Message;
                        result.FailedCount++;
                        System.Diagnostics.Debug.WriteLine(
                            $"Auto-Buchung fehlgeschlagen (Rg. {match.InvoiceNumberDisplay}): {ex.Message}");
                    }
                }

                result.Success = true;
                result.Message = result.BookedCount > 0
                    ? $"Automatisch gebucht: {result.BookedCount} Zahlung(en)" +
                      (result.FailedCount > 0 ? $", {result.FailedCount} fehlgeschlagen" : "") +
                      (result.NeedsConfirmationCount > 0 ? $", {result.NeedsConfirmationCount} zur Prüfung" : "") + "."
                    : result.NeedsConfirmationCount > 0
                        ? $"Keine eindeutigen Buchungen; {result.NeedsConfirmationCount} Zahlung(en) zur manuellen Prüfung."
                        : "Keine neuen Zahlungseingänge zum Buchen.";

                return result;
            }
            catch (Exception ex)
            {
                // Hintergrund-Lauf darf die App nie zum Absturz bringen.
                result.Success = false;
                result.Message = $"Automatischer Abgleich fehlgeschlagen: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"AutoReconciliation-Fehler: {ex}");
                return result;
            }
        }
    }
}
