using System.Collections.Generic;

namespace Projektsoftware.Models
{
    public class DashboardStats
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int TotalTasks { get; set; }
        public int OpenTasks { get; set; }
        public int CompletedTasks { get; set; }
        public decimal TotalHoursLogged { get; set; }
        public decimal TotalBudget { get; set; }
        public int UpcomingMeetings { get; set; }
        public int OverdueTasks { get; set; }
        public int ActiveEmployees { get; set; }

        // Finanzielle Übersicht (Easybill)
        public decimal TotalRevenuePaid { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public int OpenInvoicesCount { get; set; }
        public decimal OpenInvoicesAmount { get; set; }
        public int OverdueInvoicesCount { get; set; }
        public decimal OverdueInvoicesAmount { get; set; }
        public int DraftInvoicesCount { get; set; }
        public bool IsFinancialDataLoaded { get; set; }

        // Einkauf
        public int OpenPurchaseOrdersCount { get; set; }
        public int TotalPurchaseDocumentsCount { get; set; }
        public int SyncedPurchaseDocumentsCount { get; set; }
        public bool EasybillConfigured { get; set; }

        // Budget-Auslastung pro Projekt
        public List<ProjectBudgetStat> TopBudgetProjects { get; set; } = new();
    }

    public class ProjectBudgetStat
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public decimal LoggedHours { get; set; }
        /// <summary>Logged hours expressed as percentage of budgeted hours (Budget / DefaultHourlyRate).</summary>
        public decimal BudgetUsagePercent { get; set; }
    }
}
