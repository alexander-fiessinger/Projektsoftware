using System.Text.Json.Serialization;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Easybill PDF-Vorlage
    /// </summary>
    public class EasybillPdfTemplate
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("pdf_template")]
        public string PdfTemplate { get; set; }

        [JsonPropertyName("is_default")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }
    }

    /// <summary>
    /// Easybill Kontakt (Ansprechpartner)
    /// </summary>
    public class EasybillContact
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("customer_id")]
        public long CustomerId { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("salutation")]
        public int? Salutation { get; set; } // 0=Herr, 1=Frau

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("street")]
        public string Street { get; set; }

        [JsonPropertyName("zip_code")]
        public string ZipCode { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("phone_1")]
        public string Phone1 { get; set; }

        [JsonPropertyName("phone_2")]
        public string Phone2 { get; set; }

        [JsonPropertyName("fax")]
        public string Fax { get; set; }

        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("department")]
        public string Department { get; set; }

        [JsonPropertyName("personal_note")]
        public string PersonalNote { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }

        // Display Properties
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string SalutationText => Salutation == 0 ? "Herr" : Salutation == 1 ? "Frau" : "";
    }

    /// <summary>
    /// Easybill Aufgabe
    /// </summary>
    public class EasybillTask
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } // PROCESSING, DONE

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("priority")]
        public string Priority { get; set; } // NORMAL, HIGH

        [JsonPropertyName("finish_date")]
        public string FinishDate { get; set; } // yyyy-MM-dd

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("project_id")]
        public long? ProjectId { get; set; }

        [JsonPropertyName("customer_id")]
        public long? CustomerId { get; set; }

        [JsonPropertyName("login_id")]
        public long? LoginId { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }

        // Display Properties
        public string DisplayStatus => Status == "DONE" ? "Erledigt" : "In Bearbeitung";
        public string DisplayPriority => Priority == "HIGH" ? "Hoch" : "Normal";
    }

    /// <summary>
    /// Easybill Text-Vorlage
    /// </summary>
    public class EasybillTextTemplate
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } // EMAIL, DOCUMENT

        [JsonPropertyName("document_type")]
        public string DocumentType { get; set; } // INVOICE, OFFER, etc.

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("text_suffix")]
        public string TextSuffix { get; set; }

        [JsonPropertyName("is_default")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }
    }

    /// <summary>
    /// Easybill Anhang
    /// </summary>
    public class EasybillAttachment
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("document_id")]
        public long DocumentId { get; set; }

        [JsonPropertyName("file_name")]
        public string FileName { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }
    }

    public class EasybillAttachmentList
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("items")]
        public EasybillAttachment[]? Items { get; set; }
    }

    /// <summary>
    /// Easybill Lagerbestand
    /// </summary>
    public class EasybillStock
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("position_id")]
        public long PositionId { get; set; }

        [JsonPropertyName("stock_count")]
        public decimal StockCount { get; set; }

        [JsonPropertyName("note")]
        public string Note { get; set; }

        [JsonPropertyName("document_id")]
        public long? DocumentId { get; set; }

        [JsonPropertyName("document_position_id")]
        public long? DocumentPositionId { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }
    }

    /// <summary>
    /// Easybill Webhook
    /// </summary>
    public class EasybillWebhook
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } // json, form

        [JsonPropertyName("secret")]
        public string Secret { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("events")]
        public string[] Events { get; set; } // document.create, document.update, etc.

        [JsonPropertyName("last_response")]
        public WebhookResponse LastResponse { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }
    }

    public class WebhookResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("time")]
        public string Time { get; set; }
    }

    /// <summary>
    /// Easybill Rabatt
    /// </summary>
    public class EasybillDiscount
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("customer_id")]
        public long CustomerId { get; set; }

        [JsonPropertyName("position_id")]
        public long? PositionId { get; set; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }

        [JsonPropertyName("discount_type")]
        public string DiscountType { get; set; } // PERCENT, ABSOLUTE

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }
    }

    /// <summary>
    /// Easybill Sepa-Mandat
    /// </summary>
    public class EasybillSepaMandate
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("customer_id")]
        public long CustomerId { get; set; }

        [JsonPropertyName("reference")]
        public string Reference { get; set; }

        [JsonPropertyName("iban")]
        public string Iban { get; set; }

        [JsonPropertyName("bic")]
        public string Bic { get; set; }

        [JsonPropertyName("mandate_reference")]
        public string MandateReference { get; set; }

        [JsonPropertyName("signature_date")]
        public string SignatureDate { get; set; } // yyyy-MM-dd

        [JsonPropertyName("creditor_identifier")]
        public string CreditorIdentifier { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }
    }

    /// <summary>
    /// Easybill Notiz
    /// </summary>
    public class EasybillNote
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("document_id")]
        public long? DocumentId { get; set; }

        [JsonPropertyName("customer_id")]
        public long? CustomerId { get; set; }

        [JsonPropertyName("project_id")]
        public long? ProjectId { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("priority")]
        public string? Priority { get; set; }

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        public string DisplayPriority => Priority switch
        {
            "HIGH" => "🔴 Hoch",
            "NORMAL" => "🟡 Normal",
            "LOW" => "🟢 Niedrig",
            _ => "Normal"
        };

        public string DisplayInfo
        {
            get
            {
                if (string.IsNullOrEmpty(Note)) return DisplayPriority;
                var maxLength = Math.Min(50, Note.Length);
                return $"{DisplayPriority} - {Note.Substring(0, maxLength)}...";
            }
        }
    }

    public class EasybillNoteList
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("items")]
        public EasybillNote[]? Items { get; set; }
    }

    /// <summary>
    /// Easybill Aktivität
    /// </summary>
    public class EasybillActivity
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("customer_id")]
        public long? CustomerId { get; set; }

        [JsonPropertyName("contact_id")]
        public long? ContactId { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        public string DisplayType => Type switch
        {
            "CALL" => "📞 Anruf",
            "EMAIL" => "📧 E-Mail",
            "MEETING" => "🤝 Meeting",
            "NOTE" => "📝 Notiz",
            _ => Type ?? "Unbekannt"
        };

        public string DisplayStatus => Status switch
        {
            "PLANNED" => "⏳ Geplant",
            "DONE" => "✅ Erledigt",
            "CANCELLED" => "❌ Abgebrochen",
            _ => Status ?? ""
        };

        public string DisplayInfo => $"{DisplayType} - {Subject}";
    }

    public class EasybillActivityList
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("items")]
        public EasybillActivity[]? Items { get; set; }
    }

    /// <summary>
    /// Easybill Steuerregel
    /// </summary>
    public class EasybillTaxRule
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("tax_type")]
        public string? TaxType { get; set; }

        [JsonPropertyName("tax_percent")]
        public decimal? TaxPercent { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        public string DisplayInfo => $"{Name} - {TaxPercent}% ({Country ?? "Alle Länder"})";
    }

    public class EasybillTaxRuleList
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("items")]
        public EasybillTaxRule[]? Items { get; set; }
    }
}
