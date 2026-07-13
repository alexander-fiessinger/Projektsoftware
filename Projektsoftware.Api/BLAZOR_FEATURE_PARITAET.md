# Blazor ↔ WPF – Feature-Paritäts-Roadmap

Diese Datei dokumentiert den Abgleich zwischen der WPF-Desktop-App (`Projektsoftware`)
und der mobilen/responsiven Blazor-App (`Projektsoftware.Api`, Ordner `Components/Pages`).
Sie dient als Arbeitsplan, um die Blazor-App schrittweise auf vollständige Funktions-
parität zu bringen.

> Hinweis: Viele WPF-„Dialoge" sind Bearbeiten-Dialoge **innerhalb** eines Moduls
> (z. B. `CustomerDialog` gehört zum Kunden-Modul). In der Blazor-App werden solche
> Bearbeiten-Funktionen in den jeweiligen Tab integriert, nicht als eigene Seite.

## Legende
- ✅ vorhanden – Modul existiert in Blazor
- 🟡 teilweise – Tab vorhanden, aber Funktionsumfang unvollständig (Detail-/Edit-Dialoge fehlen)
- ❌ fehlt – noch nicht in Blazor abgebildet

## Modul-Abgleich (Haupttabs)

| Bereich | WPF (Views) | Blazor-Tab | Status |
|---|---|---|---|
| Dashboard | DashboardControl | `DashboardTab` | 🟡 KPIs prüfen |
| Projekte | ProjectDialog, ProjectSelectionDialog, ProjectDocumentsDialog | `ProjectsTab` | 🟡 Dokumente/Edit |
| Aufgaben | TaskDialog | `TasksTab` | 🟡 Edit-Dialog |
| Kanban | KanbanBoardView | `KanbanTab` | 🟡 Drag&Drop |
| Team/Mitarbeiter | EmployeeDialog | `EmployeesTab` | 🟡 Edit |
| Zeiterfassung | TimeEntryDialog, TimeEntryTemplatesDialog, WeeklyTimesheet | `TimeEntriesTab` | 🟡 Vorlagen/Wochenansicht |
| Kunden | CustomerDialog, CustomersListDialog, CustomerDocumentsDialog, CustomerPickerDialog | `CustomersTab` | 🟡 Dokumente/Picker |
| Tickets | TicketsView, TicketDashboard, TicketDetails/Edit/Email/Management, TicketAiAssistant | `TicketsTab` | 🟡 KI-Assistent, E-Mail |
| CRM | CrmView, CrmActivity/Contact/Deal-Dialog | `CrmTab` | 🟡 Edit-Dialoge |
| Meetings | MeetingCalendarView, MeetingDialog, MeetingProtocolDialog | `MeetingsTab` | 🟡 Kalender/Protokoll |
| Einkauf | PurchaseView, PurchaseDocument/Invoice/Order-Dialog | `PurchaseTab` | 🟡 Edit-Dialoge |
| Protokolle | MeetingProtocolDialog | `ProtocolsTab` | 🟡 |
| Lieferanten | SupplierDialog, SupplierRatingDialog | `SuppliersTab`, `SupplierRatingTab` | 🟡 Edit |
| Vertrieb | SalesView, SalesLead/Appointment-Dialog, LeadKanban, LeadStatistics | `SalesTab`, `SalesCalendarTab` | 🟡 Lead-Kanban/Statistik |
| Globale Suche | GlobalSearchDialog | `SearchTab` | 🟡 |
| Benachrichtigungen | NotificationsDialog | `NotificationsTab` | ✅ |
| Audit-Log | AuditLogDialog | `AuditLogTab` | ✅ |
| Auswertungen/KPI | KpiDashboardDialog, ExpensesAnalyticsDialog, LeadStatistics | `AnalyticsTab`, `KpiTab`, `ExpenseAnalysisTab` | 🟡 |
| Benutzerverwaltung | UserManagementDialog, UserEditDialog, ChangePasswordDialog | `UserManagementTab` | 🟡 Passwort ändern |
| Einstellungen | Div. Config-Dialoge | `SettingsTab` | 🟡 siehe unten |
| Zeitstrahl | GanttDialog | `TimelineTab` | 🟡 Gantt |
| Export | CsvExportService, ExportService | `ExportTab`, `EasybillExportTab` | 🟡 |
| Brief/Vertrag | LetterGeneratorDialog, ContractGeneratorDialog | `LetterTab`, `ContractTab` | 🟡 PDF |
| Budget | BudgetTrackingDialog | `BudgetTab` | 🟡 |
| SLA | SlaMonitoringDialog | `SlaTab` | 🟡 |
| Wiedervorlage | FollowUpDialog | `FollowUpTab` | ✅ |
| MwSt | VatService | `VatTab` | ✅ |
| Vorlagen | ProjectTemplatesDialog | `TemplateTab` | 🟡 |
| Dokumente | Div. Documents-Dialoge | `DocumentsTab` | 🟡 |

## Noch nicht abgebildete Bereiche (❌ / eigene Seite nötig)

| WPF-Feature | Views/Services | Priorität | Anmerkung |
|---|---|---|---|
| Easybill-Vollintegration | EasybillCustomers/Documents/Products/Project/SendEmail/TimeExport-Dialog, EasybillService | Hoch | Nur Export-Tab vorhanden |
| Exchange/EWS-Postfach | ExchangeInboxDialog, EwsSettings, ExchangeSettings, EwsService | Mittel | E-Mail-Posteingang |
| Webex-Integration | WebexConfigDialog, WebexService | Niedrig | Meeting-Links |
| Rechnungserstellung | CreateInvoiceFromProject, CreateProforma, CreateOrderConfirmation, InvoiceOverview/Selection | Hoch | Fakturierung |
| Angebots-/Dokument-Konvertierung | ConvertDocumentDialog, OfferHistory/Selection, AddDocumentItem | Mittel | |
| Zahlungsabgleich | PaymentReconciliationDialog | Mittel | Bank-Abgleich |
| Portal-Bestellungen (intern) | PortalOrdersDialog | Mittel | Portal existiert (`Portal*.razor`) |
| Produktverwaltung | ProductDialog, ProductSyncService, CsvProductImport | Mittel | |
| LogicC-KI | LogicCConfigDialog, LogicCAiService | Niedrig | KI-Konfiguration |
| Verbindungs-Diagnose | ConnectionDiagnosticDialog, ConfigDebugDialog | Niedrig | Admin-Tooling |
| Update-Funktion | UpdateDialog, UpdateService | Entfällt | Desktop-spezifisch |
| Öffentliches Ticketformular | PublicTicketFormApp, TicketFormWindow | Mittel | Web-Formular |
| PDF-Erzeugung | PdfExportService (QuestPDF) | Hoch | Serverseitig via API |

## Empfohlene Umsetzungs-Reihenfolge
1. **Fakturierung & Easybill** (Rechnungen, Angebote, Dokumente) – höchster Geschäftswert
   - ✅ Zahlungsstatus sichtbar gemacht (Bezahlt / Teilw. bezahlt / Offen / Überfällig)
     über `paid_amount` vs. Bruttobetrag; korrektes Betrags-Mapping (`amount`);
     Schnellfilter + offener Betrag in `DocumentsTab`.
   - ✅ „Als bezahlt markieren" (document-payments, Beträge in Cent).
   - ✅ Angebot→Rechnung / Auftragsbestätigung konvertieren; Rechnung stornieren.
   - ✅ Rechnung/Angebot aus Projekt erstellen (Zeiteinträge→Positionen mit Stundensatz,
     MwSt, Leistungszeitraum; optional direkt abschließen) im `EasybillExportTab`.
   - ✅ Proforma-Rechnung aus Projekt erstellen (`EasybillExportTab`).
   - ✅ Mahnung (DUNNING) zu offener Rechnung erstellen im `DocumentsTab`.
2. **Detail-/Edit-Dialoge** je Tab vervollständigen (Kunden, Projekte, Tickets, CRM)
3. **PDF-Erzeugung** serverseitig (Brief, Vertrag, Rechnung) über API-Endpunkte
4. **E-Mail/Exchange-Posteingang** und Ticket-E-Mail-Versand
5. **Restliche Spezialmodule** (Zahlungsabgleich, Produkt-Sync, LogicC)

## Nicht zu portieren (Desktop-/Windows-spezifisch)
- `UpdateDialog`/`UpdateService` (Auto-Update der Desktop-EXE)
- `ToastNotificationWindow` (Windows-Toast) → Web-Benachrichtigungen stattdessen
- Direkte Fenster-/Window-Logik
