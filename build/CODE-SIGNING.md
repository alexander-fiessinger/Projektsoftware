# Code-Signing – Einrichtungsanleitung

Dieses Dokument beschreibt Schritt für Schritt, wie ein Code-Signing-Zertifikat
gekauft, installiert und in den Build-Prozess integriert wird, um SmartScreen- und
Antivirus-Warnungen (z. B. SmartDefender) dauerhaft zu beseitigen.

---

## Warum wird die Software geblockt?

Windows SmartScreen und Antivirenprogramme prüfen bei unbekannten `.exe`-Dateien,
ob der Herausgeber **vertrauenswürdig** ist. Ohne Code-Signing-Zertifikat gilt
jede EXE als „unbekannter Herausgeber" und wird geblockt oder mit Warnung geöffnet.

**Code-Signing löst das dauerhaft** – die Software bekommt eine kryptografische
Signatur, die Windows als vertrauenswürdig einstuft.

---

## Zertifikats-Typen

| Typ | SmartScreen-Effekt | Preis | Empfehlung |
|-----|--------------------|-------|------------|
| **EV (Extended Validation)** | Warnung **sofort** weg | ~220–350 €/Jahr | ⭐ Beste Wahl |
| **OV (Organization Validation)** | Warnung baut sich ab (~100-200 Downloads) | ~90–150 €/Jahr | Gut für Start |
| Selbstsigniert | Keine Verbesserung | Kostenlos | ❌ Nicht geeignet |

---

## Schritt 1: Zertifikat kaufen

**Empfohlene Anbieter (günstigster zuerst):**

| Anbieter | OV-Preis/Jahr | EV-Preis/Jahr | Link |
|----------|---------------|---------------|------|
| **Certum** | ~30 € | ~150 € | https://shop.certum.eu/data-safety/code-signing-certificates.html |
| **Sectigo** | ~90 € | ~220 € | https://sectigo.com/code-signing-certificates |
| **DigiCert** | ~150 € | ~350 € | https://www.digicert.com/signing/code-signing-certificates |

> 💡 **Tipp für EV:** Bei EV-Zertifikaten wird ein Hardware-Token (USB-Stick)
> geliefert. Das Signing läuft dann über den Token – `signtool.exe` unterstützt das direkt.

**Für die Bestellung wird benötigt:**
- Unternehmensname (z. B. „AF Software Engineering")  
- Unternehmensadresse (muss offiziell eingetragen sein)  
- E-Mail-Adresse  
- Telefonnummer (für Verifizierungsanruf)

---

## Schritt 2: Zertifikat installieren

### OV-Zertifikat (Datei-basiert)
Der Anbieter liefert eine `.pfx`- oder `.p12`-Datei per E-Mail.

```powershell
# PFX in den persönlichen Zertifikatsspeicher importieren
# (Passwort wird vom Anbieter mitgeliefert)
certutil -importpfx "C:\Pfad\zum\zertifikat.pfx"
```

Alternativ: Doppelklick auf die `.pfx` → Assistent → **„Aktueller Benutzer"** →
Passwort eingeben → **„Automatisch, basierend auf Zertifikatstyp"** auswählen.

### EV-Zertifikat (Hardware-Token)
1. USB-Token einstecken
2. Treiber-Software des Anbieters installieren
3. Zertifikat ist automatisch im Speicher sichtbar (bleibt auf dem Token)

---

## Schritt 3: Thumbprint automatisch eintragen

Nach der Installation das Einrichtungsskript ausführen:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\setup-codesigning.ps1
```

Das Skript:
- Findet alle installierten Code-Signing-Zertifikate automatisch
- Lässt eins auswählen
- Trägt den Thumbprint in `build\publish.config.json` ein
- Führt einen Signing-Test durch

---

## Schritt 4: Manuell eintragen (optional)

Falls das Skript nicht verwendet werden soll, den Thumbprint manuell ermitteln:

```powershell
# Alle Code-Signing-Zertifikate mit Thumbprint anzeigen
Get-ChildItem Cert:\CurrentUser\My | Where-Object {
    $_.EnhancedKeyUsageList | Where-Object { $_.ObjectId -eq "1.3.6.1.5.5.7.3.3" }
} | Select-Object Subject, Thumbprint, NotAfter
```

Dann in `build\publish.config.json` eintragen:

```json
{
  "GitHubRepo":  "alexander-fiessinger/Projektsoftware",
  "GitHubToken": "...",
  "SigningCertThumbprint": "HIER_THUMBPRINT_EINTRAGEN",
  "SigningTimestampUrl":   "http://timestamp.sectigo.com",
  "SigningDescription":    "Projektierungssoftware Professional",
  "SigningDescriptionUrl": "https://github.com/alexander-fiessinger/Projektsoftware"
}
```

**Timestamp-URLs je nach Anbieter:**
| Anbieter | Timestamp-URL |
|----------|--------------|
| Sectigo / Certum / Comodo | `http://timestamp.sectigo.com` |
| DigiCert | `http://timestamp.digicert.com` |
| GlobalSign | `http://timestamp.globalsign.com/scripts/timstamp.dll` |

---

## Schritt 5: signtool.exe installieren (falls nicht vorhanden)

`signtool.exe` ist Teil des **Windows SDK** – kostenlos von Microsoft:

```
https://developer.microsoft.com/windows/downloads/windows-sdk/
```

Installation: Nur **„Windows SDK Signing Tools for Desktop Apps"** auswählen
(spart Speicherplatz, Rest ist nicht nötig).

---

## Schritt 6: Nächsten Release erstellen

Ab jetzt läuft `publish.ps1` **vollautomatisch**:

```powershell
powershell -ExecutionPolicy Bypass -File .\build\publish.ps1
```

Der Build-Prozess signiert automatisch:
1. ✅ `Projektsoftware.exe` (nach dotnet publish)
2. ✅ `setup-x.x.x.exe` (nach Inno Setup)

---

## Verifizieren nach dem Release

```powershell
# Signatur einer EXE prüfen
Get-AuthenticodeSignature ".\publish\Projektsoftware.exe"
Get-AuthenticodeSignature ".\build\Output\setup-1.0.61.exe"
```

Erwartetes Ergebnis:
```
Status            : Valid
SignerCertificate : [CN=AF Software Engineering, O=AF Software Engineering, ...]
```

---

## Häufige Fehler

| Fehler | Lösung |
|--------|--------|
| `No certificates were found that met all the given criteria` | Zertifikat nicht im persönlichen Speicher oder falscher Thumbprint |
| `The specified timestamp server either could not be reached` | Timestamp-URL prüfen, Internet-Verbindung |
| `Access is denied` (EV-Token) | Token-Treiber nicht installiert oder PIN falsch |
| SmartScreen zeigt trotzdem Warnung | Bei OV normal – baut sich über Downloads auf; EV sofort weg |
