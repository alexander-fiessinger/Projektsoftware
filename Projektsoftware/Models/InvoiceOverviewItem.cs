using System;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Kategorisierung einer Rechnung für die Dashboard-Übersicht.
    /// </summary>
    public enum InvoiceOverviewStatus
    {
        Paid,
        PartiallyPaid,
        Open,
        Overdue,
        Draft
    }

    /// <summary>
    /// Aufbereitete Rechnung für die Dashboard-Übersicht (offen/bezahlt mit Details).
    /// Wird aus einem <see cref="EasybillDocument"/> abgeleitet und enthält nur
    /// Anzeige-relevante Felder samt Formatierungs-Helfern.
    /// </summary>
    public class InvoiceOverviewItem
    {
        public long? Id { get; set; }
        public string Number { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public decimal GrossAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime? DocumentDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? PaidAt { get; set; }
        public InvoiceOverviewStatus Status { get; set; }

        /// <summary>Noch offener Betrag (Brutto abzüglich bereits gezahlter Beträge, min. 0).</summary>
        public decimal OpenAmount
        {
            get
            {
                var open = GrossAmount - PaidAmount;
                return open > 0 ? open : 0m;
            }
        }

        public string NumberDisplay => string.IsNullOrWhiteSpace(Number) ? "–" : Number;

        public string CustomerDisplay => string.IsNullOrWhiteSpace(CustomerName) ? "–" : CustomerName;

        public string GrossAmountDisplay => GrossAmount.ToString("N2") + " €";

        public string OpenAmountDisplay => OpenAmount.ToString("N2") + " €";

        public string DocumentDateDisplay => DocumentDate?.ToString("dd.MM.yyyy") ?? "–";

        public string DueDateDisplay => DueDate?.ToString("dd.MM.yyyy") ?? "–";

        public string PaidAtDisplay => PaidAt?.ToString("dd.MM.yyyy") ?? "–";

        public bool IsPaid => Status == InvoiceOverviewStatus.Paid;

        public bool IsOpenOrOverdue =>
            Status is InvoiceOverviewStatus.Open
                   or InvoiceOverviewStatus.Overdue
                   or InvoiceOverviewStatus.PartiallyPaid;

        public int DaysOverdue =>
            Status == InvoiceOverviewStatus.Overdue && DueDate.HasValue
                ? Math.Max(0, (DateTime.Today - DueDate.Value.Date).Days)
                : 0;

        public string StatusDisplay => Status switch
        {
            InvoiceOverviewStatus.Paid          => "✅ Bezahlt",
            InvoiceOverviewStatus.PartiallyPaid => "🟠 Teilbezahlt",
            InvoiceOverviewStatus.Open          => "🟡 Offen",
            InvoiceOverviewStatus.Overdue       => $"🔴 Überfällig ({DaysOverdue} T.)",
            InvoiceOverviewStatus.Draft         => "📝 Entwurf",
            _ => "–"
        };
    }
}
