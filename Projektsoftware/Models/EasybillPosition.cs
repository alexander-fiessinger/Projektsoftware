using System.Text.Json.Serialization;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Easybill Position/Artikel für Rechnungen
    /// Dokumentation: https://api.easybill.de/rest/v1/positions
    /// </summary>
    public class EasybillPosition
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "SERVICE"; // SERVICE, PRODUCT, TEXT

        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("sale_price")]
        public decimal? SalePrice { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; } = 1;

        [JsonPropertyName("unit")]
        public string? Unit { get; set; } = "Stunden";

        [JsonPropertyName("group_name")]
        public string? GroupName { get; set; }

        [JsonPropertyName("customer_id")]
        public long? CustomerId { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("export_identifier")]
        public string? ExportIdentifier { get; set; }
    }

    public class EasybillPositionList
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
        public EasybillPosition[] Items { get; set; } = Array.Empty<EasybillPosition>();
    }
}
