using MySql.Data.MySqlClient;
using TicketAPI.Models;
using System.Data;

namespace TicketAPI.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Database connection string not configured");
        }

        public async Task<int> AddTicketAsync(Ticket ticket)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO tickets (customer_name, customer_email, customer_phone, customer_id, 
                           subject, description, priority, status, category, 
                           ip_address, user_agent, assigned_to_employee_id, resolution, resolved_at, 
                           created_at, updated_at)
                           VALUES (@customerName, @customerEmail, @customerPhone, @customerId, 
                           @subject, @description, @priority, @status, @category,
                           @ipAddress, @userAgent, @assignedToEmployeeId, @resolution, @resolvedAt,
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
            cmd.Parameters.AddWithValue("@resolution", ticket.Resolution ?? "");
            cmd.Parameters.AddWithValue("@resolvedAt", ticket.ResolvedAt.HasValue ? (object)ticket.ResolvedAt.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@createdAt", ticket.CreatedAt);
            cmd.Parameters.AddWithValue("@updatedAt", ticket.UpdatedAt.HasValue ? (object)ticket.UpdatedAt.Value : DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<Ticket?> GetTicketByIdAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT * FROM tickets WHERE id = @id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Ticket
                {
                    Id = reader.GetInt32("id"),
                    CustomerName = reader.GetString("customer_name"),
                    CustomerEmail = reader.GetString("customer_email"),
                    CustomerPhone = reader.IsDBNull("customer_phone") ? "" : reader.GetString("customer_phone"),
                    CustomerId = reader.IsDBNull("customer_id") ? null : reader.GetInt32("customer_id"),
                    Subject = reader.GetString("subject"),
                    Description = reader.GetString("description"),
                    Priority = (TicketPriority)reader.GetInt32("priority"),
                    Status = (TicketStatus)reader.GetInt32("status"),
                    Category = (TicketCategory)reader.GetInt32("category"),
                    IpAddress = reader.IsDBNull("ip_address") ? "" : reader.GetString("ip_address"),
                    UserAgent = reader.IsDBNull("user_agent") ? "" : reader.GetString("user_agent"),
                    AssignedToEmployeeId = reader.IsDBNull("assigned_to_employee_id") ? null : reader.GetInt32("assigned_to_employee_id"),
                    Resolution = reader.IsDBNull("resolution") ? "" : reader.GetString("resolution"),
                    ResolvedAt = reader.IsDBNull("resolved_at") ? null : reader.GetDateTime("resolved_at"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.IsDBNull("updated_at") ? null : reader.GetDateTime("updated_at")
                };
            }

            return null;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                return connection.State == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }
    }
}
