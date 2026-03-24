using System;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Ticket-Statistiken für Dashboard
    /// </summary>
    public class TicketStatistics
    {
        public int TotalTickets { get; set; }
        public int NewTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int WaitingTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }
        
        public int UrgentTickets { get; set; }
        public int HighPriorityTickets { get; set; }
        
        public int UnassignedTickets { get; set; }
        
        public double AverageResolutionTimeHours { get; set; }
        
        public int TodayTickets { get; set; }
        public int WeekTickets { get; set; }
        public int MonthTickets { get; set; }

        public string AverageResolutionTimeText
        {
            get
            {
                if (AverageResolutionTimeHours < 1)
                    return $"{(int)(AverageResolutionTimeHours * 60)} Minuten";
                if (AverageResolutionTimeHours < 24)
                    return $"{AverageResolutionTimeHours:F1} Stunden";
                return $"{(AverageResolutionTimeHours / 24):F1} Tage";
            }
        }
    }
}
