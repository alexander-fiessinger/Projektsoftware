using MySql.Data.MySqlClient;
using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    public class AuditLogService
    {
        private readonly string connectionString;

        public AuditLogService(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Schreibt einen Audit-Log-Eintrag (fire-and-forget sicher).
        /// </summary>
        public async Task LogAsync(string entityType, string entityId, string action, string details = "")
        {
            try
            {
                var userName = AuthenticationService.CurrentUser?.Username ?? "System";

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                const string query = @"
                    INSERT INTO audit_log (timestamp, user_name, entity_type, entity_id, action, details)
                    VALUES (@timestamp, @userName, @entityType, @entityId, @action, @details)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@timestamp", DateTime.Now);
                cmd.Parameters.AddWithValue("@userName", userName);
                cmd.Parameters.AddWithValue("@entityType", entityType);
                cmd.Parameters.AddWithValue("@entityId", entityId);
                cmd.Parameters.AddWithValue("@action", action);
                cmd.Parameters.AddWithValue("@details", details ?? string.Empty);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuditLog] Fehler beim Schreiben: {ex.Message}");
            }
        }

        /// <summary>
        /// Liest alle Audit-Log-Einträge (neueste zuerst).
        /// </summary>
        public async Task<List<AuditLogEntry>> GetAllAsync(int limit = 500)
        {
            var entries = new List<AuditLogEntry>();

            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                string query = $@"
                    SELECT id, timestamp, user_name, entity_type, entity_id, action, details
                    FROM audit_log
                    ORDER BY timestamp DESC
                    LIMIT {limit}";

                using var cmd = new MySqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    entries.Add(new AuditLogEntry
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Timestamp = reader.GetDateTime(reader.GetOrdinal("timestamp")),
                        UserName = reader.GetString(reader.GetOrdinal("user_name")),
                        EntityType = reader.GetString(reader.GetOrdinal("entity_type")),
                        EntityId = reader.IsDBNull(reader.GetOrdinal("entity_id")) ? string.Empty : reader.GetString(reader.GetOrdinal("entity_id")),
                        Action = reader.GetString(reader.GetOrdinal("action")),
                        Details = reader.IsDBNull(reader.GetOrdinal("details")) ? string.Empty : reader.GetString(reader.GetOrdinal("details"))
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuditLog] Fehler beim Lesen: {ex.Message}");
            }

            return entries;
        }
    }
}
