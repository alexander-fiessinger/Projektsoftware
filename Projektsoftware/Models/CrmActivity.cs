using System;

namespace Projektsoftware.Models
{
    public enum CrmActivityType
    {
        Note = 0,
        Call = 1,
        Email = 2,
        Meeting = 3,
        Task = 4
    }

    public class CrmActivity
    {
        public int Id { get; set; }
        public int? ContactId { get; set; }
        public string ContactName { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public CrmActivityType Type { get; set; }
        public string Subject { get; set; }
        public string Notes { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsCompleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public CrmActivity()
        {
            CreatedAt = DateTime.Now;
        }

        public string TypeText => Type switch
        {
            CrmActivityType.Note => "📝 Notiz",
            CrmActivityType.Call => "📞 Anruf",
            CrmActivityType.Email => "✉️ E-Mail",
            CrmActivityType.Meeting => "🤝 Meeting",
            CrmActivityType.Task => "✓ Aufgabe",
            _ => "Aktivität"
        };

        public string StatusText
        {
            get
            {
                if (IsCompleted) return "✅ Abgeschlossen";
                if (DueDate.HasValue && DueDate.Value < DateTime.Now) return "⚠️ Überfällig";
                return "⏳ Offen";
            }
        }
    }
}
