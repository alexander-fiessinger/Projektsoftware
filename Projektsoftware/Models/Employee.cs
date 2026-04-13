using System;

namespace Projektsoftware.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public decimal HourlyRate { get; set; }
        public bool IsActive { get; set; }
        public DateTime HireDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int VacationDaysTotal { get; set; } = 30;
        public int VacationDaysUsed { get; set; }

        public string FullName => $"{FirstName} {LastName}";
        public int VacationDaysRemaining => VacationDaysTotal - VacationDaysUsed;
    }
}
