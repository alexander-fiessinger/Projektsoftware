using System;

namespace Projektsoftware.Models
{
    public class TimeEntry
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string EmployeeName { get; set; }
        public string ClientName { get; set; }
        public long? EasybillCustomerId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Duration { get; set; }
        public string Description { get; set; }
        public string Activity { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsExported { get; set; }
        public DateTime? ExportedToEasybillAt { get; set; }
        public long? EasybillPositionId { get; set; }

        public TimeEntry()
        {
            CreatedAt = DateTime.Now;
            Date = DateTime.Now;
            IsExported = false;
        }
    }
}
