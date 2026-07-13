using MySql.Data.MySqlClient;
using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    public partial class DatabaseService
    {
        private async Task EnsureSalesAppointmentsTableAsync(MySqlConnection connection)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS sales_appointments (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    title VARCHAR(255) NOT NULL,
                    contact_name VARCHAR(255) NOT NULL DEFAULT '',
                    contact_email VARCHAR(255) NOT NULL DEFAULT '',
                    contact_company VARCHAR(255),
                    contact_phone VARCHAR(50),
                    appointment_date DATETIME NOT NULL,
                    appointment_end DATETIME NOT NULL,
                    location VARCHAR(255),
                    notes TEXT,
                    created_by VARCHAR(100),
                    created_at DATETIME NOT NULL,
                    rsvp_status INT NOT NULL DEFAULT 0,
                    rsvp_answered_at DATETIME,
                    ical_uid VARCHAR(255),
                    webex_meeting_id VARCHAR(255),
                    webex_join_link VARCHAR(1024),
                    rsvp_details TEXT,
                    INDEX idx_appointment_date (appointment_date),
                    INDEX idx_rsvp_status (rsvp_status)
                )";
            using var cmd = new MySqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();

            foreach (var (col, def) in new[]
            {
                ("webex_meeting_id", "VARCHAR(255)"),
                ("webex_join_link",  "VARCHAR(1024)"),
                ("rsvp_details",     "TEXT")
            })
            {
                const string checkCol = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='sales_appointments' AND COLUMN_NAME=@col";
                using var chk = new MySqlCommand(checkCol, connection);
                chk.Parameters.AddWithValue("@col", col);
                var exists = Convert.ToInt32(await chk.ExecuteScalarAsync()) > 0;
                if (!exists)
                {
                    using var alter = new MySqlCommand($"ALTER TABLE sales_appointments ADD COLUMN {col} {def}", connection);
                    await alter.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<SalesAppointment>> GetAllSalesAppointmentsAsync()
        {
            var list = new List<SalesAppointment>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            const string sql = "SELECT * FROM sales_appointments ORDER BY appointment_date DESC";
            using var cmd = new MySqlCommand(sql, connection);
            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(MapSalesAppointment(reader));
            return list;
        }

        public async Task<int> AddSalesAppointmentAsync(SalesAppointment a)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            const string sql = @"INSERT INTO sales_appointments
                (title, contact_name, contact_email, contact_company, contact_phone,
                 appointment_date, appointment_end, location, notes, created_by, created_at, rsvp_status, ical_uid,
                 webex_meeting_id, webex_join_link, rsvp_details)
                VALUES (@title, @contact_name, @contact_email, @contact_company, @contact_phone,
                        @appointment_date, @appointment_end, @location, @notes, @created_by, @created_at, @rsvp_status, @ical_uid,
                        @webex_meeting_id, @webex_join_link, @rsvp_details);
                SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(sql, connection);
            BindAppointmentParams(cmd, a);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task UpdateSalesAppointmentAsync(SalesAppointment a)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            const string sql = @"UPDATE sales_appointments SET
                title=@title, contact_name=@contact_name, contact_email=@contact_email,
                contact_company=@contact_company, contact_phone=@contact_phone,
                appointment_date=@appointment_date, appointment_end=@appointment_end,
                location=@location, notes=@notes,
                ical_uid=@ical_uid, webex_meeting_id=@webex_meeting_id, webex_join_link=@webex_join_link,
                rsvp_details=@rsvp_details
                WHERE id=@id";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", a.Id);
            BindAppointmentParams(cmd, a);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateSalesAppointmentRsvpAsync(int id, RsvpStatus status,
            Dictionary<string, RsvpStatus>? details = null)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            const string sql = @"UPDATE sales_appointments
                SET rsvp_status=@status, rsvp_answered_at=@answered, rsvp_details=@details
                WHERE id=@id";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@status", (int)status);
            cmd.Parameters.AddWithValue("@answered", DateTime.Now);
            cmd.Parameters.AddWithValue("@details",
                details != null ? JsonSerializer.Serialize(details) : (object)DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteSalesAppointmentAsync(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            const string sql = "DELETE FROM sales_appointments WHERE id=@id";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        private static void BindAppointmentParams(MySqlCommand cmd, SalesAppointment a)
        {
            cmd.Parameters.AddWithValue("@title", a.Title);
            cmd.Parameters.AddWithValue("@contact_name", a.ContactName);
            cmd.Parameters.AddWithValue("@contact_email", a.ContactEmail);
            cmd.Parameters.AddWithValue("@contact_company", (object?)a.ContactCompany ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@contact_phone", (object?)a.ContactPhone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@appointment_date", a.AppointmentDate);
            cmd.Parameters.AddWithValue("@appointment_end", a.AppointmentEnd);
            cmd.Parameters.AddWithValue("@location", (object?)a.Location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", (object?)a.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@created_by", (object?)a.CreatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@created_at", DateTime.Now);
            cmd.Parameters.AddWithValue("@rsvp_status", (int)a.RsvpStatus);
            cmd.Parameters.AddWithValue("@ical_uid", (object?)a.ICalUid ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@webex_meeting_id", string.IsNullOrEmpty(a.WebexMeetingId) ? (object)DBNull.Value : a.WebexMeetingId);
            cmd.Parameters.AddWithValue("@webex_join_link",  string.IsNullOrEmpty(a.WebexJoinLink)  ? (object)DBNull.Value : a.WebexJoinLink);
            var detailsJson = a.RsvpDetails.Count > 0 ? JsonSerializer.Serialize(a.RsvpDetails) : null;
            cmd.Parameters.AddWithValue("@rsvp_details", (object?)detailsJson ?? DBNull.Value);
        }

        private static SalesAppointment MapSalesAppointment(MySqlDataReader r)
        {
            var appt = new SalesAppointment
            {
                Id              = r.GetInt32("id"),
                Title           = r.IsDBNull(r.GetOrdinal("title")) ? "" : r.GetString("title"),
                ContactName     = r.IsDBNull(r.GetOrdinal("contact_name")) ? "" : r.GetString("contact_name"),
                ContactEmail    = r.IsDBNull(r.GetOrdinal("contact_email")) ? "" : r.GetString("contact_email"),
                ContactCompany  = r.IsDBNull(r.GetOrdinal("contact_company")) ? "" : r.GetString("contact_company"),
                ContactPhone    = r.IsDBNull(r.GetOrdinal("contact_phone")) ? "" : r.GetString("contact_phone"),
                AppointmentDate = DateTime.SpecifyKind(r.GetDateTime("appointment_date"), DateTimeKind.Local),
                AppointmentEnd  = DateTime.SpecifyKind(r.GetDateTime("appointment_end"),  DateTimeKind.Local),
                Location        = r.IsDBNull(r.GetOrdinal("location")) ? "" : r.GetString("location"),
                Notes           = r.IsDBNull(r.GetOrdinal("notes")) ? "" : r.GetString("notes"),
                CreatedBy       = r.IsDBNull(r.GetOrdinal("created_by")) ? "" : r.GetString("created_by"),
                CreatedAt       = r.GetDateTime("created_at"),
                RsvpStatus      = (RsvpStatus)r.GetInt32("rsvp_status"),
                RsvpAnsweredAt  = r.IsDBNull(r.GetOrdinal("rsvp_answered_at")) ? null : r.GetDateTime("rsvp_answered_at"),
                ICalUid         = r.IsDBNull(r.GetOrdinal("ical_uid")) ? "" : r.GetString("ical_uid"),
                WebexMeetingId  = r.IsDBNull(r.GetOrdinal("webex_meeting_id")) ? "" : r.GetString("webex_meeting_id"),
                WebexJoinLink   = r.IsDBNull(r.GetOrdinal("webex_join_link"))  ? "" : r.GetString("webex_join_link"),
            };

            var ordDetails = r.GetOrdinal("rsvp_details");
            if (!r.IsDBNull(ordDetails))
            {
                try
                {
                    var json = r.GetString(ordDetails);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, RsvpStatus>>(json);
                    if (dict != null) appt.RsvpDetails = dict;
                }
                catch { }
            }

            return appt;
        }

        // ─── Sales Leads ───────────────────────────────────────────────────────────

        private async Task EnsureSalesLeadsTableAsync(MySqlConnection connection)
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
            using var cmd = new MySqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<SalesLead>> GetSalesLeadsAsync()
        {
            var list = new List<SalesLead>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            await EnsureSalesLeadsTableAsync(connection);
            using var cmd = new MySqlCommand(
                "SELECT * FROM sales_leads ORDER BY lead_date DESC, created_at DESC", connection);
            using var r = await cmd.ExecuteReaderAsync() as MySqlDataReader;
            while (r != null && await r.ReadAsync())
                list.Add(MapSalesLead(r));
            return list;
        }

        public async Task<int> AddSalesLeadAsync(SalesLead lead)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            await EnsureSalesLeadsTableAsync(connection);
            const string sql = @"INSERT INTO sales_leads
                (title, contact_name, contact_company, contact_email, contact_phone,
                 source, status, lead_date, notes, original_file_name, file_data, created_by, created_at)
                VALUES (@title,@cname,@ccomp,@cemail,@cphone,@src,@status,@ldate,@notes,@fname,@fdata,@cby,@now);
                SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(sql, connection);
            AddSalesLeadParams(cmd, lead);
            cmd.Parameters.AddWithValue("@now", DateTime.Now);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task UpdateSalesLeadAsync(SalesLead lead)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            const string sql = @"UPDATE sales_leads SET
                title=@title, contact_name=@cname, contact_company=@ccomp,
                contact_email=@cemail, contact_phone=@cphone, source=@src,
                status=@status, lead_date=@ldate, notes=@notes,
                original_file_name=@fname,
                file_data=COALESCE(@fdata, file_data)
                WHERE id=@id";
            using var cmd = new MySqlCommand(sql, connection);
            AddSalesLeadParams(cmd, lead);
            cmd.Parameters.AddWithValue("@id", lead.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteSalesLeadAsync(int id)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var cmd = new MySqlCommand("DELETE FROM sales_leads WHERE id=@id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        private static void AddSalesLeadParams(MySqlCommand cmd, SalesLead lead)
        {
            cmd.Parameters.AddWithValue("@title",  lead.Title ?? "");
            cmd.Parameters.AddWithValue("@cname",  lead.ContactName ?? "");
            cmd.Parameters.AddWithValue("@ccomp",  (object?)lead.ContactCompany ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cemail", (object?)lead.ContactEmail   ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cphone", (object?)lead.ContactPhone   ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@src",    (object?)lead.Source         ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", (int)lead.Status);
            cmd.Parameters.AddWithValue("@ldate",  lead.LeadDate.Date);
            cmd.Parameters.AddWithValue("@notes",  lead.Notes ?? "");
            cmd.Parameters.AddWithValue("@fname",  (object?)lead.OriginalFileName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fdata",  (object?)lead.FileData ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cby",    lead.CreatedBy ?? "");
        }

        private static SalesLead MapSalesLead(MySqlDataReader r)
        {
            var lead = new SalesLead
            {
                Id             = r.GetInt32("id"),
                Title          = r.IsDBNull(r.GetOrdinal("title"))          ? "" : r.GetString("title"),
                ContactName    = r.IsDBNull(r.GetOrdinal("contact_name"))   ? "" : r.GetString("contact_name"),
                ContactCompany = r.IsDBNull(r.GetOrdinal("contact_company"))? "" : r.GetString("contact_company"),
                ContactEmail   = r.IsDBNull(r.GetOrdinal("contact_email"))  ? "" : r.GetString("contact_email"),
                ContactPhone   = r.IsDBNull(r.GetOrdinal("contact_phone"))  ? "" : r.GetString("contact_phone"),
                Source         = r.IsDBNull(r.GetOrdinal("source"))         ? "" : r.GetString("source"),
                Status         = (LeadStatus)r.GetInt32("status"),
                LeadDate       = r.GetDateTime("lead_date"),
                Notes          = r.IsDBNull(r.GetOrdinal("notes"))          ? "" : r.GetString("notes"),
                OriginalFileName = r.IsDBNull(r.GetOrdinal("original_file_name")) ? "" : r.GetString("original_file_name"),
                CreatedBy      = r.IsDBNull(r.GetOrdinal("created_by"))     ? "" : r.GetString("created_by"),
                CreatedAt      = r.GetDateTime("created_at"),
            };
            try
            {
                int fdOrd = r.GetOrdinal("file_data");
                lead.FileData = r.IsDBNull(fdOrd) ? null : (byte[])r.GetValue(fdOrd);
            }
            catch { }
            return lead;
        }
    }
}
