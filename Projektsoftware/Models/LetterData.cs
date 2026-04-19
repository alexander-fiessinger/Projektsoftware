using System;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Datenmodell für den Briefgenerator (DIN 5008, Fensterumschlag-optimiert).
    /// </summary>
    public class LetterData
    {
        // Absender (af software Engineering – aus ContractorConfig / Easybill)
        public string SenderCompany { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderStreet { get; set; } = string.Empty;
        public string SenderZipCity { get; set; } = string.Empty;
        public string SenderPhone { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderWeb { get; set; } = string.Empty;
        public string SenderVatId { get; set; } = string.Empty;
        public string SenderTaxNumber { get; set; } = string.Empty;
        public string SenderBankName { get; set; } = string.Empty;
        public string SenderIban { get; set; } = string.Empty;
        public string SenderBic { get; set; } = string.Empty;

        // Empfänger
        public string RecipientCompany { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientStreet { get; set; } = string.Empty;
        public string RecipientZipCity { get; set; } = string.Empty;

        // Brief-Metadaten
        public string Subject { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Today;
        public string Place { get; set; } = "Marktredwitz";
        public string Reference { get; set; } = string.Empty;

        // Inhalt
        public string Salutation { get; set; } = "Sehr geehrte Damen und Herren,";
        public string Body { get; set; } = string.Empty;
        public string Closing { get; set; } = "Mit freundlichen Grüßen";
        public string SignatureName { get; set; } = string.Empty;

        /// <summary>Optionaler Pfad zu einem Logo-Bild (PNG/JPG).</summary>
        public string? LogoPath { get; set; }
    }
}
