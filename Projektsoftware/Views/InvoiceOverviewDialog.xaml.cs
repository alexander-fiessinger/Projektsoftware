using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class InvoiceOverviewDialog : Window
    {
        private readonly List<InvoiceOverviewItem> allItems;
        private readonly ObservableCollection<InvoiceOverviewItem> view = new();

        public InvoiceOverviewDialog(IEnumerable<InvoiceOverviewItem> items)
        {
            InitializeComponent();

            allItems = items?.ToList() ?? new List<InvoiceOverviewItem>();
            InvoicesDataGrid.ItemsSource = view;

            UpdateKpis();
            ApplyFilter();
        }

        private void UpdateKpis()
        {
            decimal paid = allItems.Where(i => i.Status == InvoiceOverviewStatus.Paid).Sum(i => i.GrossAmount);
            decimal open = allItems.Where(i => i.Status is InvoiceOverviewStatus.Open or InvoiceOverviewStatus.PartiallyPaid)
                                   .Sum(i => i.OpenAmount);
            decimal overdue = allItems.Where(i => i.Status == InvoiceOverviewStatus.Overdue).Sum(i => i.OpenAmount);

            int openCount = allItems.Count(i => i.Status is InvoiceOverviewStatus.Open or InvoiceOverviewStatus.PartiallyPaid);
            int overdueCount = allItems.Count(i => i.Status == InvoiceOverviewStatus.Overdue);
            int draftCount = allItems.Count(i => i.Status == InvoiceOverviewStatus.Draft);

            KpiPaidText.Text = paid.ToString("N2") + " €";
            KpiOpenText.Text = $"{open:N2} € · {openCount}";
            KpiOverdueText.Text = $"{overdue:N2} € · {overdueCount}";
            KpiDraftText.Text = draftCount.ToString();
        }

        private void ApplyFilter()
        {
            // Kann durch ComboBox-Initialisierung (IsSelected) bereits während
            // InitializeComponent() ausgelöst werden – dann sind Felder/Controls noch nicht bereit.
            if (allItems == null || InvoicesDataGrid == null)
                return;

            var selectedIndex = FilterComboBox?.SelectedIndex ?? 0;

            IEnumerable<InvoiceOverviewItem> filtered = selectedIndex switch
            {
                1 => allItems.Where(i => i.Status is InvoiceOverviewStatus.Open or InvoiceOverviewStatus.PartiallyPaid),
                2 => allItems.Where(i => i.Status == InvoiceOverviewStatus.Overdue),
                3 => allItems.Where(i => i.Status == InvoiceOverviewStatus.Paid),
                4 => allItems.Where(i => i.Status == InvoiceOverviewStatus.Draft),
                _ => allItems
            };

            // Offene/überfällige zuerst, dann nach Fälligkeit; sonst nach Dokumentdatum (neueste zuerst).
            var ordered = filtered
                .OrderByDescending(i => i.IsOpenOrOverdue)
                .ThenBy(i => i.DueDate ?? System.DateTime.MaxValue)
                .ThenByDescending(i => i.DocumentDate ?? System.DateTime.MinValue)
                .ToList();

            view.Clear();
            foreach (var item in ordered)
                view.Add(item);

            ResultCountText.Text = $"{ordered.Count} von {allItems.Count} Rechnungen";
            EmptyHintText.Visibility = ordered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        /// <summary>
        /// Öffnet die angeklickte Rechnung als PDF (Doppelklick auf eine Zeile).
        /// </summary>
        private async void InvoicesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (InvoicesDataGrid.SelectedItem is InvoiceOverviewItem item)
                await OpenInvoicePdfAsync(item);
        }

        /// <summary>Öffnet die Rechnung der Zeile über die "Öffnen"-Schaltfläche als PDF.</summary>
        private async void OpenInvoice_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is InvoiceOverviewItem item)
                await OpenInvoicePdfAsync(item);
        }

        private async System.Threading.Tasks.Task OpenInvoicePdfAsync(InvoiceOverviewItem item)
        {
            if (item?.Id == null)
            {
                MessageBox.Show(
                    "Für diese Rechnung ist keine Easybill-ID hinterlegt, sie kann nicht geöffnet werden.",
                    "Rechnung öffnen", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var easybill = new EasybillService();
                if (!easybill.IsConfigured)
                {
                    MessageBox.Show(
                        "Easybill ist nicht konfiguriert. Die Rechnung kann nicht geöffnet werden.",
                        "Rechnung öffnen", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var pdfData = await easybill.DownloadDocumentPdfAsync(item.Id.Value);

                var safeNumber = string.Concat((item.NumberDisplay ?? "Rechnung")
                    .Split(Path.GetInvalidFileNameChars()));
                var tempPath = Path.Combine(Path.GetTempPath(), $"Rechnung_{safeNumber}.pdf");
                File.WriteAllBytes(tempPath, pdfData);

                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Die Rechnung konnte nicht geöffnet werden:\n\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
