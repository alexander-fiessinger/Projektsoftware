using System;
using System.Linq;
using System.Windows;
using Projektsoftware.Models;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class KpiDashboardDialog : Window
    {
        private readonly KpiService _kpiService;
        private readonly DatabaseService _db;

        public KpiDashboardDialog()
        {
            InitializeComponent();
            _db = new DatabaseService();
            _kpiService = new KpiService(_db);
            Loaded += async (_, __) => await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                await _kpiService.UpdateAllKpisAsync();

                var tickets = await _db.GetAllTicketsAsync();
                var projects = await _db.GetAllProjectsAsync();
                var tasks = await _db.GetAllTasksAsync();
                var leads = await _db.GetSalesLeadsAsync();

                var openTickets = tickets.Count(t =>
                    t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);
                var activeProjects = projects.Count(p =>
                    p.Status != "Abgeschlossen" && p.Status != "Beendet");
                var overdueTasks = tasks.Count(t =>
                    t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && t.Status != "Erledigt");

                var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var monthLeads = leads.Where(l => l.CreatedAt >= monthStart).ToList();
                var converted = monthLeads.Count(l => l.Status == LeadStatus.Qualifiziert);
                var conversion = monthLeads.Count > 0 ? (decimal)converted / monthLeads.Count * 100m : 0m;

                OpenTicketsText.Text = openTickets.ToString("N0");
                ActiveProjectsText.Text = activeProjects.ToString("N0");
                LeadConversionText.Text = $"{conversion:N1} %";
                OverdueTasksText.Text = overdueTasks.ToString("N0");

                try
                {
                    var stats = await _db.GetDashboardStatsAsync();
                    MonthRevenueText.Text = $"{stats.ThisMonthRevenue:N2} €";
                    TotalRevenueText.Text = $"{stats.TotalRevenuePaid:N2} €";
                }
                catch
                {
                    MonthRevenueText.Text = "n/a";
                    TotalRevenueText.Text = "n/a";
                }

                var warnings = await _kpiService.GenerateDueDateWarningsAsync();
                WarningsGrid.ItemsSource = warnings;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der KPIs:\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
