using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Linq;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class TicketDashboard : Window
    {
        private readonly DatabaseService databaseService;

        public TicketDashboard()
        {
            InitializeComponent();
            databaseService = new DatabaseService();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadStatisticsAsync();
        }

        private async System.Threading.Tasks.Task LoadStatisticsAsync()
        {
            try
            {
                var stats = await databaseService.GetTicketStatisticsAsync();

                // Übersicht
                TotalTicketsText.Text = stats.TotalTickets.ToString();
                NewTicketsText.Text = stats.NewTickets.ToString();
                InProgressText.Text = stats.InProgressTickets.ToString();
                UrgentText.Text = stats.UrgentTickets.ToString();
                ResolvedText.Text = stats.ResolvedTickets.ToString();
                WaitingText.Text = stats.WaitingTickets.ToString();
                UnassignedText.Text = stats.UnassignedTickets.ToString();
                ClosedText.Text = stats.ClosedTickets.ToString();

                // Zeitliche Übersicht
                TodayText.Text = stats.TodayTickets.ToString();
                WeekText.Text = stats.WeekTickets.ToString();
                MonthText.Text = stats.MonthTickets.ToString();

                // Performance
                if (stats.AverageResolutionTimeHours > 0)
                {
                    AvgTimeText.Text = stats.AverageResolutionTimeText;
                }
                else
                {
                    AvgTimeText.Text = "Keine gelösten Tickets";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Statistiken:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadStatisticsAsync();
        }

        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Datei (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*",
                    FileName = $"Dashboard_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Statistiken und letzte Tickets laden
                    var stats = await databaseService.GetTicketStatisticsAsync();
                    var recentTickets = await databaseService.GetAllTicketsAsync();
                    recentTickets = recentTickets.OrderByDescending(t => t.CreatedAt).Take(20).ToList();

                    var pdfService = new PdfExportService();
                    pdfService.ExportStatisticsToPdf(stats, recentTickets, saveDialog.FileName);

                    MessageBox.Show($"Dashboard-Report erfolgreich als PDF exportiert!\n\nDatei: {saveDialog.FileName}", 
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

        private void ShowAllTickets_Click(object sender, RoutedEventArgs e)
        {
            var ticketDialog = new TicketManagementDialog();
            ticketDialog.Show();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
