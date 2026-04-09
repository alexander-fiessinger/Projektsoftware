using MySql.Data.MySqlClient;
using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    public partial class DatabaseService
    {
        #region CRM Contacts

        public async Task<List<CrmContact>> GetAllCrmContactsAsync(bool includeInactive = false)
        {
            var contacts = new List<CrmContact>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT c.*, COALESCE(cu.company_name, CONCAT(cu.first_name, ' ', cu.last_name)) AS customer_display_name
                FROM crm_contacts c
                LEFT JOIN customers cu ON c.customer_id = cu.id
                WHERE (@includeInactive = 1 OR c.is_active = 1)
                ORDER BY c.last_name, c.first_name";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@includeInactive", includeInactive ? 1 : 0);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                contacts.Add(ReadContact(reader));

            return contacts;
        }

        public async Task<int> AddCrmContactAsync(CrmContact contact)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO crm_contacts (customer_id, first_name, last_name, position, email, phone, mobile, notes, is_active, created_at)
                VALUES (@customerId, @firstName, @lastName, @position, @email, @phone, @mobile, @notes, @isActive, @createdAt);
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            SetContactParameters(cmd, contact);
            cmd.Parameters.AddWithValue("@createdAt", contact.CreatedAt);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task UpdateCrmContactAsync(CrmContact contact)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                UPDATE crm_contacts SET customer_id=@customerId, first_name=@firstName, last_name=@lastName,
                    position=@position, email=@email, phone=@phone, mobile=@mobile, notes=@notes,
                    is_active=@isActive, updated_at=@updatedAt
                WHERE id=@id";

            using var cmd = new MySqlCommand(query, connection);
            SetContactParameters(cmd, contact);
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@id", contact.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteCrmContactAsync(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "UPDATE crm_contacts SET is_active=0, updated_at=@updatedAt WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        private static CrmContact ReadContact(System.Data.Common.DbDataReader reader)
        {
            return new CrmContact
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                CustomerId = reader.IsDBNull(reader.GetOrdinal("customer_id")) ? null : reader.GetInt32(reader.GetOrdinal("customer_id")),
                CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_display_name")) ? "" : reader.GetString(reader.GetOrdinal("customer_display_name")),
                FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? "" : reader.GetString(reader.GetOrdinal("first_name")),
                LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? "" : reader.GetString(reader.GetOrdinal("last_name")),
                Position = reader.IsDBNull(reader.GetOrdinal("position")) ? "" : reader.GetString(reader.GetOrdinal("position")),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString(reader.GetOrdinal("email")),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? "" : reader.GetString(reader.GetOrdinal("phone")),
                Mobile = reader.IsDBNull(reader.GetOrdinal("mobile")) ? "" : reader.GetString(reader.GetOrdinal("mobile")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? "" : reader.GetString(reader.GetOrdinal("notes")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        private static void SetContactParameters(MySqlCommand cmd, CrmContact contact)
        {
            cmd.Parameters.AddWithValue("@customerId", (object)contact.CustomerId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@firstName", contact.FirstName ?? "");
            cmd.Parameters.AddWithValue("@lastName", contact.LastName ?? "");
            cmd.Parameters.AddWithValue("@position", contact.Position ?? "");
            cmd.Parameters.AddWithValue("@email", contact.Email ?? "");
            cmd.Parameters.AddWithValue("@phone", contact.Phone ?? "");
            cmd.Parameters.AddWithValue("@mobile", contact.Mobile ?? "");
            cmd.Parameters.AddWithValue("@notes", contact.Notes ?? "");
            cmd.Parameters.AddWithValue("@isActive", contact.IsActive);
        }

        #endregion

        #region CRM Activities

        public async Task<List<CrmActivity>> GetAllCrmActivitiesAsync()
        {
            var activities = new List<CrmActivity>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT a.*,
                    CONCAT(COALESCE(c.first_name,''), ' ', COALESCE(c.last_name,'')) AS contact_display_name,
                    COALESCE(cu.company_name, CONCAT(cu.first_name, ' ', cu.last_name)) AS customer_display_name
                FROM crm_activities a
                LEFT JOIN crm_contacts c ON a.contact_id = c.id
                LEFT JOIN customers cu ON a.customer_id = cu.id
                ORDER BY a.created_at DESC";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                activities.Add(ReadActivity(reader));

            return activities;
        }

        public async Task<int> AddCrmActivityAsync(CrmActivity activity)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO crm_activities (contact_id, customer_id, type, subject, notes, due_date, completed_at, is_completed, created_by, created_at)
                VALUES (@contactId, @customerId, @type, @subject, @notes, @dueDate, @completedAt, @isCompleted, @createdBy, @createdAt);
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            SetActivityParameters(cmd, activity);
            cmd.Parameters.AddWithValue("@createdAt", activity.CreatedAt);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task UpdateCrmActivityAsync(CrmActivity activity)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                UPDATE crm_activities SET contact_id=@contactId, customer_id=@customerId, type=@type,
                    subject=@subject, notes=@notes, due_date=@dueDate, completed_at=@completedAt,
                    is_completed=@isCompleted, created_by=@createdBy
                WHERE id=@id";

            using var cmd = new MySqlCommand(query, connection);
            SetActivityParameters(cmd, activity);
            cmd.Parameters.AddWithValue("@id", activity.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteCrmActivityAsync(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var cmd = new MySqlCommand("DELETE FROM crm_activities WHERE id=@id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        private static CrmActivity ReadActivity(System.Data.Common.DbDataReader reader)
        {
            return new CrmActivity
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                ContactId = reader.IsDBNull(reader.GetOrdinal("contact_id")) ? null : reader.GetInt32(reader.GetOrdinal("contact_id")),
                ContactName = reader.IsDBNull(reader.GetOrdinal("contact_display_name")) ? "" : reader.GetString(reader.GetOrdinal("contact_display_name")).Trim(),
                CustomerId = reader.IsDBNull(reader.GetOrdinal("customer_id")) ? null : reader.GetInt32(reader.GetOrdinal("customer_id")),
                CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_display_name")) ? "" : reader.GetString(reader.GetOrdinal("customer_display_name")),
                Type = (CrmActivityType)reader.GetInt32(reader.GetOrdinal("type")),
                Subject = reader.IsDBNull(reader.GetOrdinal("subject")) ? "" : reader.GetString(reader.GetOrdinal("subject")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? "" : reader.GetString(reader.GetOrdinal("notes")),
                DueDate = reader.IsDBNull(reader.GetOrdinal("due_date")) ? null : reader.GetDateTime(reader.GetOrdinal("due_date")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("completed_at")) ? null : reader.GetDateTime(reader.GetOrdinal("completed_at")),
                IsCompleted = reader.GetBoolean(reader.GetOrdinal("is_completed")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? "" : reader.GetString(reader.GetOrdinal("created_by")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            };
        }

        private static void SetActivityParameters(MySqlCommand cmd, CrmActivity activity)
        {
            cmd.Parameters.AddWithValue("@contactId", (object)activity.ContactId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@customerId", (object)activity.CustomerId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@type", (int)activity.Type);
            cmd.Parameters.AddWithValue("@subject", activity.Subject ?? "");
            cmd.Parameters.AddWithValue("@notes", activity.Notes ?? "");
            cmd.Parameters.AddWithValue("@dueDate", (object)activity.DueDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@completedAt", (object)activity.CompletedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@isCompleted", activity.IsCompleted);
            cmd.Parameters.AddWithValue("@createdBy", activity.CreatedBy ?? "");
        }

        #endregion

        #region CRM Deals

        public async Task<List<CrmDeal>> GetAllCrmDealsAsync()
        {
            var deals = new List<CrmDeal>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT d.*,
                    COALESCE(cu.company_name, CONCAT(cu.first_name, ' ', cu.last_name)) AS customer_display_name,
                    CONCAT(COALESCE(c.first_name,''), ' ', COALESCE(c.last_name,'')) AS contact_display_name
                FROM crm_deals d
                LEFT JOIN customers cu ON d.customer_id = cu.id
                LEFT JOIN crm_contacts c ON d.contact_id = c.id
                ORDER BY d.created_at DESC";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                deals.Add(ReadDeal(reader));

            return deals;
        }

        public async Task<int> AddCrmDealAsync(CrmDeal deal)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO crm_deals (customer_id, contact_id, title, value, stage, probability, expected_close_date, notes, assigned_to, created_at, won_at, lost_at, lost_reason)
                VALUES (@customerId, @contactId, @title, @value, @stage, @probability, @expectedCloseDate, @notes, @assignedTo, @createdAt, @wonAt, @lostAt, @lostReason);
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            SetDealParameters(cmd, deal);
            cmd.Parameters.AddWithValue("@createdAt", deal.CreatedAt);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task UpdateCrmDealAsync(CrmDeal deal)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                UPDATE crm_deals SET customer_id=@customerId, contact_id=@contactId, title=@title, value=@value,
                    stage=@stage, probability=@probability, expected_close_date=@expectedCloseDate, notes=@notes,
                    assigned_to=@assignedTo, updated_at=@updatedAt, won_at=@wonAt, lost_at=@lostAt, lost_reason=@lostReason
                WHERE id=@id";

            using var cmd = new MySqlCommand(query, connection);
            SetDealParameters(cmd, deal);
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@id", deal.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteCrmDealAsync(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var cmd = new MySqlCommand("DELETE FROM crm_deals WHERE id=@id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        private static CrmDeal ReadDeal(System.Data.Common.DbDataReader reader)
        {
            return new CrmDeal
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                CustomerId = reader.IsDBNull(reader.GetOrdinal("customer_id")) ? null : reader.GetInt32(reader.GetOrdinal("customer_id")),
                CustomerName = reader.IsDBNull(reader.GetOrdinal("customer_display_name")) ? "" : reader.GetString(reader.GetOrdinal("customer_display_name")),
                ContactId = reader.IsDBNull(reader.GetOrdinal("contact_id")) ? null : reader.GetInt32(reader.GetOrdinal("contact_id")),
                ContactName = reader.IsDBNull(reader.GetOrdinal("contact_display_name")) ? "" : reader.GetString(reader.GetOrdinal("contact_display_name")).Trim(),
                Title = reader.IsDBNull(reader.GetOrdinal("title")) ? "" : reader.GetString(reader.GetOrdinal("title")),
                Value = reader.GetDecimal(reader.GetOrdinal("value")),
                Stage = (DealStage)reader.GetInt32(reader.GetOrdinal("stage")),
                Probability = reader.GetInt32(reader.GetOrdinal("probability")),
                ExpectedCloseDate = reader.IsDBNull(reader.GetOrdinal("expected_close_date")) ? null : reader.GetDateTime(reader.GetOrdinal("expected_close_date")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? "" : reader.GetString(reader.GetOrdinal("notes")),
                AssignedTo = reader.IsDBNull(reader.GetOrdinal("assigned_to")) ? "" : reader.GetString(reader.GetOrdinal("assigned_to")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                WonAt = reader.IsDBNull(reader.GetOrdinal("won_at")) ? null : reader.GetDateTime(reader.GetOrdinal("won_at")),
                LostAt = reader.IsDBNull(reader.GetOrdinal("lost_at")) ? null : reader.GetDateTime(reader.GetOrdinal("lost_at")),
                LostReason = reader.IsDBNull(reader.GetOrdinal("lost_reason")) ? "" : reader.GetString(reader.GetOrdinal("lost_reason"))
            };
        }

        private static void SetDealParameters(MySqlCommand cmd, CrmDeal deal)
        {
            cmd.Parameters.AddWithValue("@customerId", (object)deal.CustomerId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@contactId", (object)deal.ContactId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@title", deal.Title ?? "");
            cmd.Parameters.AddWithValue("@value", deal.Value);
            cmd.Parameters.AddWithValue("@stage", (int)deal.Stage);
            cmd.Parameters.AddWithValue("@probability", deal.Probability);
            cmd.Parameters.AddWithValue("@expectedCloseDate", (object)deal.ExpectedCloseDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", deal.Notes ?? "");
            cmd.Parameters.AddWithValue("@assignedTo", deal.AssignedTo ?? "");
            cmd.Parameters.AddWithValue("@wonAt", (object)deal.WonAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lostAt", (object)deal.LostAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lostReason", deal.LostReason ?? "");
        }

        #endregion
    }
}
