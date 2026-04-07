using System;

namespace Projektsoftware.Models
{
    public class TimeEntryTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string? Activity { get; set; }
        public string? Description { get; set; }
        public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromHours(1);
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string DisplayName =>
            string.IsNullOrWhiteSpace(ProjectName)
                ? Name
                : $"{Name}  [{ProjectName}]";

        public string DurationDisplay =>
            DefaultDuration.TotalHours >= 1
                ? $"{(int)DefaultDuration.TotalHours}h {DefaultDuration.Minutes:D2}m"
                : $"{DefaultDuration.Minutes}m";
    }
}
