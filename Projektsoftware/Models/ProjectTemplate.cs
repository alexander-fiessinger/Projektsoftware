using System.Collections.Generic;

namespace Projektsoftware.Models
{
    public class ProjectTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int DefaultDurationDays { get; set; } = 30;
        public List<TemplateTask> Tasks { get; set; } = new();
    }

    public class TemplateTask
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Priority { get; set; } = "Normal";
        public int DueAfterDays { get; set; } = 7;
    }
}
