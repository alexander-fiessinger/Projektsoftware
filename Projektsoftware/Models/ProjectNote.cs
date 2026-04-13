using System;

namespace Projektsoftware.Models
{
    public class ProjectNote
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string Display => $"[{CreatedAt:dd.MM.yyyy HH:mm}] {Author}: {Text}";
    }
}
