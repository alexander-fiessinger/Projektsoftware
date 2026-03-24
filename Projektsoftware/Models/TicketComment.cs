using System;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Kommentare/Notizen zu Tickets
    /// </summary>
    public class TicketComment
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Comment { get; set; }
        public bool IsInternal { get; set; } // true = interne Notiz, false = Kunden-Antwort
        public DateTime CreatedAt { get; set; }

        public TicketComment()
        {
            CreatedAt = DateTime.Now;
            IsInternal = true;
        }

        public string CommentTypeText => IsInternal ? "Interne Notiz" : "Kunden-Antwort";
    }
}
