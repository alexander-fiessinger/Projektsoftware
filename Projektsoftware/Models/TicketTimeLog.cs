using System;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Zeiterfassung für Tickets
    /// </summary>
    public class TicketTimeLog
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Description { get; set; }
        public int MinutesSpent { get; set; }
        public DateTime LoggedAt { get; set; }

        public TicketTimeLog()
        {
            LoggedAt = DateTime.Now;
        }

        public string DurationText
        {
            get
            {
                var hours = MinutesSpent / 60;
                var minutes = MinutesSpent % 60;
                if (hours > 0)
                    return $"{hours}h {minutes}min";
                return $"{minutes}min";
            }
        }
    }
}
