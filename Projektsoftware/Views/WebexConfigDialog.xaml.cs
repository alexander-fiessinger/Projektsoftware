using Projektsoftware.Services;
using System;
using System.Windows;
using System.Windows.Media;

namespace Projektsoftware.Views
{
    public partial class WebexConfigDialog : Window
    {
        public WebexConfigDialog()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            var config = WebexConfig.Load();
            AccessTokenBox.Password = config.AccessToken;
            BotNameBox.Text = config.BotName;
            SendEmailsCheckBox.IsChecked = config.SendInviteEmails;
        }

        private async void Test_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AccessTokenBox.Password))
            {
                ShowStatus("Bitte geben Sie zuerst ein Access Token ein.", false);
                return;
            }

            TestButton.IsEnabled = false;
            TestButton.Content = "Teste Verbindung...";
            StatusBorder.Visibility = Visibility.Collapsed;

            try
            {
                // Temp-Config zum Testen speichern
                var tempConfig = new WebexConfig
                {
                    AccessToken = AccessTokenBox.Password,
                    BotName = BotNameBox.Text,
                    SendInviteEmails = SendEmailsCheckBox.IsChecked == true
                };
                tempConfig.Save();

                var service = new WebexService();
                var person = await service.TestConnectionAsync();
                ShowStatus($"Verbindung erfolgreich!\nAngemeldet als: {person.DisplayName} ({person.Email})", true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Verbindung fehlgeschlagen:\n{ex.Message}", false);
            }
            finally
            {
                TestButton.IsEnabled = true;
                TestButton.Content = "Verbindung testen";
            }
        }

        private void ShowStatus(string message, bool success)
        {
            StatusBorder.Visibility = Visibility.Visible;
            StatusBorder.Background = new SolidColorBrush(
                success ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));
            StatusTextBlock.Text = message;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AccessTokenBox.Password))
            {
                MessageBox.Show("Bitte geben Sie ein Access Token ein.",
                    "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var config = new WebexConfig
            {
                AccessToken = AccessTokenBox.Password,
                BotName = BotNameBox.Text?.Trim() ?? "Projektierungssoftware",
                SendInviteEmails = SendEmailsCheckBox.IsChecked == true
            };
            config.Save();

            MessageBox.Show("Webex-Konfiguration gespeichert.",
                "Gespeichert", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
