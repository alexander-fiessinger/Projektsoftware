using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class ExchangeSettingsDialog : Window
    {
        public ExchangeSettingsDialog()
        {
            InitializeComponent();
            var config = ExchangeConfig.Load();
            SenderNameTextBox.Text = config.SenderName;
            SmtpServerTextBox.Text = config.SmtpServer;
            EmailTextBox.Text = config.Email;
            PasswordBox.Password = config.Password;
            PortTextBox.Text = config.SmtpPort > 0 ? config.SmtpPort.ToString() : "465";
            UseSslCheckBox.IsChecked = config.SmtpPort != 587;
            AcceptInvalidCertificatesCheckBox.IsChecked = config.AcceptInvalidCertificates;
        }

        private async void SendTestEmail_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;
            SuccessPanel.Visibility = ErrorPanel.Visibility = System.Windows.Visibility.Collapsed;
            var config = BuildConfig();
            ShowStatus($"⏳ Test-E-Mail wird an {config.Email} gesendet...");
            try
            {
                await new ExchangeEmailService(config).SendEmailAsync(
                    config.Email,
                    "SMTP Test – Projektsoftware",
                    $"Diese Test-E-Mail wurde erfolgreich über die SMTP-Konfiguration der Projektsoftware gesendet.\n\nServer: {config.SmtpServer}\nPort: {config.SmtpPort}\nAbsender: {config.Email}");
                ShowSuccess($"✅ Test-E-Mail erfolgreich gesendet!\nEmpfänger: {config.Email}");
            }
            catch (Exception ex)
            {
                ShowError("Senden fehlgeschlagen: " + (ex.InnerException?.Message ?? ex.Message));
            }
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;
            SuccessPanel.Visibility = ErrorPanel.Visibility = System.Windows.Visibility.Collapsed;
            ShowStatus("⏳ Verbindung wird getestet...");
            var result = await new ExchangeEmailService(BuildConfig()).TestConnectionAsync();
            if (result.Success)
                ShowSuccess($"✅ Verbindung erfolgreich!\nServer: {SmtpServerTextBox.Text.Trim()}  Port: {PortTextBox.Text}");
            else
ShowError("Verbindung fehlgeschlagen: " + result.Message);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;
            try
            {
                var config = BuildConfig();
                config.Save();

                // Benutzerspezifische Zugangsdaten in DB speichern
                var db = new DatabaseService();
                await UserCredentialService.SaveManyAsync(new Dictionary<string, string>
                {
                    [UserCredentialService.ExchangeEmail] = config.Email,
                    [UserCredentialService.ExchangePassword] = config.Password,
                    [UserCredentialService.ExchangeSenderName] = config.SenderName
                }, db);

                MessageBox.Show("SMTP-Konfiguration erfolgreich gespeichert!", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Speichern: " + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool Validate()
        {
            if (string.IsNullOrWhiteSpace(SmtpServerTextBox.Text))
            { MessageBox.Show("Bitte SMTP-Server eingeben.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            { MessageBox.Show("Bitte Login-ID eingeben.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            { MessageBox.Show("Bitte Passwort eingeben.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            return true;
        }

        private ExchangeConfig BuildConfig()
        {
            int.TryParse(PortTextBox.Text, out var port);
            if (port == 0) port = 465;
            return new ExchangeConfig
            {
                SenderName = SenderNameTextBox.Text.Trim(),
                SmtpServer = SmtpServerTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                Password = PasswordBox.Password,
                SmtpPort = port,
                UseSsl = UseSslCheckBox.IsChecked == true,
                AcceptInvalidCertificates = AcceptInvalidCertificatesCheckBox.IsChecked == true
            };
        }

private void ShowStatus(string msg) { StatusTextBlock.Text = msg; SuccessPanel.Visibility = System.Windows.Visibility.Collapsed; ErrorPanel.Visibility = System.Windows.Visibility.Collapsed; }
        private void ShowSuccess(string msg) { StatusTextBlock.Text = ""; SuccessPanel.Visibility = System.Windows.Visibility.Visible; SuccessText.Text = msg; ErrorPanel.Visibility = System.Windows.Visibility.Collapsed; }
        private void ShowError(string msg) { StatusTextBlock.Text = ""; ErrorPanel.Visibility = System.Windows.Visibility.Visible; ErrorText.Text = msg; SuccessPanel.Visibility = System.Windows.Visibility.Collapsed; }
    }
}
