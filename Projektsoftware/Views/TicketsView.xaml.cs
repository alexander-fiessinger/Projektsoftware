using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Projektsoftware.Views
{
    public partial class TicketsView : UserControl
    {
        private readonly DatabaseService db;
        private List<Ticket> allTickets;
        private DispatcherTimer autoRefreshTimer;

        public TicketsView()
        {
            InitializeComponent();
            db = new DatabaseService();
            allTickets = new List<Ticket>();

            autoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            autoRefreshTimer.Tick += async (s, e) => await LoadDataAsync();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        public async System.Threading.Tasks.Task RefreshAsync()
        {
            await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                // Load statistics
                var stats = await db.GetTicketStatisticsAsync();
                TotalTicketsText.Text = stats.TotalTickets.ToString();
                NewTicketsText.Text   = stats.NewTickets.ToString();
                InProgressText.Text   = stats.InProgressTickets.ToString();
                UrgentText.Text       = stats.UrgentTickets.ToString();
                ResolvedText.Text     = stats.ResolvedTickets.ToString();
                ClosedText.Text       = stats.ClosedTickets.ToString();

                // Load ticket list
                allTickets = await db.GetAllTicketsAsync();
                ApplyFilters();
                LastUpdateTextBlock.Text = $"Zuletzt aktualisiert: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Tickets:\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Fehler beim Laden";
            }
        }

        private void ApplyFilters()
        {
            var filtered = allTickets.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                string search = SearchBox.Text.ToLower();
                filtered = filtered.Where(t =>
                    t.TicketNumber.ToLower().Contains(search) ||
                    t.CustomerName.ToLower().Contains(search) ||
                    t.CustomerEmail.ToLower().Contains(search) ||
                    t.Subject.ToLower().Contains(search) ||
                    t.Description.ToLower().Contains(search));
            }

            if (StatusFilterComboBox.SelectedIndex > 0)
            {
                var status = (TicketStatus)(StatusFilterComboBox.SelectedIndex - 1);
                filtered = filtered.Where(t => t.Status == status);
            }

            if (PriorityFilterComboBox.SelectedIndex > 0)
            {
                var priority = (TicketPriority)(PriorityFilterComboBox.SelectedIndex - 1);
                filtered = filtered.Where(t => t.Priority == priority);
            }

            if (CategoryFilterComboBox.SelectedIndex > 0)
            {
                var category = (TicketCategory)(CategoryFilterComboBox.SelectedIndex - 1);
                filtered = filtered.Where(t => t.Category == category);
            }

            TicketsDataGrid.ItemsSource = filtered.ToList();
            UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
            int total       = allTickets.Count;
            int newTickets  = allTickets.Count(t => t.Status == TicketStatus.New);
            int inProgress  = allTickets.Count(t => t.Status == TicketStatus.InProgress);
            int resolved    = allTickets.Count(t => t.Status == TicketStatus.Resolved);
            int urgent      = allTickets.Count(t => t.Priority == TicketPriority.Urgent);
            int displayed   = TicketsDataGrid.Items.Count;

            string text = $"Gesamt: {total} | Angezeigt: {displayed} | Neu: {newTickets} | In Bearbeitung: {inProgress} | Gelöst: {resolved}";
            if (urgent > 0) text += $" | 🔥 Dringend: {urgent}";
            StatusTextBlock.Text = text;
        }

        private void NewTicket_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TicketFormWindow();
            if (dialog.ShowDialog() == true)
                Refresh_Click(sender, e);
        }

        private void EditTicket_Click(object sender, RoutedEventArgs e)
        {
            if (TicketsDataGrid.SelectedItem is Ticket ticket)
            {
                var dialog = new TicketEditDialog(ticket);
                if (dialog.ShowDialog() == true)
                    Refresh_Click(sender, e);
            }
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (TicketsDataGrid.SelectedItem is Ticket ticket)
            {
                var dialog = new TicketDetailsDialog(ticket);
                dialog.ShowDialog();
                Refresh_Click(sender, e);
            }
        }

        private async void DeleteTicket_Click(object sender, RoutedEventArgs e)
        {
            if (TicketsDataGrid.SelectedItem is Ticket ticket)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie das Ticket {ticket.TicketNumber} wirklich löschen?\n\n" +
                    "⚠️ Achtung: Alle Kommentare und Zeiterfassungen werden ebenfalls gelöscht!",
                    "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await db.DeleteTicketAsync(ticket.Id);
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Löschen:\n{ex.Message}",
                            "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ApplyFilters();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) ApplyFilters();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            SearchBox.Focus();
        }

        private void AutoRefresh_Changed(object sender, RoutedEventArgs e)
        {
            if (AutoRefreshCheckBox.IsChecked == true)
                autoRefreshTimer.Start();
            else
                autoRefreshTimer.Stop();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Datei (*.csv)|*.csv|Alle Dateien (*.*)|*.*",
                    FileName = $"Tickets_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToCsv(saveDialog.FileName);
                    var open = MessageBox.Show($"CSV-Export erfolgreich!\n\nDatei: {saveDialog.FileName}\n\nMöchten Sie die Datei jetzt öffnen?",
                        "Export", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (open == MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = saveDialog.FileName, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim CSV-Export:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Datei (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*",
                    FileName = $"Tickets_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var tickets = TicketsDataGrid.ItemsSource as List<Ticket> ?? allTickets;
                    var pdfService = new PdfExportService();
                    pdfService.ExportTicketsToPdf(tickets, saveDialog.FileName);

                    var open = MessageBox.Show($"PDF-Export erfolgreich!\n\n{tickets.Count} Tickets exportiert.\n\nMöchten Sie das PDF jetzt öffnen?",
                        "Export", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (open == MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = saveDialog.FileName, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim PDF-Export:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(string filename)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Ticket#;Erstellt;Kunde;E-Mail;Telefon;Betreff;Beschreibung;Kategorie;Priorität;Status;Zugewiesen;Gelöst am");
            var tickets = TicketsDataGrid.ItemsSource as List<Ticket> ?? allTickets;
            foreach (var t in tickets)
            {
                csv.AppendLine(string.Join(";",
                    EscapeCsv(t.TicketNumber),
                    t.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                    EscapeCsv(t.CustomerName),
                    EscapeCsv(t.CustomerEmail),
                    EscapeCsv(t.CustomerPhone),
                    EscapeCsv(t.Subject),
                    EscapeCsv(t.Description),
                    t.CategoryText, t.PriorityText, t.StatusText,
                    EscapeCsv(t.AssignedToEmployeeName),
                    t.ResolvedAt?.ToString("dd.MM.yyyy HH:mm") ?? ""));
            }
            File.WriteAllText(filename, csv.ToString(), Encoding.UTF8);
        }

        private static string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Contains(';') || text.Contains('"') || text.Contains('\n'))
                return $"\"{text.Replace("\"", "\"\"")}\"";
            return text;
        }

        private void TicketsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool has = TicketsDataGrid.SelectedItem != null;
            EditButton.IsEnabled        = has;
            ViewDetailsButton.IsEnabled = has;
            DeleteButton.IsEnabled      = has;
        }

        private void TicketsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TicketsDataGrid.SelectedItem is Ticket)
                ViewDetails_Click(sender, null);
        }
    }
}
