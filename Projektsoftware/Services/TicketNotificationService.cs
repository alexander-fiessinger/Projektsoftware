using Projektsoftware.Models;
using System;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Sendet automatische E-Mail-Benachrichtigungen an Kunden bei Ticket-Änderungen.
    /// </summary>
    public static class TicketNotificationService
    {
        /// <summary>
        /// Sendet eine Benachrichtigung wenn sich Status oder Lösung geändert haben.
        /// Wird still im Hintergrund ausgeführt – Fehler werden nur geloggt.
        /// </summary>
        public static async Task SendUpdateNotificationAsync(
            Ticket updatedTicket,
            TicketStatus previousStatus,
            string previousResolution)
        {
            try
            {
                var smtpConfig = TicketSmtpConfig.Load();
                if (!smtpConfig.IsConfigured) return;

                bool statusChanged = updatedTicket.Status != previousStatus;
                bool resolutionChanged = !string.IsNullOrWhiteSpace(updatedTicket.Resolution)
                    && updatedTicket.Resolution != (previousResolution ?? string.Empty);

                if (!statusChanged && !resolutionChanged) return;

                var subject = $"Aktualisierung zu Ticket {updatedTicket.TicketNumber} – {updatedTicket.Subject}";
                var body = BuildEmailBody(updatedTicket, previousStatus, statusChanged, resolutionChanged);

                var emailService = new ExchangeEmailService(smtpConfig.ToExchangeConfig());
                await emailService.SendEmailAsync(updatedTicket.CustomerEmail, subject, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Ticket-Benachrichtigung konnte nicht gesendet werden: {ex.Message}");
            }
        }

        private static string BuildEmailBody(
            Ticket ticket,
            TicketStatus previousStatus,
            bool statusChanged,
            bool resolutionChanged)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"Sehr geehrte/r {ticket.CustomerName},");
            sb.AppendLine();
            sb.AppendLine($"Ihr Support-Ticket wurde aktualisiert.");
            sb.AppendLine();
            sb.AppendLine("────────────────────────────────");
            sb.AppendLine($"Ticket-Nummer : {ticket.TicketNumber}");
            sb.AppendLine($"Betreff       : {ticket.Subject}");
            sb.AppendLine($"Priorität     : {ticket.PriorityText}");

            if (statusChanged)
            {
                sb.AppendLine($"Status        : {StatusLabel(previousStatus)} → {ticket.StatusText}");
            }
            else
            {
                sb.AppendLine($"Status        : {ticket.StatusText}");
            }

            sb.AppendLine("────────────────────────────────");
            sb.AppendLine();

            // Statusspezifische Nachricht
            if (statusChanged)
            {
                sb.AppendLine(StatusMessage(ticket.Status));
                sb.AppendLine();
            }

            // Lösung anhängen wenn vorhanden
            if (!string.IsNullOrWhiteSpace(ticket.Resolution))
            {
                sb.AppendLine("Lösung / Kommentar:");
                sb.AppendLine("────────────────────────────────");
                sb.AppendLine(ticket.Resolution);
                sb.AppendLine("────────────────────────────────");
                sb.AppendLine();
            }

            sb.AppendLine("Mit freundlichen Grüßen");
            sb.AppendLine("Ihr Support-Team");

            return sb.ToString();
        }

        private static string StatusLabel(TicketStatus status) => status switch
        {
            TicketStatus.New        => "Neu",
            TicketStatus.InProgress => "In Bearbeitung",
            TicketStatus.Waiting    => "Warten auf Rückmeldung",
            TicketStatus.Resolved   => "Gelöst",
            TicketStatus.Closed     => "Geschlossen",
            _                       => status.ToString()
        };

        private static string StatusMessage(TicketStatus status) => status switch
        {
            TicketStatus.InProgress =>
                "Wir haben Ihr Ticket aufgenommen und arbeiten nun aktiv daran.\n" +
                "Wir melden uns, sobald es Neuigkeiten gibt.",

            TicketStatus.Waiting =>
                "Wir benötigen weitere Informationen, um Ihr Anliegen vollständig bearbeiten zu können.\n" +
                "Bitte antworten Sie auf diese E-Mail oder kontaktieren Sie uns.",

            TicketStatus.Resolved =>
                "Ihr Ticket wurde als gelöst markiert. Wir hoffen, dass Ihr Anliegen " +
                "vollständig beantwortet wurde.\n" +
                "Falls Sie weitere Fragen haben, können Sie jederzeit ein neues Ticket erstellen.",

            TicketStatus.Closed =>
                "Ihr Ticket wurde geschlossen.\n" +
                "Vielen Dank, dass Sie unseren Support in Anspruch genommen haben.",

            _ => "Ihr Ticket wurde aktualisiert."
        };
    }
}
