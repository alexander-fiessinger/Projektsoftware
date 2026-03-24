using System;

namespace Projektsoftware.Models
{
    public class ProjectTask
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AssignedTo { get; set; }
        public string ClientName { get; set; }
        public long? EasybillCustomerId { get; set; }
        public string Status { get; set; } // "Offen", "In Arbeit", "Erledigt", "Blockiert"
        public string Priority { get; set; } // "Niedrig", "Normal", "Hoch", "Kritisch"
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public int EstimatedHours { get; set; }
        public int ActualHours { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
