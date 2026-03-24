using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Windows;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class LoginDialog : Window
    {
        private readonly DatabaseService databaseService;
        private int loginAttempts = 0;
        private const int MAX_ATTEMPTS = 3;

        public User AuthenticatedUser { get; private set; }

        public LoginDialog()
        {
            InitializeComponent();
            databaseService = new DatabaseService();
            UsernameTextBox.Focus();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            await AttemptLoginAsync();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PasswordBox.Focus();
            }
        }

        private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await AttemptLoginAsync();
            }
        }

        private async System.Threading.Tasks.Task AttemptLoginAsync()
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Bitte geben Sie einen Benutzernamen ein.");
                UsernameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Bitte geben Sie ein Passwort ein.");
                PasswordBox.Focus();
                return;
            }

            try
            {
                // Hash das Passwort
                string passwordHash = AuthenticationService.HashPassword(password);

                // Authentifiziere
                AuthenticatedUser = await databaseService.AuthenticateUserAsync(username, passwordHash);

                if (AuthenticatedUser != null)
                {
                    // Erfolgreiche Anmeldung
                    AuthenticationService.CurrentUser = AuthenticatedUser;
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    // Fehlgeschlagene Anmeldung
                    loginAttempts++;

                    if (loginAttempts >= MAX_ATTEMPTS)
                    {
                        MessageBox.Show(
                            $"Sie haben {MAX_ATTEMPTS} fehlerhafte Anmeldeversuche gemacht.\n\n" +
                            "Die Anwendung wird beendet.",
                            "Zu viele Fehlversuche",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        
                        DialogResult = false;
                        Close();
                        return;
                    }

                    ShowError($"Ungültige Anmeldedaten! ({loginAttempts}/{MAX_ATTEMPTS} Versuche)");
                    PasswordBox.Password = "";
                    PasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Fehler bei der Anmeldung: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorPanel.Visibility = Visibility.Visible;
        }
    }
}
