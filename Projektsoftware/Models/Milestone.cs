using System;

namespace Projektsoftware.Models
{
    public class Milestone
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Status { get; set; } // "Geplant", "In Arbeit", "Erreicht", "Verzögert"
        public int CompletionPercentage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
