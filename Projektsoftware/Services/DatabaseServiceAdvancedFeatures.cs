using MySql.Data.MySqlClient;
using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Partial-Erweiterung von DatabaseService für erweiterte Features
    /// Diese Stubs ermöglichen es, dass KpiService, SlaMonitoringService, BudgetTrackingService
    /// compilieren können, auch wenn die vollständige Implementierung noch ausstehend ist.
    /// </summary>
    public partial class DatabaseService
    {
        // ===== SLA-Management =====

        private async Task EnsureSlaRulesTableAsync(MySqlConnection conn)
        {
            const string ddl = @"CREATE TABLE IF NOT EXISTS sla_rules (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                priority VARCHAR(20) NULL,
                ticket_category VARCHAR(50) NULL,
                first_response_minutes INT NOT NULL DEFAULT 60,
                resolution_time_hours INT NOT NULL DEFAULT 24,
                is_active TINYINT(1) NOT NULL DEFAULT 1,
                escalation_email VARCHAR(200) NULL,
                business_hours_only TINYINT(1) NOT NULL DEFAULT 1,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            using var cmd = new MySqlCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
        }

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

        public async Task<List<SlaRule>> GetSlaRulesAsync()
        {
            var list = new List<SlaRule>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureSlaRulesTableAsync(conn);

            using var cmd = new MySqlCommand("SELECT * FROM sla_rules ORDER BY is_active DESC, name", conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                int o(string n) => r.GetOrdinal(n);
                list.Add(new SlaRule
                {
                    Id = r.GetInt32(o("id")),
                    Name = r.IsDBNull(o("name")) ? "" : r.GetString(o("name")),
                    Priority = r.IsDBNull(o("priority")) ? null : r.GetString(o("priority")),
                    TicketCategory = r.IsDBNull(o("ticket_category")) ? null : r.GetString(o("ticket_category")),
                    FirstResponseMinutes = r.GetInt32(o("first_response_minutes")),
                    ResolutionTimeHours = r.GetInt32(o("resolution_time_hours")),
                    IsActive = !r.IsDBNull(o("is_active")) && r.GetBoolean(o("is_active")),
                    EscalationEmail = r.IsDBNull(o("escalation_email")) ? null : r.GetString(o("escalation_email")),
                    BusinessHoursOnly = !r.IsDBNull(o("business_hours_only")) && r.GetBoolean(o("business_hours_only")),
                    CreatedAt = r.IsDBNull(o("created_at")) ? DateTime.Now : r.GetDateTime(o("created_at")),
                    UpdatedAt = r.IsDBNull(o("updated_at")) ? DateTime.Now : r.GetDateTime(o("updated_at"))
                });
            }
            return list;
        }

        public async Task<TicketSlaStatus?> GetTicketSlaStatusAsync(int ticketId)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureTicketSlaStatusTableAsync(conn);

            using var cmd = new MySqlCommand(
                "SELECT * FROM ticket_sla_status WHERE ticket_id = @id LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@id", ticketId);
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;

            int o(string n) => r.GetOrdinal(n);
            return new TicketSlaStatus
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

        public async Task SaveTicketSlaStatusAsync(TicketSlaStatus status)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureTicketSlaStatusTableAsync(conn);

            const string sql = @"INSERT INTO ticket_sla_status
                (ticket_id, sla_rule_id, first_response_due, first_response_at, resolution_due,
                 resolved_at, is_breached, breach_type, escalation_level, created_at, updated_at)
                VALUES (@tid, @rid, @frd, @fra, @rd, @ra, @ib, @bt, @el, @ca, @ua)
                ON DUPLICATE KEY UPDATE
                    sla_rule_id = @rid,
                    first_response_due = @frd,
                    first_response_at = @fra,
                    resolution_due = @rd,
                    resolved_at = @ra,
                    is_breached = @ib,
                    breach_type = @bt,
                    escalation_level = @el,
                    updated_at = @ua;";
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
            cmd.Parameters.AddWithValue("@ca", status.CreatedAt);
            cmd.Parameters.AddWithValue("@ua", DateTime.Now);
            await cmd.ExecuteNonQueryAsync();
        }

        // ===== Dashboard KPI =====

        private async Task EnsureDashboardKpisTableAsync(MySqlConnection conn)
        {
            const string ddl = @"CREATE TABLE IF NOT EXISTS dashboard_kpis (
                id INT AUTO_INCREMENT PRIMARY KEY,
                kpi_type VARCHAR(50) NOT NULL,
                title VARCHAR(100) NOT NULL,
                current_value DECIMAL(18,4) NOT NULL DEFAULT 0,
                previous_value DECIMAL(18,4) NULL,
                target_value DECIMAL(18,4) NULL,
                unit VARCHAR(20) NULL,
                trend VARCHAR(20) NULL,
                color VARCHAR(20) NULL,
                icon VARCHAR(50) NULL,
                time_period VARCHAR(20) NULL,
                calculation_query TEXT NULL,
                display_order INT NOT NULL DEFAULT 0,
                is_visible TINYINT(1) NOT NULL DEFAULT 1,
                last_updated DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UNIQUE KEY uq_kpi_type (kpi_type)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            using var cmd = new MySqlCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<DashboardKpi>> GetDashboardKpisAsync()
        {
            var list = new List<DashboardKpi>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureDashboardKpisTableAsync(conn);

            using var cmd = new MySqlCommand(
                "SELECT * FROM dashboard_kpis WHERE is_visible = 1 ORDER BY display_order, id", conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                int o(string n) => r.GetOrdinal(n);
                list.Add(new DashboardKpi
                {
                    Id = r.GetInt32(o("id")),
                    KpiType = r.IsDBNull(o("kpi_type")) ? "" : r.GetString(o("kpi_type")),
                    Title = r.IsDBNull(o("title")) ? "" : r.GetString(o("title")),
                    CurrentValue = r.GetDecimal(o("current_value")),
                    PreviousValue = r.IsDBNull(o("previous_value")) ? null : r.GetDecimal(o("previous_value")),
                    TargetValue = r.IsDBNull(o("target_value")) ? null : r.GetDecimal(o("target_value")),
                    Unit = r.IsDBNull(o("unit")) ? null : r.GetString(o("unit")),
                    Trend = r.IsDBNull(o("trend")) ? null : r.GetString(o("trend")),
                    Color = r.IsDBNull(o("color")) ? null : r.GetString(o("color")),
                    Icon = r.IsDBNull(o("icon")) ? null : r.GetString(o("icon")),
                    TimePeriod = r.IsDBNull(o("time_period")) ? null : r.GetString(o("time_period")),
                    CalculationQuery = r.IsDBNull(o("calculation_query")) ? null : r.GetString(o("calculation_query")),
                    DisplayOrder = r.GetInt32(o("display_order")),
                    IsVisible = !r.IsDBNull(o("is_visible")) && r.GetBoolean(o("is_visible")),
                    LastUpdated = r.IsDBNull(o("last_updated")) ? DateTime.Now : r.GetDateTime(o("last_updated")),
                    CreatedAt = r.IsDBNull(o("created_at")) ? DateTime.Now : r.GetDateTime(o("created_at"))
                });
            }
            return list;
        }

        public async Task SaveDashboardKpiAsync(DashboardKpi kpi)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureDashboardKpisTableAsync(conn);

            const string sql = @"INSERT INTO dashboard_kpis
                (kpi_type, title, current_value, previous_value, target_value, unit, trend, color, icon,
                 time_period, calculation_query, display_order, is_visible, last_updated, created_at)
                VALUES (@kt, @t, @cv, @pv, @tv, @u, @tr, @c, @i, @tp, @cq, @do, @iv, @lu, @ca)
                ON DUPLICATE KEY UPDATE
                    title = @t, current_value = @cv, previous_value = @pv, target_value = @tv,
                    unit = @u, trend = @tr, color = @c, icon = @i, time_period = @tp,
                    calculation_query = @cq, display_order = @do, is_visible = @iv, last_updated = @lu;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@kt", kpi.KpiType ?? "");
            cmd.Parameters.AddWithValue("@t", kpi.Title ?? "");
            cmd.Parameters.AddWithValue("@cv", kpi.CurrentValue);
            cmd.Parameters.AddWithValue("@pv", (object?)kpi.PreviousValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tv", (object?)kpi.TargetValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@u", (object?)kpi.Unit ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tr", (object?)kpi.Trend ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@c", (object?)kpi.Color ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@i", (object?)kpi.Icon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tp", (object?)kpi.TimePeriod ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cq", (object?)kpi.CalculationQuery ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@do", kpi.DisplayOrder);
            cmd.Parameters.AddWithValue("@iv", kpi.IsVisible);
            cmd.Parameters.AddWithValue("@lu", DateTime.Now);
            cmd.Parameters.AddWithValue("@ca", kpi.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
        }

        // ===== Activity Feed =====

        private async Task EnsureActivityFeedTableAsync(MySqlConnection conn)
        {
            const string ddl = @"CREATE TABLE IF NOT EXISTS activity_feed (
                id INT AUTO_INCREMENT PRIMARY KEY,
                timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                user_name VARCHAR(100) NULL,
                action VARCHAR(200) NULL,
                entity_type VARCHAR(50) NULL,
                details VARCHAR(500) NULL,
                INDEX idx_timestamp (timestamp)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            using var cmd = new MySqlCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<ActivityFeedItem>> GetActivityFeedAsync(int limit = 50)
        {
            var list = new List<ActivityFeedItem>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureActivityFeedTableAsync(conn);

            using (var cmd = new MySqlCommand(
                "SELECT timestamp, user_name, action, entity_type, details FROM activity_feed " +
                "ORDER BY timestamp DESC LIMIT @lim", conn))
            {
                cmd.Parameters.AddWithValue("@lim", limit);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new ActivityFeedItem
                    {
                        Timestamp = r.IsDBNull(0) ? DateTime.Now : r.GetDateTime(0),
                        UserName = r.IsDBNull(1) ? "" : r.GetString(1),
                        Action = r.IsDBNull(2) ? "" : r.GetString(2),
                        EntityType = r.IsDBNull(3) ? "" : r.GetString(3),
                        Details = r.IsDBNull(4) ? "" : r.GetString(4)
                    });
                }
            }

            // Fallback: aus audit_log falls activity_feed leer
            if (list.Count == 0)
            {
                try
                {
                    using var cmd = new MySqlCommand(
                        "SELECT timestamp, user_name, action, entity_type, details FROM audit_log " +
                        "ORDER BY timestamp DESC LIMIT @lim", conn);
                    cmd.Parameters.AddWithValue("@lim", limit);
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        list.Add(new ActivityFeedItem
                        {
                            Timestamp = r.IsDBNull(0) ? DateTime.Now : r.GetDateTime(0),
                            UserName = r.IsDBNull(1) ? "" : r.GetString(1),
                            Action = r.IsDBNull(2) ? "" : r.GetString(2),
                            EntityType = r.IsDBNull(3) ? "" : r.GetString(3),
                            Details = r.IsDBNull(4) ? "" : r.GetString(4)
                        });
                    }
                }
                catch
                {
                    // audit_log evtl. nicht vorhanden
                }
            }

            return list;
        }

        public async Task AddActivityFeedItemAsync(ActivityFeedItem item)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureActivityFeedTableAsync(conn);

            const string sql = @"INSERT INTO activity_feed (timestamp, user_name, action, entity_type, details)
                                 VALUES (@ts, @un, @ac, @et, @d);";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ts", item.Timestamp == default ? DateTime.Now : item.Timestamp);
            cmd.Parameters.AddWithValue("@un", item.UserName ?? "");
            cmd.Parameters.AddWithValue("@ac", item.Action ?? "");
            cmd.Parameters.AddWithValue("@et", item.EntityType ?? "");
            cmd.Parameters.AddWithValue("@d", item.Details ?? "");
            await cmd.ExecuteNonQueryAsync();
        }

        // ===== Lead Statistics =====

        private async Task EnsureLeadStatisticsTableAsync(MySqlConnection conn)
        {
            const string ddl = @"CREATE TABLE IF NOT EXISTS lead_statistics (
                id INT AUTO_INCREMENT PRIMARY KEY,
                period_start DATETIME NOT NULL,
                period_end DATETIME NOT NULL,
                total_leads INT NOT NULL DEFAULT 0,
                converted_leads INT NOT NULL DEFAULT 0,
                lost_leads INT NOT NULL DEFAULT 0,
                active_leads INT NOT NULL DEFAULT 0,
                conversion_rate DECIMAL(8,4) NOT NULL DEFAULT 0,
                average_conversion_time_days DECIMAL(10,2) NULL,
                total_revenue DECIMAL(15,2) NOT NULL DEFAULT 0,
                average_deal_value DECIMAL(15,2) NOT NULL DEFAULT 0,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UNIQUE KEY uq_period (period_start, period_end)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            using var cmd = new MySqlCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<LeadStatistics?> GetLeadStatisticsAsync(DateTime periodStart, DateTime periodEnd)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureLeadStatisticsTableAsync(conn);

            // 1) gespeicherter Snapshot?
            using (var cmd = new MySqlCommand(
                "SELECT * FROM lead_statistics WHERE period_start = @ps AND period_end = @pe LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@ps", periodStart);
                cmd.Parameters.AddWithValue("@pe", periodEnd);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    int o(string n) => r.GetOrdinal(n);
                    return new LeadStatistics
                    {
                        Id = r.GetInt32(o("id")),
                        PeriodStart = r.GetDateTime(o("period_start")),
                        PeriodEnd = r.GetDateTime(o("period_end")),
                        TotalLeads = r.GetInt32(o("total_leads")),
                        ConvertedLeads = r.GetInt32(o("converted_leads")),
                        LostLeads = r.GetInt32(o("lost_leads")),
                        ActiveLeads = r.GetInt32(o("active_leads")),
                        ConversionRate = r.GetDecimal(o("conversion_rate")),
                        AverageConversionTimeDays = r.IsDBNull(o("average_conversion_time_days")) ? null : r.GetDecimal(o("average_conversion_time_days")),
                        TotalRevenue = r.GetDecimal(o("total_revenue")),
                        AverageDeadValue = r.GetDecimal(o("average_deal_value")),
                        CreatedAt = r.IsDBNull(o("created_at")) ? DateTime.Now : r.GetDateTime(o("created_at"))
                    };
                }
            }

            // 2) Live aus leads-Tabelle berechnen (falls vorhanden)
            try
            {
                var stats = new LeadStatistics
                {
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    CreatedAt = DateTime.Now
                };

                using (var cmd = new MySqlCommand(@"
                    SELECT
                        COUNT(*) AS total,
                        SUM(CASE WHEN LOWER(status) IN ('converted','gewonnen','won','closed_won') THEN 1 ELSE 0 END) AS converted,
                        SUM(CASE WHEN LOWER(status) IN ('lost','verloren','closed_lost') THEN 1 ELSE 0 END) AS lost,
                        SUM(CASE WHEN LOWER(status) NOT IN ('converted','gewonnen','won','closed_won','lost','verloren','closed_lost') THEN 1 ELSE 0 END) AS active
                    FROM leads
                    WHERE created_at BETWEEN @ps AND @pe", conn))
                {
                    cmd.Parameters.AddWithValue("@ps", periodStart);
                    cmd.Parameters.AddWithValue("@pe", periodEnd);
                    using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        stats.TotalLeads = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0));
                        stats.ConvertedLeads = r.IsDBNull(1) ? 0 : Convert.ToInt32(r.GetValue(1));
                        stats.LostLeads = r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2));
                        stats.ActiveLeads = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3));
                    }
                }

                stats.ConversionRate = stats.TotalLeads > 0
                    ? Math.Round((decimal)stats.ConvertedLeads / stats.TotalLeads * 100m, 2)
                    : 0m;
                return stats;
            }
            catch
            {
                return null;
            }
        }

        public async Task SaveLeadStatisticsAsync(LeadStatistics stats)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureLeadStatisticsTableAsync(conn);

            const string sql = @"INSERT INTO lead_statistics
                (period_start, period_end, total_leads, converted_leads, lost_leads, active_leads,
                 conversion_rate, average_conversion_time_days, total_revenue, average_deal_value, created_at)
                VALUES (@ps, @pe, @tl, @cl, @ll, @al, @cr, @act, @tr, @adv, @ca)
                ON DUPLICATE KEY UPDATE
                    total_leads = @tl, converted_leads = @cl, lost_leads = @ll, active_leads = @al,
                    conversion_rate = @cr, average_conversion_time_days = @act,
                    total_revenue = @tr, average_deal_value = @adv;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ps", stats.PeriodStart);
            cmd.Parameters.AddWithValue("@pe", stats.PeriodEnd);
            cmd.Parameters.AddWithValue("@tl", stats.TotalLeads);
            cmd.Parameters.AddWithValue("@cl", stats.ConvertedLeads);
            cmd.Parameters.AddWithValue("@ll", stats.LostLeads);
            cmd.Parameters.AddWithValue("@al", stats.ActiveLeads);
            cmd.Parameters.AddWithValue("@cr", stats.ConversionRate);
            cmd.Parameters.AddWithValue("@act", (object?)stats.AverageConversionTimeDays ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tr", stats.TotalRevenue);
            cmd.Parameters.AddWithValue("@adv", stats.AverageDeadValue);
            cmd.Parameters.AddWithValue("@ca", stats.CreatedAt == default ? DateTime.Now : stats.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
        }

        // ===== Due Date Warnings =====

        private async Task EnsureDueDateWarningsTableAsync(MySqlConnection conn)
        {
            const string ddl = @"CREATE TABLE IF NOT EXISTS due_date_warnings (
                id INT AUTO_INCREMENT PRIMARY KEY,
                entity_type VARCHAR(50) NOT NULL,
                entity_id INT NOT NULL,
                entity_title VARCHAR(200) NULL,
                due_date DATETIME NOT NULL,
                assigned_to VARCHAR(100) NULL,
                priority VARCHAR(20) NULL,
                warning_level VARCHAR(20) NULL,
                is_dismissed TINYINT(1) NOT NULL DEFAULT 0,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                INDEX idx_due (due_date),
                INDEX idx_entity (entity_type, entity_id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            using var cmd = new MySqlCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<DueDateWarning>> GetDueDateWarningsAsync()
        {
            var list = new List<DueDateWarning>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureDueDateWarningsTableAsync(conn);

            using (var cmd = new MySqlCommand(
                "SELECT * FROM due_date_warnings WHERE is_dismissed = 0 ORDER BY due_date ASC", conn))
            {
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    int o(string n) => r.GetOrdinal(n);
                    list.Add(new DueDateWarning
                    {
                        Id = r.GetInt32(o("id")),
                        EntityType = r.IsDBNull(o("entity_type")) ? "" : r.GetString(o("entity_type")),
                        EntityId = r.GetInt32(o("entity_id")),
                        EntityTitle = r.IsDBNull(o("entity_title")) ? null : r.GetString(o("entity_title")),
                        DueDate = r.GetDateTime(o("due_date")),
                        AssignedTo = r.IsDBNull(o("assigned_to")) ? null : r.GetString(o("assigned_to")),
                        Priority = r.IsDBNull(o("priority")) ? null : r.GetString(o("priority")),
                        WarningLevel = r.IsDBNull(o("warning_level")) ? null : r.GetString(o("warning_level")),
                        IsDismissed = !r.IsDBNull(o("is_dismissed")) && r.GetBoolean(o("is_dismissed")),
                        CreatedAt = r.IsDBNull(o("created_at")) ? DateTime.Now : r.GetDateTime(o("created_at"))
                    });
                }
            }

            // Zusätzlich: Live-Ableitung aus offenen Tickets / Tasks
            try
            {
                using var cmd = new MySqlCommand(@"
                    SELECT id, title, due_date, assigned_to, priority
                    FROM tickets
                    WHERE due_date IS NOT NULL
                      AND due_date <= DATE_ADD(NOW(), INTERVAL 7 DAY)
                      AND LOWER(COALESCE(status,'')) NOT IN ('closed','resolved','geschlossen','done')
                    ORDER BY due_date ASC", conn);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var due = r.IsDBNull(2) ? DateTime.Now : r.GetDateTime(2);
                    string level = due < DateTime.Now ? "Overdue"
                        : due.Date == DateTime.Today ? "DueToday"
                        : due.Date == DateTime.Today.AddDays(1) ? "DueTomorrow"
                        : "DueThisWeek";
                    list.Add(new DueDateWarning
                    {
                        EntityType = "Ticket",
                        EntityId = r.GetInt32(0),
                        EntityTitle = r.IsDBNull(1) ? null : r.GetString(1),
                        DueDate = due,
                        AssignedTo = r.IsDBNull(3) ? null : r.GetString(3),
                        Priority = r.IsDBNull(4) ? null : r.GetString(4),
                        WarningLevel = level,
                        CreatedAt = DateTime.Now
                    });
                }
            }
            catch
            {
                // tickets-Tabelle/Spalten evtl. anders – ignorieren
            }

            return list;
        }

        public async Task SaveDueDateWarningAsync(DueDateWarning warning)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureDueDateWarningsTableAsync(conn);

            const string sql = @"INSERT INTO due_date_warnings
                (entity_type, entity_id, entity_title, due_date, assigned_to, priority,
                 warning_level, is_dismissed, created_at)
                VALUES (@et, @ei, @ti, @dd, @at, @p, @wl, @id, @ca);";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@et", warning.EntityType ?? "");
            cmd.Parameters.AddWithValue("@ei", warning.EntityId);
            cmd.Parameters.AddWithValue("@ti", (object?)warning.EntityTitle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dd", warning.DueDate);
            cmd.Parameters.AddWithValue("@at", (object?)warning.AssignedTo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@p", (object?)warning.Priority ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@wl", (object?)warning.WarningLevel ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", warning.IsDismissed);
            cmd.Parameters.AddWithValue("@ca", warning.CreatedAt == default ? DateTime.Now : warning.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
        }

        // ===== Budget Tracking =====

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

        public async Task<ProjectBudget?> GetProjectBudgetAsync(int projectId)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureProjectBudgetsTableAsync(conn);

            // 1) Existierende Budget-Zeile lesen (falls vorhanden)
            ProjectBudget? budget = null;
            using (var cmd = new MySqlCommand(
                "SELECT * FROM project_budgets WHERE project_id = @pid LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@pid", projectId);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    int o(string n) => r.GetOrdinal(n);
                    budget = new ProjectBudget
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

            // 2) Fallback: aus Projekt-Stammdaten (projects.budget) synthetisieren
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

                budget = new ProjectBudget
                {
                    ProjectId = projectId,
                    TotalPlannedBudget = projectBudget,
                    Currency = "EUR",
                    LastUpdated = DateTime.Now
                };
            }

            // 3) Tatsächliche Stunden aus time_entries summieren
            try
            {
                using var cmd = new MySqlCommand(
                    "SELECT COALESCE(SUM(TIME_TO_SEC(duration))/3600,0) FROM time_entries WHERE project_id = @pid", conn);
                cmd.Parameters.AddWithValue("@pid", projectId);
                var val = await cmd.ExecuteScalarAsync();
                if (val != null && val != DBNull.Value)
                    budget.TotalActualHours = Math.Round(Convert.ToDecimal(val), 2);
            }
            catch
            {
                // time_entries fehlt evtl. – ignorieren
            }

            // 4) Geplante Stunden ableiten, falls keine gesetzt: aus geplantem Budget / Standardstundensatz (75 €)
            if (budget.TotalPlannedHours == 0 && budget.TotalPlannedBudget > 0)
            {
                budget.TotalPlannedHours = Math.Round(budget.TotalPlannedBudget / 75m, 2);
            }

            // 5) Tatsächliches Budget = Ist-Stunden * Stundensatz (aus Plan abgeleitet, fallback 75 €)
            decimal rate = (budget.TotalPlannedHours > 0)
                ? budget.TotalPlannedBudget / budget.TotalPlannedHours
                : 75m;
            if (budget.TotalActualBudget == 0)
            {
                budget.TotalActualBudget = Math.Round(budget.TotalActualHours * rate, 2);
            }

            return budget;
        }

        public async Task<List<BudgetEntry>> GetBudgetEntriesByProjectAsync(int projectId)
        {
            var list = new List<BudgetEntry>();
            using var conn = new MySqlConnection(connectionString);
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
                    list.Add(new BudgetEntry
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
                        Notes = r.IsDBNull(o("notes")) ? null : r.GetString(o("notes")),
                        CreatedAt = r.IsDBNull(o("created_at")) ? DateTime.Now : r.GetDateTime(o("created_at")),
                        UpdatedAt = r.IsDBNull(o("updated_at")) ? DateTime.Now : r.GetDateTime(o("updated_at"))
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
                        list.Add(new BudgetEntry
                        {
                            ProjectId = projectId,
                            Category = "Arbeitszeit",
                            Description = name,
                            ActualHours = hours,
                            CostPerHour = 75m,
                            ActualAmount = Math.Round(hours * 75m, 2),
                            PlannedAmount = 0m,
                            EntryDate = lastDate,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                    }
                }
                catch
                {
                    // time_entries fehlt evtl. – ignorieren
                }
            }

            return list;
        }

        public async Task SaveBudgetEntryAsync(BudgetEntry entry)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureBudgetEntriesTableAsync(conn);

            const string sql = @"INSERT INTO project_budget_entries
                (project_id, category, description, planned_amount, actual_amount,
                 planned_hours, actual_hours, cost_per_hour, entry_date, notes, created_at, updated_at)
                VALUES
                (@p, @c, @d, @pa, @aa, @ph, @ah, @cph, @ed, @n, @ca, @ua);";
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
            cmd.Parameters.AddWithValue("@ca", entry.CreatedAt);
            cmd.Parameters.AddWithValue("@ua", entry.UpdatedAt);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveProjectBudgetAsync(ProjectBudget budget)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureProjectBudgetsTableAsync(conn);

            const string sql = @"INSERT INTO project_budgets
                (project_id, total_planned_budget, total_actual_budget, total_planned_hours, total_actual_hours, currency, last_updated)
                VALUES (@p, @pb, @ab, @ph, @ah, @cur, @lu)
                ON DUPLICATE KEY UPDATE
                    total_planned_budget = @pb,
                    total_actual_budget = @ab,
                    total_planned_hours = @ph,
                    total_actual_hours = @ah,
                    currency = @cur,
                    last_updated = @lu;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@p", budget.ProjectId);
            cmd.Parameters.AddWithValue("@pb", budget.TotalPlannedBudget);
            cmd.Parameters.AddWithValue("@ab", budget.TotalActualBudget);
            cmd.Parameters.AddWithValue("@ph", budget.TotalPlannedHours);
            cmd.Parameters.AddWithValue("@ah", budget.TotalActualHours);
            cmd.Parameters.AddWithValue("@cur", budget.Currency ?? "EUR");
            cmd.Parameters.AddWithValue("@lu", DateTime.Now);
            await cmd.ExecuteNonQueryAsync();
        }

        // ===== Gantt / Timeline =====

        private async Task EnsureGanttTasksTableAsync(MySqlConnection conn)
        {
            const string ddl = @"CREATE TABLE IF NOT EXISTS gantt_tasks (
                id INT AUTO_INCREMENT PRIMARY KEY,
                project_id INT NOT NULL,
                task_name VARCHAR(200) NOT NULL,
                description LONGTEXT NULL,
                start_date DATETIME NOT NULL,
                end_date DATETIME NOT NULL,
                actual_start_date DATETIME NULL,
                actual_end_date DATETIME NULL,
                progress_percentage INT NOT NULL DEFAULT 0,
                assigned_to VARCHAR(100) NULL,
                parent_task_id INT NULL,
                is_milestone TINYINT(1) NOT NULL DEFAULT 0,
                dependencies VARCHAR(200) NULL,
                priority VARCHAR(20) NULL,
                status VARCHAR(50) NULL,
                color VARCHAR(20) NULL,
                display_order INT NOT NULL DEFAULT 0,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                INDEX idx_project (project_id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            using var cmd = new MySqlCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<GanttTask>> GetGanttTasksByProjectAsync(int projectId)
        {
            var list = new List<GanttTask>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureGanttTasksTableAsync(conn);

            using var cmd = new MySqlCommand(
                "SELECT * FROM gantt_tasks WHERE project_id = @pid ORDER BY display_order, start_date", conn);
            cmd.Parameters.AddWithValue("@pid", projectId);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                int o(string n) => r.GetOrdinal(n);
                list.Add(new GanttTask
                {
                    Id = r.GetInt32(o("id")),
                    ProjectId = r.GetInt32(o("project_id")),
                    TaskName = r.IsDBNull(o("task_name")) ? "" : r.GetString(o("task_name")),
                    Description = r.IsDBNull(o("description")) ? null : r.GetString(o("description")),
                    StartDate = r.GetDateTime(o("start_date")),
                    EndDate = r.GetDateTime(o("end_date")),
                    ActualStartDate = r.IsDBNull(o("actual_start_date")) ? null : r.GetDateTime(o("actual_start_date")),
                    ActualEndDate = r.IsDBNull(o("actual_end_date")) ? null : r.GetDateTime(o("actual_end_date")),
                    ProgressPercentage = r.GetInt32(o("progress_percentage")),
                    AssignedTo = r.IsDBNull(o("assigned_to")) ? null : r.GetString(o("assigned_to")),
                    ParentTaskId = r.IsDBNull(o("parent_task_id")) ? null : r.GetInt32(o("parent_task_id")),
                    IsMilestone = !r.IsDBNull(o("is_milestone")) && r.GetBoolean(o("is_milestone")),
                    Dependencies = r.IsDBNull(o("dependencies")) ? null : r.GetString(o("dependencies")),
                    Priority = r.IsDBNull(o("priority")) ? null : r.GetString(o("priority")),
                    Status = r.IsDBNull(o("status")) ? null : r.GetString(o("status")),
                    Color = r.IsDBNull(o("color")) ? null : r.GetString(o("color")),
                    DisplayOrder = r.GetInt32(o("display_order")),
                    CreatedAt = r.IsDBNull(o("created_at")) ? DateTime.Now : r.GetDateTime(o("created_at")),
                    UpdatedAt = r.IsDBNull(o("updated_at")) ? DateTime.Now : r.GetDateTime(o("updated_at"))
                });
            }
            return list;
        }

        public async Task SaveGanttTaskAsync(GanttTask task)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureGanttTasksTableAsync(conn);

            string sql = task.Id > 0
                ? @"UPDATE gantt_tasks SET
                        project_id=@pid, task_name=@tn, description=@d, start_date=@sd, end_date=@ed,
                        actual_start_date=@asd, actual_end_date=@aed, progress_percentage=@pp,
                        assigned_to=@at, parent_task_id=@pt, is_milestone=@im, dependencies=@dep,
                        priority=@p, status=@s, color=@c, display_order=@do, updated_at=@ua
                   WHERE id=@id;"
                : @"INSERT INTO gantt_tasks
                    (project_id, task_name, description, start_date, end_date, actual_start_date, actual_end_date,
                     progress_percentage, assigned_to, parent_task_id, is_milestone, dependencies,
                     priority, status, color, display_order, created_at, updated_at)
                    VALUES (@pid, @tn, @d, @sd, @ed, @asd, @aed, @pp, @at, @pt, @im, @dep, @p, @s, @c, @do, @ca, @ua);";

            using var cmd = new MySqlCommand(sql, conn);
            if (task.Id > 0) cmd.Parameters.AddWithValue("@id", task.Id);
            cmd.Parameters.AddWithValue("@pid", task.ProjectId);
            cmd.Parameters.AddWithValue("@tn", task.TaskName ?? "");
            cmd.Parameters.AddWithValue("@d", (object?)task.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sd", task.StartDate);
            cmd.Parameters.AddWithValue("@ed", task.EndDate);
            cmd.Parameters.AddWithValue("@asd", (object?)task.ActualStartDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@aed", (object?)task.ActualEndDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pp", task.ProgressPercentage);
            cmd.Parameters.AddWithValue("@at", (object?)task.AssignedTo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pt", (object?)task.ParentTaskId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@im", task.IsMilestone);
            cmd.Parameters.AddWithValue("@dep", (object?)task.Dependencies ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@p", (object?)task.Priority ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@s", (object?)task.Status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@c", (object?)task.Color ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@do", task.DisplayOrder);
            if (task.Id == 0) cmd.Parameters.AddWithValue("@ca", task.CreatedAt == default ? DateTime.Now : task.CreatedAt);
            cmd.Parameters.AddWithValue("@ua", DateTime.Now);
            await cmd.ExecuteNonQueryAsync();
        }

        // ===== Email & Offer History =====

        private async Task EnsureEmailHistoryTableAsync(MySqlConnection conn)
        {
            const string ddl = @"CREATE TABLE IF NOT EXISTS email_history (
                id INT AUTO_INCREMENT PRIMARY KEY,
                contact_id INT NULL,
                customer_id INT NULL,
                lead_id INT NULL,
                subject VARCHAR(300) NOT NULL,
                from_address VARCHAR(200) NULL,
                to_address VARCHAR(200) NULL,
                cc_address VARCHAR(500) NULL,
                body LONGTEXT NULL,
                body_preview VARCHAR(500) NULL,
                sent_date DATETIME NOT NULL,
                received_date DATETIME NULL,
                direction VARCHAR(20) NULL,
                has_attachments TINYINT(1) NOT NULL DEFAULT 0,
                attachment_count INT NOT NULL DEFAULT 0,
                exchange_message_id VARCHAR(500) NULL,
                conversation_id VARCHAR(200) NULL,
                is_read TINYINT(1) NOT NULL DEFAULT 0,
                importance VARCHAR(20) NULL,
                category VARCHAR(50) NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                INDEX idx_customer (customer_id),
                INDEX idx_contact (contact_id),
                INDEX idx_lead (lead_id),
                INDEX idx_sent_date (sent_date)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            using var cmd = new MySqlCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task EnsureOfferHistoryTableAsync(MySqlConnection conn)
        {
            const string ddl = @"CREATE TABLE IF NOT EXISTS offer_history (
                id INT AUTO_INCREMENT PRIMARY KEY,
                contact_id INT NULL,
                customer_id INT NULL,
                lead_id INT NULL,
                easybill_document_id BIGINT NULL,
                offer_number VARCHAR(50) NULL,
                offer_title VARCHAR(200) NULL,
                offer_date DATETIME NOT NULL,
                valid_until DATETIME NULL,
                total_amount DECIMAL(15,2) NOT NULL DEFAULT 0,
                currency VARCHAR(10) NOT NULL DEFAULT 'EUR',
                status VARCHAR(50) NULL,
                sent_date DATETIME NULL,
                accepted_date DATETIME NULL,
                declined_date DATETIME NULL,
                converted_to_invoice_id BIGINT NULL,
                sent_via VARCHAR(50) NULL,
                notes LONGTEXT NULL,
                created_by VARCHAR(100) NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                INDEX idx_customer (customer_id),
                INDEX idx_contact (contact_id),
                INDEX idx_lead (lead_id),
                INDEX idx_offer_date (offer_date)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            using var cmd = new MySqlCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<EmailHistory>> GetEmailHistoryByContactAsync(int contactId)
        {
            var list = new List<EmailHistory>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureEmailHistoryTableAsync(conn);

            const string sql = @"SELECT * FROM email_history
                                 WHERE customer_id = @id OR contact_id = @id
                                 ORDER BY sent_date DESC";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", contactId);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                int o(string n) => r.GetOrdinal(n);
                list.Add(new EmailHistory
                {
                    Id = r.GetInt32(o("id")),
                    ContactId = r.IsDBNull(o("contact_id")) ? null : r.GetInt32(o("contact_id")),
                    CustomerId = r.IsDBNull(o("customer_id")) ? null : r.GetInt32(o("customer_id")),
                    LeadId = r.IsDBNull(o("lead_id")) ? null : r.GetInt32(o("lead_id")),
                    Subject = r.IsDBNull(o("subject")) ? "" : r.GetString(o("subject")),
                    FromAddress = r.IsDBNull(o("from_address")) ? null : r.GetString(o("from_address")),
                    ToAddress = r.IsDBNull(o("to_address")) ? null : r.GetString(o("to_address")),
                    CcAddress = r.IsDBNull(o("cc_address")) ? null : r.GetString(o("cc_address")),
                    Body = r.IsDBNull(o("body")) ? null : r.GetString(o("body")),
                    BodyPreview = r.IsDBNull(o("body_preview")) ? null : r.GetString(o("body_preview")),
                    SentDate = r.GetDateTime(o("sent_date")),
                    ReceivedDate = r.IsDBNull(o("received_date")) ? null : r.GetDateTime(o("received_date")),
                    Direction = r.IsDBNull(o("direction")) ? null : r.GetString(o("direction")),
                    HasAttachments = !r.IsDBNull(o("has_attachments")) && r.GetBoolean(o("has_attachments")),
                    AttachmentCount = r.IsDBNull(o("attachment_count")) ? 0 : r.GetInt32(o("attachment_count")),
                    ExchangeMessageId = r.IsDBNull(o("exchange_message_id")) ? null : r.GetString(o("exchange_message_id")),
                    ConversationId = r.IsDBNull(o("conversation_id")) ? null : r.GetString(o("conversation_id")),
                    IsRead = !r.IsDBNull(o("is_read")) && r.GetBoolean(o("is_read")),
                    Importance = r.IsDBNull(o("importance")) ? null : r.GetString(o("importance")),
                    Category = r.IsDBNull(o("category")) ? null : r.GetString(o("category")),
                    CreatedAt = r.IsDBNull(o("created_at")) ? DateTime.Now : r.GetDateTime(o("created_at"))
                });
            }
            return list;
        }

        public async Task<List<OfferHistory>> GetOfferHistoryByContactAsync(int contactId)
        {
            var list = new List<OfferHistory>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureOfferHistoryTableAsync(conn);

            const string sql = @"SELECT * FROM offer_history
                                 WHERE customer_id = @id OR contact_id = @id
                                 ORDER BY offer_date DESC";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", contactId);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                int o(string n) => r.GetOrdinal(n);
                list.Add(new OfferHistory
                {
                    Id = r.GetInt32(o("id")),
                    ContactId = r.IsDBNull(o("contact_id")) ? null : r.GetInt32(o("contact_id")),
                    CustomerId = r.IsDBNull(o("customer_id")) ? null : r.GetInt32(o("customer_id")),
                    LeadId = r.IsDBNull(o("lead_id")) ? null : r.GetInt32(o("lead_id")),
                    EasybillDocumentId = r.IsDBNull(o("easybill_document_id")) ? null : r.GetInt64(o("easybill_document_id")),
                    OfferNumber = r.IsDBNull(o("offer_number")) ? null : r.GetString(o("offer_number")),
                    OfferTitle = r.IsDBNull(o("offer_title")) ? null : r.GetString(o("offer_title")),
                    OfferDate = r.GetDateTime(o("offer_date")),
                    ValidUntil = r.IsDBNull(o("valid_until")) ? null : r.GetDateTime(o("valid_until")),
                    TotalAmount = r.IsDBNull(o("total_amount")) ? 0m : r.GetDecimal(o("total_amount")),
                    Currency = r.IsDBNull(o("currency")) ? "EUR" : r.GetString(o("currency")),
                    Status = r.IsDBNull(o("status")) ? null : r.GetString(o("status")),
                    SentDate = r.IsDBNull(o("sent_date")) ? null : r.GetDateTime(o("sent_date")),
                    AcceptedDate = r.IsDBNull(o("accepted_date")) ? null : r.GetDateTime(o("accepted_date")),
                    DeclinedDate = r.IsDBNull(o("declined_date")) ? null : r.GetDateTime(o("declined_date")),
                    ConvertedToInvoiceId = r.IsDBNull(o("converted_to_invoice_id")) ? null : r.GetInt64(o("converted_to_invoice_id")),
                    SentVia = r.IsDBNull(o("sent_via")) ? null : r.GetString(o("sent_via")),
                    Notes = r.IsDBNull(o("notes")) ? null : r.GetString(o("notes")),
                    CreatedBy = r.IsDBNull(o("created_by")) ? null : r.GetString(o("created_by")),
                    CreatedAt = r.IsDBNull(o("created_at")) ? DateTime.Now : r.GetDateTime(o("created_at")),
                    UpdatedAt = r.IsDBNull(o("updated_at")) ? DateTime.Now : r.GetDateTime(o("updated_at"))
                });
            }
            return list;
        }

        public async Task SaveEmailHistoryAsync(EmailHistory h)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureEmailHistoryTableAsync(conn);

            const string sql = @"INSERT INTO email_history
                (contact_id, customer_id, lead_id, subject, from_address, to_address, cc_address,
                 body, body_preview, sent_date, received_date, direction, has_attachments,
                 attachment_count, exchange_message_id, conversation_id, is_read, importance, category, created_at)
                VALUES
                (@contact_id, @customer_id, @lead_id, @subject, @from_address, @to_address, @cc_address,
                 @body, @body_preview, @sent_date, @received_date, @direction, @has_attachments,
                 @attachment_count, @exchange_message_id, @conversation_id, @is_read, @importance, @category, @created_at);";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@contact_id", (object?)h.ContactId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@customer_id", (object?)h.CustomerId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lead_id", (object?)h.LeadId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@subject", h.Subject ?? string.Empty);
            cmd.Parameters.AddWithValue("@from_address", (object?)h.FromAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@to_address", (object?)h.ToAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cc_address", (object?)h.CcAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@body", (object?)h.Body ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@body_preview", (object?)h.BodyPreview ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sent_date", h.SentDate);
            cmd.Parameters.AddWithValue("@received_date", (object?)h.ReceivedDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@direction", (object?)h.Direction ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@has_attachments", h.HasAttachments);
            cmd.Parameters.AddWithValue("@attachment_count", h.AttachmentCount);
            cmd.Parameters.AddWithValue("@exchange_message_id", (object?)h.ExchangeMessageId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@conversation_id", (object?)h.ConversationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@is_read", h.IsRead);
            cmd.Parameters.AddWithValue("@importance", (object?)h.Importance ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@category", (object?)h.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@created_at", h.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveOfferHistoryAsync(OfferHistory o)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureOfferHistoryTableAsync(conn);

            const string sql = @"INSERT INTO offer_history
                (contact_id, customer_id, lead_id, easybill_document_id, offer_number, offer_title,
                 offer_date, valid_until, total_amount, currency, status, sent_date, accepted_date,
                 declined_date, converted_to_invoice_id, sent_via, notes, created_by, created_at, updated_at)
                VALUES
                (@contact_id, @customer_id, @lead_id, @easybill_document_id, @offer_number, @offer_title,
                 @offer_date, @valid_until, @total_amount, @currency, @status, @sent_date, @accepted_date,
                 @declined_date, @converted_to_invoice_id, @sent_via, @notes, @created_by, @created_at, @updated_at);";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@contact_id", (object?)o.ContactId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@customer_id", (object?)o.CustomerId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lead_id", (object?)o.LeadId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@easybill_document_id", (object?)o.EasybillDocumentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@offer_number", (object?)o.OfferNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@offer_title", (object?)o.OfferTitle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@offer_date", o.OfferDate);
            cmd.Parameters.AddWithValue("@valid_until", (object?)o.ValidUntil ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@total_amount", o.TotalAmount);
            cmd.Parameters.AddWithValue("@currency", o.Currency ?? "EUR");
            cmd.Parameters.AddWithValue("@status", (object?)o.Status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sent_date", (object?)o.SentDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@accepted_date", (object?)o.AcceptedDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@declined_date", (object?)o.DeclinedDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@converted_to_invoice_id", (object?)o.ConvertedToInvoiceId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sent_via", (object?)o.SentVia ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", (object?)o.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@created_by", (object?)o.CreatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@created_at", o.CreatedAt);
            cmd.Parameters.AddWithValue("@updated_at", o.UpdatedAt);
            await cmd.ExecuteNonQueryAsync();
        }

        // ===== Supplier Ratings =====

        private async Task EnsureSupplierRatingsTableAsync(MySqlConnection conn)
        {
            const string ddl = @"CREATE TABLE IF NOT EXISTS supplier_ratings (
                id INT AUTO_INCREMENT PRIMARY KEY,
                supplier_id INT NOT NULL,
                rating_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                quality_rating INT NOT NULL DEFAULT 5,
                delivery_rating INT NOT NULL DEFAULT 5,
                price_rating INT NOT NULL DEFAULT 5,
                service_rating INT NOT NULL DEFAULT 5,
                communication_rating INT NOT NULL DEFAULT 5,
                overall_rating DECIMAL(5,2) NOT NULL DEFAULT 5,
                review_text LONGTEXT NULL,
                pros LONGTEXT NULL,
                cons LONGTEXT NULL,
                would_recommend TINYINT(1) NOT NULL DEFAULT 1,
                rated_by VARCHAR(100) NULL,
                order_reference VARCHAR(100) NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                INDEX idx_supplier (supplier_id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            using var cmd = new MySqlCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<SupplierRating>> GetSupplierRatingsAsync()
        {
            var list = new List<SupplierRating>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureSupplierRatingsTableAsync(conn);

            using var cmd = new MySqlCommand(
                "SELECT * FROM supplier_ratings ORDER BY rating_date DESC", conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                int o(string n) => r.GetOrdinal(n);
                list.Add(new SupplierRating
                {
                    Id = r.GetInt32(o("id")),
                    SupplierId = r.GetInt32(o("supplier_id")),
                    RatingDate = r.GetDateTime(o("rating_date")),
                    QualityRating = r.GetInt32(o("quality_rating")),
                    DeliveryRating = r.GetInt32(o("delivery_rating")),
                    PriceRating = r.GetInt32(o("price_rating")),
                    ServiceRating = r.GetInt32(o("service_rating")),
                    CommunicationRating = r.GetInt32(o("communication_rating")),
                    OverallRating = r.GetDecimal(o("overall_rating")),
                    ReviewText = r.IsDBNull(o("review_text")) ? null : r.GetString(o("review_text")),
                    Pros = r.IsDBNull(o("pros")) ? null : r.GetString(o("pros")),
                    Cons = r.IsDBNull(o("cons")) ? null : r.GetString(o("cons")),
                    WouldRecommend = !r.IsDBNull(o("would_recommend")) && r.GetBoolean(o("would_recommend")),
                    RatedBy = r.IsDBNull(o("rated_by")) ? null : r.GetString(o("rated_by")),
                    OrderReference = r.IsDBNull(o("order_reference")) ? null : r.GetString(o("order_reference")),
                    CreatedAt = r.IsDBNull(o("created_at")) ? DateTime.Now : r.GetDateTime(o("created_at")),
                    UpdatedAt = r.IsDBNull(o("updated_at")) ? DateTime.Now : r.GetDateTime(o("updated_at"))
                });
            }
            return list;
        }

        public async Task SaveSupplierRatingAsync(SupplierRating rating)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureSupplierRatingsTableAsync(conn);

            rating.CalculateOverallRating();

            string sql = rating.Id > 0
                ? @"UPDATE supplier_ratings SET
                        supplier_id=@sid, rating_date=@rd, quality_rating=@qr, delivery_rating=@dr,
                        price_rating=@pr, service_rating=@sr, communication_rating=@cr,
                        overall_rating=@or, review_text=@rt, pros=@pros, cons=@cons,
                        would_recommend=@wr, rated_by=@rb, order_reference=@oref, updated_at=@ua
                   WHERE id=@id;"
                : @"INSERT INTO supplier_ratings
                    (supplier_id, rating_date, quality_rating, delivery_rating, price_rating, service_rating,
                     communication_rating, overall_rating, review_text, pros, cons, would_recommend,
                     rated_by, order_reference, created_at, updated_at)
                    VALUES (@sid, @rd, @qr, @dr, @pr, @sr, @cr, @or, @rt, @pros, @cons, @wr, @rb, @oref, @ca, @ua);";

            using var cmd = new MySqlCommand(sql, conn);
            if (rating.Id > 0) cmd.Parameters.AddWithValue("@id", rating.Id);
            cmd.Parameters.AddWithValue("@sid", rating.SupplierId);
            cmd.Parameters.AddWithValue("@rd", rating.RatingDate);
            cmd.Parameters.AddWithValue("@qr", rating.QualityRating);
            cmd.Parameters.AddWithValue("@dr", rating.DeliveryRating);
            cmd.Parameters.AddWithValue("@pr", rating.PriceRating);
            cmd.Parameters.AddWithValue("@sr", rating.ServiceRating);
            cmd.Parameters.AddWithValue("@cr", rating.CommunicationRating);
            cmd.Parameters.AddWithValue("@or", rating.OverallRating);
            cmd.Parameters.AddWithValue("@rt", (object?)rating.ReviewText ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pros", (object?)rating.Pros ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cons", (object?)rating.Cons ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@wr", rating.WouldRecommend);
            cmd.Parameters.AddWithValue("@rb", (object?)rating.RatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@oref", (object?)rating.OrderReference ?? DBNull.Value);
            if (rating.Id == 0) cmd.Parameters.AddWithValue("@ca", rating.CreatedAt == default ? DateTime.Now : rating.CreatedAt);
            cmd.Parameters.AddWithValue("@ua", DateTime.Now);
            await cmd.ExecuteNonQueryAsync();
        }

        // ===== Expenses =====

        private async Task EnsureExpenseCategoriesTableAsync(MySqlConnection conn)
        {
            const string ddl = @"CREATE TABLE IF NOT EXISTS expense_categories (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                description VARCHAR(300) NULL,
                parent_category_id INT NULL,
                icon VARCHAR(50) NULL,
                color VARCHAR(20) NULL,
                budget_limit DECIMAL(15,2) NULL,
                is_active TINYINT(1) NOT NULL DEFAULT 1,
                display_order INT NOT NULL DEFAULT 0,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            using var cmd = new MySqlCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task EnsureMonthlyExpensesTableAsync(MySqlConnection conn)
        {
            const string ddl = @"CREATE TABLE IF NOT EXISTS monthly_expenses (
                id INT AUTO_INCREMENT PRIMARY KEY,
                year INT NOT NULL,
                month INT NOT NULL,
                category_id INT NULL,
                total_amount DECIMAL(15,2) NOT NULL DEFAULT 0,
                transaction_count INT NOT NULL DEFAULT 0,
                budget_amount DECIMAL(15,2) NULL,
                variance DECIMAL(15,2) NULL,
                currency VARCHAR(10) NOT NULL DEFAULT 'EUR',
                last_updated DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UNIQUE KEY uq_month (year, month, category_id),
                INDEX idx_year_month (year, month)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            using var cmd = new MySqlCommand(ddl, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<MonthlyExpense>> GetMonthlyExpensesAsync(int? supplierId = null)
        {
            var list = new List<MonthlyExpense>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureMonthlyExpensesTableAsync(conn);

            // 1) Vorberechnete Snapshots
            using (var cmd = new MySqlCommand(
                "SELECT * FROM monthly_expenses ORDER BY year DESC, month DESC", conn))
            {
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    int o(string n) => r.GetOrdinal(n);
                    list.Add(new MonthlyExpense
                    {
                        Id = r.GetInt32(o("id")),
                        Year = r.GetInt32(o("year")),
                        Month = r.GetInt32(o("month")),
                        CategoryId = r.IsDBNull(o("category_id")) ? null : r.GetInt32(o("category_id")),
                        TotalAmount = r.GetDecimal(o("total_amount")),
                        TransactionCount = r.GetInt32(o("transaction_count")),
                        BudgetAmount = r.IsDBNull(o("budget_amount")) ? null : r.GetDecimal(o("budget_amount")),
                        Variance = r.IsDBNull(o("variance")) ? null : r.GetDecimal(o("variance")),
                        Currency = r.IsDBNull(o("currency")) ? "EUR" : r.GetString(o("currency")),
                        LastUpdated = r.IsDBNull(o("last_updated")) ? DateTime.Now : r.GetDateTime(o("last_updated"))
                    });
                }
            }

            // 2) Live-Aggregation aus purchase_invoices / purchase_orders falls keine Snapshots
            if (list.Count == 0)
            {
                // 2a) Bevorzugt purchase_invoices (echte Ausgaben)
                try
                {
                    string sqlInv = supplierId.HasValue
                        ? @"SELECT YEAR(invoice_date) y, MONTH(invoice_date) m,
                                   COALESCE(SUM(total_gross),0) total, COUNT(*) cnt
                            FROM purchase_invoices
                            WHERE supplier_id = @sid AND invoice_date IS NOT NULL
                            GROUP BY YEAR(invoice_date), MONTH(invoice_date)
                            ORDER BY y DESC, m DESC"
                        : @"SELECT YEAR(invoice_date) y, MONTH(invoice_date) m,
                                   COALESCE(SUM(total_gross),0) total, COUNT(*) cnt
                            FROM purchase_invoices
                            WHERE invoice_date IS NOT NULL
                            GROUP BY YEAR(invoice_date), MONTH(invoice_date)
                            ORDER BY y DESC, m DESC";
                    using var cmd = new MySqlCommand(sqlInv, conn);
                    if (supplierId.HasValue) cmd.Parameters.AddWithValue("@sid", supplierId.Value);
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        list.Add(new MonthlyExpense
                        {
                            Year = r.GetInt32(0),
                            Month = r.GetInt32(1),
                            TotalAmount = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2)),
                            TransactionCount = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3)),
                            Currency = "EUR",
                            LastUpdated = DateTime.Now
                        });
                    }
                }
                catch
                {
                    // purchase_invoices existiert evtl. nicht
                }

                // 2b) Falls weiterhin leer: aus purchase_orders (Spalte total_gross)
                if (list.Count == 0)
                {
                    try
                    {
                        string sql = supplierId.HasValue
                            ? @"SELECT YEAR(order_date) y, MONTH(order_date) m,
                                       COALESCE(SUM(total_gross),0) total, COUNT(*) cnt
                                FROM purchase_orders
                                WHERE supplier_id = @sid AND order_date IS NOT NULL
                                GROUP BY YEAR(order_date), MONTH(order_date)
                                ORDER BY y DESC, m DESC"
                            : @"SELECT YEAR(order_date) y, MONTH(order_date) m,
                                       COALESCE(SUM(total_gross),0) total, COUNT(*) cnt
                                FROM purchase_orders
                                WHERE order_date IS NOT NULL
                                GROUP BY YEAR(order_date), MONTH(order_date)
                                ORDER BY y DESC, m DESC";
                        using var cmd = new MySqlCommand(sql, conn);
                        if (supplierId.HasValue) cmd.Parameters.AddWithValue("@sid", supplierId.Value);
                        using var r = await cmd.ExecuteReaderAsync();
                        while (await r.ReadAsync())
                        {
                            list.Add(new MonthlyExpense
                            {
                                Year = r.GetInt32(0),
                                Month = r.GetInt32(1),
                                TotalAmount = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2)),
                                TransactionCount = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3)),
                                Currency = "EUR",
                                LastUpdated = DateTime.Now
                            });
                        }
                    }
                    catch
                    {
                        // purchase_orders existiert evtl. nicht
                    }
                }
            }

            return list;
        }

        public async Task<List<ExpenseCategory>> GetExpenseCategoriesAsync()
        {
            var list = new List<ExpenseCategory>();
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureExpenseCategoriesTableAsync(conn);

            using var cmd = new MySqlCommand(
                "SELECT * FROM expense_categories WHERE is_active = 1 ORDER BY display_order, name", conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                int o(string n) => r.GetOrdinal(n);
                list.Add(new ExpenseCategory
                {
                    Id = r.GetInt32(o("id")),
                    Name = r.IsDBNull(o("name")) ? "" : r.GetString(o("name")),
                    Description = r.IsDBNull(o("description")) ? null : r.GetString(o("description")),
                    ParentCategoryId = r.IsDBNull(o("parent_category_id")) ? null : r.GetInt32(o("parent_category_id")),
                    Icon = r.IsDBNull(o("icon")) ? null : r.GetString(o("icon")),
                    Color = r.IsDBNull(o("color")) ? null : r.GetString(o("color")),
                    BudgetLimit = r.IsDBNull(o("budget_limit")) ? null : r.GetDecimal(o("budget_limit")),
                    IsActive = !r.IsDBNull(o("is_active")) && r.GetBoolean(o("is_active")),
                    DisplayOrder = r.GetInt32(o("display_order")),
                    CreatedAt = r.IsDBNull(o("created_at")) ? DateTime.Now : r.GetDateTime(o("created_at"))
                });
            }
            return list;
        }

        public async Task SaveMonthlyExpenseAsync(MonthlyExpense expense)
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            await EnsureMonthlyExpensesTableAsync(conn);

            const string sql = @"INSERT INTO monthly_expenses
                (year, month, category_id, total_amount, transaction_count, budget_amount, variance, currency, last_updated)
                VALUES (@y, @m, @cid, @ta, @tc, @ba, @v, @cur, @lu)
                ON DUPLICATE KEY UPDATE
                    total_amount = @ta, transaction_count = @tc, budget_amount = @ba,
                    variance = @v, currency = @cur, last_updated = @lu;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@y", expense.Year);
            cmd.Parameters.AddWithValue("@m", expense.Month);
            cmd.Parameters.AddWithValue("@cid", (object?)expense.CategoryId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ta", expense.TotalAmount);
            cmd.Parameters.AddWithValue("@tc", expense.TransactionCount);
            cmd.Parameters.AddWithValue("@ba", (object?)expense.BudgetAmount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v", (object?)expense.Variance ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cur", expense.Currency ?? "EUR");
            cmd.Parameters.AddWithValue("@lu", DateTime.Now);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
