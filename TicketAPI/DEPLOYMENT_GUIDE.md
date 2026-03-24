# Ticket-API & Web-Formular - Komplette Anleitung

## 🎯 Übersicht

Diese Lösung ermöglicht es Kunden, Support-Tickets über ein HTML-Formular auf Ihrer Website zu erstellen, **ohne dass die Desktop-Anwendung läuft**. Die Tickets werden direkt in der MySQL-Datenbank gespeichert.

### Komponenten:
1. **ASP.NET Core Web API** - REST-API zum Empfangen von Tickets
2. **HTML-Formular (Vollversion)** - Standalone-Seite
3. **HTML-Widget (Einbettung)** - Für bestehende Websites
4. **MySQL-Datenbank** - Verwendet die gleiche DB wie die Desktop-App

---

## 📁 Dateistruktur

```
TicketAPI/
├── Controllers/
│   └── TicketsController.cs      # REST-API Endpunkte
├── Models/
│   ├── Ticket.cs                  # Ticket-Datenmodell
│   └── TicketDto.cs               # Data Transfer Objects
├── Services/
│   └── DatabaseService.cs         # Datenbankzugriff
├── wwwroot/
│   ├── ticket-form.html           # Vollständiges Formular
│   └── ticket-widget-embed.html   # Einbettbares Widget
├── appsettings.json               # Konfiguration
└── Program.cs                     # API-Startup
```

---

## 🚀 Installation & Setup

### Schritt 1: Datenbankverbindung konfigurieren

Öffnen Sie `TicketAPI/appsettings.json` und passen Sie die Datenbankverbindung an:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=IHR_SERVER;Database=IHR_DATENBANKNAME;Uid=IHR_USER;Pwd=IHR_PASSWORT;"
  }
}
```

**Beispiele:**
- Lokal: `Server=localhost;Database=projektdb;Uid=root;Pwd=;`
- Webserver: `Server=db.ihre-domain.de;Database=projektdb;Uid=dbuser;Pwd=sicheresPasswort123;`

### Schritt 2: API starten

```bash
cd TicketAPI
dotnet run
```

Die API läuft standardmäßig auf:
- **HTTP**: http://localhost:5000
- **HTTPS**: https://localhost:7001

### Schritt 3: API testen

Öffnen Sie im Browser:
```
https://localhost:7001/health
```

Sie sollten sehen:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00",
  "service": "Ticket API"
}
```

---

## 🌐 HTML-Formular einbinden

### Option A: Vollständige Seite (ticket-form.html)

1. Datei `TicketAPI/wwwroot/ticket-form.html` öffnen
2. API-URL anpassen (Zeile 230):
   ```javascript
   const API_URL = 'https://ihre-domain.de/api/tickets'; // HIER ANPASSEN!
   ```
3. Datei auf Ihren Webserver hochladen
4. Aufrufen: `https://ihre-website.de/ticket-form.html`

### Option B: Widget einbetten (ticket-widget-embed.html)

Für **bestehende Websites** - Widget wird in Ihre Seite integriert:

1. Kopieren Sie den kompletten Inhalt von `ticket-widget-embed.html`
2. Fügen Sie ihn in Ihre bestehende HTML-Seite ein
3. Passen Sie die API-URL an (Zeile 212):
   ```javascript
   const TICKET_API_URL = 'https://ihre-domain.de/api/tickets';
   ```

**Beispiel-Integration:**

```html
<!DOCTYPE html>
<html>
<head>
    <title>Meine Website - Support</title>
    <!-- Ihr bestehendes CSS -->
</head>
<body>
    <header>
        <!-- Ihre Navigation -->
    </header>

    <main>
        <h1>Brauchen Sie Hilfe?</h1>
        
        <!-- HIER DAS TICKET-WIDGET EINFÜGEN -->
        <div id="ticket-widget-container">
            <!-- Inhalt von ticket-widget-embed.html hier einfügen -->
        </div>
    </main>

    <footer>
        <!-- Ihr Footer -->
    </footer>
</body>
</html>
```

---

## 🖥️ API auf Server deployen

### Variante 1: Windows Server (IIS)

1. **Projekt veröffentlichen:**
   ```bash
   cd TicketAPI
   dotnet publish -c Release -o ./publish
   ```

2. **IIS-Website erstellen:**
   - IIS Manager öffnen
   - Neue Website erstellen
   - Physikalischer Pfad: `C:\inetpub\wwwroot\ticketapi`
   - Publish-Ordner kopieren
   - Application Pool auf "No Managed Code" setzen

3. **ASP.NET Core Hosting Bundle installieren:**
   [Download](https://dotnet.microsoft.com/download/dotnet/10.0)

### Variante 2: Linux Server (mit Nginx)

1. **App veröffentlichen:**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Auf Server kopieren:**
   ```bash
   scp -r ./publish user@server:/var/www/ticketapi
   ```

3. **Systemd Service erstellen** (`/etc/systemd/system/ticketapi.service`):
   ```ini
   [Unit]
   Description=Ticket API

   [Service]
   WorkingDirectory=/var/www/ticketapi
   ExecStart=/usr/bin/dotnet /var/www/ticketapi/TicketAPI.dll
   Restart=always
   RestartSec=10
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production

   [Install]
   WantedBy=multi-user.target
   ```

4. **Nginx konfigurieren** (`/etc/nginx/sites-available/ticketapi`):
   ```nginx
   server {
       listen 80;
       server_name api.ihre-domain.de;

       location / {
           proxy_pass http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
       }
   }
   ```

5. **Service starten:**
   ```bash
   sudo systemctl enable ticketapi
   sudo systemctl start ticketapi
   sudo systemctl restart nginx
   ```

### Variante 3: Docker

**Dockerfile erstellen:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY ./publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "TicketAPI.dll"]
```

**Docker-Container starten:**
```bash
docker build -t ticketapi .
docker run -d -p 5000:80 --name ticketapi ticketapi
```

---

## 🔒 CORS & Sicherheit

### CORS konfigurieren

In `appsettings.json`:
```json
{
  "CorsSettings": {
    "AllowedOrigins": [
      "https://ihre-website.de",
      "https://www.ihre-website.de",
      "http://localhost:3000"
    ]
  }
}
```

### Empfohlene Sicherheitsmaßnahmen:

1. **HTTPS verwenden** (SSL-Zertifikat von Let's Encrypt)
2. **Rate Limiting** implementieren:
   ```bash
   dotnet add package AspNetCoreRateLimit
   ```

3. **Input Validation** - bereits implementiert in `CreateTicketDto`
4. **SQL Injection** - geschützt durch parametrisierte Queries
5. **API-Key** (optional):
   ```csharp
   // In TicketsController.cs
   [ServiceFilter(typeof(ApiKeyAuthFilter))]
   ```

---

## 📧 E-Mail-Benachrichtigungen (Optional)

### E-Mail-Service hinzufügen:

1. **NuGet-Paket installieren:**
   ```bash
   dotnet add package MailKit
   ```

2. **EmailService erstellen** (`Services/EmailService.cs`):
   ```csharp
   public class EmailService
   {
       private readonly IConfiguration _config;

       public EmailService(IConfiguration config)
       {
           _config = config;
       }

       public async Task SendTicketConfirmationAsync(Ticket ticket)
       {
           var smtp = new SmtpClient();
           await smtp.ConnectAsync(
               _config["EmailSettings:SmtpServer"],
               int.Parse(_config["EmailSettings:SmtpPort"]),
               SecureSocketOptions.StartTls
           );

           await smtp.AuthenticateAsync(
               _config["EmailSettings:SmtpUsername"],
               _config["EmailSettings:SmtpPassword"]
           );

           var message = new MimeMessage();
           message.From.Add(new MailboxAddress(
               _config["EmailSettings:FromName"],
               _config["EmailSettings:FromEmail"]
           ));
           message.To.Add(new MailboxAddress(
               ticket.CustomerName,
               ticket.CustomerEmail
           ));
           message.Subject = $"Ticket {ticket.TicketNumber} - Bestätigung";

           message.Body = new TextPart("html")
           {
               Text = $@"
                   <h2>Vielen Dank für Ihre Anfrage!</h2>
                   <p>Ihr Ticket wurde erfolgreich erstellt.</p>
                   <p><strong>Ticket-Nummer:</strong> {ticket.TicketNumber}</p>
                   <p><strong>Betreff:</strong> {ticket.Subject}</p>
                   <p>Wir werden uns schnellstmöglich bei Ihnen melden.</p>
               "
           };

           await smtp.SendAsync(message);
           await smtp.DisconnectAsync(true);
       }
   }
   ```

3. **In Controller verwenden:**
   ```csharp
   [HttpPost]
   public async Task<ActionResult> CreateTicket([FromBody] CreateTicketDto dto)
   {
       // ... Ticket erstellen ...
       
       await _emailService.SendTicketConfirmationAsync(ticket);
       
       return Ok(response);
   }
   ```

---

## 🧪 Testen

### Mit curl:
```bash
curl -X POST https://localhost:7001/api/tickets \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Max Mustermann",
    "customerEmail": "max@example.com",
    "customerPhone": "+49 123 456789",
    "subject": "Test-Ticket",
    "description": "Dies ist ein Test-Ticket für die API",
    "category": 0,
    "priority": 1
  }'
```

### Mit Postman:
1. Neue POST-Anfrage erstellen
2. URL: `https://localhost:7001/api/tickets`
3. Headers: `Content-Type: application/json`
4. Body (raw JSON):
   ```json
   {
     "customerName": "Test User",
     "customerEmail": "test@example.com",
     "subject": "API Test",
     "description": "Testing the ticket creation endpoint",
     "category": 0,
     "priority": 1
   }
   ```

---

## 📊 Monitoring & Logging

### Application Insights (Azure):
```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Serilog (File Logging):
```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
```

```csharp
builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.File("logs/ticketapi-.txt", rollingInterval: RollingInterval.Day);
});
```

---

## 🔧 Troubleshooting

### Problem: CORS-Fehler im Browser
**Lösung:** Überprüfen Sie `AllowedOrigins` in `appsettings.json`

### Problem: 500 Internal Server Error
**Lösung:** Logs prüfen:
```bash
dotnet run --verbosity detailed
```

### Problem: Datenbankverbindung schlägt fehl
**Lösung:** Connection String testen:
```bash
curl https://localhost:7001/api/tickets/health
```

### Problem: Tickets werden nicht gespeichert
**Lösung:** Überprüfen Sie, ob die `tickets`-Tabelle existiert:
```sql
SHOW TABLES LIKE 'tickets';
```

---

## 📱 Mobile-Optimierung

Das HTML-Formular ist bereits responsive. Für bessere Mobile-UX:

```css
@media (max-width: 480px) {
    .container {
        margin: 0;
        border-radius: 0;
    }
    
    .form-container {
        padding: 20px 15px;
    }
}
```

---

## 🚀 Performance-Tipps

1. **Connection Pooling** (bereits aktiv in MySQL.Data)
2. **Response Caching**:
   ```csharp
   [ResponseCache(Duration = 60)]
   public async Task<IActionResult> GetTicket(int id)
   ```
3. **Async/Await** - bereits implementiert
4. **CDN** für statische HTML-Dateien

---

## 📈 Statistiken & Reporting

### Ticket-Statistiken abrufen:

```csharp
[HttpGet("stats")]
public async Task<IActionResult> GetStats()
{
    // In DatabaseService implementieren
    var stats = await _databaseService.GetTicketStatsAsync();
    return Ok(stats);
}
```

---

## 🎓 Weitere Ressourcen

- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core)
- [MySQL .NET Connector](https://dev.mysql.com/doc/connector-net/en/)
- [CORS in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/cors)

---

## ✅ Checkliste für Production

- [ ] HTTPS konfiguriert (SSL-Zertifikat)
- [ ] CORS nur für Ihre Domain
- [ ] Datenbank-Passwort sicher
- [ ] API-URL in HTML-Formularen angepasst
- [ ] Logging aktiviert
- [ ] E-Mail-Benachrichtigungen getestet
- [ ] Backup-Strategie für Datenbank
- [ ] Rate Limiting implementiert
- [ ] Monitoring eingerichtet
- [ ] Fehlerbehandlung getestet

---

**🎉 Viel Erfolg mit Ihrer Ticket-API!**

Bei Fragen: support@ihre-domain.de
