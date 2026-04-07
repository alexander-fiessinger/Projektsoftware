using System.Text.Json.Serialization;
using Projektsoftware.Converters;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Easybill Dokument (Rechnung, Angebot, Lieferschein, etc.)
    /// </summary>
    public class EasybillDocument
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // INVOICE, OFFER, ORDER_CONFIRMATION, DELIVERY_NOTE, CREDIT, etc.

        [JsonPropertyName("customer_id")]
        [JsonConverter(typeof(NullableLongConverter))]
        public long? CustomerId { get; set; }

        [JsonPropertyName("project_id")]
        public long? ProjectId { get; set; }

        [JsonPropertyName("document_date")]
        public string? DocumentDate { get; set; } // yyyy-MM-dd

        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("text_suffix")]
        public string? TextSuffix { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; } // DRAFT, SENT, PAID, CANCELLED, etc.

        [JsonPropertyName("service_date")]
        public ServiceDate? ServiceDate { get; set; }

        [JsonPropertyName("total_gross")]
        [JsonConverter(typeof(EasybillPriceConverter))]
        public decimal? TotalGross { get; set; }

        [JsonPropertyName("total_net")]
        [JsonConverter(typeof(EasybillPriceConverter))]
        public decimal? TotalNet { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; } // EUR, USD, etc.

        [JsonPropertyName("due_date")]
        public string? DueDate { get; set; } // yyyy-MM-dd

        [JsonPropertyName("due_in_days")]
        public int? DueInDays { get; set; }

        [JsonPropertyName("discount")]
        [JsonConverter(typeof(NullableDecimalConverter))]
        public decimal? Discount { get; set; }

        [JsonPropertyName("discount_type")]
        public string? DiscountType { get; set; } // PERCENT, ABSOLUTE

        [JsonPropertyName("items")]
        public EasybillDocumentItem[]? Items { get; set; }

        [JsonPropertyName("pdf_pages")]
        public int? PdfPages { get; set; }

        [JsonPropertyName("pdf_template_id")]
        public long? PdfTemplateId { get; set; }

        [JsonPropertyName("paid_at")]
        public string? PaidAt { get; set; } // yyyy-MM-dd HH:mm:ss

        [JsonPropertyName("is_draft")]
        public bool IsDraft { get; set; }

        [JsonPropertyName("is_archive")]
        public bool IsArchive { get; set; }

        [JsonPropertyName("customer_snapshot")]
        public CustomerSnapshot? CustomerSnapshot { get; set; }

        [JsonPropertyName("contact_id")]
        public long? ContactId { get; set; }

        [JsonPropertyName("buyer_reference")]
        public string? BuyerReference { get; set; }

        [JsonPropertyName("payment_types")]
        public string[]? PaymentTypes { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        // Display Properties (JsonIgnore: these are local-only, not part of the Easybill API)
        [JsonIgnore]
        public string DisplayType => Type switch
        {
            "INVOICE" => "Rechnung",
            "OFFER" => "Angebot",
            "ORDER_CONFIRMATION" => "Auftragsbestätigung",
            "DELIVERY_NOTE" => "Lieferschein",
            "CREDIT" => "Gutschrift",
            "DUNNING" => "Mahnung",
            "INVOICE_CANCELLATION" => "Storno",
            "PROFORMA_INVOICE" => "Proforma-Rechnung",
            _ => Type
        };

        [JsonIgnore]
        public string DisplayStatus
        {
            get
            {
                if (IsDraft) return "Entwurf";
                return Status switch
                {
                    "DRAFT" => "Entwurf",
                    "SENT" => "Gesendet",
                    "PAID" => "Bezahlt",
                    "CANCELLED" => "Storniert",
                    "OVERDUE" => "Überfällig",
                    "PARTIALLY_PAID" => "Teilweise bezahlt",
                    null or "" => "–",
                    _ => Status
                };
            }
        }

        // Helper property for DataGrid display
        [JsonIgnore]
        public string? CustomerDisplay { get; set; }

        [JsonIgnore]
        public string TotalGrossDisplay
        {
            get
            {
                var amount = TotalGross
                    ?? Items?.Sum(item => item.TotalPriceGross ?? 0m);
                return amount.HasValue
                    ? amount.Value.ToString("C2", new System.Globalization.CultureInfo("de-DE"))
                    : "–";
            }
        }
    }

    public class ServiceDate
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; } // DEFAULT, DATE, TEXT, FROM_TO

        [JsonPropertyName("date")]
        public string? Date { get; set; } // yyyy-MM-dd

        [JsonPropertyName("date_from")]
        public string? DateFrom { get; set; } // yyyy-MM-dd

        [JsonPropertyName("date_to")]
        public string? DateTo { get; set; } // yyyy-MM-dd

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public class EasybillDocumentItem
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("quantity")]
        [JsonConverter(typeof(DecimalConverter))]
        public decimal Quantity { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // POSITION, ITEM, TEXT, SUBTOTAL, PAGEBREAK

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("single_price_net")]
        [JsonConverter(typeof(EasybillPriceConverter))]
        public decimal? SinglePriceNet { get; set; }

        [JsonPropertyName("single_price_gross")]
        [JsonConverter(typeof(EasybillPriceConverter))]
        public decimal? SinglePriceGross { get; set; }

        [JsonPropertyName("vat_percent")]
        public int VatPercent { get; set; }

        [JsonPropertyName("discount")]
        [JsonConverter(typeof(NullableDecimalConverter))]
        public decimal? Discount { get; set; }

        [JsonPropertyName("discount_type")]
        public string? DiscountType { get; set; } // PERCENT, ABSOLUTE

        [JsonPropertyName("position_id")]
        public long? PositionId { get; set; }

        [JsonPropertyName("total_price_net")]
        [JsonConverter(typeof(EasybillPriceConverter))]
        public decimal? TotalPriceNet { get; set; }

        [JsonPropertyName("total_price_gross")]
        [JsonConverter(typeof(EasybillPriceConverter))]
        public decimal? TotalPriceGross { get; set; }

        [JsonPropertyName("export_identifier")]
        public string? ExportIdentifier { get; set; }

        [JsonPropertyName("customer_id")]
        [JsonConverter(typeof(NullableLongConverter))]
        public long? CustomerId { get; set; }
    }

    public class EasybillDocumentList
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
        public EasybillDocument[]? Items { get; set; }
    }

    public class CustomerSnapshot
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("company_name")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("salutation")]
        public int? Salutation { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("suffix_1")]
        public string? Suffix1 { get; set; }

        [JsonPropertyName("suffix_2")]
        public string? Suffix2 { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("zip_code")]
        public string? ZipCode { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("phone_1")]
        public string? Phone1 { get; set; }

        [JsonPropertyName("phone_2")]
        public string? Phone2 { get; set; }

        [JsonPropertyName("fax")]
        public string? Fax { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("www")]
        public string? Www { get; set; }

        [JsonPropertyName("tax_number")]
        public string? TaxNumber { get; set; }

        [JsonPropertyName("vat_identifier")]
        public string? VatIdentifier { get; set; }
    }
}
