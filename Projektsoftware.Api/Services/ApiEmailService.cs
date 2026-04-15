using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

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
}
