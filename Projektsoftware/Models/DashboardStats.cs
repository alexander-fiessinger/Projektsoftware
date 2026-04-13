using System;
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

        // Aktivitätsprotokoll
        public List<ActivityFeedItem> RecentActivities { get; set; } = new();

        // Fälligkeitskalender
        public List<DeadlineItem> UpcomingDeadlines { get; set; } = new();
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

    public class ActivityFeedItem
    {
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;

        public string Icon => EntityType switch
        {
            "Projekt" => "📁",
            "Aufgabe" => "✓",
            "Zeiterfassung" => "⏱",
            "Kunde" => "👤",
            "Mitarbeiter" => "👥",
            _ => "📋"
        };

        public string Display => $"{Icon} {UserName}: {Action} {EntityType}" +
            (string.IsNullOrEmpty(Details) ? "" : $" – {Details}");

        public string TimeAgo
        {
            get
            {
                var diff = DateTime.Now - Timestamp;
                if (diff.TotalMinutes < 1) return "gerade eben";
                if (diff.TotalMinutes < 60) return $"vor {(int)diff.TotalMinutes} Min.";
                if (diff.TotalHours < 24) return $"vor {(int)diff.TotalHours} Std.";
                return $"vor {(int)diff.TotalDays} Tag(en)";
            }
        }
    }

    public class DeadlineItem
    {
        public string Title { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string Type { get; set; } = "Aufgabe"; // Aufgabe or Meilenstein
        public string AssignedTo { get; set; } = string.Empty;

        public int DaysUntilDue => (DueDate.Date - DateTime.Today).Days;
        public string DueDateDisplay => DueDate.ToString("dd.MM.yyyy");
        public string UrgencyDisplay => DaysUntilDue switch
        {
            0 => "⚠ Heute fällig!",
            1 => "Morgen fällig",
            < 0 => $"🔴 {Math.Abs(DaysUntilDue)} Tag(e) überfällig",
            _ => $"in {DaysUntilDue} Tagen"
        };
    }
}
