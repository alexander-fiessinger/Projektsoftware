using MySqlConnector;
using Projektsoftware.Api.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
            )",
            // Lokaler Artikelkatalog (unabhängig von Easybill) – Preisquelle für das Kundenportal
            @"CREATE TABLE IF NOT EXISTS products (
                id INT AUTO_INCREMENT PRIMARY KEY,
                number VARCHAR(100) NOT NULL,
                name VARCHAR(255) NOT NULL,
                description TEXT,
                unit VARCHAR(50) DEFAULT 'Stück',
                net_price DECIMAL(12,2) NOT NULL DEFAULT 0,
                vat_percent INT NOT NULL DEFAULT 19,
                is_active BOOLEAN DEFAULT TRUE,
                created_at DATETIME NOT NULL,
                updated_at DATETIME,
                UNIQUE KEY uk_product_number (number),
                INDEX idx_products_is_active (is_active)
            )",
            // Portal-Benutzerkonten (getrennt von Mitarbeiter-Benutzern), verknüpft mit customers
            @"CREATE TABLE IF NOT EXISTS customer_portal_users (
                id INT AUTO_INCREMENT PRIMARY KEY,
                customer_id INT NULL,
                email VARCHAR(255) NOT NULL,
                password_hash VARCHAR(255) NOT NULL,
                contact_name VARCHAR(150),
                is_approved BOOLEAN DEFAULT FALSE,
                is_active BOOLEAN DEFAULT TRUE,
                created_at DATETIME NOT NULL,
                approved_at DATETIME,
                last_login DATETIME,
                FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE SET NULL,
                UNIQUE KEY uk_portal_email (email),
                INDEX idx_portal_customer_id (customer_id)
            )",
            // Portal-Webshop: Bestellköpfe
            @"CREATE TABLE IF NOT EXISTS portal_orders (
                id INT AUTO_INCREMENT PRIMARY KEY,
                order_number VARCHAR(40) NOT NULL,
                portal_user_id INT NULL,
                customer_id INT NULL,
                total_net DECIMAL(12,2) NOT NULL DEFAULT 0,
                total_gross DECIMAL(12,2) NOT NULL DEFAULT 0,
                payment_method INT NOT NULL DEFAULT 0,
                status INT NOT NULL DEFAULT 0,
                note TEXT,
                created_at DATETIME NOT NULL,
                processed_at DATETIME,
                FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE SET NULL,
                UNIQUE KEY uk_portal_order_number (order_number),
                INDEX idx_portal_orders_status (status),
                INDEX idx_portal_orders_customer (customer_id)
            )",
            // Portal-Webshop: Bestellpositionen
            @"CREATE TABLE IF NOT EXISTS portal_order_items (
                id INT AUTO_INCREMENT PRIMARY KEY,
                order_id INT NOT NULL,
                product_id INT NULL,
                number VARCHAR(100),
                name VARCHAR(255) NOT NULL,
                unit VARCHAR(50) DEFAULT 'Stück',
                net_price DECIMAL(12,2) NOT NULL DEFAULT 0,
                vat_percent INT NOT NULL DEFAULT 19,
                quantity INT NOT NULL DEFAULT 1,
                FOREIGN KEY (order_id) REFERENCES portal_orders(id) ON DELETE CASCADE,
                INDEX idx_portal_order_items_order (order_id)
            )"
        ];

        foreach (var sql in tableStatements)
        {
            using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // Migration: discount_percent in customers (genereller Kundenrabatt fürs Portal)
        const string checkDiscountColumn = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'customers' AND COLUMN_NAME = 'discount_percent'";
        using (var checkColCmd = new MySqlCommand(checkDiscountColumn, conn))
        {
            var hasColumn = Convert.ToInt32(await checkColCmd.ExecuteScalarAsync()) > 0;
            if (!hasColumn)
            {
                using var alterCmd = new MySqlCommand(
                    "ALTER TABLE customers ADD COLUMN discount_percent DECIMAL(5,2) NOT NULL DEFAULT 0", conn);
                await alterCmd.ExecuteNonQueryAsync();
            }
        }

        // Migration: invoice_allowed in customers (Bonitätsfreigabe für Rechnungskauf im Portal)
        const string checkInvoiceAllowedColumn = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'customers' AND COLUMN_NAME = 'invoice_allowed'";
        using (var checkInvCmd = new MySqlCommand(checkInvoiceAllowedColumn, conn))
        {
            var hasColumn = Convert.ToInt32(await checkInvCmd.ExecuteScalarAsync()) > 0;
            if (!hasColumn)
            {
                using var alterCmd = new MySqlCommand(
                    "ALTER TABLE customers ADD COLUMN invoice_allowed BOOLEAN NOT NULL DEFAULT FALSE", conn);
                await alterCmd.ExecuteNonQueryAsync();
            }
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

    // ── Benutzerverwaltung ──────────────────────────────────────────
    public async Task<List<UserDto>> GetUsersAsync()
    {
        var list = new List<UserDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string query = @"SELECT u.id, u.username, u.role, u.is_active, u.must_change_password,
                u.created_at, u.last_login, CONCAT(COALESCE(e.first_name,''),' ',COALESCE(e.last_name,'')) AS employee_name
            FROM users u LEFT JOIN employees e ON u.employee_id = e.id ORDER BY u.username";
        using var cmd = new MySqlCommand(query, conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new UserDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                Username = r.GetString(r.GetOrdinal("username")),
                Role = r.IsDBNull(r.GetOrdinal("role")) ? "User" : r.GetString(r.GetOrdinal("role")),
                IsActive = r.GetBoolean(r.GetOrdinal("is_active")),
                MustChangePassword = !r.IsDBNull(r.GetOrdinal("must_change_password")) && r.GetBoolean(r.GetOrdinal("must_change_password")),
                EmployeeName = r.IsDBNull(r.GetOrdinal("employee_name")) ? "" : r.GetString(r.GetOrdinal("employee_name")).Trim(),
                CreatedAt = r.IsDBNull(r.GetOrdinal("created_at")) ? DateTime.MinValue : r.GetDateTime(r.GetOrdinal("created_at")),
                LastLogin = r.IsDBNull(r.GetOrdinal("last_login")) ? null : r.GetDateTime(r.GetOrdinal("last_login"))
            });
        }
        return list;
    }

    public async Task<int> CreateUserAsync(string username, string password, string role)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string query = @"INSERT INTO users (username, password_hash, role, is_active, must_change_password, created_at)
            VALUES (@u, @p, @r, TRUE, TRUE, NOW()); SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@p", HashPassword(password));
        cmd.Parameters.AddWithValue("@r", role);
        var id = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(id);
    }

    public async Task UpdateUserAsync(int userId, string role, bool isActive)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string query = "UPDATE users SET role = @r, is_active = @a WHERE id = @id";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@r", role);
        cmd.Parameters.AddWithValue("@a", isActive);
        cmd.Parameters.AddWithValue("@id", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ResetUserPasswordAsync(int userId, string newPassword)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string query = "UPDATE users SET password_hash = @p, must_change_password = TRUE WHERE id = @id";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@p", HashPassword(newPassword));
        cmd.Parameters.AddWithValue("@id", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteUserAsync(int userId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using (var del = new MySqlCommand("DELETE FROM user_permissions WHERE user_id = @id", conn))
        {
            del.Parameters.AddWithValue("@id", userId);
            await del.ExecuteNonQueryAsync();
        }
        using var cmd = new MySqlCommand("DELETE FROM users WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> GetActiveAdminCountAsync()
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE role = 'Admin' AND is_active = TRUE", conn);
        var o = await cmd.ExecuteScalarAsync();
        return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
    }

    public async Task SetUserPermissionsAsync(int userId, List<string> moduleKeys)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using (var del = new MySqlCommand("DELETE FROM user_permissions WHERE user_id = @id", conn))
        {
            del.Parameters.AddWithValue("@id", userId);
            await del.ExecuteNonQueryAsync();
        }
        foreach (var key in moduleKeys.Distinct())
        {
            using var ins = new MySqlCommand("INSERT INTO user_permissions (user_id, module_key) VALUES (@id, @k)", conn);
            ins.Parameters.AddWithValue("@id", userId);
            ins.Parameters.AddWithValue("@k", key);
            await ins.ExecuteNonQueryAsync();
        }
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
                ProgressPercent = total > 0 ? (int)(done * 100.0 / total) : 0,
                EasybillCustomerId = r.IsDBNull(r.GetOrdinal("easybill_customer_id")) ? null : r.GetInt64(r.GetOrdinal("easybill_customer_id")),
                EasybillProjectId = r.IsDBNull(r.GetOrdinal("easybill_project_id")) ? null : r.GetInt64(r.GetOrdinal("easybill_project_id"))
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

    public async Task UpdateTaskAsync(int id, string title, string description, string assignedTo, string status, string priority, DateTime? dueDate, int estimatedHours)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string query = @"UPDATE tasks SET
                                   title=@title, description=@desc, assigned_to=@assignedTo, status=@status,
                                   priority=@priority, due_date=@dueDate, estimated_hours=@estHours
                               WHERE id=@id";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@desc", description ?? "");
        cmd.Parameters.AddWithValue("@assignedTo", assignedTo ?? "");
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@priority", priority);
        cmd.Parameters.AddWithValue("@dueDate", dueDate.HasValue ? dueDate.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@estHours", estimatedHours);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteTaskAsync(int id)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("DELETE FROM tasks WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
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
                CustomerId = r.IsDBNull(r.GetOrdinal("customer_id")) ? null : r.GetInt32(r.GetOrdinal("customer_id")),
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

    // ── Kundenportal ────────────────────────────────────────────────

    /// <summary>
    /// Registriert ein neues Portal-Konto (muss anschließend durch einen Mitarbeiter freigeschaltet werden).
    /// </summary>
    public async Task<PortalRegisterResponse> RegisterPortalUserAsync(string email, string password, string contactName)
    {
        return await RegisterPortalUserAsync(new PortalRegisterRequest(
            email, password, "", "", contactName, "", "", "", "", "Deutschland", ""));
    }

    /// <summary>
    /// Registriert ein neues Portal-Konto über das Neukundenformular und legt automatisch
    /// einen verknüpften Kunden an. Das Konto muss anschließend durch einen Mitarbeiter
    /// freigeschaltet werden, bevor Preise sichtbar sind.
    /// </summary>
    public async Task<PortalRegisterResponse> RegisterPortalUserAsync(PortalRegisterRequest request)
    {
        var email = (request.Email ?? "").Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return new PortalRegisterResponse(false, "Bitte eine gültige E-Mail-Adresse angeben.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return new PortalRegisterResponse(false, "Das Passwort muss mindestens 6 Zeichen lang sein.");

        var companyName = (request.CompanyName ?? "").Trim();
        var firstName = (request.FirstName ?? "").Trim();
        var lastName = (request.LastName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(companyName) &&
            string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return new PortalRegisterResponse(false, "Bitte geben Sie einen Firmennamen oder Vor- und Nachnamen an.");
        }

        var contactName = !string.IsNullOrWhiteSpace(companyName)
            ? companyName
            : $"{firstName} {lastName}".Trim();

        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        const string checkQuery = "SELECT COUNT(*) FROM customer_portal_users WHERE email = @email";
        using (var checkCmd = new MySqlCommand(checkQuery, conn))
        {
            checkCmd.Parameters.AddWithValue("@email", email);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            if (exists)
                return new PortalRegisterResponse(false, "Für diese E-Mail-Adresse besteht bereits ein Konto.");
        }

        using var transaction = await conn.BeginTransactionAsync();
        try
        {
            // 1) Kunde anlegen (erscheint sofort in der WPF-Kundenverwaltung)
            const string insertCustomer = @"INSERT INTO customers
                (company_name, first_name, last_name, email, phone, street, zip_code, city, country, vat_id, note, is_active, created_at)
                VALUES (@company, @first, @last, @email, @phone, @street, @zip, @city, @country, @vat, @note, TRUE, NOW());
                SELECT LAST_INSERT_ID();";
            int customerId;
            using (var custCmd = new MySqlCommand(insertCustomer, conn, transaction))
            {
                custCmd.Parameters.AddWithValue("@company", companyName);
                custCmd.Parameters.AddWithValue("@first", firstName);
                custCmd.Parameters.AddWithValue("@last", lastName);
                custCmd.Parameters.AddWithValue("@email", email);
                custCmd.Parameters.AddWithValue("@phone", (request.Phone ?? "").Trim());
                custCmd.Parameters.AddWithValue("@street", (request.Street ?? "").Trim());
                custCmd.Parameters.AddWithValue("@zip", (request.ZipCode ?? "").Trim());
                custCmd.Parameters.AddWithValue("@city", (request.City ?? "").Trim());
                custCmd.Parameters.AddWithValue("@country", string.IsNullOrWhiteSpace(request.Country) ? "Deutschland" : request.Country.Trim());
                custCmd.Parameters.AddWithValue("@vat", (request.VatId ?? "").Trim());
                custCmd.Parameters.AddWithValue("@note", $"Selbstregistrierung über Kundenportal am {DateTime.Now:dd.MM.yyyy HH:mm}");
                customerId = Convert.ToInt32(await custCmd.ExecuteScalarAsync());
            }

            // 2) Portal-Konto anlegen und direkt mit dem Kunden verknüpfen (noch nicht freigeschaltet)
            const string insertQuery = @"INSERT INTO customer_portal_users
                (customer_id, email, password_hash, contact_name, is_approved, is_active, created_at)
                VALUES (@cid, @email, @hash, @name, FALSE, TRUE, NOW())";
            using (var cmd = new MySqlCommand(insertQuery, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@cid", customerId);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@hash", HashPassword(request.Password));
                cmd.Parameters.AddWithValue("@name", contactName);
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return new PortalRegisterResponse(true, null);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Authentifiziert ein Portal-Konto. Liefert auch den generellen Kundenrabatt zurück.
    /// </summary>
    public async Task<PortalLoginResponse> AuthenticatePortalUserAsync(string email, string password)
    {
        email = (email ?? "").Trim();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        const string query = @"SELECT p.id, p.customer_id, p.email, p.password_hash, p.contact_name,
                                      p.is_approved, p.is_active, c.discount_percent
                               FROM customer_portal_users p
                               LEFT JOIN customers c ON p.customer_id = c.id
                               WHERE p.email = @email";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@email", email);
        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return new PortalLoginResponse(false, 0, null, "", "", 0, "E-Mail oder Passwort ist falsch.");

        var storedHash = reader.GetString(reader.GetOrdinal("password_hash"));
        if (!storedHash.Equals(HashPassword(password), StringComparison.OrdinalIgnoreCase))
            return new PortalLoginResponse(false, 0, null, "", "", 0, "E-Mail oder Passwort ist falsch.");

        var isActive = reader.GetBoolean(reader.GetOrdinal("is_active"));
        if (!isActive)
            return new PortalLoginResponse(false, 0, null, "", "", 0, "Ihr Konto wurde gesperrt.");

        var isApproved = reader.GetBoolean(reader.GetOrdinal("is_approved"));
        if (!isApproved)
            return new PortalLoginResponse(false, 0, null, "", "", 0, "Ihr Konto wurde noch nicht freigeschaltet.");

        var userId = reader.GetInt32(reader.GetOrdinal("id"));
        var customerId = reader.IsDBNull(reader.GetOrdinal("customer_id")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("customer_id"));
        var contactName = reader.IsDBNull(reader.GetOrdinal("contact_name")) ? "" : reader.GetString(reader.GetOrdinal("contact_name"));
        var discount = reader.IsDBNull(reader.GetOrdinal("discount_percent")) ? 0m : reader.GetDecimal(reader.GetOrdinal("discount_percent"));
        await reader.CloseAsync();

        const string updateLogin = "UPDATE customer_portal_users SET last_login = NOW() WHERE id = @id";
        using (var updateCmd = new MySqlCommand(updateLogin, conn))
        {
            updateCmd.Parameters.AddWithValue("@id", userId);
            await updateCmd.ExecuteNonQueryAsync();
        }

        return new PortalLoginResponse(true, userId, customerId, email, contactName, discount, null);
    }

    /// <summary>
    /// Liefert die aktiven Artikel als Preisliste fürs Portal, mit angewandtem Kundenrabatt.
    /// </summary>
    public async Task<List<PortalProductDto>> GetPortalProductsAsync(decimal discountPercent)
    {
        var list = new List<PortalProductDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        const string query = @"SELECT id, number, name, description, unit, net_price, vat_percent
                               FROM products
                               WHERE is_active = TRUE
                               ORDER BY number";
        using var cmd = new MySqlCommand(query, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var rawDescription = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description"));
            list.Add(new PortalProductDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Number = reader.IsDBNull(reader.GetOrdinal("number")) ? "" : reader.GetString(reader.GetOrdinal("number")),
                Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString(reader.GetOrdinal("name")),
                Description = SanitizePublicDescription(rawDescription),
                Unit = reader.IsDBNull(reader.GetOrdinal("unit")) ? "Stück" : reader.GetString(reader.GetOrdinal("unit")),
                ListNetPrice = reader.IsDBNull(reader.GetOrdinal("net_price")) ? 0 : reader.GetDecimal(reader.GetOrdinal("net_price")),
                VatPercent = reader.IsDBNull(reader.GetOrdinal("vat_percent")) ? 19 : reader.GetInt32(reader.GetOrdinal("vat_percent")),
                DiscountPercent = discountPercent
            });
        }
        return list;
    }

    /// <summary>
    /// Entfernt interne Informationen (z. B. Import-Notizen und Einkaufspreise) aus der
    /// Artikelbeschreibung, bevor sie an das Kundenportal ausgeliefert werden. Solche Daten
    /// dürfen Kunden niemals sehen. Die Filterung erfolgt bewusst serverseitig, damit der
    /// Einkaufspreis das Backend gar nicht erst verlässt.
    /// </summary>
    private static string SanitizePublicDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "";

        // Beschreibung in Sätze/Fragmente zerlegen und alle internen Hinweise verwerfen.
        var fragments = description
            .Replace("\r\n", "\n")
            .Split(new[] { '\n', '.' }, StringSplitOptions.RemoveEmptyEntries);

        var visible = new List<string>();
        foreach (var fragment in fragments)
        {
            var trimmed = fragment.Trim();
            if (trimmed.Length == 0)
                continue;

            // Interne Marker: CSV-Import-Hinweis und jegliche Einkaufspreis-Angabe.
            if (trimmed.Contains("Importiert via CSV", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("Einkauf", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("EK-Preis", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("Einkaufspreis", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("Nettopreis", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Reine Geld-/Preisangaben (z. B. "640,00 €") sind interne Altdaten aus früheren
            // Importen und ergeben als öffentliche Produktbeschreibung keinen Sinn -> entfernen.
            if (IsPriceOnlyFragment(trimmed))
                continue;

            visible.Add(trimmed);
        }

        return string.Join(". ", visible);
    }

    /// <summary>
    /// Prüft, ob ein Beschreibungsfragment im Kern nur aus einem Geldbetrag besteht
    /// (z. B. "640,00 €", "EUR 1.250", "€ 99"). Solche Werte stammen aus internen
    /// Import-/Sync-Altdaten und dürfen nicht im Kundenportal erscheinen.
    /// </summary>
    private static bool IsPriceOnlyFragment(string fragment)
    {
        // Währungssymbole/-codes entfernen und prüfen, ob nur noch eine Zahl übrig bleibt.
        var stripped = fragment
            .Replace("€", "")
            .Replace("EUR", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Euro", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (stripped.Length == 0)
            return false;

        // Reiner Zahlenwert mit optionalen Tausender-/Dezimaltrennern (z. B. "1.250,00").
        return System.Text.RegularExpressions.Regex.IsMatch(
            stripped, @"^[+-]?\d{1,3}(?:[.\s]?\d{3})*(?:[.,]\d{1,2})?$");
    }

    /// <summary>
    /// Liefert den Bonitäts-/Bestellstatus eines Portal-Kunden. Steuert, ob "Auf Rechnung"
    /// als Zahlungsart erlaubt ist (erst ab 2 abgeschlossenen Bestellungen und freigegebener Bonität).
    /// </summary>
    public async Task<PortalCustomerStatusDto> GetPortalCustomerStatusAsync(int? customerId)
    {
        var status = new PortalCustomerStatusDto();
        if (customerId is null)
            return status;

        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        // Anzahl der abgeschlossenen Bestellungen (Status 2 = Erledigt)
        const string countQuery = @"SELECT COUNT(*) FROM portal_orders
                                    WHERE customer_id = @cid AND status = 2";
        using (var countCmd = new MySqlCommand(countQuery, conn))
        {
            countCmd.Parameters.AddWithValue("@cid", customerId.Value);
            status.CompletedOrderCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        }

        // Bonitätsfreigabe aus customers
        const string invoiceQuery = "SELECT invoice_allowed FROM customers WHERE id = @cid";
        using (var invCmd = new MySqlCommand(invoiceQuery, conn))
        {
            invCmd.Parameters.AddWithValue("@cid", customerId.Value);
            var result = await invCmd.ExecuteScalarAsync();
            status.InvoiceAllowed = result != null && result != DBNull.Value && Convert.ToBoolean(result);
        }

        return status;
    }

    /// <summary>
    /// Legt eine Portal-Bestellung transaktional an (Kopf + Positionen) und erzeugt eine
    /// eindeutige Bestellnummer. Die Zahlungsart "Auf Rechnung" wird serverseitig nur zugelassen,
    /// wenn der Kunde dafür berechtigt ist – andernfalls wird auf Vorkasse zurückgesetzt.
    /// </summary>
    public async Task<PortalCheckoutResponse> CreatePortalOrderAsync(PortalCheckoutRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return new PortalCheckoutResponse(false, null, "Der Warenkorb ist leer.");

        // Serverseitige Durchsetzung der Zahlungsregel
        var paymentMethod = request.PaymentMethod;
        if (paymentMethod == PortalPaymentMethod.Invoice)
        {
            var custStatus = await GetPortalCustomerStatusAsync(request.CustomerId);
            if (!custStatus.CanPayByInvoice)
                paymentMethod = PortalPaymentMethod.Prepayment;
        }

        decimal totalNet = 0m;
        decimal totalGross = 0m;
        foreach (var item in request.Items)
        {
            if (item.Quantity < 1) item.Quantity = 1;
            totalNet += item.LineNet;
            totalGross += item.LineGross;
        }

        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();
        try
        {
            // Eindeutige Bestellnummer: WS-JJJJ-laufende Nummer
            var year = DateTime.Now.Year;
            int nextSeq;
            const string seqQuery = @"SELECT COUNT(*) FROM portal_orders
                                      WHERE order_number LIKE @prefix";
            using (var seqCmd = new MySqlCommand(seqQuery, conn, transaction))
            {
                seqCmd.Parameters.AddWithValue("@prefix", $"WS-{year}-%");
                nextSeq = Convert.ToInt32(await seqCmd.ExecuteScalarAsync()) + 1;
            }
            var orderNumber = $"WS-{year}-{nextSeq:D4}";

            // Bestellkopf
            const string insertOrder = @"INSERT INTO portal_orders
                (order_number, portal_user_id, customer_id, total_net, total_gross, payment_method, status, note, created_at)
                VALUES (@num, @uid, @cid, @net, @gross, @pay, 0, @note, NOW());
                SELECT LAST_INSERT_ID();";
            int orderId;
            using (var orderCmd = new MySqlCommand(insertOrder, conn, transaction))
            {
                orderCmd.Parameters.AddWithValue("@num", orderNumber);
                orderCmd.Parameters.AddWithValue("@uid", (object?)request.UserId ?? DBNull.Value);
                orderCmd.Parameters.AddWithValue("@cid", (object?)request.CustomerId ?? DBNull.Value);
                orderCmd.Parameters.AddWithValue("@net", totalNet);
                orderCmd.Parameters.AddWithValue("@gross", totalGross);
                orderCmd.Parameters.AddWithValue("@pay", (int)paymentMethod);
                orderCmd.Parameters.AddWithValue("@note", (request.Note ?? "").Trim());
                orderId = Convert.ToInt32(await orderCmd.ExecuteScalarAsync());
            }

            // Bestellpositionen
            const string insertItem = @"INSERT INTO portal_order_items
                (order_id, product_id, number, name, unit, net_price, vat_percent, quantity)
                VALUES (@oid, @pid, @number, @name, @unit, @price, @vat, @qty)";
            foreach (var item in request.Items)
            {
                using var itemCmd = new MySqlCommand(insertItem, conn, transaction);
                itemCmd.Parameters.AddWithValue("@oid", orderId);
                itemCmd.Parameters.AddWithValue("@pid", item.ProductId > 0 ? item.ProductId : (object)DBNull.Value);
                itemCmd.Parameters.AddWithValue("@number", (item.Number ?? "").Trim());
                itemCmd.Parameters.AddWithValue("@name", (item.Name ?? "").Trim());
                itemCmd.Parameters.AddWithValue("@unit", string.IsNullOrWhiteSpace(item.Unit) ? "Stück" : item.Unit.Trim());
                itemCmd.Parameters.AddWithValue("@price", item.NetPrice);
                itemCmd.Parameters.AddWithValue("@vat", item.VatPercent);
                itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                await itemCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return new PortalCheckoutResponse(true, orderNumber, null);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Liefert Bestellungen für das Portal. Wird <paramref name="customerId"/> angegeben,
    /// werden nur Bestellungen dieses Kunden zurückgegeben (Bestellhistorie im Portal),
    /// andernfalls alle Bestellungen (Sachbearbeiter-Sicht).
    /// </summary>
    public async Task<List<PortalOrderDto>> GetPortalOrdersAsync(int? customerId = null, bool includeItems = true)
    {
        var orders = new List<PortalOrderDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        var query = @"SELECT o.id, o.order_number, o.customer_id, o.total_net, o.total_gross,
                             o.payment_method, o.status, o.note, o.created_at,
                             COALESCE(NULLIF(TRIM(CONCAT(COALESCE(c.first_name,''),' ',COALESCE(c.last_name,''))),''),
                                      c.company_name, '') AS customer_name
                      FROM portal_orders o
                      LEFT JOIN customers c ON o.customer_id = c.id";
        if (customerId is not null)
            query += " WHERE o.customer_id = @cid";
        query += " ORDER BY o.created_at DESC";

        using (var cmd = new MySqlCommand(query, conn))
        {
            if (customerId is not null)
                cmd.Parameters.AddWithValue("@cid", customerId.Value);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(new PortalOrderDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    OrderNumber = reader.GetString(reader.GetOrdinal("order_number")),
                    CustomerId = reader.IsDBNull(reader.GetOrdinal("customer_id")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("customer_id")),
                    CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_name")) ? "" : reader.GetString(reader.GetOrdinal("customer_name")),
                    TotalNet = reader.GetDecimal(reader.GetOrdinal("total_net")),
                    TotalGross = reader.GetDecimal(reader.GetOrdinal("total_gross")),
                    PaymentMethod = (PortalPaymentMethod)reader.GetInt32(reader.GetOrdinal("payment_method")),
                    Status = reader.GetInt32(reader.GetOrdinal("status")),
                    Note = reader.IsDBNull(reader.GetOrdinal("note")) ? "" : reader.GetString(reader.GetOrdinal("note")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }
        }

        if (includeItems && orders.Count > 0)
        {
            const string itemQuery = @"SELECT product_id, number, name, unit, net_price, vat_percent, quantity
                                       FROM portal_order_items WHERE order_id = @oid";
            foreach (var order in orders)
            {
                using var itemCmd = new MySqlCommand(itemQuery, conn);
                itemCmd.Parameters.AddWithValue("@oid", order.Id);
                using var itemReader = await itemCmd.ExecuteReaderAsync();
                while (await itemReader.ReadAsync())
                {
                    order.Items.Add(new PortalCartItemDto
                    {
                        ProductId = itemReader.IsDBNull(0) ? 0 : itemReader.GetInt32(0),
                        Number = itemReader.IsDBNull(1) ? "" : itemReader.GetString(1),
                        Name = itemReader.IsDBNull(2) ? "" : itemReader.GetString(2),
                        Unit = itemReader.IsDBNull(3) ? "Stück" : itemReader.GetString(3),
                        NetPrice = itemReader.IsDBNull(4) ? 0 : itemReader.GetDecimal(4),
                        VatPercent = itemReader.IsDBNull(5) ? 19 : itemReader.GetInt32(5),
                        Quantity = itemReader.IsDBNull(6) ? 1 : itemReader.GetInt32(6)
                    });
                }
            }
        }

        return orders;
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

    public async Task UpdateProjectAsync(int id, string name, string description, string status, string clientName, DateTime startDate, DateTime? endDate, decimal budget)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"UPDATE projects SET
                               name=@name, description=@desc, status=@status, client_name=@client,
                               start_date=@start, end_date=@end, budget=@budget
                           WHERE id=@id";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@desc", description ?? "");
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@client", clientName ?? "");
        cmd.Parameters.AddWithValue("@start", startDate);
        cmd.Parameters.AddWithValue("@end", endDate.HasValue ? endDate.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@budget", budget);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteProjectAsync(int id)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("DELETE FROM projects WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
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

    public async Task UpdateCustomerAsync(int id, string companyName, string firstName, string lastName, string email, string phone, string street, string zipCode, string city, string note, bool isActive)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"UPDATE customers SET
                               company_name=@comp, first_name=@fn, last_name=@ln, email=@email, phone=@phone,
                               street=@street, zip_code=@zip, city=@city, note=@note, is_active=@active, updated_at=NOW()
                           WHERE id=@id";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@comp", companyName ?? "");
        cmd.Parameters.AddWithValue("@fn", firstName ?? "");
        cmd.Parameters.AddWithValue("@ln", lastName ?? "");
        cmd.Parameters.AddWithValue("@email", email ?? "");
        cmd.Parameters.AddWithValue("@phone", phone ?? "");
        cmd.Parameters.AddWithValue("@street", street ?? "");
        cmd.Parameters.AddWithValue("@zip", zipCode ?? "");
        cmd.Parameters.AddWithValue("@city", city ?? "");
        cmd.Parameters.AddWithValue("@note", note ?? "");
        cmd.Parameters.AddWithValue("@active", isActive);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteCustomerAsync(int id)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("DELETE FROM customers WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
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

    public async Task UpdateCrmContactAsync(int id, string firstName, string lastName, string position, string email, string phone, string mobile, string notes, int? customerId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"UPDATE crm_contacts SET
                               customer_id=@cid, first_name=@fn, last_name=@ln, position=@pos,
                               email=@email, phone=@phone, mobile=@mobile, notes=@notes
                           WHERE id=@id";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@cid", customerId.HasValue ? customerId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@fn", firstName ?? "");
        cmd.Parameters.AddWithValue("@ln", lastName ?? "");
        cmd.Parameters.AddWithValue("@pos", position ?? "");
        cmd.Parameters.AddWithValue("@email", email ?? "");
        cmd.Parameters.AddWithValue("@phone", phone ?? "");
        cmd.Parameters.AddWithValue("@mobile", mobile ?? "");
        cmd.Parameters.AddWithValue("@notes", notes ?? "");
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteCrmContactAsync(int id)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("DELETE FROM crm_contacts WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
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

    public async Task UpdateCrmActivityAsync(int id, int type, string subject, string notes, DateTime? dueDate, bool isCompleted)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"UPDATE crm_activities SET
                               type=@type, subject=@subj, notes=@notes, due_date=@due, is_completed=@done
                           WHERE id=@id";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@type", type);
        cmd.Parameters.AddWithValue("@subj", subject);
        cmd.Parameters.AddWithValue("@notes", notes ?? "");
        cmd.Parameters.AddWithValue("@due", dueDate.HasValue ? dueDate.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@done", isCompleted);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteCrmActivityAsync(int id)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("DELETE FROM crm_activities WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
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

    public async Task UpdateCrmDealAsync(int id, string title, decimal value, int probability, DateTime? expectedCloseDate, string notes, int? customerId, string assignedTo)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string q = @"UPDATE crm_deals SET
                               customer_id=@cid, title=@title, value=@val, probability=@prob,
                               expected_close_date=@close, notes=@notes, assigned_to=@assigned
                           WHERE id=@id";
        using var cmd = new MySqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@cid", customerId.HasValue ? customerId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@val", value);
        cmd.Parameters.AddWithValue("@prob", probability);
        cmd.Parameters.AddWithValue("@close", expectedCloseDate.HasValue ? expectedCloseDate.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@notes", notes ?? "");
        cmd.Parameters.AddWithValue("@assigned", assignedTo ?? "");
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteCrmDealAsync(int id)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("DELETE FROM crm_deals WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
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

    // ── Sales / Leads ───────────────────────────────────────────────

    private static async Task EnsureSalesLeadsTableAsync(MySqlConnection conn)
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS sales_leads (
                id INT AUTO_INCREMENT PRIMARY KEY,
                title VARCHAR(255) NOT NULL DEFAULT '',
                contact_name VARCHAR(255) NOT NULL DEFAULT '',
                contact_company VARCHAR(255),
                contact_email VARCHAR(255),
                contact_phone VARCHAR(50),
                source VARCHAR(100),
                status INT NOT NULL DEFAULT 0,
                lead_date DATE NOT NULL,
                notes TEXT,
                original_file_name VARCHAR(255),
                file_data LONGBLOB,
                created_by VARCHAR(100),
                created_at DATETIME NOT NULL,
                INDEX idx_lead_date (lead_date),
                INDEX idx_status (status)
            )";
        using var cmd = new MySqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<SalesLeadDto>> GetSalesLeadsAsync()
    {
        var list = new List<SalesLeadDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureSalesLeadsTableAsync(conn);

        using var cmd = new MySqlCommand(
            "SELECT * FROM sales_leads ORDER BY lead_date DESC, created_at DESC", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new SalesLeadDto
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                Title = r.IsDBNull(r.GetOrdinal("title")) ? "" : r.GetString(r.GetOrdinal("title")),
                ContactName = r.IsDBNull(r.GetOrdinal("contact_name")) ? "" : r.GetString(r.GetOrdinal("contact_name")),
                ContactCompany = r.IsDBNull(r.GetOrdinal("contact_company")) ? "" : r.GetString(r.GetOrdinal("contact_company")),
                ContactEmail = r.IsDBNull(r.GetOrdinal("contact_email")) ? "" : r.GetString(r.GetOrdinal("contact_email")),
                ContactPhone = r.IsDBNull(r.GetOrdinal("contact_phone")) ? "" : r.GetString(r.GetOrdinal("contact_phone")),
                Source = r.IsDBNull(r.GetOrdinal("source")) ? "" : r.GetString(r.GetOrdinal("source")),
                Status = r.IsDBNull(r.GetOrdinal("status")) ? 0 : r.GetInt32(r.GetOrdinal("status")),
                LeadDate = r.IsDBNull(r.GetOrdinal("lead_date")) ? DateTime.Today : r.GetDateTime(r.GetOrdinal("lead_date")),
                Notes = r.IsDBNull(r.GetOrdinal("notes")) ? "" : r.GetString(r.GetOrdinal("notes")),
                HasFile = !r.IsDBNull(r.GetOrdinal("file_data")),
                CreatedBy = r.IsDBNull(r.GetOrdinal("created_by")) ? "" : r.GetString(r.GetOrdinal("created_by")),
                CreatedAt = r.IsDBNull(r.GetOrdinal("created_at")) ? DateTime.Now : r.GetDateTime(r.GetOrdinal("created_at"))
            });
        }
        return list;
    }

    public async Task<int> CreateSalesLeadAsync(string title, string contactName, string contactCompany,
        string contactEmail, string contactPhone, string source, int status, DateTime leadDate,
        string notes, string createdBy)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureSalesLeadsTableAsync(conn);

        const string sql = @"INSERT INTO sales_leads
            (title, contact_name, contact_company, contact_email, contact_phone,
             source, status, lead_date, notes, created_by, created_at)
            VALUES (@title,@cname,@ccomp,@cemail,@cphone,@src,@status,@ldate,@notes,@cby,@now);
            SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@cname", contactName);
        cmd.Parameters.AddWithValue("@ccomp", contactCompany);
        cmd.Parameters.AddWithValue("@cemail", contactEmail);
        cmd.Parameters.AddWithValue("@cphone", contactPhone);
        cmd.Parameters.AddWithValue("@src", source);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@ldate", leadDate);
        cmd.Parameters.AddWithValue("@notes", notes);
        cmd.Parameters.AddWithValue("@cby", createdBy);
        cmd.Parameters.AddWithValue("@now", DateTime.Now);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task UpdateSalesLeadStatusAsync(int leadId, int newStatus)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string sql = "UPDATE sales_leads SET status=@status WHERE id=@id";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@status", newStatus);
        cmd.Parameters.AddWithValue("@id", leadId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateSalesLeadAsync(int id, string title, string contactName, string contactCompany,
        string contactEmail, string contactPhone, string source, int status, DateTime leadDate, string notes)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureSalesLeadsTableAsync(conn);

        const string sql = @"UPDATE sales_leads SET
            title=@title, contact_name=@cname, contact_company=@ccomp, contact_email=@cemail,
            contact_phone=@cphone, source=@src, status=@status, lead_date=@ldate, notes=@notes
            WHERE id=@id";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@cname", contactName);
        cmd.Parameters.AddWithValue("@ccomp", contactCompany);
        cmd.Parameters.AddWithValue("@cemail", contactEmail);
        cmd.Parameters.AddWithValue("@cphone", contactPhone);
        cmd.Parameters.AddWithValue("@src", source);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@ldate", leadDate);
        cmd.Parameters.AddWithValue("@notes", notes);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteSalesLeadAsync(int id)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        const string sql = "DELETE FROM sales_leads WHERE id=@id";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Notifications ───────────────────────────────────────────────

    public async Task<List<NotificationDto>> GetNotificationsAsync()
    {
        var list = new List<NotificationDto>();
        try
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // Überfällige Aufgaben
            const string overdue = @"
                SELECT t.title, COALESCE(p.name,'') AS project_name, t.due_date
                FROM tasks t
                LEFT JOIN projects p ON t.project_id = p.id
                WHERE t.due_date < CURDATE()
                  AND t.status NOT IN ('Erledigt','Abgeschlossen')
                ORDER BY t.due_date ASC
                LIMIT 20";
            using (var cmd = new MySqlCommand(overdue, conn))
            using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    var due = r.GetDateTime(2);
                    var days = (DateTime.Today - due).Days;
                    var project = r.IsDBNull(1) ? "" : r.GetString(1);
                    list.Add(new NotificationDto
                    {
                        Title = "Aufgabe überfällig",
                        Message = $"{r.GetString(0)}" + (string.IsNullOrEmpty(project) ? "" : $" ({project})") +
                                  $" — seit {days} Tag{(days == 1 ? "" : "en")} überfällig",
                        Severity = days > 7 ? "Error" : "Warning",
                        Timestamp = due
                    });
                }
            }

            // Nicht zugewiesene offene Tickets
            const string unassigned = @"
                SELECT COUNT(*) FROM tickets
                WHERE assigned_to_employee_id IS NULL AND status NOT IN (3,4)";
            using (var cmd = new MySqlCommand(unassigned, conn))
            {
                var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (count > 0)
                {
                    list.Add(new NotificationDto
                    {
                        Title = "Nicht zugewiesene Tickets",
                        Message = $"{count} offene Ticket{(count == 1 ? "" : "s")} ohne Bearbeiter",
                        Severity = count > 5 ? "Error" : "Warning",
                        Timestamp = DateTime.Now
                    });
                }
            }

            // SLA-Verletzungen
            const string sla = @"
                SELECT COUNT(*) FROM tickets
                WHERE status NOT IN (3,4) AND (
                    (priority = 3 AND TIMESTAMPDIFF(HOUR, created_at, NOW()) > 4)  OR
                    (priority = 2 AND TIMESTAMPDIFF(HOUR, created_at, NOW()) > 8)  OR
                    (priority = 1 AND TIMESTAMPDIFF(HOUR, created_at, NOW()) > 24) OR
                    (priority = 0 AND TIMESTAMPDIFF(HOUR, created_at, NOW()) > 72)
                )";
            using (var cmd = new MySqlCommand(sla, conn))
            {
                var breached = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (breached > 0)
                {
                    list.Add(new NotificationDto
                    {
                        Title = "SLA-Verletzung",
                        Message = $"{breached} Ticket{(breached == 1 ? "" : "s")} haben die SLA-Reaktionszeit überschritten",
                        Severity = "Error",
                        Timestamp = DateTime.Now
                    });
                }
            }
        }
        catch { }
        return list.OrderByDescending(n => n.Severity == "Error").ThenByDescending(n => n.Timestamp).ToList();
    }

    // ── Audit-Log ───────────────────────────────────────────────────

    public async Task<List<AuditLogDto>> GetAuditLogAsync(int limit = 300)
    {
        var list = new List<AuditLogDto>();
        try
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            var query = $@"
                SELECT id, timestamp, user_name, entity_type, entity_id, action, details
                FROM audit_log
                ORDER BY timestamp DESC
                LIMIT {limit}";
            using var cmd = new MySqlCommand(query, conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new AuditLogDto
                {
                    Id = r.GetInt32(r.GetOrdinal("id")),
                    Timestamp = r.GetDateTime(r.GetOrdinal("timestamp")),
                    UserName = r.IsDBNull(r.GetOrdinal("user_name")) ? "" : r.GetString(r.GetOrdinal("user_name")),
                    EntityType = r.IsDBNull(r.GetOrdinal("entity_type")) ? "" : r.GetString(r.GetOrdinal("entity_type")),
                    EntityId = r.IsDBNull(r.GetOrdinal("entity_id")) ? "" : r.GetString(r.GetOrdinal("entity_id")),
                    Action = r.IsDBNull(r.GetOrdinal("action")) ? "" : r.GetString(r.GetOrdinal("action")),
                    Details = r.IsDBNull(r.GetOrdinal("details")) ? "" : r.GetString(r.GetOrdinal("details"))
                });
            }
        }
        catch { }
        return list;
    }

    // ── Globale Suche ───────────────────────────────────────────────

    public async Task<List<SearchResultDto>> GlobalSearchAsync(string term, int maxResults = 50)
    {
        var list = new List<SearchResultDto>();
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return list;

        var like = $"%{term}%";
        try
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            async Task RunAsync(string sql, string type, string icon)
            {
                try
                {
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@q", like);
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        list.Add(new SearchResultDto
                        {
                            Type = type,
                            Icon = icon,
                            Id = r.GetInt32(0),
                            Title = r.IsDBNull(1) ? "" : r.GetString(1),
                            Description = r.IsDBNull(2) ? "" : r.GetString(2)
                        });
                    }
                }
                catch { }
            }

            await RunAsync(
                "SELECT id, name, COALESCE(status,'') FROM projects WHERE name LIKE @q OR description LIKE @q LIMIT 15",
                "Projekt", "📁");
            await RunAsync(
                "SELECT id, subject, COALESCE(customer_name,'') FROM tickets WHERE subject LIKE @q OR description LIKE @q LIMIT 15",
                "Ticket", "🎫");
            await RunAsync(
                "SELECT id, title, COALESCE(status,'') FROM tasks WHERE title LIKE @q OR description LIKE @q LIMIT 15",
                "Aufgabe", "📋");
            await RunAsync(
                "SELECT id, CONCAT(COALESCE(first_name,''),' ',COALESCE(last_name,'')), COALESCE(email,'') FROM employees WHERE first_name LIKE @q OR last_name LIKE @q OR email LIKE @q LIMIT 15",
                "Mitarbeiter", "👤");
            await RunAsync(
                "SELECT id, COALESCE(NULLIF(company_name,''),CONCAT(COALESCE(first_name,''),' ',COALESCE(last_name,''))), COALESCE(email,'') FROM customers WHERE company_name LIKE @q OR first_name LIKE @q OR last_name LIKE @q OR email LIKE @q LIMIT 15",
                "Kunde", "🏢");
        }
        catch { }
        return list.Take(maxResults).ToList();
    }

    // ── Analytics / Berichte ────────────────────────────────────────
    public async Task<AnalyticsDto> GetAnalyticsAsync()
    {
        var dto = new AnalyticsDto();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        async Task<int> ScalarAsync(string sql)
        {
            try
            {
                using var cmd = new MySqlCommand(sql, conn);
                var o = await cmd.ExecuteScalarAsync();
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
            }
            catch { return 0; }
        }

        // Tickets (Status int: <4 offen, >=4 gelöst/geschlossen)
        dto.TicketsTotal = await ScalarAsync("SELECT COUNT(*) FROM tickets");
        dto.TicketsOpen = await ScalarAsync("SELECT COUNT(*) FROM tickets WHERE status < 4");
        dto.TicketsResolved = await ScalarAsync("SELECT COUNT(*) FROM tickets WHERE status >= 4");
        dto.TicketResolveRate = dto.TicketsTotal > 0
            ? Math.Round(100.0 * dto.TicketsResolved / dto.TicketsTotal, 1) : 0;

        // Leads (Status int: 0 Neu, 1 InBearbeitung, 2 Qualifiziert, 3 Abgelehnt)
        dto.LeadsTotal = await ScalarAsync("SELECT COUNT(*) FROM sales_leads");
        dto.LeadsNew = await ScalarAsync("SELECT COUNT(*) FROM sales_leads WHERE status = 0");
        dto.LeadsInProgress = await ScalarAsync("SELECT COUNT(*) FROM sales_leads WHERE status = 1");
        dto.LeadsWon = await ScalarAsync("SELECT COUNT(*) FROM sales_leads WHERE status = 2");
        dto.LeadsLost = await ScalarAsync("SELECT COUNT(*) FROM sales_leads WHERE status = 3");
        var leadsClosed = dto.LeadsWon + dto.LeadsLost;
        dto.LeadConversionRate = leadsClosed > 0
            ? Math.Round(100.0 * dto.LeadsWon / leadsClosed, 1) : 0;

        // Projekte nach Status
        try
        {
            using var cmd = new MySqlCommand(
                "SELECT COALESCE(NULLIF(status,''),'Unbekannt') AS s, COUNT(*) AS c FROM projects GROUP BY s ORDER BY c DESC", conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                dto.ProjectsByStatus.Add(new AnalyticsBucket
                {
                    Label = r.GetString(0),
                    Count = r.GetInt32(1)
                });
            }
        }
        catch { }

        // Aufgaben
        dto.TasksTotal = await ScalarAsync("SELECT COUNT(*) FROM tasks");
        dto.TasksOpen = await ScalarAsync("SELECT COUNT(*) FROM tasks WHERE status <> 'Erledigt'");
        dto.TasksDone = await ScalarAsync("SELECT COUNT(*) FROM tasks WHERE status = 'Erledigt'");
        dto.TasksOverdue = await ScalarAsync("SELECT COUNT(*) FROM tasks WHERE due_date < CURDATE() AND status <> 'Erledigt'");

        return dto;
    }

    // ── Budget-Tracking ─────────────────────────────────────────────

    private async Task EnsureProjectBudgetsTableAsync(MySqlConnection conn)
    {
        const string ddl = @"CREATE TABLE IF NOT EXISTS project_budgets (
            id INT AUTO_INCREMENT PRIMARY KEY,
            project_id INT NOT NULL UNIQUE,
            total_planned_budget DECIMAL(15,2) NOT NULL DEFAULT 0,
            total_actual_budget DECIMAL(15,2) NOT NULL DEFAULT 0,
            total_planned_hours DECIMAL(10,2) NOT NULL DEFAULT 0,
            total_actual_hours DECIMAL(10,2) NOT NULL DEFAULT 0,
            currency VARCHAR(10) NOT NULL DEFAULT 'EUR',
            last_updated DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
        using var cmd = new MySqlCommand(ddl, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task EnsureBudgetEntriesTableAsync(MySqlConnection conn)
    {
        const string ddl = @"CREATE TABLE IF NOT EXISTS project_budget_entries (
            id INT AUTO_INCREMENT PRIMARY KEY,
            project_id INT NOT NULL,
            category VARCHAR(100) NOT NULL,
            description VARCHAR(300) NULL,
            planned_amount DECIMAL(15,2) NOT NULL DEFAULT 0,
            actual_amount DECIMAL(15,2) NOT NULL DEFAULT 0,
            planned_hours DECIMAL(10,2) NULL,
            actual_hours DECIMAL(10,2) NULL,
            cost_per_hour DECIMAL(10,2) NULL,
            entry_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            notes LONGTEXT NULL,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            INDEX idx_project (project_id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
        using var cmd = new MySqlCommand(ddl, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<ProjectBudgetDto?> GetProjectBudgetAsync(int projectId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureProjectBudgetsTableAsync(conn);

        ProjectBudgetDto? budget = null;
        using (var cmd = new MySqlCommand(
            "SELECT * FROM project_budgets WHERE project_id = @pid LIMIT 1", conn))
        {
            cmd.Parameters.AddWithValue("@pid", projectId);
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                int o(string n) => r.GetOrdinal(n);
                budget = new ProjectBudgetDto
                {
                    Id = r.GetInt32(o("id")),
                    ProjectId = r.GetInt32(o("project_id")),
                    TotalPlannedBudget = r.GetDecimal(o("total_planned_budget")),
                    TotalActualBudget = r.GetDecimal(o("total_actual_budget")),
                    TotalPlannedHours = r.GetDecimal(o("total_planned_hours")),
                    TotalActualHours = r.GetDecimal(o("total_actual_hours")),
                    Currency = r.IsDBNull(o("currency")) ? "EUR" : r.GetString(o("currency")),
                    LastUpdated = r.IsDBNull(o("last_updated")) ? DateTime.Now : r.GetDateTime(o("last_updated"))
                };
            }
        }

        // Fallback: aus Projekt-Stammdaten (projects.budget) synthetisieren
        if (budget == null)
        {
            decimal projectBudget = 0m;
            using (var cmd = new MySqlCommand(
                "SELECT budget FROM projects WHERE id = @pid LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@pid", projectId);
                var val = await cmd.ExecuteScalarAsync();
                if (val != null && val != DBNull.Value)
                    projectBudget = Convert.ToDecimal(val);
            }

            budget = new ProjectBudgetDto
            {
                ProjectId = projectId,
                TotalPlannedBudget = projectBudget,
                Currency = "EUR",
                LastUpdated = DateTime.Now
            };
        }

        // Tatsächliche Stunden aus time_entries summieren
        try
        {
            using var cmd = new MySqlCommand(
                "SELECT COALESCE(SUM(TIME_TO_SEC(duration))/3600,0) FROM time_entries WHERE project_id = @pid", conn);
            cmd.Parameters.AddWithValue("@pid", projectId);
            var val = await cmd.ExecuteScalarAsync();
            if (val != null && val != DBNull.Value)
                budget.TotalActualHours = Math.Round(Convert.ToDecimal(val), 2);
        }
        catch { }

        // Geplante Stunden ableiten, falls keine gesetzt: aus geplantem Budget / Standardstundensatz (75 €)
        if (budget.TotalPlannedHours == 0 && budget.TotalPlannedBudget > 0)
            budget.TotalPlannedHours = Math.Round(budget.TotalPlannedBudget / 75m, 2);

        // Tatsächliches Budget = Ist-Stunden * Stundensatz (aus Plan abgeleitet, fallback 75 €)
        decimal rate = budget.TotalPlannedHours > 0
            ? budget.TotalPlannedBudget / budget.TotalPlannedHours
            : 75m;
        if (budget.TotalActualBudget == 0)
            budget.TotalActualBudget = Math.Round(budget.TotalActualHours * rate, 2);

        return budget;
    }

    public async Task<List<BudgetEntryDto>> GetBudgetEntriesByProjectAsync(int projectId)
    {
        var list = new List<BudgetEntryDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureBudgetEntriesTableAsync(conn);

        using (var cmd = new MySqlCommand(
            "SELECT * FROM project_budget_entries WHERE project_id = @pid ORDER BY entry_date DESC", conn))
        {
            cmd.Parameters.AddWithValue("@pid", projectId);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                int o(string n) => r.GetOrdinal(n);
                list.Add(new BudgetEntryDto
                {
                    Id = r.GetInt32(o("id")),
                    ProjectId = r.GetInt32(o("project_id")),
                    Category = r.IsDBNull(o("category")) ? "" : r.GetString(o("category")),
                    Description = r.IsDBNull(o("description")) ? null : r.GetString(o("description")),
                    PlannedAmount = r.GetDecimal(o("planned_amount")),
                    ActualAmount = r.GetDecimal(o("actual_amount")),
                    PlannedHours = r.IsDBNull(o("planned_hours")) ? null : r.GetDecimal(o("planned_hours")),
                    ActualHours = r.IsDBNull(o("actual_hours")) ? null : r.GetDecimal(o("actual_hours")),
                    CostPerHour = r.IsDBNull(o("cost_per_hour")) ? null : r.GetDecimal(o("cost_per_hour")),
                    EntryDate = r.GetDateTime(o("entry_date")),
                    Notes = r.IsDBNull(o("notes")) ? null : r.GetString(o("notes"))
                });
            }
        }

        // Fallback: keine manuellen Einträge -> aus time_entries je Mitarbeiter aggregieren
        if (list.Count == 0)
        {
            try
            {
                using var cmd = new MySqlCommand(@"
                    SELECT employee_name,
                           COALESCE(SUM(TIME_TO_SEC(duration))/3600,0) AS hours,
                           MAX(date) AS last_date
                    FROM time_entries
                    WHERE project_id = @pid
                    GROUP BY employee_name
                    ORDER BY hours DESC", conn);
                cmd.Parameters.AddWithValue("@pid", projectId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var name = r.IsDBNull(0) ? "Unbekannt" : r.GetString(0);
                    var hours = r.IsDBNull(1) ? 0m : Math.Round(Convert.ToDecimal(r.GetValue(1)), 2);
                    var lastDate = r.IsDBNull(2) ? DateTime.Now : r.GetDateTime(2);
                    if (hours <= 0) continue;
                    list.Add(new BudgetEntryDto
                    {
                        ProjectId = projectId,
                        Category = "Arbeitszeit",
                        Description = name,
                        ActualHours = hours,
                        CostPerHour = 75m,
                        ActualAmount = Math.Round(hours * 75m, 2),
                        PlannedAmount = 0m,
                        EntryDate = lastDate
                    });
                }
            }
            catch { }
        }

        return list;
    }

    public async Task<BudgetOverviewDto?> GetBudgetOverviewAsync(int projectId)
    {
        var budget = await GetProjectBudgetAsync(projectId);
        if (budget == null) return null;

        var entries = await GetBudgetEntriesByProjectAsync(projectId);

        var breakdown = entries
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Category) ? "Sonstiges" : e.Category)
            .Select(g => new CategoryBreakdownDto
            {
                Category = g.Key,
                PlannedAmount = g.Sum(x => x.PlannedAmount),
                ActualAmount = g.Sum(x => x.ActualAmount)
            })
            .ToList();

        decimal variance = budget.TotalActualBudget - budget.TotalPlannedBudget;
        decimal variancePct = budget.TotalPlannedBudget > 0
            ? Math.Round(variance / budget.TotalPlannedBudget * 100m, 2)
            : 0m;

        return new BudgetOverviewDto
        {
            ProjectId = projectId,
            Budget = budget,
            CategoryBreakdown = breakdown,
            TotalVariance = variance,
            VariancePercentage = variancePct,
            IsOverBudget = budget.TotalActualBudget > budget.TotalPlannedBudget
        };
    }

    public async Task AddBudgetEntryAsync(BudgetEntryDto entry)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureBudgetEntriesTableAsync(conn);

        const string sql = @"INSERT INTO project_budget_entries
            (project_id, category, description, planned_amount, actual_amount,
             planned_hours, actual_hours, cost_per_hour, entry_date, notes, created_at, updated_at)
            VALUES
            (@p, @c, @d, @pa, @aa, @ph, @ah, @cph, @ed, @n, NOW(), NOW());";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@p", entry.ProjectId);
        cmd.Parameters.AddWithValue("@c", entry.Category ?? "");
        cmd.Parameters.AddWithValue("@d", (object?)entry.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pa", entry.PlannedAmount);
        cmd.Parameters.AddWithValue("@aa", entry.ActualAmount);
        cmd.Parameters.AddWithValue("@ph", (object?)entry.PlannedHours ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ah", (object?)entry.ActualHours ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cph", (object?)entry.CostPerHour ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ed", entry.EntryDate);
        cmd.Parameters.AddWithValue("@n", (object?)entry.Notes ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SaveProjectBudgetAsync(ProjectBudgetDto budget)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureProjectBudgetsTableAsync(conn);

        const string sql = @"INSERT INTO project_budgets
            (project_id, total_planned_budget, total_actual_budget, total_planned_hours, total_actual_hours, currency, last_updated)
            VALUES (@p, @pb, @ab, @ph, @ah, @cur, NOW())
            ON DUPLICATE KEY UPDATE
                total_planned_budget = @pb,
                total_actual_budget = @ab,
                total_planned_hours = @ph,
                total_actual_hours = @ah,
                currency = @cur,
                last_updated = NOW();";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@p", budget.ProjectId);
        cmd.Parameters.AddWithValue("@pb", budget.TotalPlannedBudget);
        cmd.Parameters.AddWithValue("@ab", budget.TotalActualBudget);
        cmd.Parameters.AddWithValue("@ph", budget.TotalPlannedHours);
        cmd.Parameters.AddWithValue("@ah", budget.TotalActualHours);
        cmd.Parameters.AddWithValue("@cur", budget.Currency ?? "EUR");
        await cmd.ExecuteNonQueryAsync();
    }

    // ── SLA-Monitoring ──────────────────────────────────────────────

    private async Task EnsureTicketSlaStatusTableAsync(MySqlConnection conn)
    {
        const string ddl = @"CREATE TABLE IF NOT EXISTS ticket_sla_status (
            id INT AUTO_INCREMENT PRIMARY KEY,
            ticket_id INT NOT NULL,
            sla_rule_id INT NULL,
            first_response_due DATETIME NULL,
            first_response_at DATETIME NULL,
            resolution_due DATETIME NULL,
            resolved_at DATETIME NULL,
            is_breached TINYINT(1) NOT NULL DEFAULT 0,
            breach_type VARCHAR(50) NULL,
            escalation_level INT NOT NULL DEFAULT 0,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            UNIQUE KEY uq_ticket (ticket_id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
        using var cmd = new MySqlCommand(ddl, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    // SLA-Stunden je Priorität (int): 3=Kritisch,2=Hoch,1=Mittel,0=Niedrig
    private static (int firstResponse, int resolution) GetSlaHoursForPriority(int priority) => priority switch
    {
        3 => (1, 4),
        2 => (2, 8),
        1 => (4, 24),
        0 => (8, 72),
        _ => (4, 24)
    };

    private static SlaStatusDto ReadSlaStatus(MySqlConnector.MySqlDataReader r)
    {
        int o(string n) => r.GetOrdinal(n);
        return new SlaStatusDto
        {
            Id = r.GetInt32(o("id")),
            TicketId = r.GetInt32(o("ticket_id")),
            SlaRuleId = r.IsDBNull(o("sla_rule_id")) ? null : r.GetInt32(o("sla_rule_id")),
            FirstResponseDue = r.IsDBNull(o("first_response_due")) ? null : r.GetDateTime(o("first_response_due")),
            FirstResponseAt = r.IsDBNull(o("first_response_at")) ? null : r.GetDateTime(o("first_response_at")),
            ResolutionDue = r.IsDBNull(o("resolution_due")) ? null : r.GetDateTime(o("resolution_due")),
            ResolvedAt = r.IsDBNull(o("resolved_at")) ? null : r.GetDateTime(o("resolved_at")),
            IsBreached = !r.IsDBNull(o("is_breached")) && r.GetBoolean(o("is_breached")),
            BreachType = r.IsDBNull(o("breach_type")) ? null : r.GetString(o("breach_type")),
            EscalationLevel = r.GetInt32(o("escalation_level")),
            CreatedAt = r.IsDBNull(o("created_at")) ? DateTime.Now : r.GetDateTime(o("created_at")),
            UpdatedAt = r.IsDBNull(o("updated_at")) ? DateTime.Now : r.GetDateTime(o("updated_at"))
        };
    }

    public async Task<SlaStatusDto?> GetTicketSlaStatusAsync(int ticketId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureTicketSlaStatusTableAsync(conn);

        using var cmd = new MySqlCommand(
            "SELECT * FROM ticket_sla_status WHERE ticket_id = @id LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@id", ticketId);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return ReadSlaStatus(r);
    }

    public async Task SaveTicketSlaStatusAsync(SlaStatusDto status)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureTicketSlaStatusTableAsync(conn);

        const string sql = @"INSERT INTO ticket_sla_status
            (ticket_id, sla_rule_id, first_response_due, first_response_at, resolution_due,
             resolved_at, is_breached, breach_type, escalation_level, created_at, updated_at)
            VALUES (@tid, @rid, @frd, @fra, @rd, @ra, @ib, @bt, @el, NOW(), NOW())
            ON DUPLICATE KEY UPDATE
                sla_rule_id = @rid,
                first_response_due = @frd,
                first_response_at = @fra,
                resolution_due = @rd,
                resolved_at = @ra,
                is_breached = @ib,
                breach_type = @bt,
                escalation_level = @el,
                updated_at = NOW();";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tid", status.TicketId);
        cmd.Parameters.AddWithValue("@rid", (object?)status.SlaRuleId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@frd", (object?)status.FirstResponseDue ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@fra", (object?)status.FirstResponseAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@rd", (object?)status.ResolutionDue ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ra", (object?)status.ResolvedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ib", status.IsBreached);
        cmd.Parameters.AddWithValue("@bt", (object?)status.BreachType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@el", status.EscalationLevel);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Prüft alle offenen Tickets: legt fehlende SLA-Status an und markiert Verletzungen samt Eskalation.
    /// </summary>
    public async Task<SlaSummaryDto> MonitorAllTicketsAsync()
    {
        var tickets = await GetTicketsAsync();
        var openTickets = tickets.Where(t => t.Status != 4 && t.Status != 5).ToList();

        foreach (var ticket in openTickets)
        {
            var slaStatus = await GetTicketSlaStatusAsync(ticket.Id);
            if (slaStatus == null)
            {
                var (frHours, resHours) = GetSlaHoursForPriority(ticket.Priority);
                await SaveTicketSlaStatusAsync(new SlaStatusDto
                {
                    TicketId = ticket.Id,
                    SlaRuleId = 1,
                    FirstResponseDue = ticket.CreatedAt.AddHours(frHours),
                    ResolutionDue = ticket.CreatedAt.AddHours(resHours),
                    IsBreached = false,
                    EscalationLevel = 0
                });
            }
            else if (slaStatus.ResolutionDue.HasValue && DateTime.Now > slaStatus.ResolutionDue.Value && !slaStatus.IsBreached)
            {
                slaStatus.IsBreached = true;
                slaStatus.BreachType = "Resolution";
                slaStatus.EscalationLevel = Math.Min(3, slaStatus.EscalationLevel + 1);
                await SaveTicketSlaStatusAsync(slaStatus);
            }
        }

        return await GetSlaSummaryAsync();
    }

    public async Task<List<SlaStatusDto>> GetMonitoredTicketsAsync()
    {
        var result = new List<SlaStatusDto>();
        var tickets = await GetTicketsAsync();
        var openTickets = tickets.Where(t => t.Status != 4 && t.Status != 5).ToList();

        foreach (var ticket in openTickets)
        {
            var slaStatus = await GetTicketSlaStatusAsync(ticket.Id);
            if (slaStatus == null) continue;
            slaStatus.TicketSubject = ticket.Subject;
            slaStatus.CustomerName = ticket.CustomerName;
            slaStatus.Priority = ticket.Priority;
            slaStatus.Status = ticket.Status;
            result.Add(slaStatus);
        }

        return result
            .OrderByDescending(s => s.Health == "Breached")
            .ThenByDescending(s => s.Health == "Warning")
            .ThenBy(s => s.ResolutionDue)
            .ToList();
    }

    public async Task<SlaSummaryDto> GetSlaSummaryAsync()
    {
        var summary = new SlaSummaryDto();
        var tickets = await GetTicketsAsync();
        var openTickets = tickets.Where(t => t.Status != 4 && t.Status != 5).ToList();
        summary.TotalTickets = openTickets.Count;

        foreach (var ticket in openTickets)
        {
            var slaStatus = await GetTicketSlaStatusAsync(ticket.Id);
            if (slaStatus != null)
            {
                if (slaStatus.IsBreached || (slaStatus.ResolutionDue.HasValue && DateTime.Now > slaStatus.ResolutionDue.Value))
                    summary.BreachedTickets++;
                else if (slaStatus.ResolutionDue.HasValue && DateTime.Now > slaStatus.ResolutionDue.Value.AddHours(-1))
                    summary.WarningTickets++;
                else
                    summary.HealthyTickets++;
            }
            else
            {
                summary.HealthyTickets++;
            }
        }

        summary.BreachPercentage = summary.TotalTickets > 0
            ? Math.Round((decimal)summary.BreachedTickets / summary.TotalTickets * 100m, 1)
            : 0m;
        return summary;
    }

    // ── Follow-up-Erinnerungen ──────────────────────────────────────

    private async Task EnsureFollowUpRemindersTableAsync(MySqlConnection conn)
    {
        const string ddl = @"CREATE TABLE IF NOT EXISTS follow_up_reminders (
            id INT AUTO_INCREMENT PRIMARY KEY,
            lead_id INT NULL,
            lead_title VARCHAR(200) NULL,
            contact_name VARCHAR(200) NULL,
            due_date DATETIME NOT NULL,
            note LONGTEXT NULL,
            completed TINYINT(1) NOT NULL DEFAULT 0,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            INDEX idx_due (due_date),
            INDEX idx_completed (completed)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
        using var cmd = new MySqlCommand(ddl, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<FollowUpReminderDto>> GetFollowUpsAsync(bool includeCompleted = true)
    {
        var list = new List<FollowUpReminderDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureFollowUpRemindersTableAsync(conn);

        var where = includeCompleted ? "" : "WHERE completed = 0";
        using var cmd = new MySqlCommand(
            $"SELECT * FROM follow_up_reminders {where} ORDER BY completed ASC, due_date ASC", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            int o(string n) => r.GetOrdinal(n);
            list.Add(new FollowUpReminderDto
            {
                Id = r.GetInt32(o("id")),
                LeadId = r.IsDBNull(o("lead_id")) ? null : r.GetInt32(o("lead_id")),
                LeadTitle = r.IsDBNull(o("lead_title")) ? "" : r.GetString(o("lead_title")),
                ContactName = r.IsDBNull(o("contact_name")) ? "" : r.GetString(o("contact_name")),
                DueDate = r.GetDateTime(o("due_date")),
                Note = r.IsDBNull(o("note")) ? "" : r.GetString(o("note")),
                Completed = !r.IsDBNull(o("completed")) && r.GetBoolean(o("completed")),
                CreatedAt = r.IsDBNull(o("created_at")) ? DateTime.Now : r.GetDateTime(o("created_at"))
            });
        }
        return list;
    }

    public async Task<int> AddFollowUpAsync(FollowUpReminderDto reminder)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureFollowUpRemindersTableAsync(conn);

        const string sql = @"INSERT INTO follow_up_reminders
            (lead_id, lead_title, contact_name, due_date, note, completed, created_at)
            VALUES (@lid, @lt, @cn, @dd, @n, @c, NOW());
            SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@lid", (object?)reminder.LeadId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@lt", (object?)reminder.LeadTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cn", (object?)reminder.ContactName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dd", reminder.DueDate);
        cmd.Parameters.AddWithValue("@n", (object?)reminder.Note ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@c", reminder.Completed);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task UpdateFollowUpAsync(FollowUpReminderDto reminder)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureFollowUpRemindersTableAsync(conn);

        const string sql = @"UPDATE follow_up_reminders SET
            lead_id=@lid, lead_title=@lt, contact_name=@cn, due_date=@dd, note=@n, completed=@c
            WHERE id=@id;";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", reminder.Id);
        cmd.Parameters.AddWithValue("@lid", (object?)reminder.LeadId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@lt", (object?)reminder.LeadTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cn", (object?)reminder.ContactName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dd", reminder.DueDate);
        cmd.Parameters.AddWithValue("@n", (object?)reminder.Note ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@c", reminder.Completed);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ToggleFollowUpAsync(int id, bool completed)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureFollowUpRemindersTableAsync(conn);

        using var cmd = new MySqlCommand(
            "UPDATE follow_up_reminders SET completed=@c WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@c", completed);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteFollowUpAsync(int id)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureFollowUpRemindersTableAsync(conn);

        using var cmd = new MySqlCommand("DELETE FROM follow_up_reminders WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Projektvorlagen ─────────────────────────────────────────────

    private static async Task EnsureProjectTemplatesTableAsync(MySqlConnection conn)
    {
        const string ddl = @"CREATE TABLE IF NOT EXISTS project_templates (
            id INT AUTO_INCREMENT PRIMARY KEY,
            name VARCHAR(255) NOT NULL,
            description TEXT,
            default_duration_days INT NOT NULL DEFAULT 30,
            tasks_json LONGTEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );";
        using var cmd = new MySqlCommand(ddl, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<ProjectTemplateDto>> GetProjectTemplatesAsync()
    {
        var list = new List<ProjectTemplateDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureProjectTemplatesTableAsync(conn);

        using var cmd = new MySqlCommand(
            "SELECT id, name, description, default_duration_days, tasks_json FROM project_templates ORDER BY name", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var tasksJson = reader.IsDBNull(4) ? "[]" : reader.GetString(4);
            list.Add(new ProjectTemplateDto
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                DefaultDurationDays = reader.GetInt32(3),
                Tasks = JsonSerializer.Deserialize<List<TemplateTaskDto>>(tasksJson) ?? new()
            });
        }
        return list;
    }

    public async Task<int> AddProjectTemplateAsync(ProjectTemplateDto template)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureProjectTemplatesTableAsync(conn);

        const string sql = @"INSERT INTO project_templates (name, description, default_duration_days, tasks_json, created_at)
            VALUES (@name, @desc, @days, @tasks, NOW()); SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@name", template.Name);
        cmd.Parameters.AddWithValue("@desc", template.Description ?? "");
        cmd.Parameters.AddWithValue("@days", template.DefaultDurationDays);
        cmd.Parameters.AddWithValue("@tasks", JsonSerializer.Serialize(template.Tasks));
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task DeleteProjectTemplateAsync(int id)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureProjectTemplatesTableAsync(conn);

        using var cmd = new MySqlCommand("DELETE FROM project_templates WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Erstellt aus einer Vorlage ein neues Projekt samt zugehöriger Aufgaben.
    /// Gibt die neue Projekt-ID zurück.
    /// </summary>
    public async Task<int> CreateProjectFromTemplateAsync(int templateId, string projectName, string clientName, DateTime startDate)
    {
        var templates = await GetProjectTemplatesAsync();
        var template = templates.FirstOrDefault(t => t.Id == templateId)
            ?? throw new InvalidOperationException($"Vorlage {templateId} nicht gefunden.");

        var endDate = startDate.AddDays(template.DefaultDurationDays);
        var projectId = await CreateProjectAsync(
            projectName, template.Description, "Geplant", clientName, startDate, endDate, 0m);

        foreach (var task in template.Tasks)
        {
            await CreateTaskAsync(new TaskCreateRequest(
                ProjectId: projectId,
                Title: task.Title,
                Description: task.Description,
                AssignedTo: "",
                Status: "Offen",
                Priority: task.Priority,
                DueDate: startDate.AddDays(task.DueAfterDays),
                EstimatedHours: 0));
        }

        return projectId;
    }

    // ── Sales-Termine ───────────────────────────────────────────────

    private static async Task EnsureSalesAppointmentsTableAsync(MySqlConnection conn)
    {
        const string ddl = @"CREATE TABLE IF NOT EXISTS sales_appointments (
            id INT AUTO_INCREMENT PRIMARY KEY,
            title VARCHAR(255) NOT NULL,
            contact_name VARCHAR(255),
            contact_email VARCHAR(255),
            contact_company VARCHAR(255),
            contact_phone VARCHAR(100),
            appointment_date DATETIME NOT NULL,
            appointment_end DATETIME NOT NULL,
            location VARCHAR(255),
            notes TEXT,
            created_by VARCHAR(100),
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            ical_uid VARCHAR(255),
            webex_join_link VARCHAR(500),
            rsvp_status INT NOT NULL DEFAULT 0
        );";
        using var cmd = new MySqlCommand(ddl, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<SalesAppointmentDto>> GetSalesAppointmentsAsync()
    {
        var list = new List<SalesAppointmentDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureSalesAppointmentsTableAsync(conn);

        const string sql = @"SELECT id, title, contact_name, contact_email, contact_company, contact_phone,
            appointment_date, appointment_end, location, notes, created_by, created_at, ical_uid, webex_join_link, rsvp_status
            FROM sales_appointments ORDER BY appointment_date DESC";
        using var cmd = new MySqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new SalesAppointmentDto
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                ContactName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                ContactEmail = reader.IsDBNull(3) ? "" : reader.GetString(3),
                ContactCompany = reader.IsDBNull(4) ? "" : reader.GetString(4),
                ContactPhone = reader.IsDBNull(5) ? "" : reader.GetString(5),
                AppointmentDate = reader.GetDateTime(6),
                AppointmentEnd = reader.GetDateTime(7),
                Location = reader.IsDBNull(8) ? "" : reader.GetString(8),
                Notes = reader.IsDBNull(9) ? "" : reader.GetString(9),
                CreatedBy = reader.IsDBNull(10) ? "" : reader.GetString(10),
                CreatedAt = reader.GetDateTime(11),
                ICalUid = reader.IsDBNull(12) ? "" : reader.GetString(12),
                WebexJoinLink = reader.IsDBNull(13) ? "" : reader.GetString(13),
                RsvpStatus = reader.GetInt32(14)
            });
        }
        return list;
    }

    public async Task<int> AddSalesAppointmentAsync(SalesAppointmentDto a)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureSalesAppointmentsTableAsync(conn);

        const string sql = @"INSERT INTO sales_appointments
            (title, contact_name, contact_email, contact_company, contact_phone, appointment_date, appointment_end,
             location, notes, created_by, created_at, ical_uid, webex_join_link, rsvp_status)
            VALUES (@title, @cn, @ce, @cc, @cp, @ad, @ae, @loc, @notes, @cb, NOW(), @uid, @webex, @rsvp);
            SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@title", a.Title);
        cmd.Parameters.AddWithValue("@cn", a.ContactName ?? "");
        cmd.Parameters.AddWithValue("@ce", a.ContactEmail ?? "");
        cmd.Parameters.AddWithValue("@cc", a.ContactCompany ?? "");
        cmd.Parameters.AddWithValue("@cp", a.ContactPhone ?? "");
        cmd.Parameters.AddWithValue("@ad", a.AppointmentDate);
        cmd.Parameters.AddWithValue("@ae", a.AppointmentEnd);
        cmd.Parameters.AddWithValue("@loc", a.Location ?? "");
        cmd.Parameters.AddWithValue("@notes", a.Notes ?? "");
        cmd.Parameters.AddWithValue("@cb", a.CreatedBy ?? "");
        cmd.Parameters.AddWithValue("@uid", string.IsNullOrEmpty(a.ICalUid) ? Guid.NewGuid().ToString() : a.ICalUid);
        cmd.Parameters.AddWithValue("@webex", a.WebexJoinLink ?? "");
        cmd.Parameters.AddWithValue("@rsvp", a.RsvpStatus);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task DeleteSalesAppointmentAsync(int id)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureSalesAppointmentsTableAsync(conn);

        using var cmd = new MySqlCommand("DELETE FROM sales_appointments WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Lieferanten-Bewertung ───────────────────────────────────────

    private static async Task EnsureSupplierRatingsTableAsync(MySqlConnection conn)
    {
        const string ddl = @"CREATE TABLE IF NOT EXISTS supplier_ratings (
            id INT AUTO_INCREMENT PRIMARY KEY,
            supplier_id INT NOT NULL,
            rating_date DATETIME DEFAULT CURRENT_TIMESTAMP,
            quality_rating INT NOT NULL DEFAULT 5,
            delivery_rating INT NOT NULL DEFAULT 5,
            price_rating INT NOT NULL DEFAULT 5,
            service_rating INT NOT NULL DEFAULT 5,
            communication_rating INT NOT NULL DEFAULT 5,
            overall_rating DECIMAL(3,2) NOT NULL DEFAULT 5.0,
            review_text TEXT,
            pros TEXT,
            cons TEXT,
            would_recommend TINYINT(1) NOT NULL DEFAULT 1,
            rated_by VARCHAR(100),
            order_reference VARCHAR(100),
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );";
        using var cmd = new MySqlCommand(ddl, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<SupplierRatingDto>> GetSupplierRatingsAsync(int? supplierId = null)
    {
        var list = new List<SupplierRatingDto>();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureSupplierRatingsTableAsync(conn);

        var where = supplierId.HasValue ? "WHERE r.supplier_id=@sid" : "";
        var sql = $@"SELECT r.id, r.supplier_id, COALESCE(s.name, ''), r.rating_date,
            r.quality_rating, r.delivery_rating, r.price_rating, r.service_rating, r.communication_rating,
            r.overall_rating, r.review_text, r.pros, r.cons, r.would_recommend, r.rated_by, r.order_reference
            FROM supplier_ratings r LEFT JOIN suppliers s ON s.id = r.supplier_id
            {where} ORDER BY r.rating_date DESC";
        using var cmd = new MySqlCommand(sql, conn);
        if (supplierId.HasValue) cmd.Parameters.AddWithValue("@sid", supplierId.Value);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new SupplierRatingDto
            {
                Id = reader.GetInt32(0),
                SupplierId = reader.GetInt32(1),
                SupplierName = reader.GetString(2),
                RatingDate = reader.GetDateTime(3),
                QualityRating = reader.GetInt32(4),
                DeliveryRating = reader.GetInt32(5),
                PriceRating = reader.GetInt32(6),
                ServiceRating = reader.GetInt32(7),
                CommunicationRating = reader.GetInt32(8),
                OverallRating = reader.GetDecimal(9),
                ReviewText = reader.IsDBNull(10) ? "" : reader.GetString(10),
                Pros = reader.IsDBNull(11) ? "" : reader.GetString(11),
                Cons = reader.IsDBNull(12) ? "" : reader.GetString(12),
                WouldRecommend = reader.GetBoolean(13),
                RatedBy = reader.IsDBNull(14) ? "" : reader.GetString(14),
                OrderReference = reader.IsDBNull(15) ? "" : reader.GetString(15)
            });
        }
        return list;
    }

    public async Task<int> AddSupplierRatingAsync(SupplierRatingDto r)
    {
        r.CalculateOverall();
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureSupplierRatingsTableAsync(conn);

        const string sql = @"INSERT INTO supplier_ratings
            (supplier_id, rating_date, quality_rating, delivery_rating, price_rating, service_rating,
             communication_rating, overall_rating, review_text, pros, cons, would_recommend, rated_by, order_reference, created_at)
            VALUES (@sid, NOW(), @q, @d, @p, @s, @c, @overall, @review, @pros, @cons, @rec, @by, @ref, NOW());
            SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@sid", r.SupplierId);
        cmd.Parameters.AddWithValue("@q", r.QualityRating);
        cmd.Parameters.AddWithValue("@d", r.DeliveryRating);
        cmd.Parameters.AddWithValue("@p", r.PriceRating);
        cmd.Parameters.AddWithValue("@s", r.ServiceRating);
        cmd.Parameters.AddWithValue("@c", r.CommunicationRating);
        cmd.Parameters.AddWithValue("@overall", r.OverallRating);
        cmd.Parameters.AddWithValue("@review", r.ReviewText ?? "");
        cmd.Parameters.AddWithValue("@pros", r.Pros ?? "");
        cmd.Parameters.AddWithValue("@cons", r.Cons ?? "");
        cmd.Parameters.AddWithValue("@rec", r.WouldRecommend);
        cmd.Parameters.AddWithValue("@by", r.RatedBy ?? "");
        cmd.Parameters.AddWithValue("@ref", r.OrderReference ?? "");
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task DeleteSupplierRatingAsync(int id)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureSupplierRatingsTableAsync(conn);

        using var cmd = new MySqlCommand("DELETE FROM supplier_ratings WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}
