# 🎯 LogicC KI-Features finden - WPF Anleitung

## Wo finden Sie alle KI-Funktionen in Ihrer WPF-Anwendung?

---

## 1️⃣ **Hauptmenü - KI-Konfiguration**

### 📍 Standort: Menüleiste oben

```
MainWindow.xaml - Zeile 97-100
```

**Navigation:**
```
🏠 Hauptfenster
  └─ Menüleiste (oben)
      └─ [⚙️ Einstellungen]
          └─ 🤖 KI-Integration (LogicC)
              ├─ Konfiguration        ← API-Key eingeben
              └─ Über LogicC AI       ← Status & Info
```

### 🎬 So öffnen Sie die Konfiguration:

1. **Starten Sie die WPF-Anwendung** (`Projektsoftware.exe`)
2. Klicken Sie oben in der Menüleiste auf **"Einstellungen"** oder **"Tools"**
3. Wählen Sie **🤖 KI-Integration (LogicC)**
4. Klicken Sie auf **"Konfiguration"**

→ **Dialog öffnet sich:** `LogicCConfigDialog`

**Was Sie hier tun können:**
- ✅ API-Key eingeben
- ✅ Modell auswählen (gpt-4o, gpt-4o-mini, etc.)
- ✅ Token-Limit einstellen (100-4000)
- ✅ Temperatur einstellen (0.0-2.0)
- ✅ Verbindung testen

---

## 2️⃣ **Ticket-Details - KI-Assistent**

### 📍 Standort: Innerhalb eines Tickets

```
TicketDetailsDialog.xaml - Zeile 170-178
```

**Navigation:**
```
🏠 Hauptfenster
  └─ [🎫 Tickets] Tab
      └─ Ticket auswählen (Doppelklick oder "Bearbeiten")
          └─ TicketDetailsDialog öffnet sich
              └─ Rechte Seite: Aktionsbereich
                  └─ 🤖 KI-Assistent Button (lila/purple)
```

### 🎬 So nutzen Sie den KI-Assistenten:

1. **Gehen Sie zum Tickets-Tab** (links im Hauptfenster)
2. **Wählen Sie ein Ticket aus** (Doppelklick oder "Bearbeiten"-Button)
3. Im **TicketDetailsDialog** rechts finden Sie:
   ```
   ┌─────────────────────────────┐
   │  🤖 KI-Assistent            │  ← Hier klicken!
   └─────────────────────────────┘
   ```
4. **KI-Assistent Dialog öffnet sich** mit 5 Funktionen

**Was Sie hier tun können:**
- ✅ **Kategorisieren** - Ticket automatisch einordnen
- ✅ **Antworten generieren** - Antwortvorschläge erstellen
- ✅ **Ähnliche Tickets** - Vergleichbare Tickets finden
- ✅ **E-Mail-Entwurf** - E-Mail-Antwort vorschlagen
- ✅ **Zusammenfassung** - Ticket zusammenfassen

---

## 3️⃣ **Tickets-Übersicht - KI-Batch**

### 📍 Standort: Tickets-Listenansicht (Toolbar)

```
TicketsView.xaml - Zeile 137-142
```

**Navigation:**
```
🏠 Hauptfenster
  └─ [🎫 Tickets] Tab
      └─ Toolbar (oben)
          └─ 🤖 KI-Batch Button (lila/purple, rechts neben "Neues Ticket")
```

### 🎬 So nutzen Sie KI-Batch:

1. **Gehen Sie zum Tickets-Tab**
2. In der **Toolbar oben** finden Sie:
   ```
   ┌──────────────┐  ┌─────────────┐
   │ + Neues      │  │ 🤖 KI-Batch │  ← Hier!
   │   Ticket     │  │             │
   └──────────────┘  └─────────────┘
   ```
3. **Klicken Sie auf "🤖 KI-Batch"**

**Was passiert:**
- ✅ Bis zu **10 unkategorisierte Tickets** werden automatisch verarbeitet
- ✅ KI kategorisiert jedes Ticket (Bug, Feature, Support, etc.)
- ✅ Priorität wird festgelegt (Low, Normal, High, Critical)
- ✅ Fortschritt wird angezeigt
- ✅ Ergebnisse werden gespeichert

---

## 📋 Vollständige Feature-Übersicht

### **KI-Features je nach Standort:**

| Standort | Button/Menü | Funktionen |
|----------|-------------|------------|
| **Menüleiste** | ⚙️ Einstellungen → 🤖 KI-Integration | • API-Konfiguration<br>• Modellauswahl<br>• Verbindungstest<br>• Status-Info |
| **Ticket-Details** | 🤖 KI-Assistent | • Kategorisierung<br>• Antwortvorschläge<br>• Ähnliche Tickets<br>• E-Mail-Entwürfe<br>• Zusammenfassungen |
| **Tickets-Liste** | 🤖 KI-Batch | • Massen-Kategorisierung<br>• Batch-Verarbeitung<br>• Automatische Priorisierung |

---

## 🗺️ Visuelle Übersicht

```
┌─────────────────────────────────────────────────────────────────────┐
│  Projektsoftware - Hauptfenster                                     │
├─────────────────────────────────────────────────────────────────────┤
│  [Datei] [Bearbeiten] [⚙️ Einstellungen] [?]                        │
│                          └─ 🤖 KI-Integration (LogicC)  ← 1. HIER  │
│                              ├─ Konfiguration                       │
│                              └─ Über LogicC AI                      │
├─────────────────────────────────────────────────────────────────────┤
│  Sidebar                │  Hauptbereich                             │
│  ┌──────────┐          │                                            │
│  │ 🏠 Home  │          │  ┌──────────────────────────────────┐    │
│  │ 📊 CRM   │          │  │                                  │    │
│  │ 🎫 Tickets│ ← Click │  │  [+ Neues Ticket] [🤖 KI-Batch] │ ← 3. HIER
│  │ 📧 E-Mail│          │  │                           ↑      │    │
│  │ 📁 Proj. │          │  │                         Batch    │    │
│  └──────────┘          │  │  ┌─────────────────────────┐   │    │
│                         │  │  │ Ticket #123             │   │    │
│                         │  │  │ Titel: Server Problem   │   │    │
│                         │  │  └─────────────────────────┘   │    │
│                         │  │                                  │    │
│                         │  │  [Doppelklick öffnet Details]   │    │
│                         │  └──────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘

Ticket Details Dialog:
┌──────────────────────────────────────────────────────┐
│  Ticket #123 bearbeiten                              │
├──────────────────────────────────────────────────────┤
│  Titel: [Server Problem          ]                   │
│  Kategorie: [Bug ▼]                                  │
│  Priorität: [High ▼]                                 │
│  Beschreibung:                                       │
│  [Server antwortet nicht...]                         │
│                                                      │
│  Kommentare: ...                                     │
│                                                      │
│  ┌────────────────────────────────────┐            │
│  │  🤖 KI-Assistent                   │ ← 2. HIER │
│  └────────────────────────────────────┘            │
│  [Speichern] [Abbrechen]                           │
└──────────────────────────────────────────────────────┘

KI-Assistent Dialog:
┌──────────────────────────────────────────────────────┐
│  🤖 KI-Assistent für Ticket #123                     │
├──────────────────────────────────────────────────────┤
│  [📋 Kategorisieren]     [💬 Antworten generieren]  │
│  [🔍 Ähnliche Tickets]   [📧 E-Mail-Entwurf]        │
│  [📝 Zusammenfassung]                               │
│                                                      │
│  Ergebnis:                                           │
│  ┌────────────────────────────────────────────┐    │
│  │  [KI-Antwort erscheint hier...]           │    │
│  └────────────────────────────────────────────┘    │
│  [Schließen]                                        │
└──────────────────────────────────────────────────────┘
```

---

## 🔍 Schnellsuche nach UI-Elementen

### Im Visual Studio:

**Suchen Sie nach diesen Begriffen in den XAML-Dateien:**

```csharp
// 1. Menü-Integration:
"🤖 KI-Integration"           → MainWindow.xaml (Zeile ~97)
"ConfigureLogicCAi_Click"     → MainWindow.xaml.cs (Zeile 2047)

// 2. Ticket-Assistent:
"🤖 KI-Assistent"             → TicketDetailsDialog.xaml (Zeile ~170)
"AiAssistant_Click"           → TicketDetailsDialog.xaml.cs

// 3. Batch-Verarbeitung:
"🤖 KI-Batch"                 → TicketsView.xaml (Zeile ~137)
"AiBatchCategorize_Click"     → TicketsView.xaml.cs
```

---

## ⚙️ Erforderliche Konfiguration

### **Bevor Sie die KI-Features nutzen können:**

1. **API-Key erforderlich!**
   - Gehen Sie zu: Menü → ⚙️ Einstellungen → 🤖 KI-Integration → Konfiguration
   - Geben Sie Ihren **LineLLM Virtual Key** ein
   - Testen Sie die Verbindung

2. **Modell auswählen:**
   - **gpt-4o** (Standard, empfohlen)
   - **gpt-4o-mini** (schneller, günstiger)
   - **gpt-5-nano** (sehr schnell)
   - **gemini-2.5-flash** (Google, schnell)

3. **Speichern & Testen:**
   - Klicken Sie auf "Verbindung testen"
   - Bei Erfolg: ✅ Verbindung erfolgreich!
   - Klicken Sie auf "Speichern"

---

## 🎯 Typische Workflows

### **Workflow 1: Einzelnes Ticket kategorisieren**
```
1. Tickets-Tab öffnen
2. Ticket doppelklicken
3. "🤖 KI-Assistent" Button klicken
4. "Kategorisieren" wählen
5. Kategorie & Priorität übernehmen
```

### **Workflow 2: Mehrere Tickets auf einmal kategorisieren**
```
1. Tickets-Tab öffnen
2. "🤖 KI-Batch" Button klicken (Toolbar oben)
3. Bis zu 10 Tickets werden automatisch verarbeitet
4. Fortschritt beobachten
5. Fertig!
```

### **Workflow 3: Antwort-Vorschlag erstellen**
```
1. Ticket öffnen
2. "🤖 KI-Assistent" klicken
3. "Antworten generieren" wählen
4. Vorschlag kopieren
5. In Kommentar einfügen
```

---

## 📚 Weitere Dokumentation

- **API-Dokumentation:** `LOGICC_AI_INTEGRATION.md`
- **Konfigurationshilfe:** `LOGICC_API_KEY_HILFE.md`
- **Status & Features:** `LOGICC_AI_INTEGRATION_STATUS.md`
- **Korrekturen:** `LOGICC_API_KORREKTUR.md`

---

## 💡 Tipps & Tricks

### **Tastaturkürzel (optional erweiterbar):**
- Aktuell keine Shortcuts definiert
- Sie könnten z.B. `Ctrl+K` für KI-Assistent hinzufügen

### **Best Practices:**
- ✅ KI-Batch am Anfang des Tages nutzen (unkategorisierte Tickets aufräumen)
- ✅ KI-Assistent für komplexe Tickets verwenden
- ✅ Antwortvorschläge immer überprüfen vor dem Versenden
- ✅ Zusammenfassungen für lange Ticket-Verläufe nutzen

---

## ❓ Häufige Fragen

**Q: Ich sehe den KI-Button nicht in meinem Ticket!**
A: Stellen Sie sicher, dass Sie das neueste Build ausführen und die Integration aktiviert ist.

**Q: KI-Batch verarbeitet keine Tickets**
A: Nur Tickets mit Kategorie "Nicht kategorisiert" werden verarbeitet.

**Q: Fehler beim Klicken auf KI-Assistent?**
A: Überprüfen Sie die API-Konfiguration im Menü → KI-Integration → Konfiguration.

**Q: Wo wird der API-Key gespeichert?**
A: In `%APPDATA%\Projektsoftware\logicc-config.json`

---

## ✅ Checkliste für erste Nutzung

- [ ] Projektsoftware.exe starten
- [ ] Menü öffnen: ⚙️ Einstellungen → 🤖 KI-Integration → Konfiguration
- [ ] API-Key eingeben (LineLLM Virtual Key vom Support)
- [ ] "Verbindung testen" klicken → ✅ Erfolg!
- [ ] "Speichern" klicken
- [ ] Zu Tickets-Tab wechseln
- [ ] "🤖 KI-Batch" Button testen (unkategorisierte Tickets)
- [ ] Einzelnes Ticket öffnen
- [ ] "🤖 KI-Assistent" Button testen
- [ ] Fertig! 🎉

---

**Status:** ✅ **Alle KI-Features erfolgreich integriert!**

Bei Fragen oder Problemen konsultieren Sie die Dokumentationsdateien im Projektverzeichnis.
