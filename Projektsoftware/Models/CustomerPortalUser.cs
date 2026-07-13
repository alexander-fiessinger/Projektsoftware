using System;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Portal-Benutzerkonto für das Kundenportal (getrennt von Mitarbeiter-Benutzern).
    /// Wird optional mit einem <see cref="Customer"/> verknüpft und muss durch
    /// einen Mitarbeiter freigeschaltet werden, bevor Preise sichtbar sind.
    /// </summary>
    public class CustomerPortalUser
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string Email { get; set; }
        public string ContactName { get; set; }
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? LastLogin { get; set; }

        // Display-only (aus JOIN auf customers)
        public string CustomerName { get; set; }

        public string StatusText => !IsActive
            ? "🔒 Gesperrt"
            : (IsApproved ? "✓ Freigeschaltet" : "⏳ Wartet auf Freischaltung");
    }
}
