using System;

namespace Projektsoftware.Models
{
    public enum LeadStatus
    {
        Neu = 0,
        InBearbeitung = 1,
        Qualifiziert = 2,
        Abgelehnt = 3
    }

    public class SalesLead
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string ContactName { get; set; } = "";
        public string ContactCompany { get; set; } = "";
        public string ContactEmail { get; set; } = "";
        public string ContactPhone { get; set; } = "";
        public string Source { get; set; } = "";
        public LeadStatus Status { get; set; } = LeadStatus.Neu;
        public DateTime LeadDate { get; set; } = DateTime.Today;
        public string Notes { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public byte[]? FileData { get; set; }
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public bool HasFile => FileData != null && FileData.Length > 0;

        public string FileStatus => HasFile ? "📎 vorhanden" : "—";

        public string StatusText => Status switch
        {
            LeadStatus.Neu           => "🆕 Neu",
            LeadStatus.InBearbeitung => "🔄 In Bearbeitung",
            LeadStatus.Qualifiziert  => "✅ Qualifiziert",
            LeadStatus.Abgelehnt     => "❌ Abgelehnt",
            _                        => "Unbekannt"
        };

        public string StatusIcon => Status switch
        {
            LeadStatus.Neu           => "🆕",
            LeadStatus.InBearbeitung => "🔄",
            LeadStatus.Qualifiziert  => "✅",
            LeadStatus.Abgelehnt     => "❌",
            _                        => "❔"
        };
    }
}
