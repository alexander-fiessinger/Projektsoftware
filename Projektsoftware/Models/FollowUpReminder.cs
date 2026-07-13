using System;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Einfache Wiedervorlage / Follow-up Erinnerung (lokal persistiert)
    /// </summary>
    public class FollowUpReminder
    {
        public int Id { get; set; }
        public int? LeadId { get; set; }
        public string LeadTitle { get; set; } = "";
        public string ContactName { get; set; } = "";
        public DateTime DueDate { get; set; }
        public string Note { get; set; } = "";
        public bool Completed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
