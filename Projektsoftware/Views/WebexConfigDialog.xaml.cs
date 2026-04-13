using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
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
            ClientIdBox.Text = config.ClientId;
            ClientSecretBox.Password = config.ClientSecret;
            AccessTokenBox.Password = config.AccessToken;
            BotNameBox.Text = config.BotName;
            SendEmailsCheckBox.IsChecked = config.SendInviteEmails;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var config = WebexConfig.Load();
            if (!config.IsConfigured) return;

            try
            {
                var service = new WebexService();
                var scopeResult = await service.TestMeetingsScopeAsync();
                if (scopeResult == false)
                {
                    ShowStatus(
                        "⚠ Der gespeicherte Token hat keinen Meetings-Scope!\n" +
                        "Führen Sie jetzt Schritt 1–3 durch, um einen neuen Token zu erhalten.",
                        false);
                }
            }
            catch { }
        }

        private void OpenBrowser_Click(object sender, RoutedEventArgs e)
        {
            var clientId = ClientIdBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(clientId))
            {
                ShowStatus("Bitte geben Sie zuerst die Client ID ein.", false);
                return;
            }

            const string scopes = "meeting:schedules_read meeting:schedules_write";
            var authUrl = "https://webexapis.com/v1/authorize" +
                $"?client_id={Uri.EscapeDataString(clientId)}" +
                "&response_type=code" +
                $"&redirect_uri={Uri.EscapeDataString("http://localhost")}" +
                $"&scope={Uri.EscapeDataString(scopes)}" +
                "&state=projektsoftware";

            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
            ShowStatusInfo("Browser ge\u00f6ffnet. Melden Sie sich bei Webex an und kopieren Sie danach die URL aus der Adressleiste.");
        }

        private async void ExchangeCode_Click(object sender, RoutedEventArgs e)
        {
            var clientId = ClientIdBox.Text.Trim();
            var clientSecret = ClientSecretBox.Password;
            var redirectUrl = RedirectUrlBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                ShowStatus("Bitte Client ID und Client Secret eingeben.", false);
                return;
            }

            var code = ExtractCodeFromUrl(redirectUrl);
            if (code == null)
            {
                ShowStatus("Kein Autorisierungscode in der URL gefunden.\nBitte kopieren Sie die vollst\u00e4ndige URL aus der Adressleiste.", false);
                return;
            }

            ExchangeCodeButton.IsEnabled = false;
            ExchangeCodeButton.Content = "3.  Anmelden...";

            try
            {
                using var client = new HttpClient();
                var form = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["code"] = code,
                    ["redirect_uri"] = "http://localhost"
                });

                var response = await client.PostAsync("https://webexapis.com/v1/access_token", form);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ShowStatus($"Token-Austausch fehlgeschlagen ({response.StatusCode}):\n{json}", false);
                    return;
                }

                var tokenData = JsonSerializer.Deserialize<WebexTokenResponse>(json);
                if (tokenData?.AccessToken == null)
                {
                    ShowStatus("Ung\u00fcltige Antwort vom Webex-Server.", false);
                    return;
                }

                var config = new WebexConfig
                {
                    AccessToken = tokenData.AccessToken,
                    RefreshToken = tokenData.RefreshToken ?? "",
                    TokenExpiry = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn),
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    BotName = BotNameBox.Text?.Trim() ?? "Projektierungssoftware",
                    SendInviteEmails = SendEmailsCheckBox.IsChecked == true
                };
                config.Save();

                // Benutzerspezifischen Anzeigenamen in DB speichern
                try
                {
                    var db = new DatabaseService();
                    await UserCredentialService.SaveAsync(
                        UserCredentialService.WebexBotName, config.BotName, db);
                }
                catch { }

                AccessTokenBox.Password = "";

                string personInfo = "";
                bool meetingsScopeOk = true;
                try
                {
                    var service = new WebexService();
                    try
                    {
                        var person = await service.TestConnectionAsync();
                        personInfo = $"\nAngemeldet als: {person.DisplayName} ({person.Email})";
                    }
                    catch { }

                    var scopeResult = await service.TestMeetingsScopeAsync();
                    if (scopeResult == false)
                        meetingsScopeOk = false;
                }
                catch { }

                if (!meetingsScopeOk)
                {
                    ShowStatus(
                        $"\u26a0 Token erhalten, aber Meetings-Scope fehlt!{personInfo}\n\n" +
                        "Der Scope 'meeting:schedules_write' ist nicht im Token vorhanden.\n" +
                        "L\u00f6sung: Klicken Sie erneut auf Schritt 1 und melden Sie sich\n" +
                        "neu an. Stellen Sie sicher, dass die Redirect-URI in Ihrer\n" +
                        "Webex-Integration auf 'http://localhost' gesetzt ist.",
                        false);
                }
                else
                {
                    ShowStatus(
                        $"\u2713 Token erfolgreich erhalten.{personInfo}\n" +
                        $"G\u00fcltig bis: {config.TokenExpiry.ToLocalTime():dd.MM.yyyy HH:mm} Uhr",
                        true);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Fehler beim Anmelden:\n{ex.Message}", false);
            }
            finally
            {
                ExchangeCodeButton.IsEnabled = true;
                ExchangeCodeButton.Content = "3.  Anmelden";
            }
        }

        private static string? ExtractCodeFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try
            {
                if (!url.StartsWith("http"))
                    url = "http://localhost?" + url;
                var uri = new Uri(url);
                var query = uri.Query.TrimStart('?');
                foreach (var param in query.Split('&'))
                {
                    var parts = param.Split('=', 2);
                    if (parts.Length == 2 && parts[0] == "code")
                        return Uri.UnescapeDataString(parts[1]);
                }
            }
            catch { }
            return null;
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
                var config = WebexConfig.Load();
                config.AccessToken = AccessTokenBox.Password;
                config.BotName = BotNameBox.Text;
                config.SendInviteEmails = SendEmailsCheckBox.IsChecked == true;
                config.Save();

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

        private void ShowStatusInfo(string message)
        {
            StatusBorder.Visibility = Visibility.Visible;
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            StatusTextBlock.Text = message;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var existingConfig = WebexConfig.Load();

            if (string.IsNullOrWhiteSpace(existingConfig.AccessToken) && string.IsNullOrWhiteSpace(AccessTokenBox.Password))
            {
                MessageBox.Show(
                    "Bitte melden Sie sich zuerst \u00fcber OAuth an oder geben Sie ein Personal Access Token ein.",
                    "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(AccessTokenBox.Password))
                existingConfig.AccessToken = AccessTokenBox.Password;

            existingConfig.ClientId = ClientIdBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(ClientSecretBox.Password))
                existingConfig.ClientSecret = ClientSecretBox.Password;
            existingConfig.BotName = BotNameBox.Text?.Trim() ?? "Projektierungssoftware";
            existingConfig.SendInviteEmails = SendEmailsCheckBox.IsChecked == true;
            existingConfig.Save();

            // Benutzerspezifischen Anzeigenamen in DB speichern
            try
            {
                var db = new DatabaseService();
                await UserCredentialService.SaveAsync(
                    UserCredentialService.WebexBotName, existingConfig.BotName, db);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern der Benutzer-Zugangsdaten: {ex.Message}");
            }

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
