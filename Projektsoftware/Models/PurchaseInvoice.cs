using System;
using System.Globalization;

namespace Projektsoftware.Models
{
    public class PurchaseInvoice
    {
        public int Id { get; set; }
        public int? SupplierId { get; set; }
        public string SupplierName { get; set; } = "";
        public string InvoiceNumber { get; set; } = "";
        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal TotalNet { get; set; }
        public decimal TotalGross { get; set; }
        public string Status { get; set; } = "Offen";
        public DateTime? PaymentDate { get; set; }
        public int? PurchaseOrderId { get; set; }
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public long? EasybillDocumentId { get; set; }
        public DateTime? EasybillDocSyncedAt { get; set; }
        public long? EasybillAttachmentId { get; set; }
        public string EasybillDocSyncStatus => EasybillAttachmentId.HasValue
            ? $"✅ {EasybillDocSyncedAt?.ToString("dd.MM.yy") ?? "Beleg hochgeladen"}"
            : EasybillDocumentId.HasValue
                ? "⚠️ altes Format"
                : "—";

        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");
        public string TotalNetDisplay => TotalNet.ToString("C2", euroFormat);
        public string TotalGrossDisplay => TotalGross.ToString("C2", euroFormat);

        public bool IsOverdue => Status == "Offen" && DueDate.HasValue && DueDate.Value < DateTime.Today;

        public string StatusDisplay => Status switch
        {
            "Offen" => IsOverdue ? "🔴 Überfällig" : "🟡 Offen",
            "Bezahlt" => "🟢 Bezahlt",
            "Storniert" => "⚫ Storniert",
            _ => Status
        };

        public string DueDateDisplay => DueDate.HasValue ? DueDate.Value.ToString("dd.MM.yyyy") : "–";
        public string PaymentDateDisplay => PaymentDate.HasValue ? PaymentDate.Value.ToString("dd.MM.yyyy") : "–";
    }
}
