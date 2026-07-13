using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Projektsoftware.Models
{
    /// <summary>
    /// SLA-Regel für Ticket-Bearbeitung
    /// </summary>
    [Table("sla_rules")]
    public class SlaRule
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("priority")]
        [MaxLength(20)]
        public string? Priority { get; set; } // "Low", "Normal", "High", "Critical"

        [Column("ticket_category")]
        [MaxLength(50)]
        public string? TicketCategory { get; set; }

        [Column("first_response_minutes")]
        public int FirstResponseMinutes { get; set; } = 60; // Zeit bis zur ersten Reaktion

        [Column("resolution_time_hours")]
        public int ResolutionTimeHours { get; set; } = 24; // Zeit bis zur Lösung

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("escalation_email")]
        [MaxLength(200)]
        public string? EscalationEmail { get; set; }

        [Column("business_hours_only")]
        public bool BusinessHoursOnly { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string FirstResponseFormatted
        {
            get
            {
                if (FirstResponseMinutes < 60)
                    return $"{FirstResponseMinutes} Min.";
                return $"{FirstResponseMinutes / 60} Std.";
            }
        }

        [NotMapped]
        public string ResolutionTimeFormatted
        {
            get
            {
                if (ResolutionTimeHours < 24)
                    return $"{ResolutionTimeHours} Std.";
                return $"{ResolutionTimeHours / 24} Tage";
            }
        }
    }

    /// <summary>
    /// SLA-Status für einzelnes Ticket
    /// </summary>
    [Table("ticket_sla_status")]
    public class TicketSlaStatus
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("ticket_id")]
        public int TicketId { get; set; }

        [Column("sla_rule_id")]
        public int? SlaRuleId { get; set; }

        [Column("first_response_due")]
        public DateTime? FirstResponseDue { get; set; }

        [Column("first_response_at")]
        public DateTime? FirstResponseAt { get; set; }

        [Column("resolution_due")]
        public DateTime? ResolutionDue { get; set; }

        [Column("resolved_at")]
        public DateTime? ResolvedAt { get; set; }

        [Column("is_breached")]
        public bool IsBreached { get; set; } = false;

        [Column("breach_type")]
        [MaxLength(50)]
        public string? BreachType { get; set; } // "FirstResponse", "Resolution"

        [Column("escalation_level")]
        public int EscalationLevel { get; set; } = 0; // 0=None, 1=Warning, 2=Critical, 3=Escalated

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string StatusColor
        {
            get
            {
                if (IsBreached) return "Red";
                if (EscalationLevel >= 2) return "Orange";
                if (EscalationLevel >= 1) return "Yellow";
                return "Green";
            }
        }

        [NotMapped]
        public bool IsFirstResponseOverdue => FirstResponseDue.HasValue && !FirstResponseAt.HasValue && DateTime.Now > FirstResponseDue.Value;

        [NotMapped]
        public bool IsResolutionOverdue => ResolutionDue.HasValue && !ResolvedAt.HasValue && DateTime.Now > ResolutionDue.Value;

        [NotMapped]
        public TimeSpan? RemainingTime
        {
            get
            {
                if (ResolvedAt.HasValue) return null;

                DateTime? targetDate = null;
                if (!FirstResponseAt.HasValue && FirstResponseDue.HasValue)
                    targetDate = FirstResponseDue.Value;
                else if (ResolutionDue.HasValue)
                    targetDate = ResolutionDue.Value;

                if (targetDate.HasValue)
                    return targetDate.Value - DateTime.Now;

                return null;
            }
        }

        [NotMapped]
        public string RemainingTimeFormatted
        {
            get
            {
                var remaining = RemainingTime;
                if (!remaining.HasValue) return "—";

                if (remaining.Value.TotalMinutes < 0)
                    return $"Überfällig: {Math.Abs(remaining.Value.TotalHours):N1} Std.";

                if (remaining.Value.TotalHours < 1)
                    return $"{remaining.Value.Minutes} Min. verbleibend";

                if (remaining.Value.TotalDays < 1)
                    return $"{remaining.Value.Hours} Std. verbleibend";

                return $"{remaining.Value.Days} Tage verbleibend";
            }
        }
    }
}
