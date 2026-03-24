using System;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Lokales Kundenmodell für die Datenbank
    /// </summary>
    public class Customer
    {
        public int Id { get; set; }
        
        // Basisinformationen
        public string CompanyName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        
        // Adresse
        public string Street { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        
        // Weitere Details
        public string VatId { get; set; }
        public string Note { get; set; }
        
        // Easybill-Synchronisation
        public long? EasybillCustomerId { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        
        // Metadaten
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        
        public Customer()
        {
            CreatedAt = DateTime.Now;
            IsActive = true;
            Country = "Deutschland";
        }
        
        // Display properties
        public string DisplayName => !string.IsNullOrEmpty(CompanyName) 
            ? CompanyName 
            : $"{FirstName} {LastName}".Trim();
        
        public string FullAddress => $"{Street}, {ZipCode} {City}, {Country}".Trim();
        
        public bool IsSyncedToEasybill => EasybillCustomerId.HasValue;
        
        public string SyncStatus => IsSyncedToEasybill 
            ? $"✓ Synchronisiert (ID: {EasybillCustomerId})" 
            : "⚠ Nicht synchronisiert";
    }
}
