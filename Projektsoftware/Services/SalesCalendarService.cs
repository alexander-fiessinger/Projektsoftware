using MimeKit;
using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Sendet iCal-Einladungen über EWS und liest RSVP-Antworten per Exchange Web Services (EWS).
    /// Verwendet immer sales@af-software-engineering.de als Absender/Organizer.
    /// </summary>
    public class SalesCalendarService
    {
        private const string SalesEmail = "sales@af-software-engineering.de";
        private const string SalesName  = "AF Software Engineering – Sales";

        private readonly WebexService _webex;

        // EwsService wird immer frisch mit der aktuellen Config aus der DB erzeugt
        private EwsService CreateEwsService() => new EwsService(EwsConfig.LoadSales());

        public SalesCalendarService()
        {
            _webex = new WebexService();
        }

        public bool IsConfigured      => EwsConfig.LoadSales().IsConfigured;
        public bool IsEwsConfigured   => EwsConfig.LoadSales().IsConfigured;
        public bool IsWebexConfigured => _webex.IsConfigured;

        /// <summary>
        /// Erstellt optional ein Webex-Meeting und legt dann den Termin als echten
        /// EWS-CalendarItem an (SendToAllAndSaveCopy). Exchange sendet dadurch einen
        /// IPM.Schedule.Meeting.Request, sodass Outlook RSVP-Buttons anzeigt und
        /// Antworten als IPM.Schedule.Meeting.Resp.* zurückkommen.
        /// </summary>
        /// <summary>
        /// Erstellt optional ein Webex-Meeting und legt dann den Termin als echten
        /// EWS-CalendarItem an. Gibt eine nicht-leere WebexWarning zurück, falls das
        /// Webex-Meeting angefordert wurde aber nicht erstellt werden konnte.
        /// </summary>
        public async Task<string?> SendInvitationAsync(SalesAppointment appointment, bool createWebexMeeting = false)
        {
            var ewsCfg = EwsConfig.LoadSales();
            if (!ewsCfg.IsConfigured || string.IsNullOrWhiteSpace(ewsCfg.EwsUrl))
                throw new InvalidOperationException(
                    "Die Sales-EWS-Konfiguration ist unvollständig.\n" +
                    "Bitte über den ⚙ EWS-Button die Zugangsdaten für das Sales-Postfach hinterlegen.");

            string? webexWarning = null;

            // 1) Optional Webex-Meeting erstellen (vor EWS, damit der Link im Einladungstext steht)
            if (createWebexMeeting)
            {
                if (!_webex.IsConfigured)
                {
                    webexWarning = "Webex ist nicht konfiguriert – kein Meeting-Link erstellt.";
                }
                else
                {
                    try
                    {
                        var (id, link) = await _webex.CreateSalesMeetingAsync(appointment);
                        appointment.WebexMeetingId = id;
                        appointment.WebexJoinLink  = link;
                    }
                    catch (Exception ex)
                    {
                        webexWarning = $"Webex Meeting konnte nicht erstellt werden:\n{ex.Message}";
                    }
                }
            }

            // 2) Termin als EWS CalendarItem anlegen → Exchange sendet Meeting-Request
            var ewsUid = await CreateEwsService().CreateCalendarItemAsync(appointment);
            appointment.ICalUid = ewsUid;

            return webexWarning;
        }

        public async Task SendCancellationAsync(SalesAppointment appointment)
        {
            if (string.IsNullOrEmpty(appointment.ICalUid)) return;

            if (!string.IsNullOrEmpty(appointment.WebexMeetingId) && _webex.IsConfigured)
                await _webex.DeleteSalesMeetingAsync(appointment.WebexMeetingId);

            // CalendarItem in Exchange löschen → Exchange sendet automatisch Meeting-Cancellation
            await CreateEwsService().CancelCalendarItemAsync(appointment.ICalUid);
        }

        // ── EWS RSVP-Polling ─────────────────────────────────────────────────

        /// <summary>
        /// Liest den Exchange-Posteingang per EWS und sucht nach Kalender-Antworten (METHOD:REPLY).
        /// Gibt eine Liste von (ICalUid, PARTSTAT, AbsenderEmail) zurück.
        /// </summary>
        public async Task<List<(string ICalUid, RsvpStatus Status, string Email)>> PollRsvpRepliesAsync()
            => (await PollRsvpRepliesWithDiagnosticsAsync()).Replies;

        public async Task<(List<(string ICalUid, RsvpStatus Status, string Email)> Replies, string Diagnostics)>
            PollRsvpRepliesWithDiagnosticsAsync()
        {
            var results = new List<(string, RsvpStatus, string)>();
            var diag    = new StringBuilder();

            var ewsCfg = EwsConfig.LoadSales();
            if (!ewsCfg.IsConfigured)
            {
                diag.AppendLine("⚠ Sales-EWS nicht konfiguriert.");
                return (results, diag.ToString());
            }

            diag.AppendLine($"EWS-URL: {ewsCfg.EwsUrl}");
            diag.AppendLine($"EWS-Email: {ewsCfg.EwsEmail}");

            try
            {
                var (responses, rawDiag) = await CreateEwsService().FetchMeetingResponsesWithDiagnosticsAsync();
                diag.Append(rawDiag);

                foreach (var (icalUid, responseType, fromEmail) in responses)
                {
                    var status = responseType.ToUpperInvariant() switch
                    {
                        "ACCEPTED"  => RsvpStatus.Accepted,
                        "ACCEPT"    => RsvpStatus.Accepted,
                        "DECLINED"  => RsvpStatus.Declined,
                        "DECLINE"   => RsvpStatus.Declined,
                        "TENTATIVE" => RsvpStatus.Tentative,
                        _           => RsvpStatus.Pending
                    };

                    if (status != RsvpStatus.Pending)
                        results.Add((icalUid, status, fromEmail));
                }
            }
            catch (Exception ex)
            {
                diag.AppendLine($"❌ Fehler: {ex.Message}");
                throw;
            }

            return (results, diag.ToString());
        }

        // ── iCal Builder ────────────────────────────────────────────────────

        private string BuildICalInvite(SalesAppointment a, string uid, string method)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//AF Software Engineering//Sales//DE");
            sb.AppendLine($"METHOD:{method}");
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{uid}");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTSTART:{a.AppointmentDate.ToUniversalTime():yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTEND:{a.AppointmentEnd.ToUniversalTime():yyyyMMddTHHmmssZ}");
            sb.AppendLine($"SUMMARY:{EscapeIcal(a.Title)}");

            // Location: Webex-Link bevorzugt wenn kein physischer Ort vorhanden
            var location = !string.IsNullOrWhiteSpace(a.Location)
                ? a.Location
                : (a.WebexJoinLink ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(location))
                sb.AppendLine($"LOCATION:{EscapeIcal(location)}");

            // Description: Notizen + Webex-Link zusammenführen
            var descParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(a.Notes))
                descParts.Add(EscapeIcal(a.Notes));
            if (!string.IsNullOrWhiteSpace(a.WebexJoinLink))
                descParts.Add($"Webex-Meeting beitreten:\\n{EscapeIcal(a.WebexJoinLink)}");
            if (descParts.Any())
                sb.AppendLine($"DESCRIPTION:{string.Join("\\n\\n", descParts)}");

            // Webex als CONFERENCE-Property (RFC 7986 – wird von modernen Clients erkannt)
            if (!string.IsNullOrWhiteSpace(a.WebexJoinLink))
                sb.AppendLine($"CONFERENCE;VALUE=URI;FEATURE=VIDEO:{a.WebexJoinLink}");
            sb.AppendLine($"ORGANIZER;CN={EscapeIcal(SalesName)}:mailto:{SalesEmail}");
            foreach (var addr in a.ContactEmail.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                sb.AppendLine($"ATTENDEE;CN={EscapeIcal(a.ContactName)};ROLE=REQ-PARTICIPANT;RSVP=TRUE:mailto:{addr}");
            if (method == "CANCEL")
                sb.AppendLine("STATUS:CANCELLED");
            sb.AppendLine("SEQUENCE:0");
            sb.AppendLine("END:VEVENT");
            sb.AppendLine("END:VCALENDAR");
            return sb.ToString();
        }

        private static readonly CultureInfo De = new("de-DE");

        private static string BuildHtmlBody(SalesAppointment a) =>
            $"""
            <p>Sehr geehrte/r {a.ContactName},</p>
            <p>wir laden Sie herzlich zu folgendem Termin ein:</p>
            <table style="border-collapse:collapse;font-family:sans-serif;font-size:14px">
              <tr><td style="padding:4px 12px;font-weight:bold">Betreff</td><td>{a.Title}</td></tr>
              <tr><td style="padding:4px 12px;font-weight:bold">Datum</td><td>{a.AppointmentDate.ToString("dddd, dd. MMMM yyyy", De)}</td></tr>
              <tr><td style="padding:4px 12px;font-weight:bold">Uhrzeit</td><td>{a.AppointmentDate.ToString("HH:mm", De)} – {a.AppointmentEnd.ToString("HH:mm", De)} Uhr</td></tr>
              {(string.IsNullOrWhiteSpace(a.Location) ? "" : $"<tr><td style='padding:4px 12px;font-weight:bold'>Ort</td><td>{a.Location}</td></tr>")}
              {(string.IsNullOrWhiteSpace(a.WebexJoinLink) ? "" : $"<tr><td style='padding:4px 12px;font-weight:bold'>Webex</td><td><a href='{a.WebexJoinLink}'>Meeting beitreten</a><br/><small>{a.WebexJoinLink}</small></td></tr>")}
            </table>
            {(string.IsNullOrWhiteSpace(a.Notes) ? "" : $"<p>{a.Notes}</p>")}
            <p>Bitte nehmen Sie die Einladung in Ihrem Kalender an oder ab.</p>
            <p>Mit freundlichen Grüßen<br/>{SalesName}</p>
            """;

        private static string BuildTextBody(SalesAppointment a) =>
            $"Einladung: {a.Title}\r\n" +
            $"Datum: {a.AppointmentDate:dd.MM.yyyy HH:mm} – {a.AppointmentEnd:HH:mm}\r\n" +
            (string.IsNullOrWhiteSpace(a.Location) ? "" : $"Ort: {a.Location}\r\n") +
            "\r\nBitte nehmen Sie die Einladung in Ihrem Kalender an oder ab.\r\n" +
            $"\r\n{SalesName}";

        private static string EscapeIcal(string s) =>
            s.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");

        private static string ExtractICalField(string ical, string fieldName)
        {
            foreach (var line in ical.Split('\n'))
            {
                var trimmed = line.TrimEnd('\r');
                if (trimmed.StartsWith(fieldName + ":", StringComparison.OrdinalIgnoreCase))
                    return trimmed[(fieldName.Length + 1)..].Trim();
                if (trimmed.Contains($";{fieldName}=", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Contains($":{fieldName}=", StringComparison.OrdinalIgnoreCase))
                {
                    var idx = trimmed.IndexOf($"{fieldName}=", StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        var val = trimmed[(idx + fieldName.Length + 1)..];
                        var end = val.IndexOfAny(new[] { ';', ':' });
                        return end >= 0 ? val[..end] : val;
                    }
                }
            }
            return "";
        }
    }
}

