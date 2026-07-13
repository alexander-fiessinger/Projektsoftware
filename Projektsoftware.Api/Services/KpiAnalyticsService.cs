using Projektsoftware.Api.Models;

namespace Projektsoftware.Api.Services;

/// <summary>
/// Berechnet Dashboard-KPIs, Lead-Statistiken, Fälligkeits-Warnungen und Ausgaben-Analysen
/// live aus den vorhandenen Daten der Datenbank. Portiert aus dem WPF-KpiService.
/// </summary>
public class KpiAnalyticsService
{
    private readonly ApiDatabaseService _db;

    public KpiAnalyticsService(ApiDatabaseService db) => _db = db;

    public async Task<List<DashboardKpiDto>> GetKpisAsync()
    {
        var kpis = new List<DashboardKpiDto>();

        var tickets = await _db.GetTicketsAsync();
        var openTickets = tickets.Count(t => t.Status != 4 && t.Status != 5);
        kpis.Add(new DashboardKpiDto
        {
            KpiType = "OpenTickets",
            Title = "Offene Tickets",
            CurrentValue = openTickets,
            Unit = "Stk.",
            Icon = "🎫",
            Color = openTickets > 10 ? "Red" : openTickets > 5 ? "Orange" : "Green"
        });

        var projects = await _db.GetProjectsAsync();
        var activeProjects = projects.Count(p =>
            !p.Status.Equals("Abgeschlossen", StringComparison.OrdinalIgnoreCase) &&
            !p.Status.Equals("Beendet", StringComparison.OrdinalIgnoreCase));
        kpis.Add(new DashboardKpiDto
        {
            KpiType = "ActiveProjects",
            Title = "Aktive Projekte",
            CurrentValue = activeProjects,
            Unit = "Stk.",
            Icon = "📁",
            Color = activeProjects > 5 ? "Orange" : "Green"
        });

        var leads = await _db.GetSalesLeadsAsync();
        var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var thisMonthLeads = leads.Where(l => l.CreatedAt >= thisMonthStart).ToList();
        var convertedLeads = thisMonthLeads.Count(l => l.Status == 2);
        decimal conversionRate = thisMonthLeads.Count > 0
            ? (decimal)convertedLeads / thisMonthLeads.Count * 100
            : 0;
        kpis.Add(new DashboardKpiDto
        {
            KpiType = "LeadConversion",
            Title = "Lead-Conversion (Monat)",
            CurrentValue = Math.Round(conversionRate, 1),
            Unit = "%",
            Icon = "💼",
            Color = conversionRate >= 30 ? "Green" : conversionRate >= 15 ? "Orange" : "Red"
        });

        var tasks = await _db.GetTasksAsync();
        var overdueTasks = tasks.Count(t =>
            t.DueDate.HasValue &&
            t.DueDate.Value.Date < DateTime.Now.Date &&
            !t.Status.Equals("Erledigt", StringComparison.OrdinalIgnoreCase) &&
            !t.Status.Equals("Abgeschlossen", StringComparison.OrdinalIgnoreCase));
        kpis.Add(new DashboardKpiDto
        {
            KpiType = "OverdueTasks",
            Title = "Überfällige Aufgaben",
            CurrentValue = overdueTasks,
            Unit = "Stk.",
            Icon = "⏰",
            Color = overdueTasks > 5 ? "Red" : overdueTasks > 0 ? "Orange" : "Green"
        });

        return kpis;
    }

    public async Task<LeadStatisticsDto> GetLeadStatisticsAsync(DateTime periodStart, DateTime periodEnd)
    {
        var allLeads = await _db.GetSalesLeadsAsync();
        var leads = allLeads.Where(l => l.CreatedAt >= periodStart && l.CreatedAt <= periodEnd).ToList();

        var converted = leads.Count(l => l.Status == 2);
        var lost = leads.Count(l => l.Status == 3);
        var active = leads.Count(l => l.Status == 0 || l.Status == 1);

        var stats = new LeadStatisticsDto
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalLeads = leads.Count,
            ConvertedLeads = converted,
            LostLeads = lost,
            ActiveLeads = active,
            ConversionRate = leads.Count > 0 ? (decimal)converted / leads.Count * 100 : 0
        };

        stats.Sources = leads
            .GroupBy(l => string.IsNullOrWhiteSpace(l.Source) ? "Unbekannt" : l.Source)
            .Select(g =>
            {
                var total = g.Count();
                var conv = g.Count(x => x.Status == 2);
                return new LeadSourceStatDto
                {
                    SourceName = g.Key,
                    TotalLeads = total,
                    ConvertedLeads = conv,
                    ConversionRate = total > 0 ? (decimal)conv / total * 100 : 0
                };
            })
            .OrderByDescending(s => s.TotalLeads)
            .ToList();

        return stats;
    }

    public async Task<List<DueDateWarningDto>> GetDueDateWarningsAsync()
    {
        var warnings = new List<DueDateWarningDto>();
        var today = DateTime.Now.Date;

        var tasks = await _db.GetTasksAsync();
        foreach (var t in tasks)
        {
            if (!t.DueDate.HasValue) continue;
            if (t.Status.Equals("Erledigt", StringComparison.OrdinalIgnoreCase) ||
                t.Status.Equals("Abgeschlossen", StringComparison.OrdinalIgnoreCase)) continue;

            var level = ClassifyDueDate(t.DueDate.Value.Date, today);
            if (level is null) continue;

            warnings.Add(new DueDateWarningDto
            {
                EntityType = "Task",
                EntityId = t.Id,
                EntityTitle = t.Title,
                DueDate = t.DueDate.Value,
                AssignedTo = t.AssignedTo,
                Priority = t.Priority,
                WarningLevel = level
            });
        }

        return warnings
            .OrderBy(w => w.DueDate)
            .ToList();
    }

    private static string? ClassifyDueDate(DateTime due, DateTime today)
    {
        if (due < today) return "Overdue";
        if (due == today) return "DueToday";
        if (due == today.AddDays(1)) return "DueTomorrow";
        if (due <= today.AddDays(7)) return "DueThisWeek";
        return null;
    }

    public async Task<ExpenseAnalysisDto> GetExpenseAnalysisAsync(DateTime periodStart, DateTime periodEnd)
    {
        var allOrders = await _db.GetPurchaseOrdersAsync();
        var orders = allOrders
            .Where(o => o.OrderDate >= periodStart && o.OrderDate <= periodEnd &&
                        !o.Status.Equals("Storniert", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var total = orders.Sum(o => o.TotalGross);

        var analysis = new ExpenseAnalysisDto
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalExpenses = total,
            TotalOrders = orders.Count,
            AverageOrderValue = orders.Count > 0 ? total / orders.Count : 0
        };

        analysis.BySupplier = orders
            .GroupBy(o => string.IsNullOrWhiteSpace(o.SupplierName) ? "Unbekannt" : o.SupplierName)
            .Select(g =>
            {
                var amount = g.Sum(o => o.TotalGross);
                return new ExpenseBySupplierDto
                {
                    SupplierName = g.Key,
                    TotalAmount = amount,
                    OrderCount = g.Count(),
                    Percentage = total > 0 ? amount / total * 100 : 0
                };
            })
            .OrderByDescending(s => s.TotalAmount)
            .ToList();

        analysis.ByMonth = orders
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new ExpenseByMonthDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalAmount = g.Sum(o => o.TotalGross),
                OrderCount = g.Count()
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();

        return analysis;
    }
}
