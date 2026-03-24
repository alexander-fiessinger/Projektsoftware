namespace Projektsoftware.Models
{
    public class DashboardStats
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int TotalTasks { get; set; }
        public int OpenTasks { get; set; }
        public int CompletedTasks { get; set; }
        public decimal TotalHoursLogged { get; set; }
        public decimal TotalBudget { get; set; }
        public int UpcomingMeetings { get; set; }
        public int OverdueTasks { get; set; }
        public int ActiveEmployees { get; set; }
    }
}
