using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projektsoftware.Api.Models;

/// <summary>
/// Flexibler Converter für Easybill-Preisfelder (Cent-Werte als long/string/decimal).
/// </summary>
public class EbPriceConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.TryGetInt64(out var l) ? l : (long)reader.GetDecimal(),
            JsonTokenType.String => long.TryParse(reader.GetString(), out var sl) ? sl : null,
            JsonTokenType.Null => null,
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value.HasValue) writer.WriteNumberValue(value.Value);
        else writer.WriteNullValue();
    }
}

// ── Easybill Document ───────────────────────────────────────────────

public class EbDocument
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("customer_id")]
    public long? CustomerId { get; set; }

    [JsonPropertyName("project_id")]
    public long? ProjectId { get; set; }

    [JsonPropertyName("document_date")]
    public string? DocumentDate { get; set; }

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
    public string? Status { get; set; }

    [JsonPropertyName("service_date")]
    public EbServiceDate? ServiceDate { get; set; }

    [JsonPropertyName("total_gross")]
    [JsonConverter(typeof(EbPriceConverter))]
    public long? TotalGross { get; set; }

    [JsonPropertyName("total_net")]
    [JsonConverter(typeof(EbPriceConverter))]
    public long? TotalNet { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("due_in_days")]
    public int? DueInDays { get; set; }

    [JsonPropertyName("items")]
    public EbDocumentItem[]? Items { get; set; }

    [JsonPropertyName("is_draft")]
    public bool IsDraft { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("customer_snapshot")]
    public EbCustomerSnapshot? CustomerSnapshot { get; set; }

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
        _ => Type ?? "–"
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
                "PARTIALLY_PAID" => "Teilw. bezahlt",
                _ => Status ?? "–"
            };
        }
    }

    [JsonIgnore]
    public string TotalGrossDisplay =>
        TotalGross.HasValue
            ? (TotalGross.Value / 100m).ToString("N2") + " €"
            : "–";

    [JsonIgnore]
    public string CustomerDisplay =>
        CustomerSnapshot?.CompanyName
        ?? $"{CustomerSnapshot?.FirstName} {CustomerSnapshot?.LastName}".Trim();
}

public class EbDocumentItem
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("single_price_net")]
    [JsonConverter(typeof(EbPriceConverter))]
    public long? SinglePriceNet { get; set; }

    [JsonPropertyName("vat_percent")]
    public int VatPercent { get; set; } = 19;

    [JsonPropertyName("position_id")]
    public long? PositionId { get; set; }

    [JsonPropertyName("total_price_gross")]
    [JsonConverter(typeof(EbPriceConverter))]
    public long? TotalPriceGross { get; set; }
}

public class EbServiceDate
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("date_from")]
    public string? DateFrom { get; set; }

    [JsonPropertyName("date_to")]
    public string? DateTo { get; set; }
}

public class EbCustomerSnapshot
{
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
}

public class EbDocumentList
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pages")]
    public int Pages { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("items")]
    public EbDocument[]? Items { get; set; }
}

// ── Easybill Customer ───────────────────────────────────────────────

public class EbCustomer
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("emails")]
    public string[]? Emails { get; set; }

    [JsonIgnore]
    public string DisplayName =>
        !string.IsNullOrEmpty(CompanyName) ? CompanyName : $"{FirstName} {LastName}".Trim();
}

public class EbCustomerList
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pages")]
    public int Pages { get; set; }

    [JsonPropertyName("items")]
    public EbCustomer[]? Items { get; set; }
}

// ── Per-User Easybill Settings ──────────────────────────────────────

public class EasybillUserSettings
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(ApiKey);
}
