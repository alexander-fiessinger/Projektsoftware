# ✅ UPLOAD-CHECKLISTE

## 📦 Diese Dateien MÜSSEN hochgeladen werden:

### Im Ordner: `C:\Users\afies\Documents\Projektsoftware\TicketAPI\webspace\`

| ✅ | Datei | Größe | Wo hochladen? | Anpassen? |
|----|-------|-------|---------------|-----------|
| ⬜ | **create-ticket.php** | ~8 KB | `/api/create-ticket.php` | ❌ NEIN |
| ⬜ | **config.php** | 2 KB | `/api/config.php` | ✅ JA! DB-Daten |
| ⬜ | **.htaccess** | 1 KB | `/api/.htaccess` | ❌ NEIN |
| ⬜ | **support.html** | 17 KB | `/support.html` | ✅ JA! API-URL |

**NICHT hochladen:** Die .md Dokumentations-Dateien!

---

## 🔧 VOR dem Upload anpassen:

### 1️⃣ config.php (Zeile 14-17)

**VOR dem Upload öffnen und ändern:**

```php
define('DB_HOST', 'localhost');         // ← IHRE Datenbank
define('DB_NAME', 'projektdb');         // ← IHRE Datenbank
define('DB_USER', 'db_user');           // ← IHRE Datenbank
define('DB_PASS', 'passwort');          // ← IHRE Datenbank
```

### 2️⃣ support.html (Zeile 302)

**VOR dem Upload öffnen und ändern:**

```javascript
const API_URL = 'https://ihre-domain.de/api/create-ticket.php';
                         ^^^^^^^^^^^^^^^^
                         IHRE Domain!
```

---

## 📤 Upload-Reihenfolge:

### Schritt 1: Ordner erstellen auf Webspace
```
/api/   ← Neuer Ordner
```

### Schritt 2: PHP-Dateien hochladen
```
create-ticket.php  →  /api/create-ticket.php
config.php         →  /api/config.php  (ANGEPASST!)
.htaccess          →  /api/.htaccess
```

### Schritt 3: HTML hochladen
```
support.html  →  /support.html  (ANGEPASST!)
```

---

## ✅ Nach Upload überprüfen:

Ihre Webspace-Struktur sollte so aussehen:

```
ihre-domain.de/
│
├── index.html              (Ihre Homepage)
├── support.html            ✅ NEU
│
├── images/                 (Ihr Ordner)
├── css/                    (Ihr Ordner)
│
└── api/                    ✅ NEU
    ├── create-ticket.php   ✅ NEU
    ├── config.php          ✅ NEU
    └── .htaccess           ✅ NEU
```

---

## 🧪 Testen:

### 1. API testen:
```
https://ihre-domain.de/api/create-ticket.php
```
**Erwartete Antwort:**
```json
{"success":false,"message":"Nur POST-Requests sind erlaubt"}
```
✅ = **Funktioniert!**

### 2. Formular testen:
```
https://ihre-domain.de/support.html
```
- Formular ausfüllen
- Absenden
- Ticketnummer notieren

### 3. Desktop-App prüfen:
```
Projektsoftware → Einstellungen → Support-Tickets
```
- Ihr Web-Ticket sollte erscheinen!

---

## 📋 Vollständige Checkliste:

- [ ] **create-ticket.php** im Ordner vorhanden
- [ ] **config.php** mit DB-Daten angepasst ✏️
- [ ] **.htaccess** vorhanden
- [ ] **support.html** mit API-URL angepasst ✏️
- [ ] FTP-Verbindung hergestellt
- [ ] Ordner `/api/` auf Webspace erstellt
- [ ] **create-ticket.php** hochgeladen → `/api/`
- [ ] **config.php** hochgeladen → `/api/`
- [ ] **.htaccess** hochgeladen → `/api/`
- [ ] **support.html** hochgeladen → Root
- [ ] API getestet (URL aufgerufen)
- [ ] Formular getestet (Ticket erstellt)
- [ ] Ticket in Desktop-App sichtbar

---

## ⚠️ WICHTIG:

### Diese Dateien NICHT hochladen:
- ❌ DATEIEN_UEBERSICHT.md
- ❌ QUICKSTART_WEBSPACE.md
- ❌ UPLOAD_GUIDE.md
- ❌ WEBSPACE_INSTALLATION.md
- ❌ ticket-form.html (ist identisch mit support.html)

Das sind nur Dokumentations-Dateien für Sie!

---

## 🎯 Zusammenfassung:

**Hochladen:**
1. `create-ticket.php` → `/api/`
2. `config.php` (ANGEPASST!) → `/api/`
3. `.htaccess` → `/api/`
4. `support.html` (ANGEPASST!) → `/`

**Fertig!** 🚀

---

**Jetzt haben Sie alle Dateien und wissen genau, was wohin muss!**
