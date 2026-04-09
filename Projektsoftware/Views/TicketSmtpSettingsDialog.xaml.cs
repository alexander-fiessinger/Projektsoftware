using Projektsoftware.Services;
using System;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class TicketSmtpSettingsDialog : Window
    {
        public TicketSmtpSettingsDialog()
        {
            InitializeComponent();
            var config = TicketSmtpConfig.Load();
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
            SuccessPanel.Visibility = ErrorPanel.Visibility = Visibility.Collapsed;
            var config = BuildConfig();
            ShowStatus($"⏳ Test-E-Mail wird an {config.Email} gesendet...");
            try
            {
                await new ExchangeEmailService(config.ToExchangeConfig()).SendEmailAsync(
                    config.Email,
                    "Ticket-SMTP Test – Projektsoftware",
                    $"Diese Test-E-Mail wurde erfolgreich über die Ticket-SMTP-Konfiguration gesendet.\n\nServer: {config.SmtpServer}\nPort: {config.SmtpPort}\nAbsender: {config.Email}");
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
            SuccessPanel.Visibility = ErrorPanel.Visibility = Visibility.Collapsed;
            ShowStatus("⏳ Verbindung wird getestet...");
            var result = await new ExchangeEmailService(BuildConfig().ToExchangeConfig()).TestConnectionAsync();
            if (result.Success)
                ShowSuccess($"✅ Verbindung erfolgreich!\nServer: {SmtpServerTextBox.Text.Trim()}  Port: {PortTextBox.Text}");
            else
ShowError("Verbindung fehlgeschlagen: " + result.Message);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;
            try
            {
                BuildConfig().Save();
                MessageBox.Show("Ticket-SMTP-Konfiguration erfolgreich gespeichert!", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private TicketSmtpConfig BuildConfig()
        {
            int.TryParse(PortTextBox.Text, out var port);
            if (port == 0) port = 465;
            return new TicketSmtpConfig
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

        private void ShowStatus(string msg) => StatusTextBlock.Text = msg;

        private void ShowSuccess(string msg)
        {
            StatusTextBlock.Text = "";
            SuccessPanel.Visibility = Visibility.Visible;
            SuccessText.Text = msg;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string msg)
        {
            StatusTextBlock.Text = "";
            ErrorPanel.Visibility = Visibility.Visible;
            ErrorText.Text = msg;
            SuccessPanel.Visibility = Visibility.Collapsed;
        }
    }
}
