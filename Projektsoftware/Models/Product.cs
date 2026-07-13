using System;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Lokaler Artikel/Produkt für den Preiskatalog des Kundenportals
    /// (unabhängig von Easybill, eigene Preisquelle in der Datenbank).
    /// </summary>
    public class Product
    {
        public int Id { get; set; }

        public string Number { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; } = "Stück";

        /// <summary>Nettopreis in Euro</summary>
        public decimal NetPrice { get; set; }

        /// <summary>Mehrwertsteuersatz in Prozent (0, 7, 19)</summary>
        public int VatPercent { get; set; } = 19;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Display properties
        public decimal GrossPrice => Math.Round(NetPrice * (1 + VatPercent / 100m), 2);

        public string DisplayInfo => $"{Number} - {Name}";
    }
}
