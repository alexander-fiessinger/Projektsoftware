# 📦 Dateien zum Hochladen - Übersicht

## ✅ Diese Dateien müssen Sie hochladen:

### Im Ordner `TicketAPI/webspace/` finden Sie:

```
TicketAPI/webspace/
├── create-ticket.php          ← PHP-API (WICHTIG!)
├── config.php                 ← Datenbank-Config (ANPASSEN!)
├── .htaccess                  ← Sicherheit
├── support.html               ← Ticket-Formular für Website
└── ticket-form.html           ← Gleich wie support.html
```

---

## 📤 Upload-Struktur auf Ihrem Webspace:

```
ihre-domain.de/
│
├── index.html              (Ihre bestehende Startseite)
├── support.html            ← webspace/support.html hochladen
│
└── api/                    ← Neuer Ordner erstellen!
    ├── create-ticket.php   ← webspace/create-ticket.php
    ├── config.php          ← webspace/config.php (ANGEPASST!)
    └── .htaccess           ← webspace/.htaccess
```

---

## ⚙️ Was Sie anpassen müssen:

### 1. config.php (VOR dem Upload!)
```php
define('DB_HOST', 'localhost');     // ← Ihr Host
define('DB_NAME', 'projektdb');     // ← Ihr DB-Name
define('DB_USER', 'db_user');       // ← Ihr Username
define('DB_PASS', 'passwort');      // ← Ihr Passwort
```

### 2. support.html (Zeile 302)
```javascript
const API_URL = 'https://ihre-domain.de/api/create-ticket.php';
                         ^^^^^^^^^^^^^^^^
                         Ihre echte Domain!
```

---

## ✅ Checkliste

- [ ] **config.php** mit DB-Daten angepasst
- [ ] **Ordner `/api/`** auf Webspace erstellt
- [ ] **3 Dateien** in `/api/` hochgeladen:
  - [ ] create-ticket.php
  - [ ] config.php
  - [ ] .htaccess
- [ ] **support.html** hochgeladen (ins Root-Verzeichnis)
- [ ] **URL angepasst** in support.html (Zeile 302)
- [ ] **Test durchgeführt** (Formular ausgefüllt & abgesendet)
- [ ] **Ticket in Desktop-App** sichtbar

---

## 🎯 Nach dem Upload testen:

### 1. API testen:
```
https://ihre-domain.de/api/create-ticket.php
```
→ Sollte zeigen: "Nur POST-Requests sind erlaubt" = ✅ **Funktioniert!**

### 2. Formular öffnen:
```
https://ihre-domain.de/support.html
```
→ Formular sollte angezeigt werden

### 3. Ticket erstellen:
- Formular ausfüllen
- Absenden
- Ticketnummer notieren

### 4. In Desktop-App prüfen:
- Projektsoftware starten
- Menü → Einstellungen → Support-Tickets
- Ihr Web-Ticket sollte erscheinen!

---

## 🔧 Fehlersuche

### Problem: "Datei nicht gefunden" (404)
**Lösung:**
- Prüfen Sie, ob alle Dateien hochgeladen wurden
- Achten Sie auf korrekte Ordnerstruktur: `/api/create-ticket.php`
- Groß-/Kleinschreibung beachten!

### Problem: "Datenbankverbindung fehlgeschlagen"
**Lösung:**
- Öffnen Sie `config.php` auf dem Server
- Prüfen Sie alle 4 DB-Werte nochmal
- Testen Sie die Verbindung in phpMyAdmin

### Problem: "CORS-Fehler" im Browser
**Lösung:**
Öffnen Sie `create-ticket.php` und ändern Sie Zeile 15:
```php
header('Access-Control-Allow-Origin: https://ihre-domain.de');
```

---

## 📞 Support

**Immer noch Probleme?**

1. ✅ Prüfen Sie **PHP Error Logs** im Webspace-Panel
2. ✅ Aktivieren Sie Debug-Modus in `create-ticket.php` (Zeile 11):
   ```php
   ini_set('display_errors', 1);
   ```
3. ✅ Testen Sie Datenbank-Verbindung in **phpMyAdmin**

---

**🎉 Viel Erfolg!**
