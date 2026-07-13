using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Projektsoftware.Models;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Lokales Protokoll aller automatisch/manuell gebuchten Kontoeingänge.
    /// Dient der Nachverfolgung und dem Schutz vor Doppelbuchungen: bereits
    /// verarbeitete Umsätze werden anhand ihres <see cref="BankTransaction.TransactionHash"/>
    /// erkannt. Speicherung als JSON unter %AppData%\Projektsoftware\.
    /// </summary>
    public class ReconciliationLogStore
    {
        private static readonly string logFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware",
            "reconciliation-log.json"
        );

        private readonly List<ReconciliationLogEntry> entries;
        private readonly HashSet<string> bookedHashes;

        public ReconciliationLogStore()
        {
            entries = Load();
            bookedHashes = new HashSet<string>(
                entries.Select(e => e.TransactionHash),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Alle Protokolleinträge, neueste zuerst.</summary>
        public IReadOnlyList<ReconciliationLogEntry> Entries =>
            entries.OrderByDescending(e => e.BookedAt).ToList();

        /// <summary>Prüft, ob ein Umsatz bereits gebucht wurde (Doppelbuchungsschutz).</summary>
        public bool IsAlreadyBooked(string transactionHash) =>
            bookedHashes.Contains(transactionHash);

        /// <summary>Fügt einen erfolgreich gebuchten Umsatz zum Protokoll hinzu und speichert.</summary>
        public void Add(ReconciliationMatch match)
        {
            var tx = match.Transaction;
            if (bookedHashes.Contains(tx.TransactionHash))
                return;

            var entry = new ReconciliationLogEntry
            {
                TransactionHash = tx.TransactionHash,
                EasybillDocumentId = match.Invoice?.Id,
                InvoiceNumber = match.MatchedInvoiceNumber ?? match.Invoice?.Number ?? "",
                PartnerName = tx.PartnerName,
                AmountCents = tx.AmountCents,
                ValueDate = tx.ValueDate,
                BookedAt = DateTime.Now
            };

            entries.Add(entry);
            bookedHashes.Add(tx.TransactionHash);
            Save();
        }

        private static List<ReconciliationLogEntry> Load()
        {
            try
            {
                if (File.Exists(logFilePath))
                {
                    var json = File.ReadAllText(logFilePath);
                    return JsonSerializer.Deserialize<List<ReconciliationLogEntry>>(json)
                           ?? new List<ReconciliationLogEntry>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden des Abgleich-Protokolls: {ex.Message}");
            }

            return new List<ReconciliationLogEntry>();
        }

        private void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(logFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern des Abgleich-Protokolls: {ex.Message}");
            }
        }
    }
}
