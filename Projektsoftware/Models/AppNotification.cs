using System;

namespace Projektsoftware.Models
{
    public enum NotificationSeverity
    {
        Info,
        Warning,
        Error
    }

    public class AppNotification
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string SeverityIcon => Severity switch
        {
            NotificationSeverity.Error   => "🔴",
            NotificationSeverity.Warning => "🟡",
            _                            => "🔵"
        };

        public string TimestampText => Timestamp.ToString("dd.MM. HH:mm");
    }
}
