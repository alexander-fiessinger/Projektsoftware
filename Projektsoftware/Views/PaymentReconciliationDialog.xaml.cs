using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class PaymentReconciliationDialog : Window
    {
        private readonly EasybillService easybillService;
        private readonly ReconciliationLogStore logStore;
        private readonly ObservableCollection<ReconciliationMatch> matches = new();

        public PaymentReconciliationDialog()
        {
            InitializeComponent();

            easybillService = new EasybillService();
            logStore = new ReconciliationLogStore();
            MatchesDataGrid.ItemsSource = matches;

            FromDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
            ToDatePicker.SelectedDate = DateTime.Today;

            LoadBankSettings();
            UpdateEmptyHint();
        }

        // ── Bank-Einstellungen ─────────────────────────────────────────

        private void LoadBankSettings()
        {
            var config = BankConfig.Load();
            ApiBaseUrlTextBox.Text = config.ApiBaseUrl;
            TenantTextBox.Text = config.Tenant;
            ClientIdTextBox.Text = config.ClientId;
            ClientSecretPasswordBox.Password = config.ClientSecret;
            AccountHolderTextBox.Text = config.AccountHolder;
            ProductIdTextBox.Text = config.ProductId;

            UpdateBankAccessStatus(config);

            if (!config.IsConfigured || !config.HasBankAccess)
            {
                BankSettingsExpander.IsExpanded = true;
            }
        }

        private void UpdateBankAccessStatus(BankConfig config)
        {
            BankAccessStatusTextBlock.Text = config.HasBankAccess
                ? $"✅ Bankzugang eingerichtet (Zugang {config.AccessId})."
                : "Es wurde noch kein Bankzugang eingerichtet. Bitte 'Bankverbindung einrichten' ausführen.";
        }

        private BankConfig BuildBankConfig()
        {
            var existing = BankConfig.Load();
            return new BankConfig
            {
                ApiBaseUrl = ApiBaseUrlTextBox.Text.Trim(),
                Tenant = TenantTextBox.Text.Trim(),
                ClientId = ClientIdTextBox.Text.Trim(),
                ClientSecret = ClientSecretPasswordBox.Password,
                AccountHolder = AccountHolderTextBox.Text.Trim(),
                ProductId = ProductIdTextBox.Text.Trim(),
                // Vom Einrichtungs-Flow verwaltete Werte erhalten:
                BanksApiUsername = existing.BanksApiUsername,
                BanksApiPassword = existing.BanksApiPassword,
                AccessId = existing.AccessId
            };
        }

        private void SaveBankSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = BuildBankConfig();
                config.Save();
                BankStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                BankStatusTextBlock.Text = "✅ Bankverbindung gespeichert.";
            }
            catch (Exception ex)
            {
                BankStatusTextBlock.Foreground = System.Windows.Media.Brushes.IndianRed;
                BankStatusTextBlock.Text = "❌ Fehler beim Speichern: " + ex.Message;
            }
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            var config = BuildBankConfig();
            if (!config.IsConfigured)
            {
                BankStatusTextBlock.Foreground = System.Windows.Media.Brushes.IndianRed;
                BankStatusTextBlock.Text = "Bitte BANKSapi-URL, Mandant, Client-ID und Client-Secret ausfüllen.";
                return;
            }

            try
            {
                IsEnabled = false;
                BankStatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
                BankStatusTextBlock.Text = "⏳ Verbindung zur BANKSapi wird getestet...";

                var (success, message) = await new BanksApiService(config).TestConnectionAsync();

                if (string.IsNullOrWhiteSpace(message))
                    message = success ? "Verbindung erfolgreich!" : "Unbekannter Fehler beim Verbindungstest.";

                BankStatusTextBlock.Foreground = success
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.IndianRed;
                BankStatusTextBlock.Text = (success ? "✅ " : "❌ ") + message;

                if (!success)
                {
                    MessageBox.Show(
                        message +
                        "\n\nBitte prüfen Sie:" +
                        "\n• BANKSapi-Server-URL (z. B. https://banksapi.io)" +
                        "\n• Mandant (Tenant) und Client-ID" +
                        "\n• Client-Secret",
                        "Verbindungstest fehlgeschlagen",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                BankStatusTextBlock.Foreground = System.Windows.Media.Brushes.IndianRed;
                BankStatusTextBlock.Text = "❌ Fehler: " + ex.Message;
            }
            finally
            {
                IsEnabled = true;
            }
        }

        // ── Bankzugang einrichten (WebForm) ────────────────────────────

        private async void SetupBankAccess_Click(object sender, RoutedEventArgs e)
        {
            var config = BuildBankConfig();
            if (!config.IsConfigured)
            {
                MessageBox.Show(
                    "Bitte zuerst die BANKSapi-Zugangsdaten (URL, Mandant, Client-ID, Client-Secret) eingeben und speichern.",
                    "Zugangsdaten fehlen", MessageBoxButton.OK, MessageBoxImage.Warning);
                BankSettingsExpander.IsExpanded = true;
                return;
            }

            try
            {
                IsEnabled = false;
                BankStatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
                BankStatusTextBlock.Text = "⏳ Bankzugang wird vorbereitet...";

                var service = new BanksApiService(config);
                var (webFormUrl, accessId) = await service.StartBankAccessSetupAsync();

                // Vom Service gesetzte Werte (User, ggf. Speicherung) übernehmen und Access-ID sichern.
                config.AccessId = accessId;
                config.Save();
                LoadBankSettings();

                if (string.IsNullOrWhiteSpace(webFormUrl))
                {
                    BankStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                    BankStatusTextBlock.Text = "✅ Bankzugang wurde direkt eingerichtet.";
                    return;
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = webFormUrl,
                    UseShellExecute = true
                });

                BankStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                BankStatusTextBlock.Text = "✅ Bank-Login im Browser geöffnet. Nach Abschluss auf 'Einrichtung abgeschlossen' klicken.";

                MessageBox.Show(
                    "Im Browser wurde die BANKSapi-WebForm geöffnet.\n\n" +
                    "Bitte wählen Sie dort Ihre Bank aus, melden Sie sich an und bestätigen Sie die Freigabe (SCA).\n\n" +
                    "Klicken Sie anschließend hier auf 'Einrichtung abgeschlossen', um die freigegebenen Konten zu laden.",
                    "Bankverbindung einrichten", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                BankStatusTextBlock.Foreground = System.Windows.Media.Brushes.IndianRed;
                BankStatusTextBlock.Text = "❌ Einrichtung fehlgeschlagen: " + ex.Message;
                MessageBox.Show(
                    "Die Einrichtung des Bankzugangs ist fehlgeschlagen:\n\n" + ex.Message +
                    "\n\nTipp: Sollte der Fehler bestehen bleiben, schließen Sie ggf. offene BANKSapi-WebForm-" +
                    "Browsertabs und versuchen Sie es erneut. Die Anwendung legt bei einem Konflikt automatisch " +
                    "einen frischen Bankzugang an.",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private async void ConfirmBankAccess_Click(object sender, RoutedEventArgs e)
        {
            var config = BuildBankConfig();
            if (!config.HasBankAccess)
            {
                MessageBox.Show(
                    "Es wurde noch kein Bankzugang gestartet. Bitte zuerst 'Bankverbindung einrichten' ausführen.",
                    "Kein Bankzugang", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsEnabled = false;
                BankStatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
                BankStatusTextBlock.Text = "⏳ Freigegebene Konten werden geladen...";

                var service = new BanksApiService(config);
                var products = await service.GetBankProductsAsync();

                if (products.Count == 0)
                {
                    BankStatusTextBlock.Foreground = System.Windows.Media.Brushes.IndianRed;
                    BankStatusTextBlock.Text = "❌ Es wurden noch keine Konten gefunden. Wurde die WebForm-Einrichtung abgeschlossen?";
                    return;
                }

                // Wenn genau ein Konto vorhanden ist, dieses direkt übernehmen.
                if (products.Count == 1 && string.IsNullOrWhiteSpace(config.ProductId))
                {
                    config.ProductId = products[0].ProductId;
                    ProductIdTextBox.Text = products[0].ProductId;
                    config.Save();
                }

                var list = string.Join("\n", products.Select(p => $"• {p.Display}  [{p.ProductId}]"));
                BankStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                BankStatusTextBlock.Text = $"✅ {products.Count} Konto/Konten gefunden.";

                MessageBox.Show(
                    "Folgende Konten sind über den Bankzugang verfügbar:\n\n" + list +
                    "\n\nTragen Sie bei Bedarf die gewünschte Konto-/Produkt-ID im Feld ein und speichern Sie.",
                    "Konten geladen", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                BankStatusTextBlock.Foreground = System.Windows.Media.Brushes.IndianRed;
                BankStatusTextBlock.Text = "❌ Fehler: " + ex.Message;
            }
            finally
            {
                IsEnabled = true;
            }
        }

        // ── Abruf + Abgleich ───────────────────────────────────────────

        private async void FetchAndMatch_Click(object sender, RoutedEventArgs e)
        {
            var config = BuildBankConfig();
            if (!config.IsConfigured)
            {
                MessageBox.Show(
                    "Die BANKSapi-Zugangsdaten sind noch nicht vollständig konfiguriert.\n\n" +
                    "Bitte URL, Mandant, Client-ID und Client-Secret eingeben und speichern.",
                    "Zugangsdaten fehlen", MessageBoxButton.OK, MessageBoxImage.Warning);
                BankSettingsExpander.IsExpanded = true;
                return;
            }

            if (!config.HasBankAccess)
            {
                MessageBox.Show(
                    "Es wurde noch kein Bankzugang eingerichtet.\n\n" +
                    "Bitte zuerst 'Bankverbindung einrichten' ausführen und den Bank-Login im Browser abschließen.",
                    "Bankzugang fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                BankSettingsExpander.IsExpanded = true;
                return;
            }

            if (!easybillService.IsConfigured)
            {
                MessageBox.Show(
                    "Easybill ist nicht konfiguriert. Bitte zuerst unter 'Easybill → Konfiguration' die API-Zugangsdaten hinterlegen.",
                    "Easybill nicht konfiguriert", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var from = FromDatePicker.SelectedDate ?? DateTime.Today.AddDays(-30);
            var to = ToDatePicker.SelectedDate ?? DateTime.Today;
            if (from > to)
            {
                MessageBox.Show("Das Startdatum liegt nach dem Enddatum.", "Ungültiger Zeitraum",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsEnabled = false;
                matches.Clear();
                UpdateEmptyHint();
                StatusTextBlock.Text = "⏳ Rufe Kontoumsätze per BANKSapi ab...";
                SummaryTextBlock.Text = "";

                var banksApi = new BanksApiService(config);
                var txResult = await banksApi.GetIncomingTransactionsAsync(from, to);

                if (!txResult.Success)
                {
                    StatusTextBlock.Text = "❌ Kontoabruf fehlgeschlagen.";
                    MessageBox.Show(txResult.Message, "Kontoabruf fehlgeschlagen",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusTextBlock.Text = "⏳ Lade offene Rechnungen aus Easybill...";
                var invoices = await easybillService.GetAllDocumentsAsync("INVOICE");

                var reconciliation = new PaymentReconciliationService();
                var results = reconciliation.Match(txResult.Transactions, invoices);

                foreach (var m in results)
                {
                    m.AlreadyBooked = logStore.IsAlreadyBooked(m.Transaction.TransactionHash);
                    if (m.IsAutoBookable)
                    {
                        m.Selected = true; // eindeutige Treffer (Voll-/Teilzahlung) für die Buchung vorauswählen
                    }
                    matches.Add(m);
                }

                UpdateEmptyHint();

                var autoCount = matches.Count(m => m.IsAutoBookable);
                StatusTextBlock.Text = $"✅ {matches.Count} Zahlungseingänge gefunden.";
                SummaryTextBlock.Text = $"Eindeutig: {autoCount} · Prüfen: " +
                    $"{matches.Count(m => m.Status == ReconciliationMatchStatus.NeedsConfirmation)} · " +
                    $"Ohne Treffer: {matches.Count(m => m.Status == ReconciliationMatchStatus.NoMatch)}";

                if (AutoBookCheckBox.IsChecked == true && autoCount > 0)
                {
                    await BookMatchesAsync(matches.Where(m => m.IsAutoBookable).ToList(), auto: true);
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "❌ Fehler beim Abgleich.";
                MessageBox.Show("Fehler beim Zahlungsabgleich:\n\n" + ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEnabled = true;
            }
        }

        // ── Buchen ─────────────────────────────────────────────────────

        private async void BookSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = matches.Where(m => m.Selected && m.CanBook).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Bitte mindestens einen buchbaren Zahlungseingang auswählen.",
                    "Keine Auswahl", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"{selected.Count} Zahlung(en) in Easybill als bezahlt buchen?",
                "Buchung bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                IsEnabled = false;
                await BookMatchesAsync(selected, auto: false);
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private async Task BookMatchesAsync(List<ReconciliationMatch> toBook, bool auto)
        {
            int booked = 0, failed = 0;

            foreach (var match in toBook)
            {
                if (!match.CanBook || match.Invoice?.Id == null) continue;

                try
                {
                    var bookingLabel = match.IsPartialPayment ? "Teilzahlung" : "Zahlung";
                    StatusTextBlock.Text = $"⏳ Buche {bookingLabel} für Rechnung {match.InvoiceNumberDisplay}...";
                    var paidAt = match.Transaction.ValueDate.ToString("yyyy-MM-dd");

                    // Teilzahlungen werden erfasst, ohne die Rechnung vollständig als bezahlt zu markieren.
                    await easybillService.MarkDocumentAsPaidAsync(
                        match.Invoice.Id.Value,
                        paidAt,
                        match.Transaction.Amount,
                        markAsPaid: !match.IsPartialPayment);

                    match.Booked = true;
                    match.Selected = false;
                    match.BookingError = null;
                    logStore.Add(match);
                    booked++;
                }
                catch (Exception ex)
                {
                    match.BookingError = ex.Message;
                    failed++;
                }
            }

            var prefix = auto ? "Automatisch gebucht" : "Gebucht";
            StatusTextBlock.Text = failed == 0
                ? $"✅ {prefix}: {booked} Zahlung(en)."
                : $"⚠️ {prefix}: {booked} · Fehler: {failed}.";

            if (!auto && failed > 0)
            {
                MessageBox.Show(
                    $"{booked} Zahlung(en) gebucht, {failed} fehlgeschlagen.\n\n" +
                    "Details finden Sie in der Statusspalte (Tooltip).",
                    "Buchung abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ── Protokoll ──────────────────────────────────────────────────

        private void ShowLog_Click(object sender, RoutedEventArgs e)
        {
            var entries = logStore.Entries;
            if (entries.Count == 0)
            {
                MessageBox.Show("Es wurden noch keine Zahlungen gebucht.", "Protokoll",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var text = string.Join(Environment.NewLine, entries.Select(en =>
                $"{en.BookedAtDisplay}  |  {en.ValueDateDisplay}  |  Rg. {en.InvoiceNumber}  |  " +
                $"{en.AmountDisplay}  |  {en.PartnerName}"));

            MessageBox.Show(text, $"Abgleich-Protokoll ({entries.Count} Einträge)",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateEmptyHint()
        {
            EmptyHintTextBlock.Visibility = matches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
