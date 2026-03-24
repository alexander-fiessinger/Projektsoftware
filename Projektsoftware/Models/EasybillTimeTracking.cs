using System.Text.Json.Serialization;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Easybill Zeiterfassung für Projekte
    /// Dokumentation: https://api.easybill.de/rest/v1/time-trackings
    /// </summary>
    public class EasybillTimeTracking
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("date_from")]
        public string DateFrom { get; set; } // Format: "YYYY-MM-DD HH:MM:SS"

        [JsonPropertyName("date_thru")]
        public string DateThru { get; set; } // Format: "YYYY-MM-DD HH:MM:SS"

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("cleared_at")]
        public string? ClearedAt { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("position_id")]
        public long? PositionId { get; set; }

        [JsonPropertyName("project_id")]
        public long? ProjectId { get; set; }

        [JsonPropertyName("login_id")]
        public long? LoginId { get; set; }

        [JsonPropertyName("timer_value")]
        public int? TimerValue { get; set; }

        [JsonPropertyName("hourly_rate")]
        public decimal? HourlyRate { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }

    public class EasybillTimeTrackingList
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
        public EasybillTimeTracking[] Items { get; set; } = Array.Empty<EasybillTimeTracking>();
    }
}
