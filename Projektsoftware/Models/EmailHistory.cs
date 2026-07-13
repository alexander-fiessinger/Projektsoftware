using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Projektsoftware.Models
{
    /// <summary>
    /// E-Mail-Verlauf für CRM-Kontakte
    /// </summary>
    [Table("email_history")]
    public class EmailHistory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("contact_id")]
        public int? ContactId { get; set; }

        [Column("customer_id")]
        public int? CustomerId { get; set; }

        [Column("lead_id")]
        public int? LeadId { get; set; }

        [Column("subject")]
        [Required]
        [MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [Column("from_address")]
        [MaxLength(200)]
        public string? FromAddress { get; set; }

        [Column("to_address")]
        [MaxLength(200)]
        public string? ToAddress { get; set; }

        [Column("cc_address")]
        [MaxLength(500)]
        public string? CcAddress { get; set; }

        [Column("body")]
        public string? Body { get; set; }

        [Column("body_preview")]
        [MaxLength(500)]
        public string? BodyPreview { get; set; }

        [Column("sent_date")]
        public DateTime SentDate { get; set; }

        [Column("received_date")]
        public DateTime? ReceivedDate { get; set; }

        [Column("direction")]
        [MaxLength(20)]
        public string? Direction { get; set; } // "Sent", "Received"

        [Column("has_attachments")]
        public bool HasAttachments { get; set; } = false;

        [Column("attachment_count")]
        public int AttachmentCount { get; set; } = 0;

        [Column("exchange_message_id")]
        [MaxLength(500)]
        public string? ExchangeMessageId { get; set; }

        [Column("conversation_id")]
        [MaxLength(200)]
        public string? ConversationId { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("importance")]
        [MaxLength(20)]
        public string? Importance { get; set; } // "Low", "Normal", "High"

        [Column("category")]
        [MaxLength(50)]
        public string? Category { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string DirectionIcon => Direction == "Sent" ? "📤" : "📥";

        [NotMapped]
        public string FormattedDate => SentDate.ToString("dd.MM.yyyy HH:mm");

        [NotMapped]
        public string ShortPreview
        {
            get
            {
                if (!string.IsNullOrEmpty(BodyPreview))
                    return BodyPreview.Length > 100 ? BodyPreview.Substring(0, 100) + "..." : BodyPreview;
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Angebots-Historie für CRM-Kontakte
    /// </summary>
    [Table("offer_history")]
    public class OfferHistory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("contact_id")]
        public int? ContactId { get; set; }

        [Column("customer_id")]
        public int? CustomerId { get; set; }

        [Column("lead_id")]
        public int? LeadId { get; set; }

        [Column("easybill_document_id")]
        public long? EasybillDocumentId { get; set; }

        [Column("offer_number")]
        [MaxLength(50)]
        public string? OfferNumber { get; set; }

        [Column("offer_title")]
        [MaxLength(200)]
        public string? OfferTitle { get; set; }

        [Column("offer_date")]
        public DateTime OfferDate { get; set; }

        [Column("valid_until")]
        public DateTime? ValidUntil { get; set; }

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("currency")]
        [MaxLength(10)]
        public string Currency { get; set; } = "EUR";

        [Column("status")]
        [MaxLength(50)]
        public string? Status { get; set; } // "Draft", "Sent", "Accepted", "Declined", "Expired", "Converted"

        [Column("sent_date")]
        public DateTime? SentDate { get; set; }

        [Column("accepted_date")]
        public DateTime? AcceptedDate { get; set; }

        [Column("declined_date")]
        public DateTime? DeclinedDate { get; set; }

        [Column("converted_to_invoice_id")]
        public long? ConvertedToInvoiceId { get; set; }

        [Column("sent_via")]
        [MaxLength(50)]
        public string? SentVia { get; set; } // "Email", "Post", "Portal"

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_by")]
        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public bool IsExpired => ValidUntil.HasValue && DateTime.Now > ValidUntil.Value && Status != "Accepted" && Status != "Converted";

        [NotMapped]
        public string StatusColor
        {
            get
            {
                return Status switch
                {
                    "Accepted" => "Green",
                    "Converted" => "DarkGreen",
                    "Declined" => "Red",
                    "Expired" => "Gray",
                    "Sent" => "Blue",
                    "Draft" => "Orange",
                    _ => "LightGray"
                };
            }
        }

        [NotMapped]
        public string StatusDisplayText
        {
            get
            {
                return Status switch
                {
                    "Draft" => "Entwurf",
                    "Sent" => "Versendet",
                    "Accepted" => "Angenommen",
                    "Declined" => "Abgelehnt",
                    "Expired" => "Abgelaufen",
                    "Converted" => "In Rechnung umgewandelt",
                    _ => Status ?? "Unbekannt"
                };
            }
        }

        [NotMapped]
        public string FormattedAmount => $"{TotalAmount:N2} {Currency}";
    }

    /// <summary>
    /// Kontakt-Import-Eintrag
    /// </summary>
    [Table("contact_imports")]
    public class ContactImport
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("file_name")]
        [MaxLength(200)]
        public string? FileName { get; set; }

        [Column("import_date")]
        public DateTime ImportDate { get; set; } = DateTime.Now;

        [Column("total_rows")]
        public int TotalRows { get; set; }

        [Column("successful_imports")]
        public int SuccessfulImports { get; set; }

        [Column("failed_imports")]
        public int FailedImports { get; set; }

        [Column("duplicate_skipped")]
        public int DuplicateSkipped { get; set; }

        [Column("import_status")]
        [MaxLength(50)]
        public string? ImportStatus { get; set; } // "InProgress", "Completed", "CompletedWithErrors", "Failed"

        [Column("error_log")]
        public string? ErrorLog { get; set; }

        [Column("imported_by")]
        [MaxLength(100)]
        public string? ImportedBy { get; set; }

        [NotMapped]
        public string SuccessRate
        {
            get
            {
                if (TotalRows == 0) return "0%";
                return $"{(SuccessfulImports * 100.0 / TotalRows):N1}%";
            }
        }
    }
}
