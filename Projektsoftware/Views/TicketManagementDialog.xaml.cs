using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Text;

namespace Projektsoftware.Views
{
    public partial class TicketManagementDialog : Window
    {
        private DatabaseService db;
        private List<Ticket> allTickets;
        private DispatcherTimer autoRefreshTimer;

        public TicketManagementDialog()
        {
            InitializeComponent();
            db = new DatabaseService();
            allTickets = new List<Ticket>();

            // Auto-Refresh Timer initialisieren
            autoRefreshTimer = new DispatcherTimer();
            autoRefreshTimer.Interval = TimeSpan.FromSeconds(30);
            autoRefreshTimer.Tick += async (s, e) => await LoadTicketsAsync();

            Loaded += async (s, e) => await LoadTicketsAsync();
        }

        private async System.Threading.Tasks.Task LoadTicketsAsync()
        {
            try
            {
                allTickets = await db.GetAllTicketsAsync();
                ApplyFilters();
                UpdateStatusBar();
                LastUpdateTextBlock.Text = $"Zuletzt aktualisiert: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Laden der Tickets:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StatusTextBlock.Text = "Fehler beim Laden";
            }
        }

        private void ApplyFilters()
        {
            var filtered = allTickets.AsEnumerable();

            // Suchfilter
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

            // Status-Filter
            if (StatusFilterComboBox.SelectedIndex > 0)
            {
                var status = (TicketStatus)(StatusFilterComboBox.SelectedIndex - 1);
                filtered = filtered.Where(t => t.Status == status);
            }

            // Prioritäts-Filter
            if (PriorityFilterComboBox.SelectedIndex > 0)
            {
                var priority = (TicketPriority)(PriorityFilterComboBox.SelectedIndex - 1);
                filtered = filtered.Where(t => t.Priority == priority);
            }

            // Kategorie-Filter
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
            var total = allTickets.Count;
            var newTickets = allTickets.Count(t => t.Status == TicketStatus.New);
            var inProgress = allTickets.Count(t => t.Status == TicketStatus.InProgress);
            var resolved = allTickets.Count(t => t.Status == TicketStatus.Resolved);
            var urgent = allTickets.Count(t => t.Priority == TicketPriority.Urgent);

            var displayedCount = TicketsDataGrid.Items.Count;

            string statusText = $"Gesamt: {total} | Angezeigt: {displayedCount} | ";
            statusText += $"Neu: {newTickets} | In Bearbeitung: {inProgress} | Gelöst: {resolved}";

            if (urgent > 0)
                statusText += $" | 🔥 Dringend: {urgent}";

            StatusTextBlock.Text = statusText;
        }

        private void NewTicket_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TicketFormWindow();
            if (dialog.ShowDialog() == true)
            {
                Refresh_Click(sender, e);
            }
        }

        private void EditTicket_Click(object sender, RoutedEventArgs e)
        {
            if (TicketsDataGrid.SelectedItem is Ticket ticket)
            {
                var dialog = new TicketEditDialog(ticket);
                if (dialog.ShowDialog() == true)
                {
                    Refresh_Click(sender, e);
                }
            }
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (TicketsDataGrid.SelectedItem is Ticket ticket)
            {
                var dialog = new TicketDetailsDialog(ticket);
                dialog.ShowDialog();
                Refresh_Click(sender, e); // Aktualisieren falls Änderungen gemacht wurden
            }
        }

        private async void DeleteTicket_Click(object sender, RoutedEventArgs e)
        {
            if (TicketsDataGrid.SelectedItem is Ticket ticket)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie das Ticket {ticket.TicketNumber} wirklich löschen?\n\n" +
                    $"⚠️ Achtung: Alle Kommentare und Zeiterfassungen werden ebenfalls gelöscht!",
                    "Löschen bestätigen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await db.DeleteTicketAsync(ticket.Id);
                        MessageBox.Show("Ticket wurde gelöscht.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadTicketsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Fehler beim Löschen:\n{ex.Message}",
                            "Fehler",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadTicketsAsync();
        }

        private void FilterChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ApplyFilters();
            }
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ApplyFilters();
            }
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            SearchBox.Focus();
        }

        private void AutoRefresh_Changed(object sender, RoutedEventArgs e)
        {
            if (AutoRefreshCheckBox.IsChecked == true)
            {
                autoRefreshTimer.Start();
                MessageBox.Show("Auto-Refresh aktiviert. Tickets werden automatisch alle 30 Sekunden aktualisiert.", 
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                autoRefreshTimer.Stop();
            }
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = new TicketDashboard();
            dashboard.Show();
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
                    MessageBox.Show($"CSV-Export erfolgreich!\n\nDatei: {saveDialog.FileName}", 
                        "Export", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Datei öffnen
                    var result = MessageBox.Show("Möchten Sie die Datei jetzt öffnen?", 
                        "Datei öffnen", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim CSV-Export:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
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

                    MessageBox.Show($"PDF-Export erfolgreich!\n\n{tickets.Count} Tickets exportiert.\n\nDatei: {saveDialog.FileName}", 
                        "Export", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Datei öffnen
                    var result = MessageBox.Show("Möchten Sie das PDF jetzt öffnen?", 
                        "PDF öffnen", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim PDF-Export:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(string filename)
        {
            var csv = new StringBuilder();

            // Header
            csv.AppendLine("Ticket#;Erstellt;Kunde;E-Mail;Telefon;Betreff;Beschreibung;Kategorie;Priorität;Status;Zugewiesen;Gelöst am");

            // Daten
            var tickets = TicketsDataGrid.ItemsSource as List<Ticket> ?? allTickets;
            foreach (var ticket in tickets)
            {
                csv.AppendLine(string.Join(";",
                    EscapeCsv(ticket.TicketNumber),
                    ticket.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                    EscapeCsv(ticket.CustomerName),
                    EscapeCsv(ticket.CustomerEmail),
                    EscapeCsv(ticket.CustomerPhone),
                    EscapeCsv(ticket.Subject),
                    EscapeCsv(ticket.Description),
                    ticket.CategoryText,
                    ticket.PriorityText,
                    ticket.StatusText,
                    EscapeCsv(ticket.AssignedToEmployeeName),
                    ticket.ResolvedAt?.ToString("dd.MM.yyyy HH:mm") ?? ""
                ));
            }

            File.WriteAllText(filename, csv.ToString(), Encoding.UTF8);
        }

        private string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Ersetze Anführungszeichen und umschließe mit Anführungszeichen wenn nötig
            if (text.Contains(";") || text.Contains("\"") || text.Contains("\n"))
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }
            return text;
        }

        private void TicketsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            bool hasSelection = TicketsDataGrid.SelectedItem != null;
            EditButton.IsEnabled = hasSelection;
            ViewDetailsButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
        }

        private void TicketsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TicketsDataGrid.SelectedItem is Ticket)
            {
                ViewDetails_Click(sender, null);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            autoRefreshTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
