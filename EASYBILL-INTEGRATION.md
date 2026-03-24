# Easybill API-Integration - Troubleshooting

## Verbindungsprobleme beheben

### Problem: "Verbindung fehlgeschlagen"

#### 1. API-Schlüssel überprüfen
- Gehen Sie zu https://www.easybill.de
- Melden Sie sich an
- Navigieren Sie zu: **Einstellungen → API-Schlüssel**
- Stellen Sie sicher, dass der API-Schlüssel:
  - ✅ Aktiviert ist
  - ✅ Die richtigen Berechtigungen hat (Lesen/Schreiben für Kunden)
  - ✅ Komplett kopiert wurde (kein Leerzeichen am Anfang/Ende)

#### 2. API-URL überprüfen
Die Standard-URL sollte sein:
```
https://api.easybill.de/rest/v1
```

**Wichtig:** OHNE `/` am Ende!

#### 3. Häufige Fehler

**401 Unauthorized:**
- API-Schlüssel ist falsch oder deaktiviert
- API-Schlüssel hat keine Berechtigung für Kunden

**403 Forbidden:**
- API-Schlüssel ist gültig, aber ohne die erforderlichen Rechte
- Lösung: Neuen API-Schlüssel mit "Kunden lesen/schreiben" erstellen

**404 Not Found:**
- API-URL ist falsch
- Überprüfen Sie die URL (ohne `/` am Ende)

**429 Too Many Requests:**
- Zu viele API-Anfragen in kurzer Zeit
- Warten Sie 1 Minute und versuchen Sie es erneut

**500 Internal Server Error:**
- Easybill-Server-Problem
- Warten Sie einige Minuten

#### 4. Netzwerk-Probleme

**Proxy-Server:**
Wenn Ihr Unternehmen einen Proxy verwendet, funktioniert die Verbindung möglicherweise nicht direkt.

**Firewall:**
Stellen Sie sicher, dass ausgehende HTTPS-Verbindungen zu `api.easybill.de` erlaubt sind.

#### 5. Test mit curl (für Fortgeschrittene)

Öffnen Sie ein Terminal/PowerShell und testen Sie:

```bash
curl -u "IHR_API_KEY:" https://api.easybill.de/rest/v1/customers?limit=1
```

**Wichtig:** Beachten Sie den Doppelpunkt nach dem API-Key (`:`) - das Passwort bleibt leer!

Alternativ mit expliziter Authorization:
```bash
curl -H "Authorization: Basic $(echo -n 'IHR_API_KEY:' | base64)" https://api.easybill.de/rest/v1/customers?limit=1
```

Wenn das funktioniert, sollte auch die App funktionieren.

## API-Schlüssel erstellen

1. **Easybill öffnen:** https://www.easybill.de
2. **Einloggen** mit Ihren Zugangsdaten
3. **Einstellungen** → **API-Schlüssel**
4. **Neuen Schlüssel erstellen:**
   - Name: z.B. "Projektsoftware"
   - Berechtigungen: **Kunden** (Lesen + Schreiben)
5. **Schlüssel kopieren** (wird nur einmal angezeigt!)
6. In der Projektsoftware einfügen unter:
   **Einstellungen → Easybill-Konfiguration**

## Kontakt

Bei weiteren Problemen:
- Easybill-Support: https://www.easybill.de/support
- Easybill API-Dokumentation: https://www.easybill.de/api/

## Technische Details

Die Integration verwendet:
- **REST API v1** von Easybill
- **HTTP Basic Authentication** mit API-Key als Benutzername (Passwort leer)
- **HTTPS** verschlüsselte Verbindung
- **JSON** Datenformat

### Authentifizierung

Easybill verwendet HTTP Basic Authentication:
```
Authorization: Basic base64(API-KEY:)
```

Der API-Key wird als Benutzername verwendet, das Passwort bleibt leer (beachten Sie den Doppelpunkt nach dem Key).
