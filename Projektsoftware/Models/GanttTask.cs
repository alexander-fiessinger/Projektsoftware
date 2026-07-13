using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Gantt-Aufgabe für Projekt-Zeitstrahl
    /// </summary>
    [Table("gantt_tasks")]
    public class GanttTask
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("project_id")]
        public int ProjectId { get; set; }

        [Column("task_name")]
        [Required]
        [MaxLength(200)]
        public string TaskName { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("actual_start_date")]
        public DateTime? ActualStartDate { get; set; }

        [Column("actual_end_date")]
        public DateTime? ActualEndDate { get; set; }

        [Column("progress_percentage")]
        public int ProgressPercentage { get; set; } = 0;

        [Column("assigned_to")]
        [MaxLength(100)]
        public string? AssignedTo { get; set; }

        [Column("parent_task_id")]
        public int? ParentTaskId { get; set; }

        [Column("is_milestone")]
        public bool IsMilestone { get; set; } = false;

        [Column("dependencies")]
        [MaxLength(200)]
        public string? Dependencies { get; set; } // Comma-separated task IDs

        [Column("priority")]
        [MaxLength(20)]
        public string? Priority { get; set; }

        [Column("status")]
        [MaxLength(50)]
        public string? Status { get; set; } // "NotStarted", "InProgress", "Completed", "Delayed", "OnHold"

        [Column("color")]
        [MaxLength(20)]
        public string? Color { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public int DurationDays => (EndDate - StartDate).Days;

        [NotMapped]
        public bool IsOverdue => DateTime.Now > EndDate && ProgressPercentage < 100;

        [NotMapped]
        public bool IsDelayed => ActualEndDate.HasValue && ActualEndDate > EndDate;

        [NotMapped]
        public string StatusColor
        {
            get
            {
                if (Status == "Completed") return "Green";
                if (Status == "OnHold") return "Gray";
                if (IsOverdue || Status == "Delayed") return "Red";
                if (Status == "InProgress") return "Blue";
                return "LightGray";
            }
        }

        [NotMapped]
        public List<int> DependencyList
        {
            get
            {
                if (string.IsNullOrEmpty(Dependencies))
                    return new List<int>();

                var result = new List<int>();
                foreach (var id in Dependencies.Split(','))
                {
                    if (int.TryParse(id.Trim(), out var taskId))
                        result.Add(taskId);
                }
                return result;
            }
        }
    }

    /// <summary>
    /// Budget-Eintrag für Projekt-Tracking
    /// </summary>
    [Table("project_budget_entries")]
    public class BudgetEntry
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("project_id")]
        public int ProjectId { get; set; }

        [Column("category")]
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty; // "Labor", "Materials", "Equipment", "Software", "Other"

        [Column("description")]
        [MaxLength(300)]
        public string? Description { get; set; }

        [Column("planned_amount")]
        public decimal PlannedAmount { get; set; }

        [Column("actual_amount")]
        public decimal ActualAmount { get; set; }

        [Column("planned_hours")]
        public decimal? PlannedHours { get; set; }

        [Column("actual_hours")]
        public decimal? ActualHours { get; set; }

        [Column("cost_per_hour")]
        public decimal? CostPerHour { get; set; }

        [Column("entry_date")]
        public DateTime EntryDate { get; set; } = DateTime.Now;

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public decimal Variance => ActualAmount - PlannedAmount;

        [NotMapped]
        public decimal VariancePercentage
        {
            get
            {
                if (PlannedAmount == 0) return 0;
                return (Variance / PlannedAmount) * 100;
            }
        }

        [NotMapped]
        public string VarianceFormatted
        {
            get
            {
                var sign = Variance >= 0 ? "+" : "";
                return $"{sign}{Variance:N2} € ({sign}{VariancePercentage:N1}%)";
            }
        }

        [NotMapped]
        public string StatusColor
        {
            get
            {
                if (Variance == 0) return "Green";
                if (Variance < 0) return "Green"; // Under budget
                if (VariancePercentage > 20) return "Red"; // Over budget by more than 20%
                if (VariancePercentage > 10) return "Orange";
                return "Yellow";
            }
        }
    }

    /// <summary>
    /// Projekt-Budget-Übersicht
    /// </summary>
    [Table("project_budgets")]
    public class ProjectBudget
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("project_id")]
        public int ProjectId { get; set; }

        [Column("total_planned_budget")]
        public decimal TotalPlannedBudget { get; set; }

        [Column("total_actual_budget")]
        public decimal TotalActualBudget { get; set; }

        [Column("total_planned_hours")]
        public decimal TotalPlannedHours { get; set; }

        [Column("total_actual_hours")]
        public decimal TotalActualHours { get; set; }

        [Column("currency")]
        [MaxLength(10)]
        public string Currency { get; set; } = "EUR";

        [Column("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [NotMapped]
        public decimal RemainingBudget => TotalPlannedBudget - TotalActualBudget;

        [NotMapped]
        public decimal BudgetUtilizationPercentage
        {
            get
            {
                if (TotalPlannedBudget == 0) return 0;
                return (TotalActualBudget / TotalPlannedBudget) * 100;
            }
        }

        [NotMapped]
        public string BudgetStatusColor
        {
            get
            {
                var percentage = BudgetUtilizationPercentage;
                if (percentage <= 75) return "Green";
                if (percentage <= 90) return "Yellow";
                if (percentage <= 100) return "Orange";
                return "Red";
            }
        }

        [NotMapped]
        public string BudgetStatusText
        {
            get
            {
                var percentage = BudgetUtilizationPercentage;
                if (percentage <= 75) return "Im Rahmen";
                if (percentage <= 90) return "Achtung";
                if (percentage <= 100) return "Limit erreicht";
                return "Budget überschritten";
            }
        }
    }
}
