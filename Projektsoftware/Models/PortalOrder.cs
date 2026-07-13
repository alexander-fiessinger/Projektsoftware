using System;
using System.Collections.Generic;
using System.Linq;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Zahlungsart einer Portal-Bestellung. Standard ist Vorkasse; Rechnung wird erst
    /// nach den ersten Bestellungen je nach Bonität freigeschaltet.
    /// </summary>
    public enum PortalPaymentMethod
    {
        Prepayment = 0, // Vorkasse
        Invoice = 1     // Auf Rechnung
    }

    /// <summary>
    /// Eine über das Kundenportal (Webshop) eingegangene Bestellung.
    /// Wird in WPF zur Weiterverarbeitung angezeigt.
    /// </summary>
    public class PortalOrder
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public int? PortalUserId { get; set; }
        public int? CustomerId { get; set; }
        public decimal TotalNet { get; set; }
        public decimal TotalGross { get; set; }
        public PortalPaymentMethod PaymentMethod { get; set; }
        public int Status { get; set; } // 0=Neu, 1=In Bearbeitung, 2=Erledigt, 3=Storniert
        public string Note { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }

        // Display-only (aus JOIN auf customers)
        public string CustomerName { get; set; } = "";

        public List<PortalOrderItem> Items { get; set; } = new();

        public string PaymentMethodText => PaymentMethod == PortalPaymentMethod.Invoice ? "Auf Rechnung" : "Vorkasse";

        public string StatusText => Status switch
        {
            0 => "🆕 Neu",
            1 => "⏳ In Bearbeitung",
            2 => "✅ Erledigt",
            3 => "❌ Storniert",
            _ => "Unbekannt"
        };

        public bool IsNew => Status == 0;

        public string CreatedAtText => CreatedAt.ToString("dd.MM.yyyy HH:mm");

        public string ItemSummary => Items.Count == 0
            ? "Keine Positionen"
            : string.Join(", ", Items.Select(i => $"{i.Quantity}× {i.Name}"));
    }

    /// <summary>
    /// Eine Position innerhalb einer <see cref="PortalOrder"/>.
    /// </summary>
    public class PortalOrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int? ProductId { get; set; }
        public string Number { get; set; } = "";
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "Stück";
        public decimal NetPrice { get; set; }
        public int VatPercent { get; set; } = 19;
        public int Quantity { get; set; } = 1;

        public decimal LineNet => Math.Round(NetPrice * Quantity, 2);
        public decimal LineGross => Math.Round(LineNet * (1 + VatPercent / 100m), 2);
    }
}
