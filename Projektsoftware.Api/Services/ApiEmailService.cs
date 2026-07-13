using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Globalization;
using System.Text;
using Projektsoftware.Api.Models;

namespace Projektsoftware.Api.Services;

/// <summary>
/// SMTP E-Mail-Service für den Versand von Dokumenten (Rechnungen, Angebote etc.)
/// Konfiguration aus appsettings (Smtp-Sektion).
/// </summary>
public class ApiEmailService
{
    private readonly string _server;
    private readonly int _port;
    private readonly bool _useSsl;
    private readonly string _email;
    private readonly string _password;
    private readonly string _senderName;

    public ApiEmailService(IConfiguration config)
    {
        var section = config.GetSection("Smtp");
        _server = section["Server"] ?? "";
        _port = int.TryParse(section["Port"], out var p) ? p : 587;
        _useSsl = bool.TryParse(section["UseSsl"], out var ssl) && ssl;
        _email = section["Email"] ?? "";
        _password = section["Password"] ?? "";
        _senderName = section["SenderName"] ?? "Projektsoftware";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_server) && !string.IsNullOrWhiteSpace(_email);

    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        string? pdfFileName = null,
        byte[]? pdfBytes = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_senderName, _email));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = body };

        if (pdfBytes != null && pdfFileName != null)
            bodyBuilder.Attachments.Add(pdfFileName, pdfBytes, new ContentType("application", "pdf"));

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        var secureOption = _useSsl
            ? SecureSocketOptions.SslOnConnect
            : (_port == 587 ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

        try
        {
            await client.ConnectAsync(_server, _port, secureOption);
        }
        catch
        {
            await client.ConnectAsync(_server, _port, SecureSocketOptions.Auto);
        }

        await client.AuthenticateAsync(_email, _password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        try
        {
            using var client = new SmtpClient();
            var secureOption = _useSsl
                ? SecureSocketOptions.SslOnConnect
                : (_port == 587 ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

            try
            {
                await client.ConnectAsync(_server, _port, secureOption);
            }
            catch
            {
                await client.ConnectAsync(_server, _port, SecureSocketOptions.Auto);
            }

            await client.AuthenticateAsync(_email, _password);
            await client.DisconnectAsync(true);
            return (true, "SMTP-Verbindung erfolgreich!");
        }
        catch (Exception ex)
        {
            return (false, $"Fehler: {ex.Message}");
        }
    }

    private static readonly CultureInfo De = new("de-DE");

    /// <summary>
    /// Versendet eine Termin-Einladung mit iCal-Anhang (text/calendar) an den Kontakt.
    /// </summary>
    public async Task SendAppointmentInvitationAsync(SalesAppointmentDto appt)
    {
        if (string.IsNullOrWhiteSpace(appt.ContactEmail))
            throw new InvalidOperationException("Keine Empfänger-E-Mail-Adresse angegeben.");

        var ical = BuildICal(appt, "REQUEST");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_senderName, _email));
        foreach (var addr in appt.ContactEmail.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            message.To.Add(MailboxAddress.Parse(addr));
        message.Subject = $"Einladung: {appt.Title}";

        var bodyBuilder = new BodyBuilder { HtmlBody = BuildHtmlBody(appt) };

        var icalPart = new TextPart("calendar")
        {
            Text = ical
        };
        icalPart.ContentType.Parameters.Add("method", "REQUEST");
        icalPart.ContentType.Parameters.Add("charset", "utf-8");
        icalPart.ContentTransferEncoding = ContentEncoding.Base64;

        var mixed = new Multipart("mixed");
        mixed.Add(bodyBuilder.ToMessageBody());
        mixed.Add(icalPart);
        message.Body = mixed;

        using var client = new SmtpClient();
        var secureOption = _useSsl
            ? SecureSocketOptions.SslOnConnect
            : (_port == 587 ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
        try { await client.ConnectAsync(_server, _port, secureOption); }
        catch { await client.ConnectAsync(_server, _port, SecureSocketOptions.Auto); }
        await client.AuthenticateAsync(_email, _password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private string BuildICal(SalesAppointmentDto a, string method)
    {
        var uid = string.IsNullOrEmpty(a.ICalUid) ? Guid.NewGuid().ToString() : a.ICalUid;
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//Projektsoftware//Sales Calendar//DE");
        sb.AppendLine($"METHOD:{method}");
        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine($"UID:{uid}");
        sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
        sb.AppendLine($"DTSTART:{a.AppointmentDate.ToUniversalTime():yyyyMMddTHHmmssZ}");
        sb.AppendLine($"DTEND:{a.AppointmentEnd.ToUniversalTime():yyyyMMddTHHmmssZ}");
        sb.AppendLine($"SUMMARY:{EscapeIcal(a.Title)}");

        var descParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(a.Notes)) descParts.Add(a.Notes);
        if (!string.IsNullOrWhiteSpace(a.WebexJoinLink)) descParts.Add($"Webex: {a.WebexJoinLink}");
        if (descParts.Count > 0)
            sb.AppendLine($"DESCRIPTION:{EscapeIcal(string.Join("\\n\\n", descParts))}");

        if (!string.IsNullOrWhiteSpace(a.Location))
            sb.AppendLine($"LOCATION:{EscapeIcal(a.Location)}");
        if (!string.IsNullOrWhiteSpace(a.WebexJoinLink))
            sb.AppendLine($"CONFERENCE;VALUE=URI;FEATURE=VIDEO:{a.WebexJoinLink}");

        sb.AppendLine($"ORGANIZER;CN={EscapeIcal(_senderName)}:mailto:{_email}");
        foreach (var addr in a.ContactEmail.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            sb.AppendLine($"ATTENDEE;CN={EscapeIcal(a.ContactName)};ROLE=REQ-PARTICIPANT;RSVP=TRUE:mailto:{addr}");

        if (method == "CANCEL") sb.AppendLine("STATUS:CANCELLED");
        sb.AppendLine("SEQUENCE:0");
        sb.AppendLine("END:VEVENT");
        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    private string BuildHtmlBody(SalesAppointmentDto a) =>
        $"""
        <p>Sehr geehrte/r {a.ContactName},</p>
        <p>wir laden Sie herzlich zu folgendem Termin ein:</p>
        <table style="border-collapse:collapse;font-family:sans-serif;font-size:14px">
          <tr><td style="padding:4px 12px;font-weight:bold">Betreff</td><td>{a.Title}</td></tr>
          <tr><td style="padding:4px 12px;font-weight:bold">Datum</td><td>{a.AppointmentDate.ToString("dddd, dd. MMMM yyyy", De)}</td></tr>
          <tr><td style="padding:4px 12px;font-weight:bold">Uhrzeit</td><td>{a.AppointmentDate.ToString("HH:mm", De)} – {a.AppointmentEnd.ToString("HH:mm", De)} Uhr</td></tr>
          {(string.IsNullOrWhiteSpace(a.Location) ? "" : $"<tr><td style='padding:4px 12px;font-weight:bold'>Ort</td><td>{a.Location}</td></tr>")}
          {(string.IsNullOrWhiteSpace(a.WebexJoinLink) ? "" : $"<tr><td style='padding:4px 12px;font-weight:bold'>Webex</td><td><a href='{a.WebexJoinLink}'>Meeting beitreten</a></td></tr>")}
        </table>
        {(string.IsNullOrWhiteSpace(a.Notes) ? "" : $"<p>{a.Notes}</p>")}
        <p>Bitte nehmen Sie die Einladung in Ihrem Kalender an oder ab.</p>
        <p>Mit freundlichen Grüßen<br/>{_senderName}</p>
        """;

    private static string EscapeIcal(string s) =>
        s.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");
}
