using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Service für Dashboard KPI-Berechnung
    /// </summary>
    public class KpiService
    {
        private readonly DatabaseService _databaseService;

        public KpiService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Aktualisiert alle Dashboard KPIs
        /// </summary>
        public async Task UpdateAllKpisAsync()
        {
            try
            {
                await UpdateOpenTicketsKpiAsync();
                await UpdateActiveProjectsKpiAsync();
                await UpdateLeadConversionKpiAsync();
                await UpdateOverdueTasksKpiAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei KPI-Update: {ex.Message}");
            }
        }

        /// <summary>
        /// KPI: Offene Tickets
        /// </summary>
        private async Task UpdateOpenTicketsKpiAsync()
        {
            try
            {
                var tickets = await _databaseService.GetAllTicketsAsync();
                var openTickets = tickets.Count(t =>
                    t.Status != TicketStatus.Resolved &&
                    t.Status != TicketStatus.Closed);

                var kpi = new DashboardKpi
                {
                    KpiType = "OpenTickets",
                    Title = "Offene Tickets",
                    CurrentValue = openTickets,
                    Unit = "Stk.",
                    Icon = "🎫",
                    Color = openTickets > 10 ? "Red" : openTickets > 5 ? "Orange" : "Green",
                    DisplayOrder = 1,
                    IsVisible = true,
                    LastUpdated = DateTime.Now
                };

                await _databaseService.SaveDashboardKpiAsync(kpi);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Tickets-KPI: {ex.Message}");
            }
        }

        /// <summary>
        /// KPI: Aktive Projekte
        /// </summary>
        private async Task UpdateActiveProjectsKpiAsync()
        {
            try
            {
                var projects = await _databaseService.GetAllProjectsAsync();
                var activeProjects = projects.Count(p =>
                    p.Status != "Abgeschlossen" &&
                    p.Status != "Beendet");

                var kpi = new DashboardKpi
                {
                    KpiType = "ActiveProjects",
                    Title = "Aktive Projekte",
                    CurrentValue = activeProjects,
                    Unit = "Stk.",
                    Icon = "📁",
                    Color = activeProjects > 5 ? "Orange" : "Green",
                    DisplayOrder = 2,
                    IsVisible = true,
                    LastUpdated = DateTime.Now
                };

                await _databaseService.SaveDashboardKpiAsync(kpi);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Projekte-KPI: {ex.Message}");
            }
        }

        /// <summary>
        /// KPI: Lead Conversion Rate
        /// </summary>
        private async Task UpdateLeadConversionKpiAsync()
        {
            try
            {
                var leads = await _databaseService.GetSalesLeadsAsync();
                var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

                var thisMonthLeads = leads.Where(l => l.CreatedAt >= thisMonthStart).ToList();
                var convertedLeads = thisMonthLeads.Count(l => l.Status == LeadStatus.Qualifiziert);

                decimal conversionRate = thisMonthLeads.Count > 0
                    ? (decimal)convertedLeads / thisMonthLeads.Count * 100
                    : 0;

                var kpi = new DashboardKpi
                {
                    KpiType = "LeadConversion",
                    Title = "Lead Conversion Rate (Monat)",
                    CurrentValue = conversionRate,
                    Unit = "%",
                    Icon = "📈",
                    Color = conversionRate > 30 ? "Green" : conversionRate > 15 ? "Orange" : "Red",
                    DisplayOrder = 3,
                    IsVisible = true,
                    LastUpdated = DateTime.Now
                };

                await _databaseService.SaveDashboardKpiAsync(kpi);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Lead-KPI: {ex.Message}");
            }
        }

        /// <summary>
        /// KPI: Überfällige Aufgaben
        /// </summary>
        private async Task UpdateOverdueTasksKpiAsync()
        {
            try
            {
                var tasks = await _databaseService.GetAllTasksAsync();
                var overdueTasks = tasks.Count(t =>
                    t.DueDate.HasValue &&
                    t.DueDate.Value < DateTime.Now &&
                    t.Status != "Erledigt");

                var kpi = new DashboardKpi
                {
                    KpiType = "OverdueTasks",
                    Title = "Überfällige Aufgaben",
                    CurrentValue = overdueTasks,
                    Unit = "Stk.",
                    Icon = "⚠️",
                    Color = overdueTasks > 5 ? "Red" : overdueTasks > 0 ? "Orange" : "Green",
                    DisplayOrder = 4,
                    IsVisible = true,
                    LastUpdated = DateTime.Now
                };

                await _databaseService.SaveDashboardKpiAsync(kpi);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Überfällige-Aufgaben-KPI: {ex.Message}");
            }
        }

        /// <summary>
        /// Berechnet Lead-Statistiken für einen Zeitraum
        /// </summary>
        public async Task<LeadStatistics?> CalculateLeadStatisticsAsync(DateTime periodStart, DateTime periodEnd)
        {
            try
            {
                var leads = await _databaseService.GetSalesLeadsAsync();
                var periodLeads = leads.Where(l => l.CreatedAt >= periodStart && l.CreatedAt <= periodEnd).ToList();

                var totalLeads = periodLeads.Count;
                var convertedLeads = periodLeads.Count(l => l.Status == LeadStatus.Qualifiziert);
                var lostLeads = periodLeads.Count(l => l.Status == LeadStatus.Abgelehnt);
                var activeLeads = periodLeads.Count(l => l.Status != LeadStatus.Qualifiziert && l.Status != LeadStatus.Abgelehnt);

                decimal conversionRate = totalLeads > 0 ? (decimal)convertedLeads / totalLeads * 100 : 0;

                return new LeadStatistics
                {
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    TotalLeads = totalLeads,
                    ConvertedLeads = convertedLeads,
                    LostLeads = lostLeads,
                    ActiveLeads = activeLeads,
                    ConversionRate = conversionRate,
                    CreatedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Lead-Statistiken: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Erstellt Fälligkeits-Warnungen
        /// </summary>
        public async Task<List<DueDateWarning>> GenerateDueDateWarningsAsync()
        {
            var warnings = new List<DueDateWarning>();

            try
            {
                // Aufgaben mit Fälligkeitsdatum
                var tasks = await _databaseService.GetAllTasksAsync();
                var taskWarnings = tasks
                    .Where(t => t.DueDate.HasValue && t.Status != "Erledigt")
                    .Select(t => new DueDateWarning
                    {
                        EntityType = "Task",
                        EntityId = t.Id,
                        EntityTitle = t.Title ?? "Unbekannt",
                        DueDate = t.DueDate!.Value,
                        AssignedTo = t.AssignedTo,
                        Priority = t.Priority ?? "Normal",
                        WarningLevel = GetWarningLevel(t.DueDate!.Value),
                        CreatedAt = DateTime.Now
                    });
                warnings.AddRange(taskWarnings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Fälligkeits-Warnungen: {ex.Message}");
            }

            return warnings.OrderBy(w => w.DueDate).ToList();
        }

        private string GetWarningLevel(DateTime dueDate)
        {
            var daysUntilDue = (dueDate - DateTime.Now).TotalDays;

            if (daysUntilDue < 0) return "Überfällig";
            if (daysUntilDue <= 1) return "Kritisch";
            if (daysUntilDue <= 3) return "Warnung";
            if (daysUntilDue <= 7) return "Info";
            return "Normal";
        }
    }
}
