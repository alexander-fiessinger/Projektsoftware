using System;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Benutzer für Authentifizierung und Berechtigungsverwaltung
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // User, Admin
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }

        // Display Properties
        public string EmployeeName { get; set; } = string.Empty;
        public string DisplayName => $"{Username} ({Role})";
        public string StatusText => IsActive ? "Aktiv" : "Gesperrt";
    }
}
