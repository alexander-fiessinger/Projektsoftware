using System.Text.Json.Serialization;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Easybill Projekt
    /// Dokumentation: https://api.easybill.de/rest/v1/projects
    /// </summary>
    public class EasybillProject
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("budget_amount")]
        public decimal? BudgetAmount { get; set; }

        [JsonPropertyName("budget_type")]
        public string BudgetType { get; set; } = "HOUR"; // HOUR, MONEY

        [JsonPropertyName("customer_id")]
        public long? CustomerId { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("due_at")]
        public string? DueAt { get; set; }

        [JsonPropertyName("hourly_rate")]
        public decimal? HourlyRate { get; set; }

        [JsonPropertyName("login_id")]
        public long? LoginId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "OPEN"; // OPEN, COMPLETED, CANCELED
    }

    public class EasybillProjectList
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
        public EasybillProject[] Items { get; set; } = Array.Empty<EasybillProject>();
    }
}
