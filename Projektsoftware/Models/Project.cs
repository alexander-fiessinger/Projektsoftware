using System;

namespace Projektsoftware.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
        public string ClientName { get; set; }
        public long? EasybillCustomerId { get; set; }
        public long? EasybillProjectId { get; set; }
        public decimal Budget { get; set; }
        public DateTime CreatedAt { get; set; }

        public Project()
        {
            CreatedAt = DateTime.Now;
            StartDate = DateTime.Now;
            Status = "Aktiv";
        }
    }
}
