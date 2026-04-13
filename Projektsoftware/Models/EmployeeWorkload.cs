namespace Projektsoftware.Models
{
    public class EmployeeWorkload
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public decimal HoursThisWeek { get; set; }
        public int OpenTasks { get; set; }
        public decimal TargetHoursPerWeek { get; set; } = 40;

        public decimal UtilizationPercent => TargetHoursPerWeek > 0
            ? System.Math.Min(100, System.Math.Round(HoursThisWeek / TargetHoursPerWeek * 100, 0))
            : 0;

        public string Display => $"{EmployeeName}: {HoursThisWeek:F1}h / {TargetHoursPerWeek}h ({UtilizationPercent}%)";
    }
}
