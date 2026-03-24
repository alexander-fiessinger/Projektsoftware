using System;
using System.Collections.Generic;

namespace Projektsoftware.Models
{
    public class MeetingProtocol
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Title { get; set; }
        public DateTime MeetingDate { get; set; }
        public string Location { get; set; }
        public string Participants { get; set; }
        public string Agenda { get; set; }
        public string Discussion { get; set; }
        public string Decisions { get; set; }
        public string ActionItems { get; set; }
        public string NextMeetingDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public MeetingProtocol()
        {
            CreatedAt = DateTime.Now;
            MeetingDate = DateTime.Now;
        }
    }
}
