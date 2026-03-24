using System;

namespace TicketAPI.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        
        // Kundendaten
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public int? CustomerId { get; set; }
        
        // Ticketdaten
        public string Subject { get; set; }
        public string Description { get; set; }
        public TicketPriority Priority { get; set; }
        public TicketStatus Status { get; set; }
        public TicketCategory Category { get; set; }
        
        // Zusätzliche Informationen
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        
        // Bearbeitung
        public int? AssignedToEmployeeId { get; set; }
        public string Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }
        
        // Metadaten
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public Ticket()
        {
            CreatedAt = DateTime.Now;
            Status = TicketStatus.New;
            Priority = TicketPriority.Medium;
            Category = TicketCategory.General;
        }
    }
    
    public enum TicketStatus
    {
        New = 0,
        InProgress = 1,
        Waiting = 2,
        Resolved = 3,
        Closed = 4
    }
    
    public enum TicketPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Urgent = 3
    }
    
    public enum TicketCategory
    {
        General = 0,
        Technical = 1,
        Billing = 2,
        FeatureRequest = 3,
        Bug = 4
    }
}
