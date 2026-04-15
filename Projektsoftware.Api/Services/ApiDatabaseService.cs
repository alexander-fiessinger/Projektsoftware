using MySqlConnector;
using Projektsoftware.Api.Models;
using System.Security.Cryptography;
using System.Text;

namespace Projektsoftware.Api.Services;

public class ApiDatabaseService
{
    private readonly string _connectionString;

    public ApiDatabaseService(IConfiguration config)
    {
        var section = config.GetSection("Database");
        var server = section["Server"] ?? "localhost";
        var port = section["Port"] ?? "3306";
        var database = section["Database"] ?? "projektsoftware";
        var user = section["User"] ?? "root";
        var password = section["Password"] ?? "";
        _connectionString = $"Server={server};Port={port};Database={database};User={user};Password={password};SslMode=None;AllowPublicKeyRetrieval=True;";
    }

    // ── DB Init ─────────────────────────────────────────────────────

    public async Task InitializeDatabaseAsync()
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        string[] tableStatements =
        [
            @"CREATE TABLE IF NOT EXISTS employees (
                id INT AUTO_INCREMENT PRIMARY KEY,
                first_name VARCHAR(100) NOT NULL,
                last_name VARCHAR(100) NOT NULL,
                email VARCHAR(255),
                phone VARCHAR(50),
                position VARCHAR(100),
                department VARCHAR(100),
                hourly_rate DECIMAL(10,2),
                is_active BOOLEAN DEFAULT TRUE,
                hire_date DATE,
                created_at DATETIME
            )",
            @"CREATE TABLE IF NOT EXISTS projects (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                description TEXT,
                start_date DATE,
                end_date DATE,
                status VARCHAR(50),
                client_name VARCHAR(255),
                easybill_customer_id BIGINT,
                budget DECIMAL(10,2),
                created_at DATETIME
            )",
            @"CREATE TABLE IF NOT EXISTS milestones (
                id INT AUTO_INCREMENT PRIMARY KEY,
                project_id INT,
                title VARCHAR(255) NOT NULL,
                description TEXT,
                due_date DATE,
                completed_date DATE,
                status VARCHAR(50),
                completion_percentage INT DEFAULT 0,
                created_at DATETIME,
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
            )",
            @"CREATE TABLE IF NOT EXISTS tasks (
                id INT AUTO_INCREMENT PRIMARY KEY,
                project_id INT,
                title VARCHAR(255) NOT NULL,
                description TEXT,
                assigned_to VARCHAR(255),
                client_name VARCHAR(255),
                easybill_customer_id BIGINT,
                status VARCHAR(50),
                priority VARCHAR(50),
                due_date DATE,
                completed_date DATE,
                estimated_hours INT DEFAULT 0,
                actual_hours INT DEFAULT 0,
                created_at DATETIME,
                updated_at DATETIME,
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
            )",
            @"CREATE TABLE IF NOT EXISTS time_entries (
                id INT AUTO_INCREMENT PRIMARY KEY,
                project_id INT,
                employee_name VARCHAR(255),
                client_name VARCHAR(255),
                easybill_customer_id BIGINT,
                date DATE,
                duration TIME,
                description TEXT,
                activity VARCHAR(255),
                created_at DATETIME,
                is_exported BOOLEAN DEFAULT FALSE,
                exported_to_easybill_at DATETIME,
                easybill_position_id BIGINT,
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
            )",
            @"CREATE TABLE IF NOT EXISTS meeting_protocols (
                id INT AUTO_INCREMENT PRIMARY KEY,
                project_id INT,
                title VARCHAR(255),
                meeting_date DATETIME,
                location VARCHAR(255),
                participants TEXT,
                agenda TEXT,
                discussion TEXT,
                decisions TEXT,
                action_items TEXT,
                next_meeting_date VARCHAR(100),
                created_at DATETIME,
                FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
            )",
            @"CREATE TABLE IF NOT EXISTS users (
                id INT AUTO_INCREMENT PRIMARY KEY,
                employee_id INT,
                username VARCHAR(100) UNIQUE NOT NULL,
                password_hash VARCHAR(255) NOT NULL,
                role VARCHAR(50) DEFAULT 'User',
                is_active BOOLEAN DEFAULT TRUE,
                created_at DATETIME,
                last_login DATETIME,
                FOREIGN KEY (employee_id) REFERENCES employees(id) ON DELETE SET NULL
            )",
            @"CREATE TABLE IF NOT EXISTS customers (
                id INT AUTO_INCREMENT PRIMARY KEY,
                company_name VARCHAR(255),
                first_name VARCHAR(100),
                last_name VARCHAR(100),
                email VARCHAR(255),
                phone VARCHAR(50),
                street VARCHAR(255),
                zip_code VARCHAR(20),
                city VARCHAR(100),
                country VARCHAR(100) DEFAULT 'Deutschland',
                vat_id VARCHAR(50),
                note TEXT,
                easybill_customer_id BIGINT,
                last_synced_at DATETIME,
                created_at DATETIME,
                updated_at DATETIME,
                is_active BOOLEAN DEFAULT TRUE
            )",
            @"CREATE TABLE IF NOT EXISTS user_permissions (
                id INT AUTO_INCREMENT PRIMARY KEY,
                user_id INT NOT NULL,
                module_key VARCHAR(50) NOT NULL,
                UNIQUE KEY uq_user_module (user_id, module_key),
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
            )",
            @"CREATE TABLE IF NOT EXISTS user_easybill_settings (
                id INT AUTO_INCREMENT PRIMARY KEY,
                user_id INT NOT NULL UNIQUE,
                easybill_email VARCHAR(255) NOT NULL,
                easybill_api_key VARCHAR(255) NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
            )",
            @"CREATE TABLE IF NOT EXISTS user_webex_settings (
                id INT AUTO_INCREMENT PRIMARY KEY,
                user_id INT NOT NULL UNIQUE,
                access_token TEXT NOT NULL,
                client_id VARCHAR(255),
                client_secret VARCHAR(255),
                refresh_token TEXT,
                token_expiry DATETIME,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
            )"
        ];

        foreach (var sql in tableStatements)
        {
            using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // Admin-User anlegen falls nicht vorhanden
        const string checkAdmin = "SELECT COUNT(*) FROM users WHERE username = 'administrator'";
        using var checkCmd = new MySqlCommand(checkAdmin, conn);
        var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

        if (count == 0)
        {
            var adminHash = HashPassword("admin");
            const string insertAdmin = "INSERT INTO users (username, password_hash, role, is_active, created_at) VALUES ('administrator', @hash, 'Admin', TRUE, NOW())";
            using var insertCmd = new MySqlCommand(insertAdmin, conn);
            insertCmd.Parameters.AddWithValue("@hash", adminHash);
            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    // ── Auth ────────────────────────────────────────────────────────

    public async Task<LoginResponse> AuthenticateAsync(string username, string password)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        const string query = "SELECT id, username, password_hash, role, is_active FROM users WHERE username = @u";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@u", username);
        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return new LoginResponse(false, null, null, null, 0, "Benutzer nicht gefunden");

        var userId = reader.GetInt32(reader.GetOrdinal("id"));
        var isActive = reader.GetBoolean(reader.GetOrdinal("is_active"));
        if (!isActive)
            return new LoginResponse(false, null, null, null, 0, "Benutzerkonto deaktiviert");

        var storedHash = reader.GetString(reader.GetOrdinal("password_hash"));
        var inputHash = HashPassword(password);
        if (!storedHash.Equals(inputHash, StringComparison.OrdinalIgnoreCase))
            return new LoginResponse(false, null, null, null, 0, "Falsches Passwort");

        var user = reader.GetString(reader.GetOrdinal("username"));
        var role = reader.GetString(reader.GetOrdinal("role"));

        // Simple token: Base64(username:timestamp)
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{DateTime.UtcNow:O}"));
        return new LoginResponse(true, token, user, role, userId, null);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        var sb = new StringBuilder();
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    public async Task<List<string>> GetUserPermissionsAsync(int userId)
    {
        var result = new List<string>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = "SELECT module_key FROM user_permissions WHERE user_id = @uid";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(reader.GetString(0));
        return result;
    }

    // ── Dashboard ───────────────────────────────────────────────────

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var dto = new DashboardDto();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand(@"
            SELECT 
                (SELECT COUNT(*) FROM projects) AS total_projects,
                (SELECT COUNT(*) FROM projects WHERE status='Aktiv') AS active_projects,
                (SELECT COUNT(*) FROM projects WHERE status='Abgeschlossen') AS completed_projects,
                (SELECT COUNT(*) FROM tasks) AS total_tasks,
                (SELECT COUNT(*) FROM tasks WHERE status='Offen') AS open_tasks,
                (SELECT COUNT(*) FROM tasks WHERE status='Erledigt') AS completed_tasks,
                (SELECT IFNULL(SUM(TIME_TO_SEC(duration))/3600,0) FROM time_entries) AS total_hours,
                (SELECT COUNT(*) FROM tasks WHERE due_date < CURDATE() AND status NOT IN ('Erledigt')) AS overdue_tasks,
                (SELECT COUNT(*) FROM employees WHERE is_active=1) AS active_employees,
                (SELECT COUNT(*) FROM customers WHERE is_active=1) AS total_customers,
                (SELECT COUNT(*) FROM tickets WHERE status < 4) AS open_tickets,
                (SELECT COUNT(*) FROM crm_deals WHERE stage < 4) AS crm_deals,
                (SELECT COUNT(*) FROM meetings WHERE start_time > NOW()) AS upcoming_meetings,
                (SELECT COUNT(*) FROM suppliers) AS total_suppliers", conn);

        using var r = await cmd.ExecuteReaderAsync();
        if (await r.ReadAsync())
        {
            dto.TotalProjects = r.GetInt32(0);
            dto.ActiveProjects = r.GetInt32(1);
            dto.CompletedProjects = r.GetInt32(2);
            dto.TotalTasks = r.GetInt32(3);
            dto.OpenTasks = r.GetInt32(4);
            dto.CompletedTasks = r.GetInt32(5);
            dto.TotalHoursLogged = r.GetDecimal(6);
            dto.OverdueTasks = r.GetInt32(7);
            dto.ActiveEmployees = r.GetInt32(8);
            dto.TotalCustomers = r.GetInt32(9);
            dto.OpenTickets = r.GetInt32(10);
            dto.CrmDeals = r.GetInt32(11);
            dto.UpcomingMeetings = r.GetInt32(12);
            dto.TotalSuppliers = r.GetInt32(13);
        }
        return dto;
    }

    // ── Projects ────────────────────────────────────────────────────

    public async Task<List<ProjectDto>> GetProjectsAsync()
    {
        var list = new List<ProjectDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand(@"
            SELECT p.*, 
                   (SELECT COUNT(*) FROM tasks WHERE project_id=p.id) AS total_tasks,
                   (SELECT COUNT(*) FROM tasks WHERE project_id=p.id AND status='Erledigt') AS done_tasks
            FROM projects p ORDER BY p.created_at DESC", conn);

        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var total = r.GetInt32(r.GetOrdinal("total_tasks"));
            var done = r.GetInt32(r.GetOrdinal("done_tasks"));
            list.Add(new ProjectDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                Name = r.GetString(r.GetOrdinal("name")),
                Description = r.IsDBNull(r.GetOrdinal("description")) ? "" : r.GetString(r.GetOrdinal("description")),
                StartDate = r.GetDateTime(r.GetOrdinal("start_date")),
                EndDate = r.IsDBNull(r.GetOrdinal("end_date")) ? null : r.GetDateTime(r.GetOrdinal("end_date")),
                Status = r.GetString(r.GetOrdinal("status")),
                ClientName = r.IsDBNull(r.GetOrdinal("client_name")) ? "" : r.GetString(r.GetOrdinal("client_name")),
                Budget = r.GetDecimal(r.GetOrdinal("budget")),
                Tags = r.IsDBNull(r.GetOrdinal("tags")) ? "" : r.GetString(r.GetOrdinal("tags")),
                ProgressPercent = total > 0 ? (int)(done * 100.0 / total) : 0
            });
        }
        return list;
    }

    // ── Tasks ───────────────────────────────────────────────────────

    public async Task<List<TaskDto>> GetTasksAsync(int? projectId = null)
    {
        var list = new List<TaskDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        var where = projectId.HasValue ? "WHERE t.project_id = @pid" : "";
        var query = $@"SELECT t.*, p.name AS project_name
                       FROM tasks t
                       LEFT JOIN projects p ON t.project_id = p.id
                       {where}
                       ORDER BY t.created_at DESC";

        using var cmd = new MySqlCommand(query, conn);
        if (projectId.HasValue)
            cmd.Parameters.AddWithValue("@pid", projectId.Value);

        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new TaskDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                ProjectId = r.GetInt32(r.GetOrdinal("project_id")),
                ProjectName = r.IsDBNull(r.GetOrdinal("project_name")) ? "" : r.GetString(r.GetOrdinal("project_name")),
                Title = r.GetString(r.GetOrdinal("title")),
                Description = r.IsDBNull(r.GetOrdinal("description")) ? "" : r.GetString(r.GetOrdinal("description")),
                AssignedTo = r.IsDBNull(r.GetOrdinal("assigned_to")) ? "" : r.GetString(r.GetOrdinal("assigned_to")),
                Status = r.GetString(r.GetOrdinal("status")),
                Priority = r.GetString(r.GetOrdinal("priority")),
                DueDate = r.IsDBNull(r.GetOrdinal("due_date")) ? null : r.GetDateTime(r.GetOrdinal("due_date")),
                CompletedDate = r.IsDBNull(r.GetOrdinal("completed_date")) ? null : r.GetDateTime(r.GetOrdinal("completed_date")),
                EstimatedHours = r.GetInt32(r.GetOrdinal("estimated_hours")),
                ActualHours = r.GetInt32(r.GetOrdinal("actual_hours"))
            });
        }
        return list;
    }

    public async Task UpdateTaskStatusAsync(int taskId, string newStatus)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        var completedDate = newStatus == "Erledigt" ? "NOW()" : "completed_date";
        var query = $"UPDATE tasks SET status=@status, completed_date={completedDate}, updated_at=NOW() WHERE id=@id";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@status", newStatus);
        cmd.Parameters.AddWithValue("@id", taskId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> CreateTaskAsync(TaskCreateRequest req)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        const string query = @"INSERT INTO tasks (project_id, title, description, assigned_to, status, priority, 
            due_date, estimated_hours, actual_hours, created_at)
            VALUES (@projectId, @title, @desc, @assignedTo, @status, @priority, @dueDate, @estHours, 0, NOW());
            SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@projectId", req.ProjectId);
        cmd.Parameters.AddWithValue("@title", req.Title);
        cmd.Parameters.AddWithValue("@desc", req.Description ?? "");
        cmd.Parameters.AddWithValue("@assignedTo", req.AssignedTo ?? "");
        cmd.Parameters.AddWithValue("@status", req.Status);
        cmd.Parameters.AddWithValue("@priority", req.Priority);
        cmd.Parameters.AddWithValue("@dueDate", req.DueDate.HasValue ? req.DueDate.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@estHours", req.EstimatedHours);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    // ── Employees ───────────────────────────────────────────────────

    public async Task<List<EmployeeDto>> GetEmployeesAsync()
    {
        var list = new List<EmployeeDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("SELECT * FROM employees ORDER BY last_name, first_name", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new EmployeeDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                FirstName = r.IsDBNull(r.GetOrdinal("first_name")) ? "" : r.GetString(r.GetOrdinal("first_name")),
                LastName = r.IsDBNull(r.GetOrdinal("last_name")) ? "" : r.GetString(r.GetOrdinal("last_name")),
                Email = r.IsDBNull(r.GetOrdinal("email")) ? "" : r.GetString(r.GetOrdinal("email")),
                Phone = r.IsDBNull(r.GetOrdinal("phone")) ? "" : r.GetString(r.GetOrdinal("phone")),
                Position = r.IsDBNull(r.GetOrdinal("position")) ? "" : r.GetString(r.GetOrdinal("position")),
                Department = r.IsDBNull(r.GetOrdinal("department")) ? "" : r.GetString(r.GetOrdinal("department")),
                HourlyRate = r.IsDBNull(r.GetOrdinal("hourly_rate")) ? 0 : r.GetDecimal(r.GetOrdinal("hourly_rate")),
                IsActive = r.IsDBNull(r.GetOrdinal("is_active")) || r.GetBoolean(r.GetOrdinal("is_active")),
                HireDate = r.IsDBNull(r.GetOrdinal("hire_date")) ? DateTime.MinValue : r.GetDateTime(r.GetOrdinal("hire_date"))
            });
        }
        return list;
    }

    // ── Time Entries ────────────────────────────────────────────────

    private TimeSpan ParseMySqlTime(object value)
    {
        if (value == null || value == DBNull.Value) return TimeSpan.Zero;
        if (value is TimeSpan ts) return ts;
        if (value is string str) return TimeSpan.TryParse(str, out var parsed) ? parsed : TimeSpan.Zero;
        return TimeSpan.Zero;
    }

    public async Task<List<TimeEntryDto>> GetTimeEntriesAsync(int? projectId = null)
    {
        var list = new List<TimeEntryDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        var where = projectId.HasValue ? "WHERE t.project_id = @pid" : "";
        var query = $@"SELECT t.*, p.name AS project_name
                       FROM time_entries t
                       LEFT JOIN projects p ON t.project_id = p.id
                       {where}
                       ORDER BY t.date DESC LIMIT 200";

        using var cmd = new MySqlCommand(query, conn);
        if (projectId.HasValue)
            cmd.Parameters.AddWithValue("@pid", projectId.Value);

        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new TimeEntryDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                ProjectId = r.IsDBNull(r.GetOrdinal("project_id")) ? 0 : r.GetInt32(r.GetOrdinal("project_id")),
                ProjectName = r.IsDBNull(r.GetOrdinal("project_name")) ? "" : r.GetString(r.GetOrdinal("project_name")),
                EmployeeName = r.IsDBNull(r.GetOrdinal("employee_name")) ? "" : r.GetString(r.GetOrdinal("employee_name")),
                Date = r.GetDateTime(r.GetOrdinal("date")),
                Duration = ParseMySqlTime(r.GetValue(r.GetOrdinal("duration"))),
                Description = r.IsDBNull(r.GetOrdinal("description")) ? "" : r.GetString(r.GetOrdinal("description")),
                Activity = r.IsDBNull(r.GetOrdinal("activity")) ? "" : r.GetString(r.GetOrdinal("activity"))
            });
        }
        return list;
    }

    public async Task CreateTimeEntryAsync(TimeEntryCreateRequest req)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        const string query = @"INSERT INTO time_entries (project_id, employee_name, date, duration, description, activity, created_at)
                               VALUES (@pid, @emp, @date, @dur, @desc, @act, NOW())";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@pid", req.ProjectId);
        cmd.Parameters.AddWithValue("@emp", req.EmployeeName);
        cmd.Parameters.AddWithValue("@date", req.Date);
        cmd.Parameters.AddWithValue("@dur", TimeSpan.FromHours(req.DurationHours));
        cmd.Parameters.AddWithValue("@desc", req.Description ?? "");
        cmd.Parameters.AddWithValue("@act", req.Activity ?? "");
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Customers ───────────────────────────────────────────────────

    public async Task<List<CustomerDto>> GetCustomersAsync()
    {
        var list = new List<CustomerDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("SELECT * FROM customers ORDER BY company_name, last_name", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new CustomerDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                CompanyName = r.IsDBNull(r.GetOrdinal("company_name")) ? "" : r.GetString(r.GetOrdinal("company_name")),
                FirstName = r.IsDBNull(r.GetOrdinal("first_name")) ? "" : r.GetString(r.GetOrdinal("first_name")),
                LastName = r.IsDBNull(r.GetOrdinal("last_name")) ? "" : r.GetString(r.GetOrdinal("last_name")),
                Email = r.IsDBNull(r.GetOrdinal("email")) ? "" : r.GetString(r.GetOrdinal("email")),
                Phone = r.IsDBNull(r.GetOrdinal("phone")) ? "" : r.GetString(r.GetOrdinal("phone")),
                Street = r.IsDBNull(r.GetOrdinal("street")) ? "" : r.GetString(r.GetOrdinal("street")),
                ZipCode = r.IsDBNull(r.GetOrdinal("zip_code")) ? "" : r.GetString(r.GetOrdinal("zip_code")),
                City = r.IsDBNull(r.GetOrdinal("city")) ? "" : r.GetString(r.GetOrdinal("city")),
                Country = r.IsDBNull(r.GetOrdinal("country")) ? "" : r.GetString(r.GetOrdinal("country")),
                Note = r.IsDBNull(r.GetOrdinal("note")) ? "" : r.GetString(r.GetOrdinal("note")),
                IsActive = r.IsDBNull(r.GetOrdinal("is_active")) || r.GetBoolean(r.GetOrdinal("is_active"))
            });
        }
        return list;
    }

    // ── Tickets ─────────────────────────────────────────────────────

    public async Task<List<TicketDto>> GetTicketsAsync()
    {
        var list = new List<TicketDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand(@"SELECT t.*, 
                CONCAT(e.first_name, ' ', e.last_name) AS assigned_name
                FROM tickets t
                LEFT JOIN employees e ON t.assigned_to_employee_id = e.id
                ORDER BY t.created_at DESC", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new TicketDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                CustomerName = r.IsDBNull(r.GetOrdinal("customer_name")) ? "" : r.GetString(r.GetOrdinal("customer_name")),
                CustomerEmail = r.IsDBNull(r.GetOrdinal("customer_email")) ? "" : r.GetString(r.GetOrdinal("customer_email")),
                Subject = r.IsDBNull(r.GetOrdinal("subject")) ? "" : r.GetString(r.GetOrdinal("subject")),
                Description = r.IsDBNull(r.GetOrdinal("description")) ? "" : r.GetString(r.GetOrdinal("description")),
                Priority = r.GetInt32(r.GetOrdinal("priority")),
                Status = r.GetInt32(r.GetOrdinal("status")),
                Category = r.GetInt32(r.GetOrdinal("category")),
                AssignedToEmployeeName = r.IsDBNull(r.GetOrdinal("assigned_name")) ? "" : r.GetString(r.GetOrdinal("assigned_name")),
                Resolution = r.IsDBNull(r.GetOrdinal("resolution")) ? "" : r.GetString(r.GetOrdinal("resolution")),
                CreatedAt = r.GetDateTime(r.GetOrdinal("created_at")),
                ResolvedAt = r.IsDBNull(r.GetOrdinal("resolved_at")) ? null : r.GetDateTime(r.GetOrdinal("resolved_at"))
            });
        }
        return list;
    }

    public async Task UpdateTicketStatusAsync(int ticketId, int newStatus)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        var resolvedAt = (newStatus == 4 || newStatus == 5) ? "NOW()" : "resolved_at";
        var query = $"UPDATE tickets SET status=@status, resolved_at={resolvedAt}, updated_at=NOW() WHERE id=@id";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@status", newStatus);
        cmd.Parameters.AddWithValue("@id", ticketId);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Meeting Protocols ───────────────────────────────────────────

    public async Task<List<MeetingProtocolDto>> GetMeetingProtocolsAsync()
    {
        var list = new List<MeetingProtocolDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand(@"SELECT m.*, p.name AS project_name
                FROM meeting_protocols m
                LEFT JOIN projects p ON m.project_id = p.id
                ORDER BY m.meeting_date DESC", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new MeetingProtocolDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                ProjectId = r.IsDBNull(r.GetOrdinal("project_id")) ? 0 : r.GetInt32(r.GetOrdinal("project_id")),
                ProjectName = r.IsDBNull(r.GetOrdinal("project_name")) ? "" : r.GetString(r.GetOrdinal("project_name")),
                Title = r.IsDBNull(r.GetOrdinal("title")) ? "" : r.GetString(r.GetOrdinal("title")),
                MeetingDate = r.GetDateTime(r.GetOrdinal("meeting_date")),
                Location = r.IsDBNull(r.GetOrdinal("location")) ? "" : r.GetString(r.GetOrdinal("location")),
                Participants = r.IsDBNull(r.GetOrdinal("participants")) ? "" : r.GetString(r.GetOrdinal("participants")),
                Agenda = r.IsDBNull(r.GetOrdinal("agenda")) ? "" : r.GetString(r.GetOrdinal("agenda")),
                Discussion = r.IsDBNull(r.GetOrdinal("discussion")) ? "" : r.GetString(r.GetOrdinal("discussion")),
                Decisions = r.IsDBNull(r.GetOrdinal("decisions")) ? "" : r.GetString(r.GetOrdinal("decisions")),
                ActionItems = r.IsDBNull(r.GetOrdinal("action_items")) ? "" : r.GetString(r.GetOrdinal("action_items"))
            });
        }
        return list;
    }

    // ── CRM Contacts ────────────────────────────────────────────────

    public async Task<List<CrmContactDto>> GetCrmContactsAsync()
    {
        var list = new List<CrmContactDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand(@"
            SELECT c.*, COALESCE(cu.company_name, CONCAT(cu.first_name, ' ', cu.last_name)) AS customer_display_name
            FROM crm_contacts c
            LEFT JOIN customers cu ON c.customer_id = cu.id
            ORDER BY c.last_name, c.first_name", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new CrmContactDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                CustomerId = r.IsDBNull(r.GetOrdinal("customer_id")) ? null : r.GetInt32(r.GetOrdinal("customer_id")),
                CustomerName = r.IsDBNull(r.GetOrdinal("customer_display_name")) ? "" : r.GetString(r.GetOrdinal("customer_display_name")),
                FirstName = r.IsDBNull(r.GetOrdinal("first_name")) ? "" : r.GetString(r.GetOrdinal("first_name")),
                LastName = r.IsDBNull(r.GetOrdinal("last_name")) ? "" : r.GetString(r.GetOrdinal("last_name")),
                Position = r.IsDBNull(r.GetOrdinal("position")) ? "" : r.GetString(r.GetOrdinal("position")),
                Email = r.IsDBNull(r.GetOrdinal("email")) ? "" : r.GetString(r.GetOrdinal("email")),
                Phone = r.IsDBNull(r.GetOrdinal("phone")) ? "" : r.GetString(r.GetOrdinal("phone")),
                Mobile = r.IsDBNull(r.GetOrdinal("mobile")) ? "" : r.GetString(r.GetOrdinal("mobile")),
                Notes = r.IsDBNull(r.GetOrdinal("notes")) ? "" : r.GetString(r.GetOrdinal("notes")),
                IsActive = r.GetBoolean(r.GetOrdinal("is_active")),
                CreatedAt = r.GetDateTime(r.GetOrdinal("created_at"))
            });
        }
        return list;
    }

    // ── CRM Activities ──────────────────────────────────────────────

    public async Task<List<CrmActivityDto>> GetCrmActivitiesAsync()
    {
        var list = new List<CrmActivityDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand(@"
            SELECT a.*,
                CONCAT(COALESCE(c.first_name,''), ' ', COALESCE(c.last_name,'')) AS contact_display_name,
                COALESCE(cu.company_name, CONCAT(cu.first_name, ' ', cu.last_name)) AS customer_display_name
            FROM crm_activities a
            LEFT JOIN crm_contacts c ON a.contact_id = c.id
            LEFT JOIN customers cu ON a.customer_id = cu.id
            ORDER BY a.created_at DESC", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new CrmActivityDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                ContactName = r.IsDBNull(r.GetOrdinal("contact_display_name")) ? "" : r.GetString(r.GetOrdinal("contact_display_name")).Trim(),
                CustomerName = r.IsDBNull(r.GetOrdinal("customer_display_name")) ? "" : r.GetString(r.GetOrdinal("customer_display_name")),
                Type = r.GetInt32(r.GetOrdinal("type")),
                Subject = r.IsDBNull(r.GetOrdinal("subject")) ? "" : r.GetString(r.GetOrdinal("subject")),
                Notes = r.IsDBNull(r.GetOrdinal("notes")) ? "" : r.GetString(r.GetOrdinal("notes")),
                DueDate = r.IsDBNull(r.GetOrdinal("due_date")) ? null : r.GetDateTime(r.GetOrdinal("due_date")),
                IsCompleted = r.GetBoolean(r.GetOrdinal("is_completed")),
                CompletedAt = r.IsDBNull(r.GetOrdinal("completed_at")) ? null : r.GetDateTime(r.GetOrdinal("completed_at")),
                CreatedBy = r.IsDBNull(r.GetOrdinal("created_by")) ? "" : r.GetString(r.GetOrdinal("created_by")),
                CreatedAt = r.GetDateTime(r.GetOrdinal("created_at"))
            });
        }
        return list;
    }

    // ── CRM Deals ───────────────────────────────────────────────────

    public async Task<List<CrmDealDto>> GetCrmDealsAsync()
    {
        var list = new List<CrmDealDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand(@"
            SELECT d.*,
                COALESCE(cu.company_name, CONCAT(cu.first_name, ' ', cu.last_name)) AS customer_display_name,
                CONCAT(COALESCE(c.first_name,''), ' ', COALESCE(c.last_name,'')) AS contact_display_name
            FROM crm_deals d
            LEFT JOIN customers cu ON d.customer_id = cu.id
            LEFT JOIN crm_contacts c ON d.contact_id = c.id
            ORDER BY d.created_at DESC", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new CrmDealDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                CustomerName = r.IsDBNull(r.GetOrdinal("customer_display_name")) ? "" : r.GetString(r.GetOrdinal("customer_display_name")),
                ContactName = r.IsDBNull(r.GetOrdinal("contact_display_name")) ? "" : r.GetString(r.GetOrdinal("contact_display_name")).Trim(),
                Title = r.IsDBNull(r.GetOrdinal("title")) ? "" : r.GetString(r.GetOrdinal("title")),
                Value = r.GetDecimal(r.GetOrdinal("value")),
                Stage = r.GetInt32(r.GetOrdinal("stage")),
                Probability = r.GetInt32(r.GetOrdinal("probability")),
                ExpectedCloseDate = r.IsDBNull(r.GetOrdinal("expected_close_date")) ? null : r.GetDateTime(r.GetOrdinal("expected_close_date")),
                Notes = r.IsDBNull(r.GetOrdinal("notes")) ? "" : r.GetString(r.GetOrdinal("notes")),
                AssignedTo = r.IsDBNull(r.GetOrdinal("assigned_to")) ? "" : r.GetString(r.GetOrdinal("assigned_to")),
                CreatedAt = r.GetDateTime(r.GetOrdinal("created_at"))
            });
        }
        return list;
    }

    public async Task UpdateDealStageAsync(int dealId, int newStage)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        var wonAt = newStage == 4 ? ", won_at=NOW()" : "";
        var lostAt = newStage == 5 ? ", lost_at=NOW()" : "";
        var query = $"UPDATE crm_deals SET stage=@stage, updated_at=NOW(){wonAt}{lostAt} WHERE id=@id";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@stage", newStage);
        cmd.Parameters.AddWithValue("@id", dealId);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Meetings ────────────────────────────────────────────────────

    public async Task<List<MeetingDto>> GetMeetingsAsync()
    {
        var list = new List<MeetingDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand(@"
            SELECT m.*, p.name AS project_name
            FROM meetings m
            LEFT JOIN projects p ON m.project_id = p.id
            ORDER BY m.start_time DESC", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new MeetingDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                Title = r.GetString(r.GetOrdinal("title")),
                Description = r.IsDBNull(r.GetOrdinal("description")) ? null : r.GetString(r.GetOrdinal("description")),
                StartTime = r.GetDateTime(r.GetOrdinal("start_time")),
                EndTime = r.GetDateTime(r.GetOrdinal("end_time")),
                Location = r.IsDBNull(r.GetOrdinal("location")) ? null : r.GetString(r.GetOrdinal("location")),
                Participants = r.IsDBNull(r.GetOrdinal("participants")) ? null : r.GetString(r.GetOrdinal("participants")),
                ProjectName = r.IsDBNull(r.GetOrdinal("project_name")) ? null : r.GetString(r.GetOrdinal("project_name")),
                CreatedAt = r.GetDateTime(r.GetOrdinal("created_at")),
                IsWebexMeeting = r.GetBoolean(r.GetOrdinal("is_webex_meeting")),
                WebexMeetingId = r.IsDBNull(r.GetOrdinal("webex_meeting_id")) ? null : r.GetString(r.GetOrdinal("webex_meeting_id")),
                WebexJoinLink = r.IsDBNull(r.GetOrdinal("webex_join_link")) ? null : r.GetString(r.GetOrdinal("webex_join_link")),
                WebexHostKey = r.IsDBNull(r.GetOrdinal("webex_host_key")) ? null : r.GetString(r.GetOrdinal("webex_host_key")),
                WebexPassword = r.IsDBNull(r.GetOrdinal("webex_password")) ? null : r.GetString(r.GetOrdinal("webex_password")),
                WebexSipAddress = r.IsDBNull(r.GetOrdinal("webex_sip_address")) ? null : r.GetString(r.GetOrdinal("webex_sip_address"))
            });
        }
        return list;
    }

    // ── Suppliers ───────────────────────────────────────────────────

    public async Task<List<SupplierDto>> GetSuppliersAsync()
    {
        var list = new List<SupplierDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("SELECT * FROM suppliers ORDER BY name", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new SupplierDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                Name = r.IsDBNull(r.GetOrdinal("name")) ? "" : r.GetString(r.GetOrdinal("name")),
                ContactPerson = r.IsDBNull(r.GetOrdinal("contact_person")) ? "" : r.GetString(r.GetOrdinal("contact_person")),
                Email = r.IsDBNull(r.GetOrdinal("email")) ? "" : r.GetString(r.GetOrdinal("email")),
                Phone = r.IsDBNull(r.GetOrdinal("phone")) ? "" : r.GetString(r.GetOrdinal("phone")),
                Address = r.IsDBNull(r.GetOrdinal("address")) ? "" : r.GetString(r.GetOrdinal("address")),
                ZipCode = r.IsDBNull(r.GetOrdinal("zip_code")) ? "" : r.GetString(r.GetOrdinal("zip_code")),
                City = r.IsDBNull(r.GetOrdinal("city")) ? "" : r.GetString(r.GetOrdinal("city")),
                Country = r.IsDBNull(r.GetOrdinal("country")) ? "Deutschland" : r.GetString(r.GetOrdinal("country")),
                Notes = r.IsDBNull(r.GetOrdinal("notes")) ? "" : r.GetString(r.GetOrdinal("notes")),
                CreatedAt = r.IsDBNull(r.GetOrdinal("created_at")) ? DateTime.Now : r.GetDateTime(r.GetOrdinal("created_at"))
            });
        }
        return list;
    }

    // ── Purchase Orders ─────────────────────────────────────────────

    public async Task<List<PurchaseOrderDto>> GetPurchaseOrdersAsync()
    {
        var list = new List<PurchaseOrderDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand(@"
            SELECT po.*, COALESCE(s.name,'') as supplier_name
            FROM purchase_orders po
            LEFT JOIN suppliers s ON s.id = po.supplier_id
            ORDER BY po.created_at DESC", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new PurchaseOrderDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                SupplierName = r.IsDBNull(r.GetOrdinal("supplier_name")) ? "" : r.GetString(r.GetOrdinal("supplier_name")),
                OrderNumber = r.IsDBNull(r.GetOrdinal("order_number")) ? "" : r.GetString(r.GetOrdinal("order_number")),
                OrderDate = r.GetDateTime(r.GetOrdinal("order_date")),
                DeliveryDateExpected = r.IsDBNull(r.GetOrdinal("delivery_date_expected")) ? null : r.GetDateTime(r.GetOrdinal("delivery_date_expected")),
                Status = r.IsDBNull(r.GetOrdinal("status")) ? "Offen" : r.GetString(r.GetOrdinal("status")),
                TotalNet = r.GetDecimal(r.GetOrdinal("total_net")),
                TotalGross = r.GetDecimal(r.GetOrdinal("total_gross")),
                Notes = r.IsDBNull(r.GetOrdinal("notes")) ? "" : r.GetString(r.GetOrdinal("notes")),
                CreatedAt = r.IsDBNull(r.GetOrdinal("created_at")) ? DateTime.Now : r.GetDateTime(r.GetOrdinal("created_at"))
            });
        }
        return list;
    }

    public async Task UpdatePurchaseOrderStatusAsync(int orderId, string newStatus)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand("UPDATE purchase_orders SET status=@status WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@status", newStatus);
        cmd.Parameters.AddWithValue("@id", orderId);
        await cmd.ExecuteNonQueryAsync();
    }

    // ══════════════════════════════════════════════════════════════════
    //  CREATE METHODS
    // ══════════════════════════════════════════════════════════════════

    public async Task<int> CreateProjectAsync(string name, string description, string status, string clientName, DateTime startDate, DateTime? endDate, decimal budget)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"INSERT INTO projects (name, description, status, client_name, start_date, end_date, budget, created_at)
                           VALUES (@name, @desc, @status, @client, @start, @end, @budget, NOW()); SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@desc", description ?? "");
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@client", clientName ?? "");
        cmd.Parameters.AddWithValue("@start", startDate);
        cmd.Parameters.AddWithValue("@end", endDate.HasValue ? endDate.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@budget", budget);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> CreateEmployeeAsync(string firstName, string lastName, string email, string phone, string position, string department, decimal hourlyRate)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"INSERT INTO employees (first_name, last_name, email, phone, position, department, hourly_rate, is_active, hire_date, created_at)
                           VALUES (@fn, @ln, @email, @phone, @pos, @dept, @rate, 1, CURDATE(), NOW()); SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@fn", firstName);
        cmd.Parameters.AddWithValue("@ln", lastName);
        cmd.Parameters.AddWithValue("@email", email ?? "");
        cmd.Parameters.AddWithValue("@phone", phone ?? "");
        cmd.Parameters.AddWithValue("@pos", position ?? "");
        cmd.Parameters.AddWithValue("@dept", department ?? "");
        cmd.Parameters.AddWithValue("@rate", hourlyRate);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> CreateCustomerAsync(string companyName, string firstName, string lastName, string email, string phone, string street, string zipCode, string city, string note)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"INSERT INTO customers (company_name, first_name, last_name, email, phone, street, zip_code, city, country, note, is_active, created_at)
                           VALUES (@comp, @fn, @ln, @email, @phone, @street, @zip, @city, 'Deutschland', @note, 1, NOW()); SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@comp", companyName ?? "");
        cmd.Parameters.AddWithValue("@fn", firstName ?? "");
        cmd.Parameters.AddWithValue("@ln", lastName ?? "");
        cmd.Parameters.AddWithValue("@email", email ?? "");
        cmd.Parameters.AddWithValue("@phone", phone ?? "");
        cmd.Parameters.AddWithValue("@street", street ?? "");
        cmd.Parameters.AddWithValue("@zip", zipCode ?? "");
        cmd.Parameters.AddWithValue("@city", city ?? "");
        cmd.Parameters.AddWithValue("@note", note ?? "");
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> CreateTicketAsync(string customerName, string customerEmail, string subject, string description, int priority, int category)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"INSERT INTO tickets (customer_name, customer_email, subject, description, priority, status, category, created_at)
                           VALUES (@name, @email, @subj, @desc, @prio, 0, @cat, NOW()); SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@name", customerName ?? "");
        cmd.Parameters.AddWithValue("@email", customerEmail ?? "");
        cmd.Parameters.AddWithValue("@subj", subject);
        cmd.Parameters.AddWithValue("@desc", description ?? "");
        cmd.Parameters.AddWithValue("@prio", priority);
        cmd.Parameters.AddWithValue("@cat", category);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> CreateMeetingAsync(string title, string description, DateTime startTime, DateTime endTime,
        string location, string participants, int? projectId,
        bool isWebex = false, string? webexMeetingId = null, string? webexJoinLink = null,
        string? webexHostKey = null, string? webexPassword = null, string? webexSipAddress = null)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"INSERT INTO meetings (title, description, start_time, end_time, location, participants, project_id,
                            is_webex_meeting, webex_meeting_id, webex_join_link, webex_host_key, webex_password, webex_sip_address, created_at)
                           VALUES (@title, @desc, @start, @end, @loc, @part, @pid,
                            @isWebex, @wxId, @wxLink, @wxHost, @wxPwd, @wxSip, NOW()); SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@desc", description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@start", startTime);
        cmd.Parameters.AddWithValue("@end", endTime);
        cmd.Parameters.AddWithValue("@loc", location ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@part", participants ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@pid", projectId.HasValue ? projectId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@isWebex", isWebex);
        cmd.Parameters.AddWithValue("@wxId", webexMeetingId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@wxLink", webexJoinLink ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@wxHost", webexHostKey ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@wxPwd", webexPassword ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@wxSip", webexSipAddress ?? (object)DBNull.Value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> CreateSupplierAsync(string name, string contactPerson, string email, string phone, string address, string zipCode, string city, string notes)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"INSERT INTO suppliers (name, contact_person, email, phone, address, zip_code, city, country, notes, created_at)
                           VALUES (@name, @cp, @email, @phone, @addr, @zip, @city, 'Deutschland', @notes, NOW()); SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@cp", contactPerson ?? "");
        cmd.Parameters.AddWithValue("@email", email ?? "");
        cmd.Parameters.AddWithValue("@phone", phone ?? "");
        cmd.Parameters.AddWithValue("@addr", address ?? "");
        cmd.Parameters.AddWithValue("@zip", zipCode ?? "");
        cmd.Parameters.AddWithValue("@city", city ?? "");
        cmd.Parameters.AddWithValue("@notes", notes ?? "");
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> CreateCrmContactAsync(string firstName, string lastName, string position, string email, string phone, string mobile, string notes, int? customerId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"INSERT INTO crm_contacts (customer_id, first_name, last_name, position, email, phone, mobile, notes, is_active, created_at)
                           VALUES (@cid, @fn, @ln, @pos, @email, @phone, @mobile, @notes, 1, NOW()); SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@cid", customerId.HasValue ? customerId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@fn", firstName ?? "");
        cmd.Parameters.AddWithValue("@ln", lastName ?? "");
        cmd.Parameters.AddWithValue("@pos", position ?? "");
        cmd.Parameters.AddWithValue("@email", email ?? "");
        cmd.Parameters.AddWithValue("@phone", phone ?? "");
        cmd.Parameters.AddWithValue("@mobile", mobile ?? "");
        cmd.Parameters.AddWithValue("@notes", notes ?? "");
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> CreateCrmActivityAsync(int type, string subject, string notes, DateTime? dueDate, int? contactId, int? customerId, string createdBy)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"INSERT INTO crm_activities (contact_id, customer_id, type, subject, notes, due_date, is_completed, created_by, created_at)
                           VALUES (@cid, @custId, @type, @subj, @notes, @due, 0, @by, NOW()); SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@cid", contactId.HasValue ? contactId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@custId", customerId.HasValue ? customerId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@type", type);
        cmd.Parameters.AddWithValue("@subj", subject);
        cmd.Parameters.AddWithValue("@notes", notes ?? "");
        cmd.Parameters.AddWithValue("@due", dueDate.HasValue ? dueDate.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@by", createdBy ?? "");
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> CreateCrmDealAsync(string title, decimal value, int probability, DateTime? expectedCloseDate, string notes, int? customerId, string assignedTo)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"INSERT INTO crm_deals (customer_id, title, value, stage, probability, expected_close_date, notes, assigned_to, created_at)
                           VALUES (@cid, @title, @val, 0, @prob, @close, @notes, @assigned, NOW()); SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@cid", customerId.HasValue ? customerId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@val", value);
        cmd.Parameters.AddWithValue("@prob", probability);
        cmd.Parameters.AddWithValue("@close", expectedCloseDate.HasValue ? expectedCloseDate.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@notes", notes ?? "");
        cmd.Parameters.AddWithValue("@assigned", assignedTo ?? "");
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> CreateMeetingProtocolAsync(int? projectId, string title, DateTime meetingDate, string location, string participants, string agenda, string discussion, string decisions, string actionItems)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"INSERT INTO meeting_protocols (project_id, title, meeting_date, location, participants, agenda, discussion, decisions, action_items, created_at)
                           VALUES (@pid, @title, @date, @loc, @part, @agenda, @disc, @dec, @actions, NOW()); SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@pid", projectId.HasValue ? projectId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@date", meetingDate);
        cmd.Parameters.AddWithValue("@loc", location ?? "");
        cmd.Parameters.AddWithValue("@part", participants ?? "");
        cmd.Parameters.AddWithValue("@agenda", agenda ?? "");
        cmd.Parameters.AddWithValue("@disc", discussion ?? "");
        cmd.Parameters.AddWithValue("@dec", decisions ?? "");
        cmd.Parameters.AddWithValue("@actions", actionItems ?? "");
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    // ── Easybill User Settings ──────────────────────────────────────

    public async Task<EasybillUserSettings?> GetEasybillSettingsAsync(int userId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = "SELECT user_id, easybill_email, easybill_api_key FROM user_easybill_settings WHERE user_id = @uid";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new EasybillUserSettings
        {
            UserId = r.GetInt32(0),
            Email = r.GetString(1),
            ApiKey = r.GetString(2)
        };
    }

    public async Task SaveEasybillSettingsAsync(int userId, string email, string apiKey)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"INSERT INTO user_easybill_settings (user_id, easybill_email, easybill_api_key)
                           VALUES (@uid, @email, @key)
                           ON DUPLICATE KEY UPDATE easybill_email = @email, easybill_api_key = @key";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@key", apiKey);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteEasybillSettingsAsync(int userId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = "DELETE FROM user_easybill_settings WHERE user_id = @uid";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Webex User Settings ─────────────────────────────────────────

    public async Task<WebexUserSettings?> GetWebexSettingsAsync(int userId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = "SELECT user_id, access_token, client_id, client_secret, refresh_token, token_expiry FROM user_webex_settings WHERE user_id = @uid";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new WebexUserSettings
        {
            UserId = r.GetInt32(0),
            AccessToken = r.GetString(1),
            ClientId = r.IsDBNull(2) ? "" : r.GetString(2),
            ClientSecret = r.IsDBNull(3) ? "" : r.GetString(3),
            RefreshToken = r.IsDBNull(4) ? "" : r.GetString(4),
            TokenExpiry = r.IsDBNull(5) ? DateTime.MinValue : r.GetDateTime(5)
        };
    }

    public async Task SaveWebexSettingsAsync(int userId, string accessToken,
        string? clientId = null, string? clientSecret = null, string? refreshToken = null, DateTime? tokenExpiry = null)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"INSERT INTO user_webex_settings (user_id, access_token, client_id, client_secret, refresh_token, token_expiry)
                           VALUES (@uid, @token, @cid, @csec, @rtok, @exp)
                           ON DUPLICATE KEY UPDATE access_token = @token, client_id = @cid, client_secret = @csec, refresh_token = @rtok, token_expiry = @exp";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@token", accessToken);
        cmd.Parameters.AddWithValue("@cid", clientId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@csec", clientSecret ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@rtok", refreshToken ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@exp", tokenExpiry.HasValue ? tokenExpiry.Value : DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteWebexSettingsAsync(int userId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = "DELETE FROM user_webex_settings WHERE user_id = @uid";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        await cmd.ExecuteNonQueryAsync();
    }
}
