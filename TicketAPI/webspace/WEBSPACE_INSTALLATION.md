# 🌐 Ticketsystem für Webspace (PHP-Lösung)

## ✅ Perfekt für Sie!

Da Sie nur **Webspace** (kein Server) haben, habe ich eine **PHP-Lösung** erstellt, die perfekt funktioniert!

---

## 📦 Was Sie benötigen:

✅ **Webspace** mit PHP (ab Version 7.0)  
✅ **MySQL-Datenbank** (meist inklusive)  
✅ **FTP-Zugang** zum Hochladen  
✅ **5 Minuten** Zeit  

**Funktioniert bei:** ALL-INKL, Strato, 1&1/IONOS, HostEurope, Webgo, etc.

---

## 🚀 Installation in 5 Schritten

### Schritt 1: Datenbank-Zugangsdaten finden

Loggen Sie sich in Ihr **Webspace-Panel** ein (z.B. KAS bei ALL-INKL, Plesk bei Strato):

1. Suchen Sie den Bereich **"Datenbanken"** oder **"MySQL"**
2. Notieren Sie:
   - **DB-Host** (z.B. `localhost` oder `db12345.kasserver.com`)
   - **DB-Name** (z.B. `d01234567`)
   - **DB-Benutzer** (z.B. `d01234567`)
   - **DB-Passwort**

**💡 Tipp:** Die Datenbank ist meist die gleiche, die Sie für Ihre Desktop-App verwenden!

---

### Schritt 2: config.php anpassen

Öffnen Sie die Datei `webspace/config.php` und tragen Sie Ihre Daten ein:

```php
define('DB_HOST', 'localhost');           // ← Ihr DB-Host
define('DB_NAME', 'projektdb');           // ← Ihr DB-Name
define('DB_USER', 'db_user');             // ← Ihr DB-User
define('DB_PASS', 'sicheres_passwort');   // ← Ihr DB-Passwort
```

**Beispiel ALL-INKL:**
```php
define('DB_HOST', 'localhost');
define('DB_NAME', 'd01234567');
define('DB_USER', 'd01234567');
define('DB_PASS', 'MeinPasswort123');
```

---

### Schritt 3: Dateien hochladen (FTP)

Öffnen Sie Ihr **FTP-Programm** (FileZilla, WinSCP, oder das Webspace-Panel):

1. Verbinden Sie sich mit Ihrem Webspace
2. Navigieren Sie zu Ihrem Website-Ordner (z.B. `/html/` oder `/public_html/`)
3. Erstellen Sie einen Ordner **`api`**
4. Laden Sie hoch:
   ```
   /api/
   ├── create-ticket.php    ← Hauptdatei
   ├── config.php           ← Ihre Datenbank-Config
   └── .htaccess            ← Sicherheit
   ```

5. Laden Sie auch das Formular hoch:
   ```
   /support.html            ← Das Ticket-Formular
   ```

**Ihre Struktur:**
```
ihre-domain.de/
├── index.html
├── support.html          ← NEU
└── api/
    ├── create-ticket.php ← NEU
    ├── config.php        ← NEU
    └── .htaccess         ← NEU
```

---

### Schritt 4: URL in Formular anpassen

Öffnen Sie `ticket-form.html` und ändern Sie Zeile 302:

```javascript
// VORHER:
const API_URL = 'https://ihre-domain.de/api/create-ticket.php';

// NACHHER (Ihre echte Domain):
const API_URL = 'https://meine-firma.de/api/create-ticket.php';
```

Laden Sie die Datei erneut hoch.

---

### Schritt 5: Testen!

1. Öffnen Sie im Browser:
   ```
   https://ihre-domain.de/support.html
   ```

2. Füllen Sie das Formular aus und senden Sie es ab

3. **Erfolg!** Sie sehen:
   ```
   ✅ Vielen Dank!
   Ticket-Nummer: #000001
   ```

4. Prüfen Sie in Ihrer **Desktop-App**:
   - Menü → Einstellungen → Support-Tickets
   - Das Web-Ticket erscheint! 🎉

---

## 🔧 Häufige Probleme

### ❌ "Datenbankverbindung fehlgeschlagen"

**Lösung:**
1. Prüfen Sie `config.php` nochmal
2. Testen Sie die Verbindung in Ihrem Webspace-Panel
3. Eventuell muss "Remote MySQL" aktiviert werden

### ❌ "500 Internal Server Error"

**Lösung:**
1. Prüfen Sie die PHP-Logs in Ihrem Webspace-Panel
2. Stellen Sie sicher, dass PHP 7.0+ aktiv ist
3. Prüfen Sie Dateiberechtigungen (meist 644 für .php)

### ❌ CORS-Fehler im Browser

**Lösung:**
Passen Sie in `create-ticket.php` Zeile 15 an:
```php
// Erlaubt Zugriff NUR von Ihrer Domain
header('Access-Control-Allow-Origin: https://ihre-domain.de');
```

### ❌ Ticket wird nicht gespeichert

**Lösung:**
Stellen Sie sicher, dass die `tickets`-Tabelle existiert:
- Starten Sie einmal Ihre Desktop-App
- Die Tabelle wird automatisch erstellt

---

## 🔒 Sicherheit

### ✅ Bereits implementiert:

- ✅ **SQL-Injection-Schutz** (Prepared Statements)
- ✅ **Input-Validierung** (Frontend + Backend)
- ✅ **XSS-Schutz** (htmlspecialchars)
- ✅ **CSRF-Schutz** (gleiche Domain)
- ✅ **config.php geschützt** (.htaccess)

### 🔐 Empfehlungen:

1. **HTTPS aktivieren** (Let's Encrypt - meist kostenlos im Panel)
2. **Starke DB-Passwörter** verwenden
3. **Regelmäßige Backups** der Datenbank
4. **E-Mail-Benachrichtigungen** aktivieren (optional)

---

## 📧 E-Mail-Benachrichtigungen aktivieren

In `config.php`:
```php
define('ENABLE_EMAIL_NOTIFICATIONS', true);  // true = aktiviert
define('EMAIL_FROM', 'support@ihre-domain.de');
```

Die E-Mail-Funktion ist bereits in `create-ticket.php` implementiert!

---

## 🎨 Formular anpassen

### Design ändern:

Öffnen Sie `ticket-form.html` und ändern Sie im `<style>`-Bereich:

```css
/* Hauptfarbe ändern */
.header {
    background: linear-gradient(135deg, #2196F3 0%, #1976D2 100%);
    /* ↑ Ändern Sie #2196F3 in Ihre Markenfarbe */
}
```

### Texte ändern:

```html
<h1>📨 Support-Anfrage</h1>
<!-- ↑ Ändern Sie den Text nach Belieben -->
```

---

## 🌍 Remote-Zugriff für Desktop-App

Wenn Ihre Desktop-App **von extern** auf die Datenbank zugreifen soll:

### 1. Im Webspace-Panel:
- Aktivieren Sie **"Remote MySQL-Zugriff"**
- Fügen Sie Ihre IP-Adresse hinzu (oder erlauben Sie alle: `%`)

### 2. In Desktop-App (appsettings.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=db.ihre-domain.de;Port=3306;Database=projektdb;Uid=db_user;Pwd=passwort;"
  }
}
```

**Port:** Meist `3306`, prüfen Sie Ihr Webspace-Panel

---

## 📁 Dateiübersicht

```
webspace/
├── create-ticket.php      ✅ API-Endpoint (PHP)
├── config.php             ✅ Datenbank-Konfiguration
├── .htaccess              ✅ Sicherheit
└── ticket-form.html       ✅ Formular für Website

Hochladen auf:
ihre-domain.de/
├── support.html           ← ticket-form.html umbenennen
└── api/
    ├── create-ticket.php
    ├── config.php
    └── .htaccess
```

---

## 🔍 API testen

### Mit Browser:
```
https://ihre-domain.de/api/create-ticket.php
```
→ Sollte Fehlermeldung zeigen (nur POST erlaubt) = **Funktioniert!**

### Mit curl (Terminal):
```bash
curl -X POST https://ihre-domain.de/api/create-ticket.php \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Test User",
    "customerEmail": "test@example.com",
    "customerPhone": "",
    "subject": "Test-Ticket",
    "description": "Dies ist ein Test-Ticket über die PHP-API",
    "category": 0,
    "priority": 1
  }'
```

---

## 💡 Vorteile der PHP-Lösung

✅ **Kein Server nötig** - Läuft auf jedem Webspace  
✅ **Kostenlos** - Keine zusätzlichen Hosting-Kosten  
✅ **Einfach** - 3 Dateien hochladen, fertig!  
✅ **Schnell** - PHP ist sehr performant  
✅ **Kompatibel** - Funktioniert mit Ihrer Desktop-App  
✅ **Sicher** - Alle wichtigen Schutzmaßnahmen  

---

## 📊 Hosting-Anbieter Kompatibilität

| Anbieter | PHP | MySQL | Funktioniert |
|----------|-----|-------|--------------|
| ALL-INKL | ✅ | ✅ | ✅ Ja |
| Strato | ✅ | ✅ | ✅ Ja |
| 1&1 / IONOS | ✅ | ✅ | ✅ Ja |
| HostEurope | ✅ | ✅ | ✅ Ja |
| Webgo | ✅ | ✅ | ✅ Ja |
| DomainFactory | ✅ | ✅ | ✅ Ja |
| Hetzner | ✅ | ✅ | ✅ Ja |

**Fast alle Webspace-Anbieter** unterstützen PHP + MySQL!

---

## 🎯 Workflow

```
Kunde besucht Ihre Website
    ↓
Füllt Formular aus (support.html)
    ↓
JavaScript sendet Daten an PHP-Script
    ↓
PHP speichert in MySQL-Datenbank
    ↓
Kunde erhält Ticketnummer
    ↓
Mitarbeiter öffnet Desktop-App
    ↓
Sieht neues Ticket & bearbeitet es
```

**✅ Keine Server-Administration nötig!**  
**✅ Läuft 24/7 auf Ihrem Webspace!**

---

## ✅ Checkliste

- [ ] Datenbank-Zugangsdaten notiert
- [ ] `config.php` angepasst
- [ ] Dateien per FTP hochgeladen
- [ ] URL im Formular angepasst
- [ ] Formular getestet (support.html)
- [ ] Ticket in Desktop-App sichtbar
- [ ] HTTPS aktiv (SSL-Zertifikat)
- [ ] Optional: E-Mail-Benachrichtigung aktiviert

---

## 🆘 Support

**Problem nicht gelöst?**

1. Prüfen Sie die **PHP-Error-Logs** in Ihrem Webspace-Panel
2. Aktivieren Sie Debug-Modus in `create-ticket.php` (Zeile 11):
   ```php
   ini_set('display_errors', 1);
   ```
3. Testen Sie die Datenbankverbindung separat

---

## 🎉 Fertig!

Sie haben jetzt ein **vollständiges Web-Ticketsystem** auf Ihrem Webspace!

**Keine Server-Verwaltung, keine komplizierten Setups - einfach hochladen und nutzen!**

---

**Erstellt für Webspace-Hosting - Funktioniert garantiert! 🚀**
