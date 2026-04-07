using System;
using System.Collections.Generic;
using System.Globalization;

namespace Projektsoftware.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }
        public int? SupplierId { get; set; }
        public string SupplierName { get; set; } = "";
        public string OrderNumber { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDateExpected { get; set; }
        public DateTime? DeliveryDateActual { get; set; }
        public string Status { get; set; } = "Offen";
        public decimal TotalNet { get; set; }
        public decimal TotalGross { get; set; }
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public List<PurchaseOrderItem> Items { get; set; } = new();
        public long? EasybillDocumentId { get; set; }
        public DateTime? EasybillDocSyncedAt { get; set; }
        public string EasybillSyncStatus => EasybillDocumentId.HasValue
            ? $"✅ {EasybillDocSyncedAt?.ToString("dd.MM.yy") ?? "verknüpft"}"
            : "—";

        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");
        public string TotalNetDisplay => TotalNet.ToString("C2", euroFormat);
        public string TotalGrossDisplay => TotalGross.ToString("C2", euroFormat);

        public string StatusDisplay => Status switch
        {
            "Offen" => "🟡 Offen",
            "Bestellt" => "🔵 Bestellt",
            "Teilweise geliefert" => "🟠 Teilw. geliefert",
            "Geliefert" => "🟢 Geliefert",
            "Storniert" => "🔴 Storniert",
            _ => Status
        };
    }

    public class PurchaseOrderItem
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public string Description { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Stk.";
        public decimal UnitPriceNet { get; set; }
        public decimal TotalNet { get; set; }
        public decimal VatPercent { get; set; } = 19;

        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");
        public string UnitPriceDisplay => UnitPriceNet.ToString("C2", euroFormat);
        public string TotalNetDisplay => TotalNet.ToString("C2", euroFormat);
    }
}
