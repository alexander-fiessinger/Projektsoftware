using MySql.Data.MySqlClient;
using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Datenbankoperationen für Meetings (Kalender) und Webex-Integration
    /// </summary>
    public partial class DatabaseService
    {
        #region Meeting Methods

        public async Task<List<Meeting>> GetAllMeetingsAsync()
        {
            var meetings = new List<Meeting>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT m.*, p.name AS project_name
                FROM meetings m
                LEFT JOIN projects p ON m.project_id = p.id
                ORDER BY m.start_time ASC";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                meetings.Add(ReadMeeting(reader));
            }

            return meetings;
        }

        public async Task<List<Meeting>> GetMeetingsByMonthAsync(int year, int month)
        {
            var meetings = new List<Meeting>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT m.*, p.name AS project_name
                FROM meetings m
                LEFT JOIN projects p ON m.project_id = p.id
                WHERE YEAR(m.start_time) = @year AND MONTH(m.start_time) = @month
                ORDER BY m.start_time ASC";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@year", year);
            cmd.Parameters.AddWithValue("@month", month);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                meetings.Add(ReadMeeting(reader));
            }

            return meetings;
        }

        public async Task<List<Meeting>> GetMeetingsByDayAsync(DateTime date)
        {
            var meetings = new List<Meeting>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT m.*, p.name AS project_name
                FROM meetings m
                LEFT JOIN projects p ON m.project_id = p.id
                WHERE DATE(m.start_time) = @date
                ORDER BY m.start_time ASC";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@date", date.Date.ToString("yyyy-MM-dd"));
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                meetings.Add(ReadMeeting(reader));
            }

            return meetings;
        }

        public async Task<int> AddMeetingAsync(Meeting meeting)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO meetings 
                (title, description, start_time, end_time, location, participants, project_id,
                 is_webex_meeting, webex_meeting_id, webex_join_link, webex_host_key, webex_password, webex_sip_address, created_at)
                VALUES
                (@title, @description, @startTime, @endTime, @location, @participants, @projectId,
                 @isWebex, @webexId, @webexLink, @webexHostKey, @webexPassword, @webexSip, @createdAt);
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@title", meeting.Title);
            cmd.Parameters.AddWithValue("@description", meeting.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@startTime", meeting.StartTime);
            cmd.Parameters.AddWithValue("@endTime", meeting.EndTime);
            cmd.Parameters.AddWithValue("@location", meeting.Location ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@participants", meeting.Participants ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@projectId", meeting.ProjectId.HasValue ? (object)meeting.ProjectId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@isWebex", meeting.IsWebexMeeting);
            cmd.Parameters.AddWithValue("@webexId", meeting.WebexMeetingId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@webexLink", meeting.WebexJoinLink ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@webexHostKey", meeting.WebexHostKey ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@webexPassword", meeting.WebexPassword ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@webexSip", meeting.WebexSipAddress ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@createdAt", meeting.CreatedAt);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task UpdateMeetingAsync(Meeting meeting)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                UPDATE meetings SET
                    title = @title,
                    description = @description,
                    start_time = @startTime,
                    end_time = @endTime,
                    location = @location,
                    participants = @participants,
                    project_id = @projectId,
                    is_webex_meeting = @isWebex,
                    webex_meeting_id = @webexId,
                    webex_join_link = @webexLink,
                    webex_host_key = @webexHostKey,
                    webex_password = @webexPassword,
                    webex_sip_address = @webexSip
                WHERE id = @id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", meeting.Id);
            cmd.Parameters.AddWithValue("@title", meeting.Title);
            cmd.Parameters.AddWithValue("@description", meeting.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@startTime", meeting.StartTime);
            cmd.Parameters.AddWithValue("@endTime", meeting.EndTime);
            cmd.Parameters.AddWithValue("@location", meeting.Location ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@participants", meeting.Participants ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@projectId", meeting.ProjectId.HasValue ? (object)meeting.ProjectId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@isWebex", meeting.IsWebexMeeting);
            cmd.Parameters.AddWithValue("@webexId", meeting.WebexMeetingId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@webexLink", meeting.WebexJoinLink ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@webexHostKey", meeting.WebexHostKey ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@webexPassword", meeting.WebexPassword ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@webexSip", meeting.WebexSipAddress ?? (object)DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteMeetingAsync(int meetingId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "DELETE FROM meetings WHERE id = @id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", meetingId);
            await cmd.ExecuteNonQueryAsync();
        }

        private static Meeting ReadMeeting(DbDataReader reader)
        {
            return new Meeting
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                StartTime = reader.GetDateTime(reader.GetOrdinal("start_time")),
                EndTime = reader.GetDateTime(reader.GetOrdinal("end_time")),
                Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString(reader.GetOrdinal("location")),
                Participants = reader.IsDBNull(reader.GetOrdinal("participants")) ? null : reader.GetString(reader.GetOrdinal("participants")),
                ProjectId = reader.IsDBNull(reader.GetOrdinal("project_id")) ? null : reader.GetInt32(reader.GetOrdinal("project_id")),
                ProjectName = reader.IsDBNull(reader.GetOrdinal("project_name")) ? null : reader.GetString(reader.GetOrdinal("project_name")),
                IsWebexMeeting = reader.GetBoolean(reader.GetOrdinal("is_webex_meeting")),
                WebexMeetingId = reader.IsDBNull(reader.GetOrdinal("webex_meeting_id")) ? null : reader.GetString(reader.GetOrdinal("webex_meeting_id")),
                WebexJoinLink = reader.IsDBNull(reader.GetOrdinal("webex_join_link")) ? null : reader.GetString(reader.GetOrdinal("webex_join_link")),
                WebexHostKey = reader.IsDBNull(reader.GetOrdinal("webex_host_key")) ? null : reader.GetString(reader.GetOrdinal("webex_host_key")),
                WebexPassword = reader.IsDBNull(reader.GetOrdinal("webex_password")) ? null : reader.GetString(reader.GetOrdinal("webex_password")),
                WebexSipAddress = reader.IsDBNull(reader.GetOrdinal("webex_sip_address")) ? null : reader.GetString(reader.GetOrdinal("webex_sip_address")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            };
        }

        #endregion
    }
}
