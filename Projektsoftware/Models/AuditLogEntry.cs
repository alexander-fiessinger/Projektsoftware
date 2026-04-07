using System;

namespace Projektsoftware.Models
{
    public class AuditLogEntry
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;

        public string TimestampText => Timestamp.ToString("dd.MM.yyyy HH:mm:ss");

        public string ActionIcon => Action switch
        {
            "Erstellt" => "➕",
            "Aktualisiert" => "✏️",
            "Gelöscht" => "🗑️",
            "Angemeldet" => "🔑",
            "Exportiert" => "📤",
            _ => "📋"
        };
    }
}
