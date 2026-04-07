using System;

namespace Projektsoftware.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string ContactPerson { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public string City { get; set; } = "";
        public string Country { get; set; } = "Deutschland";
        public string TaxNumber { get; set; } = "";
        public string BankIban { get; set; } = "";
        public string BankBic { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public long? EasybillCustomerId { get; set; }
        public DateTime? EasybillSyncedAt { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "(Unbekannt)" : Name;
        public string DisplayAddress => $"{ZipCode} {City}".Trim();
        public string EasybillSyncStatus => EasybillCustomerId.HasValue
            ? $"✅ {EasybillSyncedAt?.ToString("dd.MM.yy") ?? "verknüpft"}"
            : "—";
    }
}
