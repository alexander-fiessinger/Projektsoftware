using MySql.Data.MySqlClient;
using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    public class NotificationService
    {
        private readonly string connectionString;

        public NotificationService(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<List<AppNotification>> GetNotificationsAsync()
        {
            var notifications = new List<AppNotification>();

            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Overdue tasks
                string overdueTasksQuery = @"
                    SELECT t.title, p.name as project_name, t.due_date
                    FROM tasks t
                    LEFT JOIN projects p ON t.project_id = p.id
                    WHERE t.due_date < CURDATE()
                      AND t.status NOT IN ('Erledigt', 'Abgeschlossen')
                    ORDER BY t.due_date ASC
                    LIMIT 20";

                using var cmdTasks = new MySqlCommand(overdueTasksQuery, connection);
                using var rdrTasks = await cmdTasks.ExecuteReaderAsync();
                while (await rdrTasks.ReadAsync())
                {
                    var dueDate = rdrTasks.GetDateTime(2);
                    var daysOverdue = (DateTime.Today - dueDate).Days;
                    var project = rdrTasks.IsDBNull(1) ? "" : rdrTasks.GetString(1);
                    notifications.Add(new AppNotification
                    {
                        Title = "Aufgabe überfällig",
                        Message = $"{rdrTasks.GetString(0)}" +
                                  (string.IsNullOrEmpty(project) ? "" : $" ({project})") +
                                  $" — seit {daysOverdue} Tag{(daysOverdue == 1 ? "" : "en")} überfällig",
                        Severity = daysOverdue > 7 ? NotificationSeverity.Error : NotificationSeverity.Warning,
                        Timestamp = dueDate
                    });
                }
                rdrTasks.Close();

                // Unassigned open tickets
                string unassignedQuery = @"
                    SELECT COUNT(*) FROM tickets
                    WHERE assigned_to_employee_id IS NULL
                      AND status NOT IN (3, 4)";

                using var cmdTickets = new MySqlCommand(unassignedQuery, connection);
                var unassignedCount = Convert.ToInt32(await cmdTickets.ExecuteScalarAsync());
                if (unassignedCount > 0)
                {
                    notifications.Add(new AppNotification
                    {
                        Title = "Nicht zugewiesene Tickets",
                        Message = $"{unassignedCount} offene Ticket{(unassignedCount == 1 ? "" : "s")} ohne Bearbeiter",
                        Severity = unassignedCount > 5 ? NotificationSeverity.Error : NotificationSeverity.Warning,
                        Timestamp = DateTime.Now
                    });
                }

                // SLA breached tickets
                string slaQuery = @"
                    SELECT COUNT(*) FROM tickets
                    WHERE status NOT IN (3, 4)
                      AND (
                        (priority = 3 AND TIMESTAMPDIFF(HOUR, created_at, NOW()) > 4)  OR
                        (priority = 2 AND TIMESTAMPDIFF(HOUR, created_at, NOW()) > 8)  OR
                        (priority = 1 AND TIMESTAMPDIFF(HOUR, created_at, NOW()) > 24) OR
                        (priority = 0 AND TIMESTAMPDIFF(HOUR, created_at, NOW()) > 72)
                      )";

                using var cmdSla = new MySqlCommand(slaQuery, connection);
                var slaBreached = Convert.ToInt32(await cmdSla.ExecuteScalarAsync());
                if (slaBreached > 0)
                {
                    notifications.Add(new AppNotification
                    {
                        Title = "SLA-Verletzung",
                        Message = $"{slaBreached} Ticket{(slaBreached == 1 ? "" : "s")} haben die SLA-Reaktionszeit überschritten",
                        Severity = NotificationSeverity.Error,
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch
            {
                // Silent fail — notifications are non-critical
            }

            return notifications;
        }
    }
}
