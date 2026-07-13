using Projektsoftware.Services;
using System;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class LogicCConfigDialog : Window
    {
        private LogicCConfig config;
        private string currentApiKey = string.Empty;

        public LogicCConfigDialog()
        {
            InitializeComponent();
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                config = LogicCConfig.Load();

                // Set API Key (masked)
                if (!string.IsNullOrEmpty(config.ApiKey))
                {
                    ApiKeyPasswordBox.Password = config.ApiKey;
                    currentApiKey = config.ApiKey;
                }

                // Set API URL
                ApiUrlTextBox.Text = config.ApiUrl;

                // Set Model
                var modelIndex = config.Model switch
                {
                    "gpt-4o" => 0,
                    "gpt-4o-mini" => 1,
                    "gpt-5-nano" => 2,
                    "o3-mini" => 3,
                    "gemini-2.5-flash" => 4,
                    "gemini-2.5-pro" => 5,
                    _ => 0
                };
                ModelComboBox.SelectedIndex = modelIndex;

                // Set Max Tokens
                MaxTokensSlider.Value = config.MaxTokens;

                // Set Temperature
                TemperatureSlider.Value = config.Temperature;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Konfiguration: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApiKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            currentApiKey = ApiKeyPasswordBox.Password;
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentApiKey))
            {
                TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                TestResultTextBlock.Text = "❌ Bitte geben Sie einen API-Key ein.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ApiUrlTextBox.Text))
            {
                TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                TestResultTextBlock.Text = "❌ Bitte geben Sie eine API-URL ein.";
                return;
            }

            // Warnung wenn der Key verdächtig kurz ist
            if (currentApiKey.Length < 20)
            {
                var result = MessageBox.Show(
                    "⚠️ Der eingegebene API-Key scheint sehr kurz zu sein.\n\n" +
                    "LogicC verwendet 'LineLLM Virtual Keys' die typischerweise länger sind.\n\n" +
                    "Möchten Sie dennoch fortfahren?",
                    "API-Key Warnung",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            TestConnectionButton.IsEnabled = false;
            TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
            TestResultTextBlock.Text = "⏳ Verbindung wird getestet...";

            try
            {
                // Temporäre Konfiguration für Test erstellen
                var tempConfig = new LogicCConfig
                {
                    ApiKey = currentApiKey,
                    ApiUrl = ApiUrlTextBox.Text.TrimEnd('/'),
                    Model = (ModelComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "gpt-4",
                    MaxTokens = (int)MaxTokensSlider.Value,
                    Temperature = TemperatureSlider.Value
                };

                // Temporär speichern für den Test
                var originalConfig = LogicCConfig.Load();
                tempConfig.Save();

                try
                {
                    var testService = new LogicCAiService();
                    var (success, message) = await testService.TestConnectionAsync();

                    if (success)
                    {
                        TestResultTextBlock.Foreground = System.Windows.Media.Brushes.LimeGreen;
                        TestResultTextBlock.Text = message;
                    }
                    else
                    {
                        TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;

                        // Spezielle Meldung bei Auth-Fehler
                        if (message.Contains("401") || message.Contains("Unauthorized") || message.Contains("Authentication"))
                        {
                            TestResultTextBlock.Text = $"❌ Authentifizierung fehlgeschlagen!\n\n" +
                                $"Der API-Key ist ungültig oder hat das falsche Format.\n" +
                                $"LogicC erwartet einen 'LineLLM Virtual Key'.\n\n" +
                                $"Bitte kontaktieren Sie den LogicC Support.\n\n" +
                                $"Details: {message}";
                        }
                        else
                        {
                            TestResultTextBlock.Text = $"❌ {message}";
                        }
                    }
                }
                finally
                {
                    // Original-Konfiguration wiederherstellen falls Test fehlschlägt
                    if (!TestResultTextBlock.Text.Contains("✅"))
                    {
                        originalConfig.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                TestResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                TestResultTextBlock.Text = $"❌ Verbindungsfehler: {ex.Message}";
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentApiKey))
            {
                MessageBox.Show("Bitte geben Sie einen API-Key ein.", 
                    "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ApiUrlTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie eine API-URL ein.", 
                    "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                config.ApiKey = currentApiKey;
                config.ApiUrl = ApiUrlTextBox.Text.TrimEnd('/');

                config.Model = (ModelComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "gpt-4o";
                config.MaxTokens = (int)MaxTokensSlider.Value;
                config.Temperature = TemperatureSlider.Value;

                config.Save();

                MessageBox.Show("LogicC AI Konfiguration wurde erfolgreich gespeichert!", 
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
