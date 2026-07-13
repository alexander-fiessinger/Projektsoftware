using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Service für SLA-Überwachung und Ticketeskalation
    /// </summary>
    public class SlaMonitoringService
    {
        private readonly DatabaseService _databaseService;

        public SlaMonitoringService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Erstellt SLA-Status für neues Ticket
        /// </summary>
        public async Task<TicketSlaStatus?> CreateSlaStatusForTicketAsync(Ticket ticket)
        {
            try
            {
                // Berechne SLA-Zeiten basierend auf Priorität
                var (firstResponseHours, resolutionHours) = GetSlaHoursForPriority(ticket.Priority);

                var slaStatus = new TicketSlaStatus
                {
                    TicketId = ticket.Id,
                    SlaRuleId = 1,
                    FirstResponseDue = DateTime.Now.AddHours(firstResponseHours),
                    ResolutionDue = DateTime.Now.AddHours(resolutionHours),
                    IsBreached = false,
                    EscalationLevel = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _databaseService.SaveTicketSlaStatusAsync(slaStatus);
                return slaStatus;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Erstellen von SLA-Status: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Berechnet SLA-Stunden basierend auf Ticket-Priorität
        /// </summary>
        private (int firstResponse, int resolution) GetSlaHoursForPriority(TicketPriority priority)
        {
            return priority switch
            {
                TicketPriority.Urgent => (1, 4),
                TicketPriority.High => (2, 8),
                TicketPriority.Medium => (4, 24),
                TicketPriority.Low => (8, 72),
                _ => (4, 24)
            };
        }

        /// <summary>
        /// Prüft alle offenen Tickets auf SLA-Verletzungen
        /// </summary>
        public async Task MonitorAllTicketsAsync()
        {
            try
            {
                var tickets = await _databaseService.GetAllTicketsAsync();
                var openTickets = tickets.Where(t =>
                    t.Status != TicketStatus.Resolved &&
                    t.Status != TicketStatus.Closed).ToList();

                foreach (var ticket in openTickets)
                {
                    var slaStatus = await _databaseService.GetTicketSlaStatusAsync(ticket.Id);

                    if (slaStatus == null)
                    {
                        await CreateSlaStatusForTicketAsync(ticket);
                    }
                    else if (slaStatus.ResolutionDue.HasValue && DateTime.Now > slaStatus.ResolutionDue.Value && !slaStatus.IsBreached)
                    {
                        slaStatus.IsBreached = true;
                        slaStatus.EscalationLevel = Math.Min(3, slaStatus.EscalationLevel + 1);
                        slaStatus.UpdatedAt = DateTime.Now;
                        await _databaseService.SaveTicketSlaStatusAsync(slaStatus);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim SLA-Monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Gibt verletzte Tickets zurück
        /// </summary>
        public async Task<List<Ticket>> GetBreachedTicketsAsync()
        {
            var breached = new List<Ticket>();

            try
            {
                var tickets = await _databaseService.GetAllTicketsAsync();
                var openTickets = tickets.Where(t =>
                    t.Status != TicketStatus.Resolved &&
                    t.Status != TicketStatus.Closed).ToList();

                foreach (var ticket in openTickets)
                {
                    var slaStatus = await _databaseService.GetTicketSlaStatusAsync(ticket.Id);
                    if (slaStatus != null && (slaStatus.IsBreached || (slaStatus.ResolutionDue.HasValue && DateTime.Now > slaStatus.ResolutionDue.Value)))
                    {
                        breached.Add(ticket);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Abrufen verletzer SLAs: {ex.Message}");
            }

            return breached;
        }

        /// <summary>
        /// Gibt SLA-Zusammenfassung zurück
        /// </summary>
        public async Task<SlaSummary> GetSlaSummaryAsync()
        {
            var summary = new SlaSummary();

            try
            {
                var tickets = await _databaseService.GetAllTicketsAsync();
                var openTickets = tickets.Where(t =>
                    t.Status != TicketStatus.Resolved &&
                    t.Status != TicketStatus.Closed).ToList();

                summary.TotalTickets = openTickets.Count;

                foreach (var ticket in openTickets)
                {
                    var slaStatus = await _databaseService.GetTicketSlaStatusAsync(ticket.Id);

                    if (slaStatus != null)
                    {
                        if (slaStatus.IsBreached || (slaStatus.ResolutionDue.HasValue && DateTime.Now > slaStatus.ResolutionDue.Value))
                        {
                            summary.BreachedTickets++;
                        }
                        else if (slaStatus.ResolutionDue.HasValue && DateTime.Now > slaStatus.ResolutionDue.Value.AddHours(-1))
                        {
                            summary.WarningTickets++;
                        }
                        else
                        {
                            summary.HealthyTickets++;
                        }
                    }
                    else
                    {
                        summary.HealthyTickets++;
                    }
                }

                summary.BreachPercentage = summary.TotalTickets > 0
                    ? (decimal)summary.BreachedTickets / summary.TotalTickets * 100
                    : 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei SLA-Zusammenfassung: {ex.Message}");
            }

            return summary;
        }

        public class SlaSummary
        {
            public int TotalTickets { get; set; }
            public int HealthyTickets { get; set; }
            public int WarningTickets { get; set; }
            public int BreachedTickets { get; set; }
            public decimal BreachPercentage { get; set; }
        }
    }
}
