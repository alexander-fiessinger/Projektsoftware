using System;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Vertragstyp: Dienstleistungsvertrag (Robotik) oder Werkvertrag
    /// </summary>
    public enum ContractType
    {
        Dienstleistungsvertrag,
        Werkvertrag
    }

    /// <summary>
    /// Datenmodell für die Vertragsgenerierung
    /// </summary>
    public class ContractData
    {
        // Vertragstyp
        public ContractType ContractType { get; set; }

        // Auftragnehmer (eigenes Unternehmen)
        public string ContractorCompany { get; set; } = string.Empty;
        public string ContractorName { get; set; } = string.Empty;
        public string ContractorStreet { get; set; } = string.Empty;
        public string ContractorZipCity { get; set; } = string.Empty;
        public string ContractorEmail { get; set; } = string.Empty;
        public string ContractorPhone { get; set; } = string.Empty;
        public string ContractorVatId { get; set; } = string.Empty;
        public string ContractorTaxNumber { get; set; } = string.Empty;

        // Auftraggeber (Kunde)
        public string ClientCompany { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string ClientStreet { get; set; } = string.Empty;
        public string ClientZipCity { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;

        // Vertragsinhalte
        public string ContractSubject { get; set; } = string.Empty;
        public string ServiceDescription { get; set; } = string.Empty;
        public decimal NetAmount { get; set; }
        public decimal VatRate { get; set; } = 19m;
        public string PaymentTerms { get; set; } = "14 Tage nach Rechnungsstellung";

        // Laufzeit
        public DateTime ContractStart { get; set; } = DateTime.Today;
        public DateTime? ContractEnd { get; set; }
        public string NoticePeriod { get; set; } = "3 Monate zum Quartalsende";

        // Werkvertrag-spezifisch
        public DateTime? DeliveryDate { get; set; }
        public string AcceptanceCriteria { get; set; } = string.Empty;
        public string WarrantyPeriod { get; set; } = "12 Monate ab Abnahme";
        public string WorkPaymentSchedule { get; set; } = "30 % bei Auftragserteilung, 30 % bei dokumentierter Teilerreichung eines definierten Meilensteins, 40 % nach Abnahme des Werkes";

        // Dienstleistungsvertrag-spezifisch
        public string ServiceLocation { get; set; } = "Vor Ort beim Auftraggeber und/oder remote";
        public string WorkingHours { get; set; } = string.Empty;
        public decimal? HourlyRate { get; set; }

        // Sonstiges
        public string AdditionalClauses { get; set; } = string.Empty;
        public string Jurisdiction { get; set; } = string.Empty;

        // Berechnete Werte
        public decimal GrossAmount => NetAmount * (1 + VatRate / 100m);
        public decimal VatAmount => NetAmount * VatRate / 100m;

        public string ContractTypeDisplay => ContractType == ContractType.Dienstleistungsvertrag
            ? "Dienstleistungsvertrag (Robotik)"
            : "Werkvertrag";
    }
}
