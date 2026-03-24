using Projektsoftware.Converters;
using System.Text.Json.Serialization;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Easybill Artikel/Produkt
    /// </summary>
    public class EasybillProduct
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } // PRODUCT, SERVICE

        [JsonPropertyName("number")]
        public string Number { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("sale_price")]
        [JsonConverter(typeof(EasybillPriceConverterNotNullable))]
        public decimal SalePrice { get; set; }

        [JsonPropertyName("sale_price2")]
        [JsonConverter(typeof(EasybillPriceConverter))]
        public decimal? SalePrice2 { get; set; }

        [JsonPropertyName("sale_price3")]
        [JsonConverter(typeof(EasybillPriceConverter))]
        public decimal? SalePrice3 { get; set; }

        [JsonPropertyName("sale_price4")]
        [JsonConverter(typeof(EasybillPriceConverter))]
        public decimal? SalePrice4 { get; set; }

        [JsonPropertyName("sale_price5")]
        [JsonConverter(typeof(EasybillPriceConverter))]
        public decimal? SalePrice5 { get; set; }

        [JsonPropertyName("purchase_price")]
        [JsonConverter(typeof(EasybillPriceConverter))]
        public decimal? PurchasePrice { get; set; }

        [JsonPropertyName("purchase_price_net_gross")]
        public string PurchasePriceNetGross { get; set; } // NET, GROSS

        [JsonPropertyName("vat_percent")]
        public int VatPercent { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        [JsonPropertyName("group_id")]
        public long? GroupId { get; set; }

        [JsonPropertyName("stock_count")]
        public decimal? StockCount { get; set; }

        [JsonPropertyName("stock_limit_notify")]
        public bool StockLimitNotify { get; set; }

        [JsonPropertyName("stock_limit_notify_frequency")]
        public string StockLimitNotifyFrequency { get; set; } // ALWAYS, ONCE

        [JsonPropertyName("stock_limit")]
        public decimal? StockLimit { get; set; }

        [JsonPropertyName("export_identifier")]
        public string ExportIdentifier { get; set; }

        [JsonPropertyName("login_id")]
        public long? LoginId { get; set; }

        [JsonPropertyName("note")]
        public string Note { get; set; }

        [JsonPropertyName("is_archived")]
        public bool IsArchived { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }

        // Display Properties
        public string DisplayType => Type == "SERVICE" ? "Dienstleistung" : "Produkt";
        public string DisplayInfo => $"{Number} - {Description}";
    }

    public class EasybillProductList
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
        public EasybillProduct[] Items { get; set; }
    }
}
