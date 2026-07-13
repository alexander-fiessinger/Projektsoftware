using System;
using System.Linq;
using System.Windows;
using Projektsoftware.Models;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class LeadStatisticsDialog : Window
    {
        private readonly DatabaseService _db = new DatabaseService();
        private readonly KpiService _kpi;

        public LeadStatisticsDialog()
        {
            InitializeComponent();
            _kpi = new KpiService(_db);
            Loaded += async (_, __) => await LoadAsync();
        }

        public class SourceStat
        {
            public string Source { get; set; } = "";
            public int Total { get; set; }
            public int Won { get; set; }
            public decimal ConversionPercent => Total > 0 ? (decimal)Won / Total * 100m : 0;
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var end = DateTime.Now;
                var start = end.AddDays(-90);
                var stats = await _kpi.CalculateLeadStatisticsAsync(start, end);
                var leads = (await _db.GetSalesLeadsAsync())
                    .Where(l => l.CreatedAt >= start && l.CreatedAt <= end)
                    .ToList();

                TotalText.Text = (stats?.TotalLeads ?? leads.Count).ToString("N0");
                WonText.Text = (stats?.ConvertedLeads ?? leads.Count(l => l.Status == LeadStatus.Qualifiziert)).ToString("N0");
                LostText.Text = (stats?.LostLeads ?? leads.Count(l => l.Status == LeadStatus.Abgelehnt)).ToString("N0");
                var conv = stats?.ConversionRate ?? (leads.Count > 0
                    ? (decimal)leads.Count(l => l.Status == LeadStatus.Qualifiziert) / leads.Count * 100m : 0);
                ConversionText.Text = $"{conv:N1} %";

                var sources = leads
                    .GroupBy(l => string.IsNullOrWhiteSpace(l.Source) ? "Unbekannt" : l.Source)
                    .Select(g => new SourceStat
                    {
                        Source = g.Key,
                        Total = g.Count(),
                        Won = g.Count(l => l.Status == LeadStatus.Qualifiziert)
                    })
                    .OrderByDescending(s => s.Total)
                    .ToList();
                SourcesGrid.ItemsSource = sources;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
