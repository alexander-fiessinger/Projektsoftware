using System;

namespace Projektsoftware.Models
{
    public class Meeting
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public string? Participants { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public bool IsWebexMeeting { get; set; }
        public string? WebexMeetingId { get; set; }
        public string? WebexJoinLink { get; set; }
        public string? WebexHostKey { get; set; }
        public string? WebexPassword { get; set; }
        public string? WebexSipAddress { get; set; }
        public DateTime CreatedAt { get; set; }

        public Meeting()
        {
            CreatedAt = DateTime.Now;
            StartTime = DateTime.Now.AddHours(1);
            EndTime = DateTime.Now.AddHours(2);
        }

        public string DisplayTime => $"{StartTime:HH:mm} - {EndTime:HH:mm}";
        public string DisplayDate => StartTime.ToString("dd.MM.yyyy");
        public string DurationText
        {
            get
            {
                var duration = EndTime - StartTime;
                return duration.TotalHours >= 1
                    ? $"{(int)duration.TotalHours}h {duration.Minutes:D2}m"
                    : $"{duration.Minutes}m";
            }
        }

        public bool IsToday => StartTime.Date == DateTime.Today;
        public bool IsUpcoming => StartTime > DateTime.Now;
        public bool IsPast => EndTime < DateTime.Now;

        public string StatusText
        {
            get
            {
                if (IsPast) return "Vergangen";
                if (StartTime <= DateTime.Now && EndTime >= DateTime.Now) return "Läuft";
                return "Geplant";
            }
        }
    }
}
