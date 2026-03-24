# Ticketsystem - Anleitung

## Übersicht

Das Ticketsystem ermöglicht es Kunden, Support-Anfragen über ein Webformular zu erstellen. Die Tickets werden in der Datenbank gespeichert und können von Mitarbeitern verwaltet werden.

## Komponenten

### 1. Ticket-Modell (`Models\Ticket.cs`)
- Enthält alle Ticket-Informationen
- Status: Neu, In Bearbeitung, Warten auf Rückmeldung, Gelöst, Geschlossen
- Priorität: Niedrig, Mittel, Hoch, Dringend
- Kategorien: Allgemein, Technisch, Abrechnung, Feature-Anfrage, Fehler

### 2. Datenbank
Die Tickets-Tabelle wird automatisch bei der Datenbankinitialisierung erstellt mit folgenden Feldern:
- Kundendaten (Name, E-Mail, Telefon)
- Ticket-Informationen (Betreff, Beschreibung, Kategorie, Priorität, Status)
- Zuweisung an Mitarbeiter
- Lösungsinformationen
- Zeitstempel

### 3. Kundenformular

#### Option A: Integriertes Formular (`Views\TicketFormWindow.xaml`)
- Einfaches Formular zur Ticket-Erstellung
- Kann von innerhalb der Anwendung aufgerufen werden

#### Option B: Öffentliches Formular (`Views\PublicTicketFormApp.xaml`)
- Professionelles, eigenständiges Formular
- Kann als separate Anwendung für Kunden bereitgestellt werden
- Optimiertes Design mit Branding
- Ausführliche Validierung

### 4. Verwaltungsdialog (`Views\TicketManagementDialog.xaml`)
- Übersicht aller Tickets
- Filter nach Status
- Tickets bearbeiten, zuweisen, löschen
- Statusübersicht

### 5. Bearbeitungsdialog (`Views\TicketEditDialog.xaml`)
- Status ändern
- Priorität anpassen
- Ticket Mitarbeitern zuweisen
- Lösungen/Kommentare hinzufügen
- Automatische Zeitstempel bei Statusänderungen

## Verwendung

### Für Administratoren

1. **Ticketverwaltung öffnen**
   - Menü → Einstellungen → Support-Tickets
   - Oder über das Dashboard (wenn implementiert)

2. **Tickets verwalten**
   - Neue Tickets anzeigen (rot markiert bei hoher Priorität)
   - Tickets Mitarbeitern zuweisen
   - Status aktualisieren
   - Lösungen dokumentieren

3. **Tickets filtern**
   - Dropdown-Menü zur Filterung nach Status
   - Alle Tickets / Neu / In Bearbeitung / Gelöst / Geschlossen

### Für Kunden

#### Variante 1: Innerhalb der Anwendung
```csharp
var dialog = new TicketFormWindow();
if (dialog.ShowDialog() == true)
{
    // Ticket wurde erstellt
}
```

#### Variante 2: Eigenständiges Formular
```csharp
// In einer separaten Anwendung oder als Startup-Projekt
var app = new PublicTicketFormApp();
app.ShowDialog();
```

## Webformular-Integration (für die Zukunft)

Das System ist vorbereitet für eine Webformular-Integration. Dazu können Sie:

### Option 1: ASP.NET Core Web API
Erstellen Sie einen API-Endpunkt, der Tickets entgegennimmt:

```csharp
[HttpPost("api/tickets")]
public async Task<IActionResult> CreateTicket([FromBody] TicketDto ticketDto)
{
    var ticket = new Ticket
    {
        CustomerName = ticketDto.CustomerName,
        CustomerEmail = ticketDto.CustomerEmail,
        CustomerPhone = ticketDto.CustomerPhone,
        Subject = ticketDto.Subject,
        Description = ticketDto.Description,
        Category = ticketDto.Category,
        Priority = ticketDto.Priority,
        Status = TicketStatus.New,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
        CreatedAt = DateTime.Now
    };

    var db = new DatabaseService();
    ticket.Id = await db.AddTicketAsync(ticket);
    
    return Ok(new { ticketNumber = ticket.TicketNumber });
}
```

### Option 2: HTML-Formular
Erstellen Sie ein HTML-Formular, das Daten an die API sendet:

```html
<form id="ticketForm">
    <input type="text" name="customerName" required>
    <input type="email" name="customerEmail" required>
    <input type="tel" name="customerPhone">
    <select name="category">
        <option value="0">Allgemein</option>
        <option value="1">Technisch</option>
        <!-- ... -->
    </select>
    <select name="priority">
        <option value="1">Mittel</option>
        <option value="2">Hoch</option>
        <!-- ... -->
    </select>
    <input type="text" name="subject" required>
    <textarea name="description" required></textarea>
    <button type="submit">Absenden</button>
</form>

<script>
document.getElementById('ticketForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const formData = new FormData(e.target);
    const data = Object.fromEntries(formData);
    
    const response = await fetch('/api/tickets', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    });
    
    if (response.ok) {
        const result = await response.json();
        alert('Ticket erstellt: ' + result.ticketNumber);
    }
});
</script>
```

## E-Mail-Benachrichtigungen (optional)

Sie können das System erweitern, um automatische E-Mails zu senden:

1. Bei Ticket-Erstellung → Bestätigung an Kunden
2. Bei Status-Änderung → Update an Kunden
3. Bei Zuweisung → Benachrichtigung an Mitarbeiter

Beispiel-Code:
```csharp
public async Task SendTicketConfirmation(Ticket ticket)
{
    var smtpClient = new SmtpClient("smtp.ihr-server.de")
    {
        Port = 587,
        Credentials = new NetworkCredential("user", "password"),
        EnableSsl = true,
    };

    var mailMessage = new MailMessage
    {
        From = new MailAddress("support@ihre-firma.de"),
        Subject = $"Ticket {ticket.TicketNumber} - Bestätigung",
        Body = $"Vielen Dank für Ihre Anfrage...",
        IsBodyHtml = true,
    };
    
    mailMessage.To.Add(ticket.CustomerEmail);
    await smtpClient.SendMailAsync(mailMessage);
}
```

## Datenbankschema

Die Tickets-Tabelle:
```sql
CREATE TABLE tickets (
    id INT AUTO_INCREMENT PRIMARY KEY,
    customer_name VARCHAR(255) NOT NULL,
    customer_email VARCHAR(255) NOT NULL,
    customer_phone VARCHAR(50),
    customer_id INT,
    subject VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    priority INT DEFAULT 1,
    status INT DEFAULT 0,
    category INT DEFAULT 0,
    ip_address VARCHAR(45),
    user_agent VARCHAR(500),
    assigned_to_employee_id INT,
    resolution TEXT,
    resolved_at DATETIME,
    created_at DATETIME,
    updated_at DATETIME,
    FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE SET NULL,
    FOREIGN KEY (assigned_to_employee_id) REFERENCES employees(id) ON DELETE SET NULL
);
```

## Best Practices

1. **Regelmäßige Überprüfung**: Prüfen Sie täglich neue Tickets
2. **Schnelle Reaktion**: Antworten Sie innerhalb von 24 Stunden
3. **Kategorisierung**: Nutzen Sie Kategorien für bessere Organisation
4. **Dokumentation**: Dokumentieren Sie Lösungen für wiederkehrende Probleme
5. **Feedback**: Fragen Sie Kunden nach Zufriedenheit (kann später implementiert werden)

## Support

Bei Fragen zum Ticketsystem wenden Sie sich an den System-Administrator.
