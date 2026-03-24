# 🚀 Quick Start - Ticket Web-Formular

## In 5 Minuten zum laufenden System!

### ✅ Schritt 1: API-Konfiguration (30 Sekunden)

Öffnen Sie `TicketAPI/appsettings.json` und passen Sie die Datenbankverbindung an:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=projektdb;Uid=root;Pwd=;"
  }
}
```

**Wichtig:** Verwenden Sie die **gleichen Zugangsdaten** wie in Ihrer Desktop-Anwendung!

---

### ✅ Schritt 2: API starten (1 Minute)

Öffnen Sie ein Terminal im Projektordner:

```bash
cd TicketAPI
dotnet run
```

Sie sehen:
```
🚀 Ticket API gestartet!
📋 Swagger UI: https://localhost:7001/swagger
🔗 API Endpoint: https://localhost:7001/api/tickets
💚 Health Check: https://localhost:7001/health
```

**Testen Sie:** Öffnen Sie `https://localhost:7001/health` im Browser

---

### ✅ Schritt 3: Formular testen (2 Minuten)

#### Option A: Lokaler Test
Öffnen Sie `TicketAPI/wwwroot/ticket-form.html` direkt im Browser

#### Option B: Auf Website einbinden

1. Öffnen Sie `TicketAPI/wwwroot/ticket-widget-embed.html`
2. Kopieren Sie den gesamten Inhalt
3. Fügen Sie ihn in Ihre HTML-Seite ein
4. **WICHTIG:** Ändern Sie die API-URL (Zeile 212):
   ```javascript
   const TICKET_API_URL = 'https://localhost:7001/api/tickets';
   ```

---

### ✅ Schritt 4: Erstes Test-Ticket erstellen

1. Öffnen Sie das Formular im Browser
2. Füllen Sie alle Felder aus:
   - Name: **Max Mustermann**
   - E-Mail: **test@example.com**
   - Betreff: **Test-Ticket**
   - Beschreibung: **Dies ist ein Test meines Ticket-Systems**
3. Klicken Sie auf **"Anfrage absenden"**

**Erfolg!** Sie sehen:
```
✅ Vielen Dank!
Ihr Support-Ticket wurde erfolgreich erstellt.
Ticket-Nummer: #000001
```

---

### ✅ Schritt 5: Ticket in Desktop-App prüfen

1. Starten Sie Ihre **Projektsoftware**
2. Gehen Sie zu: **Einstellungen → Support-Tickets**
3. Das Test-Ticket erscheint in der Liste! 🎉

---

## 🌐 Auf Website einbinden (Produktiv)

### Für Ihren Webserver:

1. **API deployen:**
   ```bash
   cd TicketAPI
   dotnet publish -c Release -o ./publish
   ```
   
2. **Publish-Ordner auf Server hochladen** (z.B. via FTP)

3. **IIS/Nginx konfigurieren** (siehe DEPLOYMENT_GUIDE.md)

4. **HTTPS aktivieren** (Let's Encrypt empfohlen)

5. **In Formular API-URL anpassen:**
   ```javascript
   const API_URL = 'https://ihre-domain.de/api/tickets';
   ```

---

## 🔥 Häufige Probleme & Lösungen

### ❌ "CORS policy error" im Browser
**Lösung:** Fügen Sie Ihre Website-URL zu `appsettings.json` hinzu:
```json
"CorsSettings": {
  "AllowedOrigins": [
    "https://ihre-website.de"
  ]
}
```

### ❌ "Connection failed" beim Ticket-Absenden
**Lösung:** 
1. Prüfen Sie, ob die API läuft: `https://localhost:7001/health`
2. Überprüfen Sie die API-URL im HTML-Formular
3. Deaktivieren Sie temporär HTTPS-Weiterleitung in `Program.cs`

### ❌ "500 Internal Server Error"
**Lösung:** Prüfen Sie die Datenbankverbindung:
```bash
dotnet run --verbosity detailed
```

### ❌ Tickets werden nicht gespeichert
**Lösung:** Stellen Sie sicher, dass die `tickets`-Tabelle existiert:
- Starten Sie einmal die Desktop-Anwendung
- Die Tabelle wird automatisch erstellt

---

## 📱 Testen mit verschiedenen Geräten

**Desktop:**
```
https://localhost:7001/wwwroot/ticket-form.html
```

**Smartphone:**
1. Finden Sie Ihre lokale IP: `ipconfig` (Windows) oder `ifconfig` (Linux/Mac)
2. Öffnen Sie auf dem Handy: `https://192.168.1.X:7001/wwwroot/ticket-form.html`
3. **Hinweis:** HTTPS-Warnung akzeptieren (Entwicklungszertifikat)

---

## 🎯 Nächste Schritte

1. ✅ **Formular-Design anpassen** (CSS in `ticket-form.html`)
2. ✅ **E-Mail-Benachrichtigungen** aktivieren (siehe DEPLOYMENT_GUIDE.md)
3. ✅ **Auf Production-Server deployen**
4. ✅ **Google Analytics** einbinden (optional)
5. ✅ **Automatische Antworten** einrichten

---

## 💡 Pro-Tipps

### Tipp 1: Formular in Ihre Website integrieren
Verwenden Sie `ticket-widget-embed.html` statt einer separaten Seite!

### Tipp 2: API als Windows-Dienst ausführen
```bash
dotnet publish -c Release
sc create TicketAPI binPath="C:\path\to\TicketAPI.exe"
sc start TicketAPI
```

### Tipp 3: Mehrere Sprachen
Duplizieren Sie `ticket-form.html` und übersetzen Sie die Texte:
- `ticket-form-de.html` (Deutsch)
- `ticket-form-en.html` (English)
- `ticket-form-fr.html` (Français)

---

## 📞 Support

**Fragen?** Schauen Sie in:
- `DEPLOYMENT_GUIDE.md` - Ausführliche Anleitung
- `TICKETSYSTEM_README.md` - Desktop-App Dokumentation

---

**🎉 Fertig! Ihr Ticket-System ist einsatzbereit!**

Die API läuft unabhängig von der Desktop-Anwendung und speichert alle Tickets direkt in der Datenbank.
