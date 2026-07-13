using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Projektsoftware.Models
{
    // ── Normalisierte Kontobewegung ─────────────────────────────────────

    /// <summary>
    /// Eine vom BANKSapi-Service normalisierte Kontobewegung. Beträge in Euro,
    /// positiv = Zahlungseingang (Gutschrift).
    /// </summary>
    public class BankTransaction
    {
        public DateTime ValueDate { get; set; }

        /// <summary>Betrag in Euro; positiv = Eingang.</summary>
        public decimal Amount { get; set; }

        /// <summary>Betrag in Cent (gerundet). Für exakte Vergleiche ohne Rundungsfehler.</summary>
        [JsonIgnore]
        public long AmountCents => (long)Math.Round(Amount * 100m, MidpointRounding.AwayFromZero);

        /// <summary>Verwendungszweck (kombiniert aus Text/Beschreibung).</summary>
        public string Purpose { get; set; } = "";

        /// <summary>Name des Auftraggebers.</summary>
        public string PartnerName { get; set; } = "";

        /// <summary>IBAN des Auftraggebers (soweit vorhanden).</summary>
        public string PartnerIban { get; set; } = "";

        /// <summary>SEPA End-to-End-Referenz (soweit vorhanden).</summary>
        public string EndToEndId { get; set; } = "";

        /// <summary>Gesamter durchsuchbarer Referenztext (Verwendungszweck + E2E-Referenz).</summary>
        [JsonIgnore]
        public string SearchText => $"{Purpose} {EndToEndId}".Trim();

        [JsonIgnore]
        public string AmountDisplay => Amount.ToString("N2") + " €";

        [JsonIgnore]
        public string ValueDateDisplay => ValueDate.ToString("dd.MM.yyyy");

        /// <summary>
        /// Stabiler Hash zur Erkennung bereits gebuchter Umsätze (Schutz vor Doppelbuchung).
        /// </summary>
        [JsonIgnore]
        public string TransactionHash
        {
            get
            {
                var raw = $"{ValueDate:yyyy-MM-dd}|{AmountCents}|{Purpose}|{PartnerName}|{PartnerIban}|{EndToEndId}";
                var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
                return Convert.ToHexString(bytes);
            }
        }
    }

    // ── Abgleich-Ergebnis ───────────────────────────────────────────────

    public enum ReconciliationMatchStatus
    {
        /// <summary>Eindeutig: genau eine offene Rechnung, Nummer + Betrag exakt → automatisch buchen.</summary>
        Automatic,

        /// <summary>Unklar: z. B. Nummer gefunden aber Betrag weicht ab, oder mehrere Kandidaten → Bestätigung nötig.</summary>
        NeedsConfirmation,

        /// <summary>Keine offene Rechnung zuordenbar.</summary>
        NoMatch
    }

    /// <summary>
    /// Zuordnung einer Kontobewegung zu einer Easybill-Rechnung inklusive Buchungszustand.
    /// Implementiert INotifyPropertyChanged, damit das WPF-DataGrid Buchungsänderungen anzeigt.
    /// </summary>
    public class ReconciliationMatch : System.ComponentModel.INotifyPropertyChanged
    {
        public BankTransaction Transaction { get; set; } = default!;

        /// <summary>Zugeordnete Rechnung (falls vorhanden).</summary>
        public EasybillDocument? Invoice { get; set; }

        /// <summary>Im Verwendungszweck gefundene Rechnungsnummer (falls vorhanden).</summary>
        public string? MatchedInvoiceNumber { get; set; }

        /// <summary>Auf der Rechnung hinterlegter Kundenname (aus dem CustomerSnapshot), sofern zugeordnet.</summary>
        public string? MatchedCustomerName { get; set; }

        /// <summary>Noch offener Betrag der zugeordneten Rechnung in Euro (Brutto abzüglich bereits gezahlter Beträge).</summary>
        public decimal? OpenAmount { get; set; }

        /// <summary>
        /// True, wenn der Zahlungseingang kleiner ist als der offene Rechnungsbetrag (Teilzahlung).
        /// Teilzahlungen werden erfasst, ohne die Rechnung vollständig als bezahlt zu markieren.
        /// </summary>
        private bool isPartialPayment;
        public bool IsPartialPayment
        {
            get => isPartialPayment;
            set { isPartialPayment = value; OnChanged(nameof(IsPartialPayment)); OnChanged(nameof(StatusDisplay)); OnChanged(nameof(BookingModeDisplay)); }
        }

        private ReconciliationMatchStatus status;
        public ReconciliationMatchStatus Status
        {
            get => status;
            set { status = value; OnChanged(nameof(Status)); OnChanged(nameof(StatusDisplay)); OnChanged(nameof(CanBook)); }
        }

        /// <summary>Erläuterung der Zuordnung (z. B. "Nummer + Betrag exakt").</summary>
        public string Reason { get; set; } = "";

        /// <summary>True, wenn der Betrag exakt dem Bruttobetrag der Rechnung entspricht.</summary>
        public bool AmountMatches { get; set; }

        /// <summary>
        /// True, wenn dem Auftraggeber der Überweisung genau EINE offene Rechnung eindeutig
        /// zugeordnet werden konnte. Nur solche eindeutigen Kundentreffer dürfen im Hintergrund
        /// vollautomatisch gebucht werden – auch als Teilzahlung.
        /// </summary>
        public bool IsUniqueCustomerMatch { get; set; }

        /// <summary>
        /// True, wenn dieser Treffer ohne manuelle Bestätigung gebucht werden darf: eine
        /// zugeordnete Rechnung existiert, sie wurde noch nicht gebucht und der Kunde ist eindeutig.
        /// Deckt sowohl eindeutige Vollzahlungen (Status = Automatic) als auch eindeutige
        /// Teilzahlungen ab.
        /// </summary>
        [JsonIgnore]
        public bool IsAutoBookable =>
            CanBook && IsUniqueCustomerMatch &&
            (Status == ReconciliationMatchStatus.Automatic ||
             (Status == ReconciliationMatchStatus.NeedsConfirmation && IsPartialPayment));

        // ── Buchungszustand (Laufzeit/UI) ──
        private bool booked;
        public bool Booked
        {
            get => booked;
            set { booked = value; OnChanged(nameof(Booked)); OnChanged(nameof(StatusDisplay)); OnChanged(nameof(CanBook)); }
        }

        private bool alreadyBooked;
        public bool AlreadyBooked
        {
            get => alreadyBooked;
            set { alreadyBooked = value; OnChanged(nameof(AlreadyBooked)); OnChanged(nameof(StatusDisplay)); OnChanged(nameof(CanBook)); }
        }

        private string? bookingError;
        public string? BookingError
        {
            get => bookingError;
            set { bookingError = value; OnChanged(nameof(BookingError)); OnChanged(nameof(StatusDisplay)); }
        }

        /// <summary>UI-Auswahl für die manuelle Sammelbuchung (Checkbox-Spalte).</summary>
        private bool selected;
        public bool Selected
        {
            get => selected;
            set { selected = value; OnChanged(nameof(Selected)); }
        }

        // ── Anzeige-Eigenschaften für das DataGrid ──

        [JsonIgnore]
        public string InvoiceNumberDisplay => MatchedInvoiceNumber ?? Invoice?.Number ?? "–";

        [JsonIgnore]
        public string InvoiceAmountDisplay =>
            Invoice?.TotalGross is decimal g ? g.ToString("N2") + " €" : "–";

        /// <summary>Betrag, der tatsächlich gebucht wird (= Zahlungseingang).</summary>
        [JsonIgnore]
        public decimal BookingAmount => Transaction?.Amount ?? 0m;

        /// <summary>Kennzeichnet die Buchungsart (Vollzahlung vs. Teilzahlung) für die Anzeige.</summary>
        [JsonIgnore]
        public string BookingModeDisplay
        {
            get
            {
                if (Invoice?.Id == null) return "–";
                if (IsPartialPayment)
                {
                    var rest = OpenAmount is decimal open ? open - BookingAmount : (decimal?)null;
                    return rest.HasValue
                        ? $"Teilzahlung (Rest {rest.Value:N2} €)"
                        : "Teilzahlung";
                }
                return "Vollzahlung";
            }
        }

        [JsonIgnore]
        public bool CanBook =>
            !Booked && !AlreadyBooked && Invoice?.Id != null &&
            (Status == ReconciliationMatchStatus.Automatic || Status == ReconciliationMatchStatus.NeedsConfirmation);

        [JsonIgnore]
        public string StatusDisplay
        {
            get
            {
                if (!string.IsNullOrEmpty(BookingError)) return "❌ Fehler";
                if (Booked) return IsPartialPayment ? "✅ Teilzahlung gebucht" : "✅ Gebucht";
                if (AlreadyBooked) return "☑️ Bereits gebucht";
                return Status switch
                {
                    ReconciliationMatchStatus.Automatic => "⚡ Automatisch",
                    ReconciliationMatchStatus.NeedsConfirmation => IsPartialPayment ? "❓ Teilzahlung prüfen" : "❓ Prüfen",
                    _ => "– Kein Treffer"
                };
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged(string name) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    // ── Lokales Abgleich-Protokoll ──────────────────────────────────────

    /// <summary>
    /// Protokolleintrag einer erfolgten Zahlungszuordnung (lokale Nachverfolgung
    /// und Schutz vor Doppelbuchung anhand des <see cref="TransactionHash"/>).
    /// </summary>
    public class ReconciliationLogEntry
    {
        public string TransactionHash { get; set; } = "";
        public long? EasybillDocumentId { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public string PartnerName { get; set; } = "";
        public long AmountCents { get; set; }
        public DateTime ValueDate { get; set; }
        public DateTime BookedAt { get; set; }

        [JsonIgnore]
        public string AmountDisplay => (AmountCents / 100m).ToString("N2") + " €";

        [JsonIgnore]
        public string ValueDateDisplay => ValueDate.ToString("dd.MM.yyyy");

        [JsonIgnore]
        public string BookedAtDisplay => BookedAt.ToString("dd.MM.yyyy HH:mm");
    }
}
