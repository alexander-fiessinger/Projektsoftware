using MySql.Data.MySqlClient;
using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    public partial class DatabaseService
    {
        private async Task EnsureTimeEntryTemplatesTableAsync(MySqlConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS time_entry_templates (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    project_id INT,
                    activity VARCHAR(255),
                    description TEXT,
                    default_duration TIME NOT NULL DEFAULT '01:00:00',
                    created_at DATETIME,
                    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE SET NULL
                )";
            using var cmd = new MySqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<TimeEntryTemplate>> GetAllTimeEntryTemplatesAsync()
        {
            var list = new List<TimeEntryTemplate>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            await EnsureTimeEntryTemplatesTableAsync(connection);

            const string query = @"
                SELECT t.*, p.name AS project_name
                FROM time_entry_templates t
                LEFT JOIN projects p ON t.project_id = p.id
                ORDER BY t.name";
            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new TimeEntryTemplate
                {
                    Id              = reader.GetInt32(reader.GetOrdinal("id")),
                    Name            = reader.GetString(reader.GetOrdinal("name")),
                    ProjectId       = reader.IsDBNull(reader.GetOrdinal("project_id")) ? null : reader.GetInt32(reader.GetOrdinal("project_id")),
                    ProjectName     = reader.IsDBNull(reader.GetOrdinal("project_name")) ? null : reader.GetString(reader.GetOrdinal("project_name")),
                    Activity        = reader.IsDBNull(reader.GetOrdinal("activity")) ? null : reader.GetString(reader.GetOrdinal("activity")),
                    Description     = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    DefaultDuration = ParseMySqlTime(reader["default_duration"]),
                    CreatedAt       = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }
            return list;
        }

        public async Task<int> AddTimeEntryTemplateAsync(TimeEntryTemplate template)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            await EnsureTimeEntryTemplatesTableAsync(connection);

            const string query = @"
                INSERT INTO time_entry_templates (name, project_id, activity, description, default_duration, created_at)
                VALUES (@name, @projectId, @activity, @description, @duration, @createdAt);
                SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@name",       template.Name);
            cmd.Parameters.AddWithValue("@projectId",  template.ProjectId.HasValue ? (object)template.ProjectId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@activity",   (object?)template.Activity ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@description",(object?)template.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@duration",   template.DefaultDuration.ToString(@"hh\:mm\:ss"));
            cmd.Parameters.AddWithValue("@createdAt",  template.CreatedAt);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task UpdateTimeEntryTemplateAsync(TimeEntryTemplate template)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            const string query = @"
                UPDATE time_entry_templates
                SET name=@name, project_id=@projectId, activity=@activity,
                    description=@description, default_duration=@duration
                WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id",         template.Id);
            cmd.Parameters.AddWithValue("@name",       template.Name);
            cmd.Parameters.AddWithValue("@projectId",  template.ProjectId.HasValue ? (object)template.ProjectId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@activity",   (object?)template.Activity ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@description",(object?)template.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@duration",   template.DefaultDuration.ToString(@"hh\:mm\:ss"));
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteTimeEntryTemplateAsync(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var cmd = new MySqlCommand("DELETE FROM time_entry_templates WHERE id=@id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
