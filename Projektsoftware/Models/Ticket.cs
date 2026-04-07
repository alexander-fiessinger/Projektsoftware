using System;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Modell für Support-Tickets
    /// </summary>
    public class Ticket
    {
        public int Id { get; set; }
        
        // Kundendaten
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public int? CustomerId { get; set; } // Optional: Verknüpfung zu bestehendem Kunden

        // Projektzuordnung für Abrechnung
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        
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
        public string AssignedToEmployeeName { get; set; }
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
        
        // Display Properties
        public string TicketNumber => $"#{Id.ToString().PadLeft(6, '0')}";
        
        public string StatusText => Status switch
        {
            TicketStatus.New => "Neu",
            TicketStatus.InProgress => "In Bearbeitung",
            TicketStatus.Waiting => "Warten auf Rückmeldung",
            TicketStatus.Resolved => "Gelöst",
            TicketStatus.Closed => "Geschlossen",
            _ => "Unbekannt"
        };
        
        public string PriorityText => Priority switch
        {
            TicketPriority.Low => "Niedrig",
            TicketPriority.Medium => "Mittel",
            TicketPriority.High => "Hoch",
            TicketPriority.Urgent => "Dringend",
            _ => "Unbekannt"
        };
        
        public string CategoryText => Category switch
        {
            TicketCategory.General => "Allgemein",
            TicketCategory.Technical => "Technisch",
            TicketCategory.Billing => "Abrechnung",
            TicketCategory.FeatureRequest => "Feature-Anfrage",
            TicketCategory.Bug => "Fehler",
            _ => "Unbekannt"
        };
        
        public string ShortDescription => Description?.Length > 100 
            ? Description.Substring(0, 100) + "..." 
            : Description;
    }
    
    public enum TicketStatus
    {
        New,
        InProgress,
        Waiting,
        Resolved,
        Closed
    }
    
    public enum TicketPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }
    
    public enum TicketCategory
    {
        General,
        Technical,
        Billing,
        FeatureRequest,
        Bug
    }
}
