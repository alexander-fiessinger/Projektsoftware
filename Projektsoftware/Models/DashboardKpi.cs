using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Projektsoftware.Models
{
    /// <summary>
    /// KPI-Widget für Dashboard-Anzeige
    /// </summary>
    [Table("dashboard_kpis")]
    public class DashboardKpi
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("kpi_type")]
        [Required]
        [MaxLength(50)]
        public string KpiType { get; set; } = string.Empty; // "OpenTickets", "RevenueTrend", "LeadConversion", etc.

        [Column("title")]
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Column("current_value")]
        public decimal CurrentValue { get; set; }

        [Column("previous_value")]
        public decimal? PreviousValue { get; set; }

        [Column("target_value")]
        public decimal? TargetValue { get; set; }

        [Column("unit")]
        [MaxLength(20)]
        public string? Unit { get; set; } // "€", "%", "Stück", etc.

        [Column("trend")]
        [MaxLength(20)]
        public string? Trend { get; set; } // "Up", "Down", "Stable"

        [Column("color")]
        [MaxLength(20)]
        public string? Color { get; set; } // "Green", "Red", "Yellow", "Blue"

        [Column("icon")]
        [MaxLength(50)]
        public string? Icon { get; set; } // Icon-Name für UI

        [Column("time_period")]
        [MaxLength(20)]
        public string? TimePeriod { get; set; } // "Today", "Week", "Month", "Year"

        [Column("calculation_query")]
        public string? CalculationQuery { get; set; } // SQL-Query oder Berechnungslogik

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [Column("is_visible")]
        public bool IsVisible { get; set; } = true;

        [Column("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string FormattedValue => Unit switch
        {
            "€" => $"{CurrentValue:N2} €",
            "%" => $"{CurrentValue:N1}%",
            _ => $"{CurrentValue:N0} {Unit ?? ""}"
        };

        [NotMapped]
        public double PercentageChange
        {
            get
            {
                if (PreviousValue == null || PreviousValue == 0) return 0;
                return (double)((CurrentValue - PreviousValue.Value) / PreviousValue.Value * 100);
            }
        }
    }
}
