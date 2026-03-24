# Easybill API - Vollständige Integration

Diese Datei dokumentiert alle verfügbaren Easybill API-Funktionen in der Projektsoftware.

## ✅ Implementierte API-Bereiche

### 1. **Kunden (Customers)** ✅
- `GetAllCustomersAsync()` - Alle Kunden abrufen
- `GetCustomerAsync(id)` - Einzelnen Kunden abrufen
- `CreateCustomerAsync(customer)` - Neuen Kunden erstellen
- `UpdateCustomerAsync(id, customer)` - Kunden aktualisieren
- `DeleteCustomerAsync(id)` - Kunden löschen
- `SearchCustomersAsync(searchTerm)` - Kunden suchen

**UI-Dialog:** ✅ `EasybillCustomersDialog` - Verfügbar über Menü → Einstellungen → Easybill-Kunden

### 2. **Dokumente (Documents)** ✅ NEU!
Unterstützte Dokumenttypen: INVOICE, OFFER, ORDER_CONFIRMATION, DELIVERY_NOTE, CREDIT, DUNNING, INVOICE_CANCELLATION

- `GetAllDocumentsAsync(type)` - Alle Dokumente abrufen (optional nach Typ filtern)
- `GetDocumentAsync(id)` - Einzelnes Dokument abrufen
- `CreateDocumentAsync(document)` - Neues Dokument erstellen
- `UpdateDocumentAsync(id, document)` - Dokument aktualisieren
- `DeleteDocumentAsync(id)` - Dokument löschen
- `FinalizeDocumentAsync(id)` - Dokument abschließen (aus Entwurf)
- `SendDocumentAsync(id, to, subject, message, cc, bcc)` - Dokument per E-Mail versenden
- `DownloadDocumentPdfAsync(id)` - PDF herunterladen
- `MarkDocumentAsPaidAsync(id, paidAt)` - Als bezahlt markieren
- `CancelDocumentAsync(id)` - Dokument stornieren

**Utility-Funktionen:**
- `CreateInvoiceFromProjectAsync(project, timeEntries, hourlyRate, text, dueInDays)` - Rechnung aus Projekt & Zeiteinträgen erstellen
- `CreateOfferFromProjectAsync(project, items, text, validityDays)` - Angebot aus Projekt erstellen

**UI-Dialog:** ✅ `EasybillDocumentsDialog` - Verfügbar über Menü → Einstellungen → 📄 Easybill-Dokumente
- PDF-Download direkt aus der Liste
- E-Mail-Versand mit eigenem Dialog
- Als bezahlt markieren
- Dokumente löschen
- Filter nach Dokumenttyp (Rechnungen, Angebote, etc.)

### 3. **Produkte/Artikel (Positions)** ✅ ERWEITERT!
- `GetAllProductsAsync()` - Alle Produkte/Artikel abrufen
- `GetProductAsync(id)` - Einzelnes Produkt abrufen
- `CreateProductAsync(product)` - Neues Produkt erstellen
- `UpdateProductAsync(id, product)` - Produkt aktualisieren
- `DeleteProductAsync(id)` - Produkt löschen

**UI-Dialog:** ✅ `EasybillProductsDialog` - Verfügbar über Menü → Einstellungen → 📦 Easybill-Produkte
- Alle Produkte und Dienstleistungen anzeigen
- Produkte löschen
- Übersicht über Verkaufspreise und MwSt.

### 4. **Zahlungen (Payments)** ✅ NEU!
Zahlungstypen: BANK_TRANSFER, BANK_CARD, CASH, CREDIT_NOTE, PAYPAL, DIRECT_DEBIT, MISC

- `GetAllPaymentsAsync()` - Alle Zahlungen abrufen
- `GetPaymentsByDocumentAsync(documentId)` - Zahlungen für ein Dokument abrufen
- `CreatePaymentAsync(payment)` - Neue Zahlung erfassen
- `DeletePaymentAsync(id)` - Zahlung löschen

### 5. **Projekte (Projects)** ✅
- `GetAllProjectsAsync()` - Alle Projekte abrufen
- `CreateProjectAsync(project)` - Neues Projekt erstellen
- `GetProjectsByCustomerAsync(customerId)` - Projekte eines Kunden abrufen

**UI-Dialog:** ✅ `EasybillProjectDialog` - Verfügbar über Menü → Einstellungen → Easybill-Projekt anlegen

### 6. **Zeiterfassung (Time Tracking)** ✅
- `CreateTimeTrackingFromEntryAsync(entry, projectId, hourlyRate)` - Zeiterfassung aus lokalem Entry erstellen
- `GetAllTimeTrackingsAsync()` - Alle Zeiterfassungen abrufen

**UI-Dialog:** ✅ `EasybillTimeExportDialog` - Verfügbar über Menü → Einstellungen → Zeiten zu Easybill exportieren

### 7. **Kontakte (Contacts)** ✅ NEU!
Ansprechpartner für Kunden

- `GetContactsByCustomerAsync(customerId)` - Alle Kontakte eines Kunden abrufen
- `CreateContactAsync(contact)` - Neuen Kontakt erstellen
- `UpdateContactAsync(customerId, contactId, contact)` - Kontakt aktualisieren
- `DeleteContactAsync(customerId, contactId)` - Kontakt löschen

### 8. **Aufgaben (Tasks)** ✅ NEU!
- `GetAllTasksAsync()` - Alle Aufgaben abrufen
- `CreateTaskAsync(task)` - Neue Aufgabe erstellen
- `UpdateTaskAsync(id, task)` - Aufgabe aktualisieren
- `DeleteTaskAsync(id)` - Aufgabe löschen

### 9. **Anhänge (Attachments)** ✅ NEU!
- `UploadAttachmentAsync(documentId, fileName, fileData)` - Anhang hochladen
- `GetAttachmentsByDocumentAsync(documentId)` - Anhänge eines Dokuments abrufen
- `DeleteAttachmentAsync(documentId, attachmentId)` - Anhang löschen
- `DownloadAttachmentAsync(documentId, attachmentId)` - Anhang herunterladen

### 10. **Lagerbestand (Stock)** ✅ NEU!
- `GetStockByProductAsync(productId)` - Lagerbestand für Produkt abrufen
- `CreateStockAsync(stock)` - Lagereintrag erstellen

### 11. **Vorlagen (Templates)** ✅ NEU!
- `GetAllPdfTemplatesAsync()` - Alle PDF-Vorlagen abrufen
- `GetAllTextTemplatesAsync()` - Alle Text-Vorlagen abrufen

### 12. **Rabatte (Discounts)** ✅ NEU!
- `GetDiscountsByCustomerAsync(customerId)` - Rabatte für Kunden abrufen
- `CreateDiscountAsync(discount)` - Rabatt erstellen
- `DeleteDiscountAsync(id)` - Rabatt löschen

### 13. **SEPA-Mandate** ✅ NEU!
- `GetSepaMandatesByCustomerAsync(customerId)` - SEPA-Mandate für Kunden abrufen
- `CreateSepaMandateAsync(mandate)` - SEPA-Mandat erstellen
- `DeleteSepaMandateAsync(id)` - SEPA-Mandat löschen

### 14. **Webhooks** ✅ NEU!
- `GetAllWebhooksAsync()` - Alle Webhooks abrufen
- `CreateWebhookAsync(webhook)` - Webhook erstellen
- `UpdateWebhookAsync(id, webhook)` - Webhook aktualisieren
- `DeleteWebhookAsync(id)` - Webhook löschen

---

## 🖥️ WPF-Dialoge (NEU!)

### 📄 Easybill Dokumentenverwaltung
**Dialog:** `EasybillDocumentsDialog`
**Menü:** Einstellungen → 📄 Easybill-Dokumente

**Funktionen:**
- ✅ Alle Dokumente anzeigen (Rechnungen, Angebote, Lieferscheine, etc.)
- ✅ Filter nach Dokumenttyp
- ✅ PDF-Download mit Speicherdialog
- ✅ E-Mail-Versand direkt aus der Liste
- ✅ Als bezahlt markieren
- ✅ Dokumente löschen
- ✅ Status-Anzeige (Entwurf, Gesendet, Bezahlt, etc.)

### 📧 E-Mail Versand Dialog
**Dialog:** `EasybillSendEmailDialog`
**Verwendung:** Wird von EasybillDocumentsDialog aufgerufen

**Funktionen:**
- ✅ E-Mail-Adresse (To)
- ✅ CC und BCC (optional)
- ✅ Betreff
- ✅ Nachricht mit Vorlagen
- ✅ Validierung

### 📦 Easybill Produktverwaltung
**Dialog:** `EasybillProductsDialog`
**Menü:** Einstellungen → 📦 Easybill-Produkte

**Funktionen:**
- ✅ Alle Produkte und Dienstleistungen anzeigen
- ✅ Verkaufspreise und MwSt. Übersicht
- ✅ Produkte löschen
- ✅ Status-Anzeige
- ✅ Aktualisieren-Funktion

---

## 📦 Neue Models

### Dokumente
- **EasybillDocument** - Rechnungen, Angebote, Lieferscheine, etc.
- **EasybillDocumentItem** - Positionen in Dokumenten
- **ServiceDate** - Leistungsdatum-Varianten

### Produkte & Zahlungen
- **EasybillProduct** - Produkte/Artikel/Dienstleistungen
- **EasybillPayment** - Zahlungseingänge

### Erweiterte Funktionen
- **EasybillContact** - Ansprechpartner
- **EasybillTask** - Aufgaben
- **EasybillAttachment** - Datei-Anhänge
- **EasybillStock** - Lagerbestände
- **EasybillPdfTemplate** - PDF-Vorlagen
- **EasybillTextTemplate** - Text-Vorlagen
- **EasybillDiscount** - Kundenrabatte
- **EasybillSepaMandate** - SEPA-Lastschriftmandate
- **EasybillWebhook** - Webhook-Konfiguration

---

## 🎯 Anwendungsbeispiele

### Rechnung aus Projekt erstellen
```csharp
var easybillService = new EasybillService();

// Hole Zeiteinträge des Projekts
var timeEntries = await databaseService.GetTimeEntriesByProjectAsync(projectId);

// Erstelle Rechnung
var invoice = await easybillService.CreateInvoiceFromProjectAsync(
    project: myProject,
    timeEntries: timeEntries,
    hourlyRate: 100.00m,
    invoiceText: "Vielen Dank für Ihren Auftrag...",
    dueInDays: 14
);

// Rechnung abschließen
await easybillService.FinalizeDocumentAsync(invoice.Id.Value);

// Rechnung versenden
await easybillService.SendDocumentAsync(
    documentId: invoice.Id.Value,
    to: "kunde@example.com",
    subject: "Ihre Rechnung",
    message: "Anbei erhalten Sie Ihre Rechnung..."
);
```

### Angebot erstellen
```csharp
var items = new List<EasybillDocumentItem>
{
    new EasybillDocumentItem
    {
        Type = "POSITION",
        Position = 1,
        Description = "Projektentwicklung",
        Quantity = 40,
        Unit = "Stunden",
        SinglePriceNet = 100.00m,
        VatPercent = 19
    }
};

var offer = await easybillService.CreateOfferFromProjectAsync(
    project: myProject,
    items: items,
    offerText: "Gerne unterbreiten wir Ihnen folgendes Angebot...",
    validityDays: 30
);
```

### Zahlung erfassen
```csharp
var payment = new EasybillPayment
{
    DocumentId = invoiceId,
    Amount = 476.00m,
    Type = "BANK_TRANSFER",
    Currency = "EUR",
    PaymentAt = DateTime.Now.ToString("yyyy-MM-dd"),
    Reference = "Überweisung vom Kunden"
};

await easybillService.CreatePaymentAsync(payment);
```

### PDF herunterladen und speichern
```csharp
var pdfData = await easybillService.DownloadDocumentPdfAsync(documentId);
File.WriteAllBytes(@"C:\Rechnungen\Rechnung_123.pdf", pdfData);
```

### Kontakt hinzufügen
```csharp
var contact = new EasybillContact
{
    CustomerId = customerId,
    FirstName = "Max",
    LastName = "Mustermann",
    Email = "max@firma.de",
    Phone1 = "+49 123 456789",
    Department = "Einkauf"
};

await easybillService.CreateContactAsync(contact);
```

### Anhang hochladen
```csharp
var fileData = File.ReadAllBytes(@"C:\Dokumente\Vertrag.pdf");
var attachment = await easybillService.UploadAttachmentAsync(
    documentId: invoiceId,
    fileName: "Vertrag.pdf",
    fileData: fileData
);
```

---

## 🔧 Status Codes & Typen

### Dokumenttypen
- **INVOICE** - Rechnung
- **OFFER** - Angebot
- **ORDER_CONFIRMATION** - Auftragsbestätigung
- **DELIVERY_NOTE** - Lieferschein
- **CREDIT** - Gutschrift
- **DUNNING** - Mahnung
- **INVOICE_CANCELLATION** - Storno

### Dokumentstatus
- **DRAFT** - Entwurf
- **SENT** - Gesendet
- **PAID** - Bezahlt
- **CANCELLED** - Storniert
- **OVERDUE** - Überfällig
- **PARTIALLY_PAID** - Teilweise bezahlt

### Zahlungstypen
- **BANK_TRANSFER** - Überweisung
- **BANK_CARD** - Kartenzahlung
- **CASH** - Barzahlung
- **CREDIT_NOTE** - Gutschrift
- **PAYPAL** - PayPal
- **DIRECT_DEBIT** - Lastschrift
- **MISC** - Sonstige

---

## 📊 Vollständigkeitsübersicht

| API-Bereich | Status | Methoden |
|------------|--------|----------|
| Customers | ✅ Vollständig | 6 |
| Documents | ✅ Vollständig | 12 |
| Positions/Products | ✅ Vollständig | 5 |
| Payments | ✅ Vollständig | 4 |
| Projects | ✅ Vollständig | 3 |
| Time Tracking | ✅ Vollständig | 2 |
| Contacts | ✅ Vollständig | 4 |
| Tasks | ✅ Vollständig | 4 |
| Attachments | ✅ Vollständig | 4 |
| Stock | ✅ Vollständig | 2 |
| PDF Templates | ✅ Vollständig | 1 |
| Text Templates | ✅ Vollständig | 1 |
| Discounts | ✅ Vollständig | 3 |
| SEPA Mandates | ✅ Vollständig | 3 |
| Webhooks | ✅ Vollständig | 4 |

**Gesamt: 58+ API-Methoden implementiert**

---

## 🚀 Nächste Schritte

Die Easybill-Integration ist nun vollständig! Sie können:

1. ✅ Rechnungen automatisch aus Projekten erstellen
2. ✅ Angebote und Auftragsbestätigungen generieren
3. ✅ Zahlungseingänge erfassen
4. ✅ PDFs herunterladen und versenden
5. ✅ Anhänge zu Dokumenten hinzufügen
6. ✅ Kontakte und Ansprechpartner verwalten
7. ✅ Rabatte für Kunden konfigurieren
8. ✅ SEPA-Lastschriften einrichten
9. ✅ Webhooks für Automatisierungen nutzen
10. ✅ Lagerbestände synchronisieren

Die komplette Easybill REST API v1 ist nun in der Projektsoftware verfügbar! 🎉
