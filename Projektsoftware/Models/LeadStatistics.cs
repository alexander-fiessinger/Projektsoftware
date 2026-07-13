using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Lead-Statistiken für Conversion-Rate und Quellen-Auswertung
    /// </summary>
    [Table("lead_statistics")]
    public class LeadStatistics
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("period_start")]
        public DateTime PeriodStart { get; set; }

        [Column("period_end")]
        public DateTime PeriodEnd { get; set; }

        [Column("total_leads")]
        public int TotalLeads { get; set; }

        [Column("converted_leads")]
        public int ConvertedLeads { get; set; }

        [Column("lost_leads")]
        public int LostLeads { get; set; }

        [Column("active_leads")]
        public int ActiveLeads { get; set; }

        [Column("conversion_rate")]
        public decimal ConversionRate { get; set; }

        [Column("average_conversion_time_days")]
        public decimal? AverageConversionTimeDays { get; set; }

        [Column("total_revenue")]
        public decimal TotalRevenue { get; set; }

        [Column("average_deal_value")]
        public decimal AverageDeadValue { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string PeriodName => $"{PeriodStart:dd.MM.yyyy} - {PeriodEnd:dd.MM.yyyy}";

        [NotMapped]
        public string ConversionRateFormatted => $"{ConversionRate:N1}%";
    }

    /// <summary>
    /// Lead-Quelle mit Auswertung
    /// </summary>
    [Table("lead_sources")]
    public class LeadSource
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("source_name")]
        [Required]
        [MaxLength(100)]
        public string SourceName { get; set; } = string.Empty; // "Website", "Referral", "Cold Call", "Social Media", etc.

        [Column("total_leads")]
        public int TotalLeads { get; set; }

        [Column("converted_leads")]
        public int ConvertedLeads { get; set; }

        [Column("conversion_rate")]
        public decimal ConversionRate { get; set; }

        [Column("total_revenue")]
        public decimal TotalRevenue { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string ConversionRateFormatted => $"{ConversionRate:N1}%";
    }

    /// <summary>
    /// Fälligkeits-Warnung für Dashboard
    /// </summary>
    [Table("due_date_warnings")]
    public class DueDateWarning
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("entity_type")]
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty; // "Task", "Ticket", "Lead", "Project"

        [Column("entity_id")]
        public int EntityId { get; set; }

        [Column("entity_title")]
        [MaxLength(200)]
        public string? EntityTitle { get; set; }

        [Column("due_date")]
        public DateTime DueDate { get; set; }

        [Column("assigned_to")]
        [MaxLength(100)]
        public string? AssignedTo { get; set; }

        [Column("priority")]
        [MaxLength(20)]
        public string? Priority { get; set; }

        [Column("warning_level")]
        [MaxLength(20)]
        public string? WarningLevel { get; set; } // "Overdue", "DueToday", "DueTomorrow", "DueThisWeek"

        [Column("is_dismissed")]
        public bool IsDismissed { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public bool IsOverdue => DueDate < DateTime.Now;

        [NotMapped]
        public string DueDateFormatted
        {
            get
            {
                if (IsOverdue)
                    return $"Überfällig seit {(DateTime.Now - DueDate).Days} Tagen";

                var span = DueDate - DateTime.Now;
                if (span.TotalHours < 24)
                    return $"Heute fällig";
                if (span.TotalDays < 2)
                    return "Morgen fällig";
                if (span.TotalDays < 7)
                    return $"In {(int)span.TotalDays} Tagen";

                return DueDate.ToString("dd.MM.yyyy");
            }
        }
    }
}
