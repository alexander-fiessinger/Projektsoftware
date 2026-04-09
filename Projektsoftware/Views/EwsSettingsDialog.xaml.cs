using Projektsoftware.Services;
using System;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class EwsSettingsDialog : Window
    {
        public EwsSettingsDialog()
        {
            InitializeComponent();
            var config = EwsConfig.Load();
            EwsUrlTextBox.Text = config.EwsUrl;
            EwsEmailTextBox.Text = config.EwsEmail;
            EwsPasswordBox.Password = config.EwsPassword;
            UseWindowsAuthCheckBox.IsChecked = config.UseWindowsAuth;
            EnableEwsCheckBox.IsChecked = config.EnableEwsFetch;
            EwsUsernameTextBox.Text = config.EwsUsername;
            EwsDomainTextBox.Text = config.EwsDomain;
            AcceptInvalidCertificatesCheckBox.IsChecked = config.AcceptInvalidCertificates;
            UseBasicAuthCheckBox.IsChecked = config.UseBasicAuth;
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            var ewsUrlRaw = EwsUrlTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(ewsUrlRaw))
            { ShowError("Bitte EWS-URL eingeben."); return; }
            if (!Uri.TryCreate(ewsUrlRaw, UriKind.Absolute, out var ewsUri)
                || (ewsUri.Scheme != "https" && ewsUri.Scheme != "http"))
            {
                ShowError("Ungültige EWS-URL. Die URL muss mit https:// beginnen,\nz. B. https://mac.hex2013.com/EWS/exchange.asmx");
                return;
            }
            if (!Validate()) return;
            SuccessPanel.Visibility = ErrorPanel.Visibility = Visibility.Collapsed;
            ShowStatus("⏳ EWS-Verbindung wird getestet...");
            var result = await new EwsService(BuildConfig()).TestConnectionAsync();
            if (result.Success)
                ShowSuccess($"✅ EWS-Verbindung erfolgreich!\n{EwsUrlTextBox.Text.Trim()}\nAngemeldet als: {result.Error}");
            else
            {
                var hint = result.Error.Contains("401")
                    ? "\n\n❗ 401 = Benutzername/Passwort falsch oder EWS nicht aktiviert\n\n" +
                      "💡 Bitte prüfen:\n" +
                      "• Passwort unter https://outlook.hex2013.com (OWA) verifizieren\n" +
                      "• Login-ID = vollständige E-Mail-Adresse\n" +
                      "• Alternative URL testen: https://outlook.hex2013.com/EWS/exchange.asmx\n" +
                      "• 'Nur Basic Auth' an/aus wechseln und erneut testen\n" +
                      "• dogado-Support: EWS für dieses Konto aktiviert?"
                    : string.Empty;
                ShowError("EWS-Verbindung fehlgeschlagen: " + result.Error + hint);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;
            try
            {
                BuildConfig().Save();
                MessageBox.Show("EWS-Konfiguration erfolgreich gespeichert!", "Erfolg",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Speichern: " + ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool Validate()
        {
            if (UseWindowsAuthCheckBox.IsChecked != true)
            {
                if (string.IsNullOrWhiteSpace(EwsEmailTextBox.Text))
                { MessageBox.Show("Bitte E-Mail / Login-ID eingeben.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
                if (string.IsNullOrWhiteSpace(EwsPasswordBox.Password))
                { MessageBox.Show("Bitte Passwort eingeben.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            }
            return true;
        }

        private EwsConfig BuildConfig() => new()
        {
            EwsUrl = EwsUrlTextBox.Text.Trim(),
            EwsEmail = EwsEmailTextBox.Text.Trim(),
            EwsPassword = EwsPasswordBox.Password,
            UseWindowsAuth = UseWindowsAuthCheckBox.IsChecked == true,
            EnableEwsFetch = EnableEwsCheckBox.IsChecked == true,
            EwsUsername = EwsUsernameTextBox.Text.Trim(),
            EwsDomain = EwsDomainTextBox.Text.Trim(),
            AcceptInvalidCertificates = AcceptInvalidCertificatesCheckBox.IsChecked == true,
            UseBasicAuth = UseBasicAuthCheckBox.IsChecked == true
        };

        private void ShowStatus(string msg) { StatusTextBlock.Text = msg; }
        private void ShowSuccess(string msg) { StatusTextBlock.Text = ""; SuccessPanel.Visibility = Visibility.Visible; SuccessText.Text = msg; ErrorPanel.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg) { StatusTextBlock.Text = ""; ErrorPanel.Visibility = Visibility.Visible; ErrorText.Text = msg; SuccessPanel.Visibility = Visibility.Collapsed; }
    }
}
