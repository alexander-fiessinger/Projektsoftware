using System;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Orchestriert die einseitige Synchronisation der Artikel von Easybill (Master)
    /// in den lokalen Portal-Katalog (products). Abgleich erfolgt über die Artikelnummer.
    /// </summary>
    public class ProductSyncService
    {
        /// <summary>Ergebnis eines Synchronisationslaufs.</summary>
        public class SyncResult
        {
            public bool Success { get; set; }
            public bool Skipped { get; set; }
            public int Created { get; set; }
            public int Updated { get; set; }
            public int Ignored { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// Lädt alle Easybill-Artikel und schreibt sie in den lokalen Katalog.
        /// Bei fehlender Easybill-Konfiguration wird der Lauf still übersprungen.
        /// </summary>
        public async Task<SyncResult> SyncAsync()
        {
            try
            {
                var easybill = new EasybillService();
                if (!easybill.IsConfigured)
                {
                    return new SyncResult
                    {
                        Success = false,
                        Skipped = true,
                        Message = "Easybill ist nicht konfiguriert – Synchronisation übersprungen."
                    };
                }

                var easybillProducts = await easybill.GetAllProductsAsync();

                var db = new DatabaseService();
                var (created, updated, ignored) = await db.SyncProductsFromEasybillAsync(easybillProducts);

                return new SyncResult
                {
                    Success = true,
                    Created = created,
                    Updated = updated,
                    Ignored = ignored,
                    Message = $"Synchronisiert: {created} neu, {updated} aktualisiert" +
                              (ignored > 0 ? $", {ignored} ohne Artikelnummer übersprungen" : "")
                };
            }
            catch (Exception ex)
            {
                return new SyncResult
                {
                    Success = false,
                    Message = $"Synchronisation fehlgeschlagen: {ex.Message}"
                };
            }
        }
    }
}
