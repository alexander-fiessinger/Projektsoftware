using System;
using System.Linq;

namespace Projektsoftware.Models
{
    public class CrmContact
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Position { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public CrmContact()
        {
            CreatedAt = DateTime.Now;
            IsActive = true;
        }

        public string DisplayName => $"{FirstName} {LastName}".Trim();
        public string ContactInfo => string.Join(" • ", new[] { Position, CustomerName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
