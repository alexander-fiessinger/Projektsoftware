using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Projektsoftware.Models;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class OfferHistoryDialog : Window
    {
        private readonly DatabaseService _db = new DatabaseService();

        public OfferHistoryDialog()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var customers = await _db.GetAllCustomersAsync();
                ContactCombo.ItemsSource = customers.OrderBy(c => c.DisplayName).ToList();
                if (customers.Any()) ContactCombo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Show_Click(object sender, RoutedEventArgs e)
        {
            if (ContactCombo.SelectedItem is not Customer c) return;
            try
            {
                var offers = await _db.GetOfferHistoryByContactAsync(c.Id);

                // Fallback: Falls keine lokalen Datensätze, live aus Easybill laden
                if (offers.Count == 0 && c.EasybillCustomerId.HasValue)
                {
                    offers = await LoadFromEasybillAsync(c);
                }

                OffersGrid.ItemsSource = offers.OrderByDescending(o => o.OfferDate).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task<List<OfferHistory>> LoadFromEasybillAsync(Customer c)
        {
            var result = new List<OfferHistory>();
            try
            {
                var easybill = new EasybillService();
                var docs = await easybill.GetAllDocumentsAsync("OFFER");
                var filtered = docs.Where(d => d.CustomerId == c.EasybillCustomerId).ToList();

                foreach (var d in filtered)
                {
                    DateTime offerDate = DateTime.TryParse(d.DocumentDate, out var od) ? od : DateTime.Now;
                    DateTime? validUntil = DateTime.TryParse(d.DueDate, out var vd) ? vd : null;
                    var offer = new OfferHistory
                    {
                        CustomerId = c.Id,
                        EasybillDocumentId = d.Id,
                        OfferNumber = d.Number,
                        OfferTitle = d.Title ?? d.Subject,
                        OfferDate = offerDate,
                        ValidUntil = validUntil,
                        TotalAmount = d.TotalGross ?? d.TotalNet ?? 0m,
                        Currency = d.Currency ?? "EUR",
                        Status = MapStatus(d),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    result.Add(offer);

                    // Persistenz für nächste Aufrufe
                    try { await _db.SaveOfferHistoryAsync(offer); } catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Easybill-Fallback fehlgeschlagen: {ex.Message}");
            }
            return result;
        }

        private static string MapStatus(EasybillDocument d)
        {
            if (d.IsDraft) return "Draft";
            return d.Status switch
            {
                "SENT" => "Sent",
                "PAID" => "Accepted",
                "CANCELLED" => "Declined",
                _ => d.Status ?? "Sent"
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
