using System;

namespace Projektsoftware.Models
{
    public class WeeklyTimesheetEntry
    {
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Hours { get; set; }

        public string DayName => Date.ToString("ddd");
    }
}
