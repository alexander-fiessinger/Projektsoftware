using System;
using System.Windows;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class SlaMonitoringDialog : Window
    {
        private readonly SlaMonitoringService _slaService;

        public SlaMonitoringDialog()
        {
            InitializeComponent();
            var db = new DatabaseService();
            _slaService = new SlaMonitoringService(db);
            Loaded += async (_, __) => await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var summary = await _slaService.GetSlaSummaryAsync();
                TotalText.Text = summary.TotalTickets.ToString("N0");
                WarningText.Text = summary.WarningTickets.ToString("N0");
                BreachedText.Text = summary.BreachedTickets.ToString("N0");
                OkText.Text = summary.HealthyTickets.ToString("N0");

                var breached = await _slaService.GetBreachedTicketsAsync();
                BreachedGrid.ItemsSource = breached;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der SLA-Daten:\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Monitor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _slaService.MonitorAllTicketsAsync();
                MessageBox.Show("SLA-Überwachung abgeschlossen.", "SLA-Überwachung",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
