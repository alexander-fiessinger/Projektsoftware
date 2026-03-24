# 🎯 Quick-Start: Webspace-Installation

## In 5 Minuten zum fertigen Ticketsystem!

---

## 📋 Schritt 1: Datenbank-Info sammeln (2 Min.)

### Loggen Sie sich in Ihr Webspace-Panel ein:

**Bei ALL-INKL (KAS):**
1. Login: https://kas.all-inkl.com
2. Klicken Sie: **MySQL-Datenbankverwaltung**
3. Notieren Sie:
   - Datenbank: `d01234567`
   - Benutzername: `d01234567`
   - Host: `localhost`

**Bei Strato:**
1. Login: https://www.strato.de/apps/CustomerService
2. Gehen Sie zu: **Datenbanken**
3. Notieren Sie:
   - Datenbank: `DB123456`
   - Benutzername: `U123456`
   - Host: `rdbms.strato.de`

**Bei 1&1 / IONOS:**
1. Login: https://www.ionos.de
2. Klicken Sie: **Hosting** → **Datenbank verwalten**
3. Notieren Sie die Zugangsdaten

---

## ⚙️ Schritt 2: config.php bearbeiten (1 Min.)

Öffnen Sie: `TicketAPI/webspace/config.php`

**Ändern Sie diese 4 Zeilen:**

```php
define('DB_HOST', 'localhost');              // ← Ihr Host
define('DB_NAME', 'projektdb');              // ← Ihr DB-Name
define('DB_USER', 'db_user');                // ← Ihr Username
define('DB_PASS', 'sicheres_passwort');      // ← Ihr Passwort
```

**Beispiel:**
```php
define('DB_HOST', 'localhost');
define('DB_NAME', 'd01234567');
define('DB_USER', 'd01234567');
define('DB_PASS', 'MeinGeheimesPasswort123');
```

**Speichern & schließen!**

---

## 📤 Schritt 3: Dateien hochladen (1 Min.)

### FTP-Programm öffnen (z.B. FileZilla):

1. **Verbindung herstellen:**
   - Host: `ftp.ihre-domain.de`
   - Benutzername: Ihr FTP-User
   - Passwort: Ihr FTP-Passwort

2. **Navigieren Sie zu:**
   - Bei ALL-INKL: `/`
   - Bei Strato: `/html/`
   - Bei 1&1: `/`

3. **Ordner erstellen:**
   - Rechtsklick → **Neuer Ordner** → Name: `api`

4. **Hochladen:**
   ```
   Lokal                        →  Server
   ─────────────────────────────────────────
   webspace/create-ticket.php   →  /api/create-ticket.php
   webspace/config.php          →  /api/config.php
   webspace/.htaccess           →  /api/.htaccess
   webspace/support.html        →  /support.html
   ```

   **HINWEIS:** Sie können auch `ticket-form.html` hochladen - ist identisch!

**Fertig!**

---

## 🔗 Schritt 4: URL anpassen (30 Sek.)

Öffnen Sie: `/support.html` (auf dem Server oder lokal vor Upload)

**Suchen Sie Zeile 302:**
```javascript
const API_URL = 'https://ihre-domain.de/api/create-ticket.php';
```

**Ändern Sie zu:**
```javascript
const API_URL = 'https://meine-firma.de/api/create-ticket.php';
                         ^^^^^^^^^^^^^^^^
                         Ihre echte Domain!
```

**Speichern & hochladen!**

---

## ✅ Schritt 5: Testen! (1 Min.)

### Im Browser öffnen:
```
https://ihre-domain.de/support.html
```

### Formular ausfüllen:
- **Name:** Max Mustermann
- **E-Mail:** test@example.com
- **Betreff:** Test-Ticket
- **Beschreibung:** Dies ist mein erstes Test-Ticket über das neue System!
- **Kategorie:** Allgemein
- **Priorität:** Normal

### Absenden!

**Erfolg:**
```
✅ Vielen Dank!
Ticket-Nummer: #000001
```

---

## 🖥️ Desktop-App prüfen:

1. Starten Sie Ihre **Projektsoftware**
2. Menü → **Einstellungen** → **Support-Tickets**
3. Das Ticket erscheint in der Liste! 🎉

---

## 🎨 BONUS: Design anpassen

### Logo hinzufügen:

Öffnen Sie `support.html`, Zeile 246:

```html
<div class="header">
    <img src="logo.png" alt="Logo" style="height: 50px; margin-bottom: 15px;">
    <h1>📨 Support-Anfrage</h1>
    <p>Wir helfen Ihnen gerne weiter!</p>
</div>
```

### Farbe ändern:

Suchen Sie im `<style>`-Bereich (Zeile ~35):

```css
.header {
    background: linear-gradient(135deg, #2196F3 0%, #1976D2 100%);
    /* Ändern Sie #2196F3 in Ihre Firmenfarbe! */
}
```

**Beispiel - Orange:**
```css
background: linear-gradient(135deg, #FF9800 0%, #F57C00 100%);
```

**Beispiel - Grün:**
```css
background: linear-gradient(135deg, #4CAF50 0%, #388E3C 100%);
```

---

## 🔧 Probleme beheben

### ❌ "Verbindungsfehler"

**Lösung:**
1. Prüfen Sie die URL in `support.html` (Zeile 302)
2. Testen Sie: `https://ihre-domain.de/api/create-ticket.php`
   - Sollte zeigen: "Nur POST-Requests sind erlaubt" = **Funktioniert!**

### ❌ "Datenbankverbindung fehlgeschlagen"

**Lösung:**
1. Öffnen Sie `config.php` nochmal
2. Prüfen Sie: Haben Sie die richtigen Zugangsdaten?
3. Testen Sie im Webspace-Panel: **phpMyAdmin** → Verbindung testen

### ❌ "500 Error"

**Lösung:**
1. Webspace-Panel öffnen
2. **Logs** → **PHP Error Log** prüfen
3. Eventuell PHP-Version ändern (mindestens 7.0)

### ❌ Ticket wird nicht in Desktop-App angezeigt

**Lösung:**
1. Starten Sie die Desktop-App einmal neu
2. Prüfen Sie: Verwendet die Desktop-App die **gleiche Datenbank**?
3. Öffnen Sie in Desktop-App: Einstellungen → Datenbank → Diagnose

---

## 📊 Dateien-Checkliste

Nach dem Upload sollten Sie haben:

```
ihre-domain.de/
│
├── index.html              (Ihre bestehende Startseite)
├── support.html            ✅ NEU - Das Ticket-Formular
│
└── api/
    ├── create-ticket.php   ✅ NEU - PHP-API
    ├── config.php          ✅ NEU - Datenbank-Config
    └── .htaccess           ✅ NEU - Sicherheit
```

---

## 🌐 Integration in bestehende Website

### Als Menüpunkt verlinken:

```html
<nav>
    <a href="index.html">Start</a>
    <a href="about.html">Über uns</a>
    <a href="support.html">Support</a>  ← NEU!
    <a href="contact.html">Kontakt</a>
</nav>
```

### Als Button auf Homepage:

```html
<a href="support.html" class="button">
    📨 Brauchen Sie Hilfe?
</a>
```

---

## ✅ Fertig!

**Sie haben jetzt:**
- ✅ Web-Formular auf Ihrer Website
- ✅ PHP-API auf Ihrem Webspace
- ✅ Integration mit Desktop-App
- ✅ Funktioniert 24/7

**Keine Server-Verwaltung nötig!**
**Keine monatlichen Kosten!**
**Einfach hochladen und nutzen!**

---

## 📞 Nächste Schritte

1. ✅ **HTTPS aktivieren** (Let's Encrypt im Webspace-Panel)
2. ✅ **E-Mail-Benachrichtigung** einrichten (optional)
3. ✅ **Design anpassen** an Ihre Marke
4. ✅ **Link auf Website** einfügen

---

**Viel Erfolg mit Ihrem neuen Ticketsystem! 🎉**

Bei Fragen schauen Sie in: `WEBSPACE_INSTALLATION.md`
