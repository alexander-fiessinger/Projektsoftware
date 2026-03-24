# 📸 Visueller Upload-Guide

## 🎯 So sieht es aus:

### 📁 Ihre lokalen Dateien (auf PC):

```
C:\Users\...\Projektsoftware\TicketAPI\webspace\
│
├── 📄 create-ticket.php          ← PHP-API
├── 📄 config.php                 ← Datenbank-Config
├── 📄 .htaccess                  ← Sicherheit
├── 📄 support.html               ← Formular
└── 📄 ticket-form.html           ← Gleich wie support.html
```

**Diese 4 Dateien hochladen! ☝️**

---

### 🌐 Ihr Webspace (nach Upload):

```
ihre-domain.de/
│
├── 📄 index.html                 (Ihre Homepage)
├── 📄 support.html               ✅ NEU! ← Hochgeladen
│
├── 📁 images/                    (Ihr bestehender Ordner)
├── 📁 css/                       (Ihr bestehender Ordner)
│
└── 📁 api/                       ✅ NEU! ← Ordner erstellen!
    ├── 📄 create-ticket.php      ✅ NEU! ← Hochgeladen
    ├── 📄 config.php             ✅ NEU! ← Hochgeladen (ANGEPASST!)
    └── 📄 .htaccess              ✅ NEU! ← Hochgeladen
```

---

## 🖱️ FileZilla Schritt-für-Schritt:

### 1. Verbindung herstellen
```
┌─────────────────────────────────────────┐
│ Host:       ftp.ihre-domain.de          │
│ Benutzername: ihr-ftp-user              │
│ Passwort:   ******************          │
│ Port:       21                          │
│             [Verbinden]                 │
└─────────────────────────────────────────┘
```

### 2. Links = Ihr PC, Rechts = Webspace
```
Lokaler Computer            │  Remote-Server
────────────────────────────┼──────────────────────────
C:\...\webspace\            │  /
  ├── create-ticket.php     │  ├── index.html
  ├── config.php            │  ├── images/
  ├── .htaccess             │  └── css/
  └── support.html          │
```

### 3. Ordner `/api/` erstellen auf Server
```
Rechtsklick auf rechte Seite (Server)
→ "Verzeichnis erstellen"
→ Name eingeben: "api"
→ OK
```

### 4. Dateien hochladen
```
Drag & Drop oder Rechtsklick "Hochladen":

create-ticket.php  → /api/create-ticket.php  ✅
config.php         → /api/config.php         ✅
.htaccess          → /api/.htaccess          ✅
support.html       → /support.html           ✅
```

---

## 🔍 Überprüfung nach Upload:

### Im Browser testen:

**1. API prüfen:**
```
https://ihre-domain.de/api/create-ticket.php
```
**Erwartete Antwort:**
```json
{"success":false,"message":"Nur POST-Requests sind erlaubt"}
```
✅ **Perfekt! Die API funktioniert!**

**2. Formular öffnen:**
```
https://ihre-domain.de/support.html
```
✅ **Sie sollten das Ticket-Formular sehen**

---

## 📋 Welche Datei wofür?

| Datei | Zweck | Wo hochladen? |
|-------|-------|---------------|
| **create-ticket.php** | PHP-API die Tickets speichert | `/api/create-ticket.php` |
| **config.php** | Datenbank-Zugangsdaten (ANPASSEN!) | `/api/config.php` |
| **.htaccess** | Schützt config.php vor Zugriff | `/api/.htaccess` |
| **support.html** | Das Formular für Kunden | `/support.html` |

---

## ⚙️ Wichtigste Anpassungen:

### ✏️ config.php (MUSS angepasst werden!)
```php
// Zeile 14-17:
define('DB_HOST', 'localhost');     // ← ANPASSEN
define('DB_NAME', 'projektdb');     // ← ANPASSEN  
define('DB_USER', 'db_user');       // ← ANPASSEN
define('DB_PASS', 'passwort');      // ← ANPASSEN
```

### ✏️ support.html (MUSS angepasst werden!)
```javascript
// Zeile 302:
const API_URL = 'https://ihre-domain.de/api/create-ticket.php';
                         ^^^^^^^^^^^^^^^^
                         IHRE Domain eintragen!
```

---

## 🎯 Workflow nach Upload:

```
Kunde öffnet:
https://ihre-domain.de/support.html
    ↓
Füllt Formular aus
    ↓
Klickt "Absenden"
    ↓
JavaScript sendet Daten an:
https://ihre-domain.de/api/create-ticket.php
    ↓
PHP speichert in MySQL-Datenbank
    ↓
Kunde sieht: "Ticket #000001 erstellt"
    ↓
Mitarbeiter öffnet Desktop-App
    ↓
Sieht Ticket & bearbeitet es
```

---

## 💡 Quick-Tipps:

### Tipp 1: Direkter Link
```html
<a href="support.html">Brauchen Sie Hilfe?</a>
```

### Tipp 2: Als Popup
```javascript
<a href="#" onclick="window.open('support.html', 'Support', 'width=800,height=700'); return false;">
    Support
</a>
```

### Tipp 3: Umbenennen erlaubt
Sie können `support.html` auch umbenennen:
- `hilfe.html`
- `ticket.html`
- `kontakt-support.html`

**Vergessen Sie nicht, die Links anzupassen!**

---

## ✅ Zusammenfassung:

**Lokale Dateien im Ordner:**
```
TicketAPI/webspace/
├── create-ticket.php  ✅ Vorhanden
├── config.php         ✅ Vorhanden
├── .htaccess          ✅ Vorhanden
├── support.html       ✅ Vorhanden (NEU erstellt)
└── ticket-form.html   ✅ Vorhanden (identisch)
```

**Sie können beide HTML-Dateien verwenden:**
- `support.html` - Empfohlen für Website
- `ticket-form.html` - Wenn Sie einen anderen Namen bevorzugen

**Beide sind identisch - nur verschiedene Namen!**

---

**Jetzt sollten Sie alle Dateien finden! 🎉**

Hochladen → Testen → Fertig! 🚀
