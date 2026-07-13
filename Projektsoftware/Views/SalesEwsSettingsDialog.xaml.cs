using Projektsoftware.Services;
using System;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class SalesEwsSettingsDialog : Window
    {
        private const string SalesEmail = "sales@af-software-engineering.de";

        public SalesEwsSettingsDialog()
        {
            InitializeComponent();

            // Sales-Config laden (füllt fehlende Werte aus Haupt-Config)
            var config = EwsConfig.LoadSales();
            EwsUrlTextBox.Text                          = config.EwsUrl;
            EwsPasswordBox.Password                     = config.EwsPassword;
            UseWindowsAuthCheckBox.IsChecked            = config.UseWindowsAuth;
            EwsUsernameTextBox.Text                     = config.EwsUsername;
            EwsDomainTextBox.Text                       = config.EwsDomain;
            AcceptInvalidCertificatesCheckBox.IsChecked = config.AcceptInvalidCertificates;
            UseBasicAuthCheckBox.IsChecked              = config.UseBasicAuth;
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;
            SuccessPanel.Visibility = ErrorPanel.Visibility = Visibility.Collapsed;
            ShowStatus("⏳ EWS-Verbindung wird getestet...");
            var result = await new EwsService(BuildConfig()).TestConnectionAsync();
            if (result.Success)
                ShowSuccess($"✅ EWS-Verbindung erfolgreich!\n{EwsUrlTextBox.Text.Trim()}\nAngemeldet als: {result.Error}");
            else
            {
                var hint = result.Error.Contains("401")
                    ? "\n\n❗ 401 = Benutzername/Passwort falsch oder EWS nicht aktiviert"
                    : string.Empty;
                ShowError("EWS-Verbindung fehlgeschlagen: " + result.Error + hint);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;
            try
            {
                await BuildConfig().SaveSalesAsync();
                MessageBox.Show("Sales-EWS-Konfiguration erfolgreich gespeichert!\n(In Datenbank – gilt für alle Rechner)",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (UseWindowsAuthCheckBox.IsChecked != true && string.IsNullOrWhiteSpace(EwsPasswordBox.Password))
            {
                MessageBox.Show("Bitte Passwort eingeben.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(EwsUrlTextBox.Text))
            {
                MessageBox.Show("Bitte EWS-URL eingeben.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private EwsConfig BuildConfig() => new()
        {
            EwsUrl                  = EwsUrlTextBox.Text.Trim(),
            EwsEmail                = SalesEmail,
            EwsPassword             = EwsPasswordBox.Password,
            UseWindowsAuth          = UseWindowsAuthCheckBox.IsChecked == true,
            EnableEwsFetch          = true,
            EwsUsername             = EwsUsernameTextBox.Text.Trim(),
            EwsDomain               = EwsDomainTextBox.Text.Trim(),
            AcceptInvalidCertificates = AcceptInvalidCertificatesCheckBox.IsChecked == true,
            UseBasicAuth            = UseBasicAuthCheckBox.IsChecked == true
        };

        private void ShowStatus(string msg) { StatusTextBlock.Text = msg; }
        private void ShowSuccess(string msg) { StatusTextBlock.Text = ""; SuccessPanel.Visibility = Visibility.Visible; SuccessText.Text = msg; ErrorPanel.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg)   { StatusTextBlock.Text = ""; ErrorPanel.Visibility = Visibility.Visible; ErrorText.Text = msg; SuccessPanel.Visibility = Visibility.Collapsed; }
    }
}
