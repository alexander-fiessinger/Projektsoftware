using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Lieferanten-Bewertung
    /// </summary>
    [Table("supplier_ratings")]
    public class SupplierRating
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("supplier_id")]
        public int SupplierId { get; set; }

        [Column("rating_date")]
        public DateTime RatingDate { get; set; } = DateTime.Now;

        [Column("quality_rating")]
        public int QualityRating { get; set; } = 5; // 1-5 Sterne

        [Column("delivery_rating")]
        public int DeliveryRating { get; set; } = 5; // 1-5 Sterne

        [Column("price_rating")]
        public int PriceRating { get; set; } = 5; // 1-5 Sterne

        [Column("service_rating")]
        public int ServiceRating { get; set; } = 5; // 1-5 Sterne

        [Column("communication_rating")]
        public int CommunicationRating { get; set; } = 5; // 1-5 Sterne

        [Column("overall_rating")]
        public decimal OverallRating { get; set; } = 5.0m;

        [Column("review_text")]
        public string? ReviewText { get; set; }

        [Column("pros")]
        public string? Pros { get; set; }

        [Column("cons")]
        public string? Cons { get; set; }

        [Column("would_recommend")]
        public bool WouldRecommend { get; set; } = true;

        [Column("rated_by")]
        [MaxLength(100)]
        public string? RatedBy { get; set; }

        [Column("order_reference")]
        [MaxLength(100)]
        public string? OrderReference { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string StarsDisplay => new string('⭐', (int)Math.Round(OverallRating));

        [NotMapped]
        public string RatingColor
        {
            get
            {
                if (OverallRating >= 4.0m) return "Green";
                if (OverallRating >= 3.0m) return "Yellow";
                if (OverallRating >= 2.0m) return "Orange";
                return "Red";
            }
        }

        public void CalculateOverallRating()
        {
            OverallRating = (QualityRating + DeliveryRating + PriceRating + ServiceRating + CommunicationRating) / 5.0m;
        }
    }

    /// <summary>
    /// Ausgaben-Auswertung nach Kategorie
    /// </summary>
    [Table("expense_analytics")]
    public class ExpenseAnalytics
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("period_start")]
        public DateTime PeriodStart { get; set; }

        [Column("period_end")]
        public DateTime PeriodEnd { get; set; }

        [Column("category")]
        [MaxLength(100)]
        public string? Category { get; set; }

        [Column("supplier_id")]
        public int? SupplierId { get; set; }

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("transaction_count")]
        public int TransactionCount { get; set; }

        [Column("average_amount")]
        public decimal AverageAmount { get; set; }

        [Column("currency")]
        [MaxLength(10)]
        public string Currency { get; set; } = "EUR";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string PeriodName
        {
            get
            {
                if (PeriodStart.Year == PeriodEnd.Year && PeriodStart.Month == PeriodEnd.Month)
                    return PeriodStart.ToString("MMMM yyyy");

                return $"{PeriodStart:dd.MM.yyyy} - {PeriodEnd:dd.MM.yyyy}";
            }
        }

        [NotMapped]
        public string FormattedAmount => $"{TotalAmount:N2} {Currency}";
    }

    /// <summary>
    /// Ausgaben-Kategorie für Einkauf
    /// </summary>
    [Table("expense_categories")]
    public class ExpenseCategory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        [MaxLength(300)]
        public string? Description { get; set; }

        [Column("parent_category_id")]
        public int? ParentCategoryId { get; set; }

        [Column("icon")]
        [MaxLength(50)]
        public string? Icon { get; set; }

        [Column("color")]
        [MaxLength(20)]
        public string? Color { get; set; }

        [Column("budget_limit")]
        public decimal? BudgetLimit { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Monatliche Ausgaben-Übersicht
    /// </summary>
    [Table("monthly_expenses")]
    public class MonthlyExpense
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("year")]
        public int Year { get; set; }

        [Column("month")]
        public int Month { get; set; }

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("transaction_count")]
        public int TransactionCount { get; set; }

        [Column("budget_amount")]
        public decimal? BudgetAmount { get; set; }

        [Column("variance")]
        public decimal? Variance { get; set; }

        [Column("currency")]
        [MaxLength(10)]
        public string Currency { get; set; } = "EUR";

        [Column("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [NotMapped]
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");

        [NotMapped]
        public decimal? BudgetUtilization
        {
            get
            {
                if (BudgetAmount == null || BudgetAmount == 0) return null;
                return (TotalAmount / BudgetAmount.Value) * 100;
            }
        }

        [NotMapped]
        public string VarianceColor
        {
            get
            {
                if (!Variance.HasValue || !BudgetAmount.HasValue) return "Gray";

                var percentage = Math.Abs(Variance.Value / BudgetAmount.Value * 100);
                if (Variance.Value <= 0) return "Green"; // Under budget
                if (percentage > 20) return "Red";
                if (percentage > 10) return "Orange";
                return "Yellow";
            }
        }
    }
}
