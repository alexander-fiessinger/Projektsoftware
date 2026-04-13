using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class EasybillConfigDialog : Window
    {
        private EasybillConfig config;

        public EasybillConfigDialog()
        {
            InitializeComponent();
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            config = EasybillConfig.Load();
            EmailTextBox.Text = config.Email;
            ApiKeyTextBox.Text = config.ApiKey;
            ApiUrlTextBox.Text = config.ApiUrl;
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            TestResultPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            StatusTextBlock.Text = "⏳ Teste Verbindung...";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;

            try
            {
                var email = EmailTextBox.Text.Trim();
                var apiKey = ApiKeyTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(email))
                {
                    MessageBox.Show("Bitte geben Sie Ihre E-Mail-Adresse ein.", "Validierung",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    StatusTextBlock.Text = "";
                    return;
                }

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show("Bitte geben Sie einen API-Schlüssel ein.", "Validierung",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    StatusTextBlock.Text = "";
                    return;
                }

                var easybillService = new EasybillService(email, apiKey);
                var (success, message) = await easybillService.TestConnectionAsync();

                if (success)
                {
                    var customers = await easybillService.GetAllCustomersAsync();

                    StatusTextBlock.Text = "";
                    TestResultPanel.Visibility = Visibility.Visible;
                    CustomerCountText.Text = $"Gefunden: {customers.Count} Kunden in Ihrem Easybill-Account";
                }
                else
                {
                    ShowError($"Verbindung fehlgeschlagen:\n\n{message}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Fehler: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Text = "";
            TestResultPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            ErrorMessageText.Text = message;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie Ihre E-Mail-Adresse ein.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ApiKeyTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen API-Schlüssel ein.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                config.ApiUrl = ApiUrlTextBox.Text.Trim();
                config.Email = EmailTextBox.Text.Trim();
                config.ApiKey = ApiKeyTextBox.Text.Trim();
                config.Save();

                // Benutzerspezifische Zugangsdaten in DB speichern
                var db = new DatabaseService();
                await UserCredentialService.SaveManyAsync(new Dictionary<string, string>
                {
                    [UserCredentialService.EasybillEmail] = config.Email,
                    [UserCredentialService.EasybillApiKey] = config.ApiKey
                }, db);

                MessageBox.Show("Easybill-Konfiguration erfolgreich gespeichert!", "Erfolg",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
