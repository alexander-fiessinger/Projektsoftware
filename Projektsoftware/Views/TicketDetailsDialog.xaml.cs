using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Projektsoftware.Views
{
    public partial class TicketDetailsDialog : Window
    {
        private readonly DatabaseService db;
        private readonly Ticket ticket;
        private int currentEmployeeId = 1; // TODO: Aktuellen Benutzer ermitteln

        public TicketDetailsDialog(Ticket ticket)
        {
            InitializeComponent();
            this.ticket = ticket;
            db = new DatabaseService();
            
            // Converter registrieren
            Resources.Add("BoolToColorConverter", new BoolToBackgroundConverter());
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadTicketDataAsync();
            await LoadCommentsAsync();
            await LoadTimeLogsAsync();
        }

        private async System.Threading.Tasks.Task LoadTicketDataAsync()
        {
            // Header
            TicketNumberText.Text = ticket.TicketNumber;
            StatusBadge.Text = ticket.StatusText.ToUpper();
            
            // Status-Farbe
            StatusBadge.Background = ticket.Status switch
            {
                TicketStatus.New => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                TicketStatus.InProgress => new SolidColorBrush(Color.FromRgb(3, 169, 244)),
                TicketStatus.Waiting => new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                TicketStatus.Resolved => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                TicketStatus.Closed => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                _ => new SolidColorBrush(Color.FromRgb(96, 125, 139))
            };

            // Kunde
            CustomerNameText.Text = ticket.CustomerName;
            CustomerEmailText.Text = $"✉️ {ticket.CustomerEmail}";
            CustomerPhoneText.Text = string.IsNullOrEmpty(ticket.CustomerPhone) 
                ? "" 
                : $"📞 {ticket.CustomerPhone}";
            CustomerPhoneText.Visibility = string.IsNullOrEmpty(ticket.CustomerPhone) 
                ? Visibility.Collapsed 
                : Visibility.Visible;

            // Details
            SubjectText.Text = ticket.Subject;
            DescriptionText.Text = ticket.Description;
            PriorityText.Text = ticket.PriorityText;
            CategoryText.Text = ticket.CategoryText;
            AssignedToText.Text = string.IsNullOrEmpty(ticket.AssignedToEmployeeName) 
                ? "Nicht zugewiesen" 
                : ticket.AssignedToEmployeeName;
            ProjectText.Text = string.IsNullOrEmpty(ticket.ProjectName)
                ? "Kein Projekt zugeordnet"
                : ticket.ProjectName;

            // Zeitstempel
            CreatedAtText.Text = $"Erstellt: {ticket.CreatedAt:dd.MM.yyyy HH:mm}";
            UpdatedAtText.Text = ticket.UpdatedAt.HasValue 
                ? $"Aktualisiert: {ticket.UpdatedAt:dd.MM.yyyy HH:mm}" 
                : "";
            UpdatedAtText.Visibility = ticket.UpdatedAt.HasValue 
                ? Visibility.Visible 
                : Visibility.Collapsed;
            ResolvedAtText.Text = ticket.ResolvedAt.HasValue 
                ? $"Gelöst: {ticket.ResolvedAt:dd.MM.yyyy HH:mm}" 
                : "";
            ResolvedAtText.Visibility = ticket.ResolvedAt.HasValue 
                ? Visibility.Visible 
                : Visibility.Collapsed;

            // Gesamtzeit
            var totalMinutes = await db.GetTotalTimeSpentAsync(ticket.Id);
            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;
            TotalTimeText.Text = hours > 0 
                ? $"{hours}h {minutes}min" 
                : $"{minutes}min";
        }

        private async System.Threading.Tasks.Task LoadCommentsAsync()
        {
            var comments = await db.GetTicketCommentsAsync(ticket.Id);
            CommentsListBox.ItemsSource = comments;
        }

        private async System.Threading.Tasks.Task LoadTimeLogsAsync()
        {
            var timeLogs = await db.GetTicketTimeLogsAsync(ticket.Id);
            TimeLogsDataGrid.ItemsSource = timeLogs;
        }

        private async void SaveComment_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewCommentBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Kommentar ein.", 
                    "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var comment = new TicketComment
                {
                    TicketId = ticket.Id,
                    EmployeeId = currentEmployeeId,
                    Comment = NewCommentBox.Text.Trim(),
                    IsInternal = IsInternalCheckBox.IsChecked == true
                };

                await db.AddTicketCommentAsync(comment);
                NewCommentBox.Clear();
                await LoadCommentsAsync();
                
                MessageBox.Show("Kommentar gespeichert.", 
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveTime_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(HoursBox.Text, out int hours) || hours < 0)
                    hours = 0;
                if (!int.TryParse(MinutesBox.Text, out int minutes) || minutes < 0)
                    minutes = 0;

                var totalMinutes = (hours * 60) + minutes;

                if (totalMinutes == 0)
                {
                    MessageBox.Show("Bitte geben Sie eine Zeit größer als 0 ein.", 
                        "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var timeLog = new TicketTimeLog
                {
                    TicketId = ticket.Id,
                    EmployeeId = currentEmployeeId,
                    Description = TimeDescriptionBox.Text.Trim(),
                    MinutesSpent = totalMinutes
                };

                await db.AddTicketTimeLogAsync(timeLog);
                
                // Felder zurücksetzen
                TimeDescriptionBox.Clear();
                HoursBox.Text = "0";
                MinutesBox.Text = "0";
                
                await LoadTimeLogsAsync();
                await LoadTicketDataAsync(); // Gesamtzeit aktualisieren
                
                MessageBox.Show("Zeiterfassung gespeichert.", 
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditTicket_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TicketEditDialog(ticket);
            if (dialog.ShowDialog() == true)
            {
                Window_Loaded(sender, e); // Neu laden
            }
        }

        private void SendEmail_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TicketEmailDialog(ticket);
            dialog.ShowDialog();
        }

        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Datei (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*",
                    FileName = $"Ticket_{ticket.TicketNumber.Replace("#", "")}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Lade alle Daten
                    var comments = await db.GetTicketCommentsAsync(ticket.Id);
                    var timeLogs = await db.GetTicketTimeLogsAsync(ticket.Id);

                    var pdfService = new PdfExportService();
                    pdfService.ExportTicketDetailToPdf(ticket, comments, timeLogs, saveDialog.FileName);

                    MessageBox.Show($"PDF-Export erfolgreich!\n\nDatei: {saveDialog.FileName}", 
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

        private async void ViewCustomerHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var customerTickets = await db.GetTicketsByCustomerEmailAsync(ticket.CustomerEmail);
                
                var message = $"Tickets von {ticket.CustomerName} ({ticket.CustomerEmail}):\n\n";
                message += $"Gesamt: {customerTickets.Count}\n";
                message += $"Neu: {customerTickets.Count(t => t.Status == TicketStatus.New)}\n";
                message += $"In Bearbeitung: {customerTickets.Count(t => t.Status == TicketStatus.InProgress)}\n";
                message += $"Gelöst: {customerTickets.Count(t => t.Status == TicketStatus.Resolved)}\n";
                message += $"Geschlossen: {customerTickets.Count(t => t.Status == TicketStatus.Closed)}\n\n";
                message += "Letzten 5 Tickets:\n";
                
                foreach (var t in customerTickets.Take(5))
                {
                    message += $"• {t.TicketNumber} - {t.Subject} ({t.StatusText})\n";
                }

                MessageBox.Show(message, "Kunden-Historie", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Converter für Kommentar-Hintergrundfarbe
    public class BoolToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInternal = (bool)value;
            return isInternal 
                ? new SolidColorBrush(Color.FromRgb(255, 249, 196)) // Gelb für intern
                : new SolidColorBrush(Color.FromRgb(227, 242, 253)); // Blau für Kunde
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
