using System;
using System.Collections.Generic;
using System.Linq;

namespace Projektsoftware.Models
{
    public class SalesAppointment
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string ContactName { get; set; } = "";
        public string ContactEmail { get; set; } = "";
        public string ContactCompany { get; set; } = "";
        public string ContactPhone { get; set; } = "";
        public DateTime AppointmentDate { get; set; }
        public DateTime AppointmentEnd { get; set; }
        public string Location { get; set; } = "";
        public string Notes { get; set; } = "";
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public RsvpStatus RsvpStatus { get; set; } = RsvpStatus.Pending;
        public DateTime? RsvpAnsweredAt { get; set; }
        public string ICalUid { get; set; } = "";
        public string WebexMeetingId { get; set; } = "";
        public string WebexJoinLink { get; set; } = "";

        /// <summary>
        /// Pro-Person RSVP: Email → Status. Wird als JSON in der DB gespeichert.
        /// </summary>
        public Dictionary<string, RsvpStatus> RsvpDetails { get; set; } = new();

        public string RsvpIcon => RsvpStatus switch
        {
            RsvpStatus.Accepted  => "✅",
            RsvpStatus.Declined  => "❌",
            RsvpStatus.Tentative => "❓",
            _                    => "⏳"
        };

        public string RsvpText => RsvpStatus switch
        {
            RsvpStatus.Accepted  => "Angenommen",
            RsvpStatus.Declined  => "Abgesagt",
            RsvpStatus.Tentative => "Vielleicht",
            _                    => "Ausstehend"
        };

        public string DurationText
        {
            get
            {
                var d = AppointmentEnd - AppointmentDate;
                return d.TotalHours >= 1
                    ? $"{(int)d.TotalHours}h {d.Minutes:00}min"
                    : $"{d.Minutes} min";
            }
        }

        /// <summary>
        /// Zusammenfassender RSVP-Text für alle Teilnehmer.
        /// </summary>
        public string RsvpSummary
        {
            get
            {
                if (RsvpDetails.Count == 0) return RsvpText;
                var accepted  = RsvpDetails.Values.Count(v => v == RsvpStatus.Accepted);
                var declined  = RsvpDetails.Values.Count(v => v == RsvpStatus.Declined);
                var tentative = RsvpDetails.Values.Count(v => v == RsvpStatus.Tentative);
                var total     = ContactEmail.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
                return $"✅ {accepted}  ❌ {declined}  ❓ {tentative}  ⏳ {total - accepted - declined - tentative}";
            }
        }
    }

    public enum RsvpStatus
    {
        Pending   = 0,
        Accepted  = 1,
        Declined  = 2,
        Tentative = 3
    }
}
