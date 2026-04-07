using System;
using System.Globalization;

namespace Projektsoftware.Models
{
    public class PurchaseDocument
    {
        public int Id { get; set; }
        public int? SupplierId { get; set; }
        public string SupplierName { get; set; } = "";
        public string DocumentName { get; set; } = "";
        public string DocumentType { get; set; } = "Rechnung";
        public DateTime DocumentDate { get; set; }
        public string OriginalFileName { get; set; } = "";
        public string LocalFilePath { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public long? EasybillAttachmentId { get; set; }
        public DateTime? EasybillSyncedAt { get; set; }

        private static readonly CultureInfo deFormat = new CultureInfo("de-DE");

        public string EasybillSyncStatus => EasybillAttachmentId.HasValue
            ? $"✅ {EasybillSyncedAt?.ToString("dd.MM.yy") ?? "hochgeladen"}"
            : "—";

        public string DocumentTypeIcon => DocumentType switch
        {
            "Rechnung" => "📄",
            "Lieferschein" => "📦",
            "Angebot" => "📋",
            "Gutschrift" => "💳",
            _ => "📁"
        };

        public string TypeDisplay => $"{DocumentTypeIcon} {DocumentType}";

        public bool FileExists => !string.IsNullOrEmpty(LocalFilePath) && System.IO.File.Exists(LocalFilePath);

        public string FileStatus => FileExists ? "📎 vorhanden" : (string.IsNullOrEmpty(LocalFilePath) ? "—" : "⚠️ fehlt");
    }
}
