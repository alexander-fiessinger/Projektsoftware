using System.Text.Json.Serialization;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Easybill Zahlung
    /// </summary>
    public class EasybillPayment
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("document_id")]
        public long DocumentId { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // BANK_TRANSFER, BANK_CARD, CASH, CREDIT_NOTE, PAYPAL, DIRECT_DEBIT, MISC

        [JsonPropertyName("currency")]
        public string? Currency { get; set; } // EUR, USD, etc.

        [JsonPropertyName("payment_at")]
        public string? PaymentAt { get; set; } // yyyy-MM-dd

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("provider_id")]
        public string? ProviderId { get; set; }

        [JsonPropertyName("provider_status")]
        public string? ProviderStatus { get; set; }

        [JsonPropertyName("login_id")]
        public long? LoginId { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        // Display Properties
        public string DisplayType => Type switch
        {
            "BANK_TRANSFER" => "Überweisung",
            "BANK_CARD" => "Kartenzahlung",
            "CASH" => "Bar",
            "CREDIT_NOTE" => "Gutschrift",
            "PAYPAL" => "PayPal",
            "DIRECT_DEBIT" => "Lastschrift",
            "MISC" => "Sonstige",
            _ => Type
        };
    }

    public class EasybillPaymentList
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
        public EasybillPayment[]? Items { get; set; }
    }
}
