using MySql.Data.MySqlClient;
using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Erweiterte Datenbankoperationen für Employees, Tasks, Milestones und Dashboard
    /// </summary>
    public partial class DatabaseService
    {
        #region Employee Methods

        public async System.Threading.Tasks.Task<List<Employee>> GetAllEmployeesAsync()
        {
            var employees = new List<Employee>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "SELECT * FROM employees ORDER BY last_name, first_name";
            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                employees.Add(new Employee
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString(reader.GetOrdinal("email")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? "" : reader.GetString(reader.GetOrdinal("phone")),
                    Position = reader.IsDBNull(reader.GetOrdinal("position")) ? "" : reader.GetString(reader.GetOrdinal("position")),
                    Department = reader.IsDBNull(reader.GetOrdinal("department")) ? "" : reader.GetString(reader.GetOrdinal("department")),
                    HourlyRate = reader.GetDecimal(reader.GetOrdinal("hourly_rate")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    HireDate = reader.GetDateTime(reader.GetOrdinal("hire_date")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    VacationDaysTotal = reader.IsDBNull(reader.GetOrdinal("vacation_days_total")) ? 30 : reader.GetInt32(reader.GetOrdinal("vacation_days_total")),
                    VacationDaysUsed = reader.IsDBNull(reader.GetOrdinal("vacation_days_used")) ? 0 : reader.GetInt32(reader.GetOrdinal("vacation_days_used"))
                });
            }

            return employees;
        }

        public async System.Threading.Tasks.Task<int> AddEmployeeAsync(Employee employee)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO employees (first_name, last_name, email, phone, position, department, hourly_rate, is_active, hire_date, created_at, vacation_days_total, vacation_days_used)
                           VALUES (@firstName, @lastName, @email, @phone, @position, @department, @hourlyRate, @isActive, @hireDate, @createdAt, @vacationTotal, @vacationUsed);
                           SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@firstName", employee.FirstName);
            cmd.Parameters.AddWithValue("@lastName", employee.LastName);
            cmd.Parameters.AddWithValue("@email", employee.Email ?? "");
            cmd.Parameters.AddWithValue("@phone", employee.Phone ?? "");
            cmd.Parameters.AddWithValue("@position", employee.Position ?? "");
            cmd.Parameters.AddWithValue("@department", employee.Department ?? "");
            cmd.Parameters.AddWithValue("@hourlyRate", employee.HourlyRate);
            cmd.Parameters.AddWithValue("@isActive", employee.IsActive);
            cmd.Parameters.AddWithValue("@hireDate", employee.HireDate);
            cmd.Parameters.AddWithValue("@createdAt", employee.CreatedAt);
            cmd.Parameters.AddWithValue("@vacationTotal", employee.VacationDaysTotal);
            cmd.Parameters.AddWithValue("@vacationUsed", employee.VacationDaysUsed);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async System.Threading.Tasks.Task UpdateEmployeeAsync(Employee employee)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"UPDATE employees SET first_name=@firstName, last_name=@lastName, email=@email, phone=@phone, 
                           position=@position, department=@department, hourly_rate=@hourlyRate, is_active=@isActive, hire_date=@hireDate,
                           vacation_days_total=@vacationTotal, vacation_days_used=@vacationUsed
                           WHERE id=@id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", employee.Id);
            cmd.Parameters.AddWithValue("@firstName", employee.FirstName);
            cmd.Parameters.AddWithValue("@lastName", employee.LastName);
            cmd.Parameters.AddWithValue("@email", employee.Email ?? "");
            cmd.Parameters.AddWithValue("@phone", employee.Phone ?? "");
            cmd.Parameters.AddWithValue("@position", employee.Position ?? "");
            cmd.Parameters.AddWithValue("@department", employee.Department ?? "");
            cmd.Parameters.AddWithValue("@hourlyRate", employee.HourlyRate);
            cmd.Parameters.AddWithValue("@isActive", employee.IsActive);
            cmd.Parameters.AddWithValue("@hireDate", employee.HireDate);
            cmd.Parameters.AddWithValue("@vacationTotal", employee.VacationDaysTotal);
            cmd.Parameters.AddWithValue("@vacationUsed", employee.VacationDaysUsed);

            await cmd.ExecuteNonQueryAsync();
        }

        public async System.Threading.Tasks.Task DeleteEmployeeAsync(int employeeId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "DELETE FROM employees WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", employeeId);

            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region ProjectTask Methods

        public async System.Threading.Tasks.Task<List<ProjectTask>> GetAllTasksAsync()
        {
            var tasks = new List<ProjectTask>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"SELECT t.*, p.name as project_name 
                           FROM tasks t
                           LEFT JOIN projects p ON t.project_id = p.id
                           ORDER BY FIELD(t.priority, 'Kritisch', 'Hoch', 'Normal', 'Niedrig'), t.due_date ASC";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tasks.Add(new ProjectTask
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    ProjectId = reader.GetInt32(reader.GetOrdinal("project_id")),
                    ProjectName = reader.IsDBNull(reader.GetOrdinal("project_name")) ? "" : reader.GetString(reader.GetOrdinal("project_name")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                    AssignedTo = reader.IsDBNull(reader.GetOrdinal("assigned_to")) ? "" : reader.GetString(reader.GetOrdinal("assigned_to")),
                    ClientName = reader.IsDBNull(reader.GetOrdinal("client_name")) ? "" : reader.GetString(reader.GetOrdinal("client_name")),
                    EasybillCustomerId = reader.IsDBNull(reader.GetOrdinal("easybill_customer_id")) ? null : reader.GetInt64(reader.GetOrdinal("easybill_customer_id")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    Priority = reader.GetString(reader.GetOrdinal("priority")),
                    DueDate = reader.IsDBNull(reader.GetOrdinal("due_date")) ? null : reader.GetDateTime(reader.GetOrdinal("due_date")),
                    CompletedDate = reader.IsDBNull(reader.GetOrdinal("completed_date")) ? null : reader.GetDateTime(reader.GetOrdinal("completed_date")),
                    EstimatedHours = reader.GetInt32(reader.GetOrdinal("estimated_hours")),
                    ActualHours = reader.GetInt32(reader.GetOrdinal("actual_hours")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                    IsRecurring = !reader.IsDBNull(reader.GetOrdinal("is_recurring")) && reader.GetBoolean(reader.GetOrdinal("is_recurring")),
                    RecurrenceIntervalDays = reader.IsDBNull(reader.GetOrdinal("recurrence_interval_days")) ? 0 : reader.GetInt32(reader.GetOrdinal("recurrence_interval_days"))
                });
            }

            return tasks;
        }

        public async System.Threading.Tasks.Task<int> AddTaskAsync(ProjectTask task)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO tasks (project_id, title, description, assigned_to, client_name, easybill_customer_id, status, priority, due_date, 
                           completed_date, estimated_hours, actual_hours, created_at, updated_at, is_recurring, recurrence_interval_days)
                           VALUES (@projectId, @title, @description, @assignedTo, @clientName, @easybillCustomerId, @status, @priority, @dueDate,
                           @completedDate, @estimatedHours, @actualHours, @createdAt, @updatedAt, @isRecurring, @recurrenceIntervalDays);
                           SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@projectId", task.ProjectId);
            cmd.Parameters.AddWithValue("@title", task.Title);
            cmd.Parameters.AddWithValue("@description", task.Description ?? "");
            cmd.Parameters.AddWithValue("@assignedTo", task.AssignedTo ?? "");
            cmd.Parameters.AddWithValue("@clientName", task.ClientName ?? "");
            cmd.Parameters.AddWithValue("@easybillCustomerId", task.EasybillCustomerId.HasValue ? (object)task.EasybillCustomerId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@status", task.Status);
            cmd.Parameters.AddWithValue("@priority", task.Priority);
            cmd.Parameters.AddWithValue("@dueDate", task.DueDate.HasValue ? (object)task.DueDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@completedDate", task.CompletedDate.HasValue ? (object)task.CompletedDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@estimatedHours", task.EstimatedHours);
            cmd.Parameters.AddWithValue("@actualHours", task.ActualHours);
            cmd.Parameters.AddWithValue("@createdAt", task.CreatedAt);
            cmd.Parameters.AddWithValue("@updatedAt", task.UpdatedAt.HasValue ? (object)task.UpdatedAt.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@isRecurring", task.IsRecurring);
            cmd.Parameters.AddWithValue("@recurrenceIntervalDays", task.RecurrenceIntervalDays);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async System.Threading.Tasks.Task UpdateTaskAsync(ProjectTask task)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"UPDATE tasks SET project_id=@projectId, title=@title, description=@description, assigned_to=@assignedTo,
                           client_name=@clientName, easybill_customer_id=@easybillCustomerId,
                           status=@status, priority=@priority, due_date=@dueDate, completed_date=@completedDate,
                           estimated_hours=@estimatedHours, actual_hours=@actualHours, updated_at=@updatedAt,
                           is_recurring=@isRecurring, recurrence_interval_days=@recurrenceIntervalDays
                           WHERE id=@id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", task.Id);
            cmd.Parameters.AddWithValue("@projectId", task.ProjectId);
            cmd.Parameters.AddWithValue("@title", task.Title);
            cmd.Parameters.AddWithValue("@description", task.Description ?? "");
            cmd.Parameters.AddWithValue("@assignedTo", task.AssignedTo ?? "");
            cmd.Parameters.AddWithValue("@clientName", task.ClientName ?? "");
            cmd.Parameters.AddWithValue("@easybillCustomerId", task.EasybillCustomerId.HasValue ? (object)task.EasybillCustomerId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@status", task.Status);
            cmd.Parameters.AddWithValue("@priority", task.Priority);
            cmd.Parameters.AddWithValue("@dueDate", task.DueDate.HasValue ? (object)task.DueDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@completedDate", task.CompletedDate.HasValue ? (object)task.CompletedDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@estimatedHours", task.EstimatedHours);
            cmd.Parameters.AddWithValue("@actualHours", task.ActualHours);
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@isRecurring", task.IsRecurring);
            cmd.Parameters.AddWithValue("@recurrenceIntervalDays", task.RecurrenceIntervalDays);

            await cmd.ExecuteNonQueryAsync();
        }

        public async System.Threading.Tasks.Task DeleteTaskAsync(int taskId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "DELETE FROM tasks WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", taskId);

            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Milestone Methods

        public async System.Threading.Tasks.Task<List<Milestone>> GetAllMilestonesAsync()
        {
            var milestones = new List<Milestone>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"SELECT m.*, p.name as project_name 
                           FROM milestones m
                           LEFT JOIN projects p ON m.project_id = p.id
                           ORDER BY m.due_date ASC";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                milestones.Add(new Milestone
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    ProjectId = reader.GetInt32(reader.GetOrdinal("project_id")),
                    ProjectName = reader.IsDBNull(reader.GetOrdinal("project_name")) ? "" : reader.GetString(reader.GetOrdinal("project_name")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                    DueDate = reader.GetDateTime(reader.GetOrdinal("due_date")),
                    CompletedDate = reader.IsDBNull(reader.GetOrdinal("completed_date")) ? null : reader.GetDateTime(reader.GetOrdinal("completed_date")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    CompletionPercentage = reader.GetInt32(reader.GetOrdinal("completion_percentage")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }

            return milestones;
        }

        public async System.Threading.Tasks.Task<int> AddMilestoneAsync(Milestone milestone)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO milestones (project_id, title, description, due_date, completed_date, status, completion_percentage, created_at)
                           VALUES (@projectId, @title, @description, @dueDate, @completedDate, @status, @completionPercentage, @createdAt);
                           SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@projectId", milestone.ProjectId);
            cmd.Parameters.AddWithValue("@title", milestone.Title);
            cmd.Parameters.AddWithValue("@description", milestone.Description ?? "");
            cmd.Parameters.AddWithValue("@dueDate", milestone.DueDate);
            cmd.Parameters.AddWithValue("@completedDate", milestone.CompletedDate.HasValue ? (object)milestone.CompletedDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@status", milestone.Status);
            cmd.Parameters.AddWithValue("@completionPercentage", milestone.CompletionPercentage);
            cmd.Parameters.AddWithValue("@createdAt", milestone.CreatedAt);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async System.Threading.Tasks.Task UpdateMilestoneAsync(Milestone milestone)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"UPDATE milestones SET project_id=@projectId, title=@title, description=@description, 
                           due_date=@dueDate, completed_date=@completedDate, status=@status, completion_percentage=@completionPercentage
                           WHERE id=@id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", milestone.Id);
            cmd.Parameters.AddWithValue("@projectId", milestone.ProjectId);
            cmd.Parameters.AddWithValue("@title", milestone.Title);
            cmd.Parameters.AddWithValue("@description", milestone.Description ?? "");
            cmd.Parameters.AddWithValue("@dueDate", milestone.DueDate);
            cmd.Parameters.AddWithValue("@completedDate", milestone.CompletedDate.HasValue ? (object)milestone.CompletedDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@status", milestone.Status);
            cmd.Parameters.AddWithValue("@completionPercentage", milestone.CompletionPercentage);

            await cmd.ExecuteNonQueryAsync();
        }

        public async System.Threading.Tasks.Task DeleteMilestoneAsync(int milestoneId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "DELETE FROM milestones WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", milestoneId);

            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Dashboard Methods

        public async System.Threading.Tasks.Task<DashboardStats> GetDashboardStatsAsync()
        {
            var stats = new DashboardStats();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // Project statistics
            string projectQuery = @"SELECT 
                                    COUNT(*) as total,
                                    COALESCE(SUM(CASE WHEN status IN ('Aktiv', 'In Bearbeitung') THEN 1 ELSE 0 END), 0) as active,
                                    COALESCE(SUM(CASE WHEN status = 'Abgeschlossen' THEN 1 ELSE 0 END), 0) as completed,
                                    COALESCE(SUM(budget), 0) as total_budget
                                  FROM projects";
            using var cmd1 = new MySqlCommand(projectQuery, connection);
            using var reader1 = await cmd1.ExecuteReaderAsync();
            if (await reader1.ReadAsync())
            {
                stats.TotalProjects = reader1.IsDBNull(0) ? 0 : reader1.GetInt32(0);
                stats.ActiveProjects = reader1.IsDBNull(1) ? 0 : reader1.GetInt32(1);
                stats.CompletedProjects = reader1.IsDBNull(2) ? 0 : reader1.GetInt32(2);
                stats.TotalBudget = reader1.IsDBNull(3) ? 0 : reader1.GetDecimal(3);
            }
            reader1.Close();

            // Task statistics
            string taskQuery = @"SELECT 
                                COUNT(*) as total,
                                COALESCE(SUM(CASE WHEN status IN ('Offen', 'In Arbeit') THEN 1 ELSE 0 END), 0) as open,
                                COALESCE(SUM(CASE WHEN status = 'Erledigt' THEN 1 ELSE 0 END), 0) as completed,
                                COALESCE(SUM(CASE WHEN status != 'Erledigt' AND due_date < CURDATE() THEN 1 ELSE 0 END), 0) as overdue
                              FROM tasks";
            using var cmd2 = new MySqlCommand(taskQuery, connection);
            using var reader2 = await cmd2.ExecuteReaderAsync();
            if (await reader2.ReadAsync())
            {
                stats.TotalTasks = reader2.IsDBNull(0) ? 0 : reader2.GetInt32(0);
                stats.OpenTasks = reader2.IsDBNull(1) ? 0 : reader2.GetInt32(1);
                stats.CompletedTasks = reader2.IsDBNull(2) ? 0 : reader2.GetInt32(2);
                stats.OverdueTasks = reader2.IsDBNull(3) ? 0 : reader2.GetInt32(3);
            }
            reader2.Close();

            // Total hours logged
            string hoursQuery = "SELECT COALESCE(SUM(TIME_TO_SEC(duration))/3600, 0) as total_hours FROM time_entries";
            using var cmd3 = new MySqlCommand(hoursQuery, connection);
            var hoursResult = await cmd3.ExecuteScalarAsync();
            stats.TotalHoursLogged = hoursResult != DBNull.Value ? Convert.ToDecimal(hoursResult) : 0;

            // Upcoming meetings (next 7 days)
            string meetingsQuery = @"SELECT COUNT(*) FROM meeting_protocols 
                                   WHERE meeting_date BETWEEN NOW() AND DATE_ADD(NOW(), INTERVAL 7 DAY)";
            using var cmd4 = new MySqlCommand(meetingsQuery, connection);
            var meetingsResult = await cmd4.ExecuteScalarAsync();
            stats.UpcomingMeetings = meetingsResult != DBNull.Value ? Convert.ToInt32(meetingsResult) : 0;

            // Active employees
            string employeesQuery = "SELECT COUNT(*) FROM employees WHERE is_active = TRUE";
            using var cmd5 = new MySqlCommand(employeesQuery, connection);
            var employeesResult = await cmd5.ExecuteScalarAsync();
            stats.ActiveEmployees = employeesResult != DBNull.Value ? Convert.ToInt32(employeesResult) : 0;

            // Budget-Auslastung: Top 5 Projekte mit Budget, geordnet nach Auslastung
            string budgetQuery = @"
                SELECT p.id, p.name, p.budget,
                       COALESCE(SUM(TIME_TO_SEC(te.duration))/3600, 0) AS logged_hours
                FROM projects p
                LEFT JOIN time_entries te ON te.project_id = p.id
                WHERE p.budget > 0 AND p.status IN ('Aktiv', 'In Bearbeitung')
                GROUP BY p.id, p.name, p.budget
                ORDER BY logged_hours / p.budget DESC
                LIMIT 5";
            using var cmd6 = new MySqlCommand(budgetQuery, connection);
            using var reader6 = await cmd6.ExecuteReaderAsync();
            while (await reader6.ReadAsync())
            {
                var budget = reader6.GetDecimal(reader6.GetOrdinal("budget"));
                var hours  = Convert.ToDecimal(reader6["logged_hours"]);
                // Estimate: treat budget as EUR, derive budgeted hours at 100 €/h fallback
                var avgRate = 100m;
                var budgetedHours = avgRate > 0 ? budget / avgRate : 0;
                var usagePct = budgetedHours > 0 ? Math.Min(hours / budgetedHours * 100m, 150m) : 0m;
                stats.TopBudgetProjects.Add(new ProjectBudgetStat
                {
                    ProjectId   = reader6.GetInt32(reader6.GetOrdinal("id")),
                    ProjectName = reader6.GetString(reader6.GetOrdinal("name")),
                    Budget      = budget,
                    LoggedHours = hours,
                    BudgetUsagePercent = Math.Round(usagePct, 1)
                });
            }
            reader6.Close();

            return stats;
        }

        #endregion

        #region Ticket Methods

        public async System.Threading.Tasks.Task<List<Ticket>> GetAllTicketsAsync()
        {
            var tickets = new List<Ticket>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"SELECT t.*, 
                            CONCAT(e.first_name, ' ', e.last_name) as employee_name,
                            p.name as project_name
                            FROM tickets t
                            LEFT JOIN employees e ON t.assigned_to_employee_id = e.id
                            LEFT JOIN projects p ON t.project_id = p.id
                            ORDER BY t.created_at DESC";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tickets.Add(new Ticket
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    CustomerName = reader.GetString(reader.GetOrdinal("customer_name")),
                    CustomerEmail = reader.GetString(reader.GetOrdinal("customer_email")),
                    CustomerPhone = reader.IsDBNull(reader.GetOrdinal("customer_phone")) ? "" : reader.GetString(reader.GetOrdinal("customer_phone")),
                    CustomerId = reader.IsDBNull(reader.GetOrdinal("customer_id")) ? null : reader.GetInt32(reader.GetOrdinal("customer_id")),
                    Subject = reader.GetString(reader.GetOrdinal("subject")),
                    Description = reader.GetString(reader.GetOrdinal("description")),
                    Priority = (TicketPriority)reader.GetInt32(reader.GetOrdinal("priority")),
                    Status = (TicketStatus)reader.GetInt32(reader.GetOrdinal("status")),
                    Category = (TicketCategory)reader.GetInt32(reader.GetOrdinal("category")),
                    IpAddress = reader.IsDBNull(reader.GetOrdinal("ip_address")) ? "" : reader.GetString(reader.GetOrdinal("ip_address")),
                    UserAgent = reader.IsDBNull(reader.GetOrdinal("user_agent")) ? "" : reader.GetString(reader.GetOrdinal("user_agent")),
                    AssignedToEmployeeId = reader.IsDBNull(reader.GetOrdinal("assigned_to_employee_id")) ? null : reader.GetInt32(reader.GetOrdinal("assigned_to_employee_id")),
                    AssignedToEmployeeName = reader.IsDBNull(reader.GetOrdinal("employee_name")) ? "" : reader.GetString(reader.GetOrdinal("employee_name")),
                    ProjectId = reader.IsDBNull(reader.GetOrdinal("project_id")) ? null : reader.GetInt32(reader.GetOrdinal("project_id")),
                    ProjectName = reader.IsDBNull(reader.GetOrdinal("project_name")) ? "" : reader.GetString(reader.GetOrdinal("project_name")),
                    Resolution = reader.IsDBNull(reader.GetOrdinal("resolution")) ? "" : reader.GetString(reader.GetOrdinal("resolution")),
                    ResolvedAt = reader.IsDBNull(reader.GetOrdinal("resolved_at")) ? null : reader.GetDateTime(reader.GetOrdinal("resolved_at")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return tickets;
        }

        public async System.Threading.Tasks.Task<int> AddTicketAsync(Ticket ticket)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO tickets (customer_name, customer_email, customer_phone, customer_id, 
                           subject, description, priority, status, category, 
                           ip_address, user_agent, assigned_to_employee_id, project_id, resolution, resolved_at, 
                           created_at, updated_at)
                           VALUES (@customerName, @customerEmail, @customerPhone, @customerId, 
                           @subject, @description, @priority, @status, @category,
                           @ipAddress, @userAgent, @assignedToEmployeeId, @projectId, @resolution, @resolvedAt,
                           @createdAt, @updatedAt);
                           SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@customerName", ticket.CustomerName);
            cmd.Parameters.AddWithValue("@customerEmail", ticket.CustomerEmail);
            cmd.Parameters.AddWithValue("@customerPhone", ticket.CustomerPhone ?? "");
            cmd.Parameters.AddWithValue("@customerId", ticket.CustomerId.HasValue ? (object)ticket.CustomerId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@subject", ticket.Subject);
            cmd.Parameters.AddWithValue("@description", ticket.Description);
            cmd.Parameters.AddWithValue("@priority", (int)ticket.Priority);
            cmd.Parameters.AddWithValue("@status", (int)ticket.Status);
            cmd.Parameters.AddWithValue("@category", (int)ticket.Category);
            cmd.Parameters.AddWithValue("@ipAddress", ticket.IpAddress ?? "");
            cmd.Parameters.AddWithValue("@userAgent", ticket.UserAgent ?? "");
            cmd.Parameters.AddWithValue("@assignedToEmployeeId", ticket.AssignedToEmployeeId.HasValue ? (object)ticket.AssignedToEmployeeId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@projectId", ticket.ProjectId.HasValue ? (object)ticket.ProjectId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@resolution", ticket.Resolution ?? "");
            cmd.Parameters.AddWithValue("@resolvedAt", ticket.ResolvedAt.HasValue ? (object)ticket.ResolvedAt.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@createdAt", ticket.CreatedAt);
            cmd.Parameters.AddWithValue("@updatedAt", ticket.UpdatedAt.HasValue ? (object)ticket.UpdatedAt.Value : DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async System.Threading.Tasks.Task UpdateTicketAsync(Ticket ticket)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"UPDATE tickets SET 
                           customer_name=@customerName, customer_email=@customerEmail, 
                           customer_phone=@customerPhone, customer_id=@customerId,
                           subject=@subject, description=@description, 
                           priority=@priority, status=@status, category=@category,
                           assigned_to_employee_id=@assignedToEmployeeId,
                           project_id=@projectId,
                           resolution=@resolution, resolved_at=@resolvedAt,
                           updated_at=@updatedAt
                           WHERE id=@id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", ticket.Id);
            cmd.Parameters.AddWithValue("@customerName", ticket.CustomerName);
            cmd.Parameters.AddWithValue("@customerEmail", ticket.CustomerEmail);
            cmd.Parameters.AddWithValue("@customerPhone", ticket.CustomerPhone ?? "");
            cmd.Parameters.AddWithValue("@customerId", ticket.CustomerId.HasValue ? (object)ticket.CustomerId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@subject", ticket.Subject);
            cmd.Parameters.AddWithValue("@description", ticket.Description);
            cmd.Parameters.AddWithValue("@priority", (int)ticket.Priority);
            cmd.Parameters.AddWithValue("@status", (int)ticket.Status);
            cmd.Parameters.AddWithValue("@category", (int)ticket.Category);
            cmd.Parameters.AddWithValue("@assignedToEmployeeId", ticket.AssignedToEmployeeId.HasValue ? (object)ticket.AssignedToEmployeeId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@projectId", ticket.ProjectId.HasValue ? (object)ticket.ProjectId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@resolution", ticket.Resolution ?? "");
            cmd.Parameters.AddWithValue("@resolvedAt", ticket.ResolvedAt.HasValue ? (object)ticket.ResolvedAt.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);

            await cmd.ExecuteNonQueryAsync();
        }

        public async System.Threading.Tasks.Task DeleteTicketAsync(int ticketId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "DELETE FROM tickets WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", ticketId);

            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Ticket Comments Methods

        public async System.Threading.Tasks.Task<List<TicketComment>> GetTicketCommentsAsync(int ticketId)
        {
            var comments = new List<TicketComment>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"SELECT tc.*, CONCAT(e.first_name, ' ', e.last_name) as employee_name
                            FROM ticket_comments tc
                            LEFT JOIN employees e ON tc.employee_id = e.id
                            WHERE tc.ticket_id = @ticketId
                            ORDER BY tc.created_at ASC";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ticketId", ticketId);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                comments.Add(new TicketComment
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    TicketId = reader.GetInt32(reader.GetOrdinal("ticket_id")),
                    EmployeeId = reader.GetInt32(reader.GetOrdinal("employee_id")),
                    EmployeeName = reader.IsDBNull(reader.GetOrdinal("employee_name")) ? "" : reader.GetString(reader.GetOrdinal("employee_name")),
                    Comment = reader.GetString(reader.GetOrdinal("comment")),
                    IsInternal = reader.GetBoolean(reader.GetOrdinal("is_internal")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }

            return comments;
        }

        public async System.Threading.Tasks.Task<int> AddTicketCommentAsync(TicketComment comment)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO ticket_comments (ticket_id, employee_id, comment, is_internal, created_at)
                            VALUES (@ticketId, @employeeId, @comment, @isInternal, @createdAt);
                            SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ticketId", comment.TicketId);
            cmd.Parameters.AddWithValue("@employeeId", comment.EmployeeId);
            cmd.Parameters.AddWithValue("@comment", comment.Comment);
            cmd.Parameters.AddWithValue("@isInternal", comment.IsInternal);
            cmd.Parameters.AddWithValue("@createdAt", comment.CreatedAt);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        #endregion

        #region Ticket Time Logs Methods

        public async System.Threading.Tasks.Task<List<TicketTimeLog>> GetTicketTimeLogsAsync(int ticketId)
        {
            var timeLogs = new List<TicketTimeLog>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"SELECT ttl.*, CONCAT(e.first_name, ' ', e.last_name) as employee_name
                            FROM ticket_time_logs ttl
                            LEFT JOIN employees e ON ttl.employee_id = e.id
                            WHERE ttl.ticket_id = @ticketId
                            ORDER BY ttl.logged_at DESC";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ticketId", ticketId);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                timeLogs.Add(new TicketTimeLog
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    TicketId = reader.GetInt32(reader.GetOrdinal("ticket_id")),
                    EmployeeId = reader.GetInt32(reader.GetOrdinal("employee_id")),
                    EmployeeName = reader.IsDBNull(reader.GetOrdinal("employee_name")) ? "" : reader.GetString(reader.GetOrdinal("employee_name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                    MinutesSpent = reader.GetInt32(reader.GetOrdinal("minutes_spent")),
                    LoggedAt = reader.GetDateTime(reader.GetOrdinal("logged_at"))
                });
            }

            return timeLogs;
        }

        public async System.Threading.Tasks.Task<int> AddTicketTimeLogAsync(TicketTimeLog timeLog)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO ticket_time_logs (ticket_id, employee_id, description, minutes_spent, logged_at)
                            VALUES (@ticketId, @employeeId, @description, @minutesSpent, @loggedAt);
                            SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ticketId", timeLog.TicketId);
            cmd.Parameters.AddWithValue("@employeeId", timeLog.EmployeeId);
            cmd.Parameters.AddWithValue("@description", timeLog.Description ?? "");
            cmd.Parameters.AddWithValue("@minutesSpent", timeLog.MinutesSpent);
            cmd.Parameters.AddWithValue("@loggedAt", timeLog.LoggedAt);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async System.Threading.Tasks.Task<int> GetTotalTimeSpentAsync(int ticketId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "SELECT COALESCE(SUM(minutes_spent), 0) FROM ticket_time_logs WHERE ticket_id = @ticketId";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ticketId", ticketId);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        #endregion

        #region Ticket Statistics Methods

        public async System.Threading.Tasks.Task<TicketStatistics> GetTicketStatisticsAsync()
        {
            var stats = new TicketStatistics();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // Gesamtanzahl und Status-Verteilung
            string statusQuery = @"SELECT 
                                    COUNT(*) as total,
                                    IFNULL(SUM(CASE WHEN status = 0 THEN 1 ELSE 0 END), 0) as new_tickets,
                                    IFNULL(SUM(CASE WHEN status = 1 THEN 1 ELSE 0 END), 0) as in_progress,
                                    IFNULL(SUM(CASE WHEN status = 2 THEN 1 ELSE 0 END), 0) as waiting,
                                    IFNULL(SUM(CASE WHEN status = 3 THEN 1 ELSE 0 END), 0) as resolved,
                                    IFNULL(SUM(CASE WHEN status = 4 THEN 1 ELSE 0 END), 0) as closed,
                                    IFNULL(SUM(CASE WHEN priority = 3 THEN 1 ELSE 0 END), 0) as urgent,
                                    IFNULL(SUM(CASE WHEN priority = 2 THEN 1 ELSE 0 END), 0) as high_prio,
                                    IFNULL(SUM(CASE WHEN assigned_to_employee_id IS NULL THEN 1 ELSE 0 END), 0) as unassigned
                                   FROM tickets";

            using var cmd1 = new MySqlCommand(statusQuery, connection);
            using var reader1 = await cmd1.ExecuteReaderAsync();
            if (await reader1.ReadAsync())
            {
                stats.TotalTickets = reader1.GetInt32(0);
                stats.NewTickets = reader1.GetInt32(1);
                stats.InProgressTickets = reader1.GetInt32(2);
                stats.WaitingTickets = reader1.GetInt32(3);
                stats.ResolvedTickets = reader1.GetInt32(4);
                stats.ClosedTickets = reader1.GetInt32(5);
                stats.UrgentTickets = reader1.GetInt32(6);
                stats.HighPriorityTickets = reader1.GetInt32(7);
                stats.UnassignedTickets = reader1.GetInt32(8);
            }
            reader1.Close();

            // Zeitbasierte Statistiken
            string timeQuery = @"SELECT 
                                    IFNULL(SUM(CASE WHEN DATE(created_at) = CURDATE() THEN 1 ELSE 0 END), 0) as today,
                                    IFNULL(SUM(CASE WHEN YEARWEEK(created_at, 1) = YEARWEEK(CURDATE(), 1) THEN 1 ELSE 0 END), 0) as week,
                                    IFNULL(SUM(CASE WHEN YEAR(created_at) = YEAR(CURDATE()) AND MONTH(created_at) = MONTH(CURDATE()) THEN 1 ELSE 0 END), 0) as month
                                 FROM tickets";

            using var cmd2 = new MySqlCommand(timeQuery, connection);
            using var reader2 = await cmd2.ExecuteReaderAsync();
            if (await reader2.ReadAsync())
            {
                stats.TodayTickets = reader2.GetInt32(0);
                stats.WeekTickets = reader2.GetInt32(1);
                stats.MonthTickets = reader2.GetInt32(2);
            }
            reader2.Close();

            // Durchschnittliche Bearbeitungszeit
            string avgQuery = @"SELECT IFNULL(AVG(TIMESTAMPDIFF(HOUR, created_at, resolved_at)), 0)
                               FROM tickets 
                               WHERE resolved_at IS NOT NULL";

            using var cmd3 = new MySqlCommand(avgQuery, connection);
            var avgResult = await cmd3.ExecuteScalarAsync();
            stats.AverageResolutionTimeHours = avgResult != DBNull.Value ? Convert.ToDouble(avgResult) : 0;

            // SLA compliance
            // SLA targets: Urgent=4h, High=8h, Medium=24h, Low=72h
            // Breached: open tickets past their deadline
            // Compliant: resolved/closed tickets that met their deadline
            string slaQuery = @"SELECT
                IFNULL(SUM(CASE
                    WHEN status NOT IN (3,4) AND priority = 3 AND TIMESTAMPDIFF(HOUR, created_at, NOW()) > 4  THEN 1
                    WHEN status NOT IN (3,4) AND priority = 2 AND TIMESTAMPDIFF(HOUR, created_at, NOW()) > 8  THEN 1
                    WHEN status NOT IN (3,4) AND priority = 1 AND TIMESTAMPDIFF(HOUR, created_at, NOW()) > 24 THEN 1
                    WHEN status NOT IN (3,4) AND priority = 0 AND TIMESTAMPDIFF(HOUR, created_at, NOW()) > 72 THEN 1
                    ELSE 0 END), 0) as sla_breached,
                IFNULL(SUM(CASE
                    WHEN status IN (3,4) AND resolved_at IS NOT NULL AND priority = 3 AND TIMESTAMPDIFF(HOUR, created_at, resolved_at) <= 4  THEN 1
                    WHEN status IN (3,4) AND resolved_at IS NOT NULL AND priority = 2 AND TIMESTAMPDIFF(HOUR, created_at, resolved_at) <= 8  THEN 1
                    WHEN status IN (3,4) AND resolved_at IS NOT NULL AND priority = 1 AND TIMESTAMPDIFF(HOUR, created_at, resolved_at) <= 24 THEN 1
                    WHEN status IN (3,4) AND resolved_at IS NOT NULL AND priority = 0 AND TIMESTAMPDIFF(HOUR, created_at, resolved_at) <= 72 THEN 1
                    ELSE 0 END), 0) as sla_compliant,
                IFNULL(COUNT(CASE WHEN status IN (3,4) AND resolved_at IS NOT NULL THEN 1 END), 0) as total_resolved
                FROM tickets";

            using var cmdSla = new MySqlCommand(slaQuery, connection);
            using var readerSla = await cmdSla.ExecuteReaderAsync();
            if (await readerSla.ReadAsync())
            {
                stats.SlaBreachedCount = readerSla.GetInt32(0);
                stats.SlaCompliantCount = readerSla.GetInt32(1);
                var totalResolved = readerSla.GetInt32(2);
                stats.SlaComplianceRate = totalResolved > 0
                    ? (double)stats.SlaCompliantCount / totalResolved * 100
                    : 100;
            }

            return stats;
        }

        public async System.Threading.Tasks.Task<List<Ticket>> SearchTicketsAsync(string searchTerm)
        {
            var tickets = new List<Ticket>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"SELECT t.*, CONCAT(e.first_name, ' ', e.last_name) as employee_name
                            FROM tickets t
                            LEFT JOIN employees e ON t.assigned_to_employee_id = e.id
                            WHERE t.subject LIKE @search 
                               OR t.description LIKE @search
                               OR t.customer_name LIKE @search
                               OR t.customer_email LIKE @search
                               OR CONCAT('#', LPAD(t.id, 6, '0')) LIKE @search
                            ORDER BY t.created_at DESC";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tickets.Add(new Ticket
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    CustomerName = reader.GetString(reader.GetOrdinal("customer_name")),
                    CustomerEmail = reader.GetString(reader.GetOrdinal("customer_email")),
                    CustomerPhone = reader.IsDBNull(reader.GetOrdinal("customer_phone")) ? "" : reader.GetString(reader.GetOrdinal("customer_phone")),
                    CustomerId = reader.IsDBNull(reader.GetOrdinal("customer_id")) ? null : reader.GetInt32(reader.GetOrdinal("customer_id")),
                    Subject = reader.GetString(reader.GetOrdinal("subject")),
                    Description = reader.GetString(reader.GetOrdinal("description")),
                    Priority = (TicketPriority)reader.GetInt32(reader.GetOrdinal("priority")),
                    Status = (TicketStatus)reader.GetInt32(reader.GetOrdinal("status")),
                    Category = (TicketCategory)reader.GetInt32(reader.GetOrdinal("category")),
                    IpAddress = reader.IsDBNull(reader.GetOrdinal("ip_address")) ? "" : reader.GetString(reader.GetOrdinal("ip_address")),
                    UserAgent = reader.IsDBNull(reader.GetOrdinal("user_agent")) ? "" : reader.GetString(reader.GetOrdinal("user_agent")),
                    AssignedToEmployeeId = reader.IsDBNull(reader.GetOrdinal("assigned_to_employee_id")) ? null : reader.GetInt32(reader.GetOrdinal("assigned_to_employee_id")),
                    AssignedToEmployeeName = reader.IsDBNull(reader.GetOrdinal("employee_name")) ? "" : reader.GetString(reader.GetOrdinal("employee_name")),
                    Resolution = reader.IsDBNull(reader.GetOrdinal("resolution")) ? "" : reader.GetString(reader.GetOrdinal("resolution")),
                    ResolvedAt = reader.IsDBNull(reader.GetOrdinal("resolved_at")) ? null : reader.GetDateTime(reader.GetOrdinal("resolved_at")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return tickets;
        }

        public async System.Threading.Tasks.Task<List<Ticket>> GetTicketsByCustomerEmailAsync(string email)
        {
            var tickets = new List<Ticket>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"SELECT t.*, CONCAT(e.first_name, ' ', e.last_name) as employee_name
                            FROM tickets t
                            LEFT JOIN employees e ON t.assigned_to_employee_id = e.id
                            WHERE t.customer_email = @email
                            ORDER BY t.created_at DESC";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@email", email);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tickets.Add(new Ticket
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    CustomerName = reader.GetString(reader.GetOrdinal("customer_name")),
                    CustomerEmail = reader.GetString(reader.GetOrdinal("customer_email")),
                    CustomerPhone = reader.IsDBNull(reader.GetOrdinal("customer_phone")) ? "" : reader.GetString(reader.GetOrdinal("customer_phone")),
                    CustomerId = reader.IsDBNull(reader.GetOrdinal("customer_id")) ? null : reader.GetInt32(reader.GetOrdinal("customer_id")),
                    Subject = reader.GetString(reader.GetOrdinal("subject")),
                    Description = reader.GetString(reader.GetOrdinal("description")),
                    Priority = (TicketPriority)reader.GetInt32(reader.GetOrdinal("priority")),
                    Status = (TicketStatus)reader.GetInt32(reader.GetOrdinal("status")),
                    Category = (TicketCategory)reader.GetInt32(reader.GetOrdinal("category")),
                    IpAddress = reader.IsDBNull(reader.GetOrdinal("ip_address")) ? "" : reader.GetString(reader.GetOrdinal("ip_address")),
                    UserAgent = reader.IsDBNull(reader.GetOrdinal("user_agent")) ? "" : reader.GetString(reader.GetOrdinal("user_agent")),
                    AssignedToEmployeeId = reader.IsDBNull(reader.GetOrdinal("assigned_to_employee_id")) ? null : reader.GetInt32(reader.GetOrdinal("assigned_to_employee_id")),
                    AssignedToEmployeeName = reader.IsDBNull(reader.GetOrdinal("employee_name")) ? "" : reader.GetString(reader.GetOrdinal("employee_name")),
                    Resolution = reader.IsDBNull(reader.GetOrdinal("resolution")) ? "" : reader.GetString(reader.GetOrdinal("resolution")),
                    ResolvedAt = reader.IsDBNull(reader.GetOrdinal("resolved_at")) ? null : reader.GetDateTime(reader.GetOrdinal("resolved_at")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at"))
                });
            }

            return tickets;
        }

        #endregion

        #region Task Assignment Notifications

        public async System.Threading.Tasks.Task CreateTaskAssignmentNotificationAsync(
            int taskId, string taskTitle, string projectName, string assignedTo, string assignedBy)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO task_assignment_notifications 
                (task_id, task_title, project_name, assigned_to, assigned_by, created_at)
                VALUES (@taskId, @taskTitle, @projectName, @assignedTo, @assignedBy, @createdAt)";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@taskId", taskId);
            cmd.Parameters.AddWithValue("@taskTitle", taskTitle);
            cmd.Parameters.AddWithValue("@projectName", projectName ?? "");
            cmd.Parameters.AddWithValue("@assignedTo", assignedTo);
            cmd.Parameters.AddWithValue("@assignedBy", assignedBy);
            cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
            await cmd.ExecuteNonQueryAsync();
        }

        public async System.Threading.Tasks.Task MarkAssignmentNotificationsReadAsync(string username)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "UPDATE task_assignment_notifications SET is_read = TRUE WHERE assigned_to = @user AND is_read = FALSE";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@user", username);
            await cmd.ExecuteNonQueryAsync();
        }

        #endregion
    }
}
