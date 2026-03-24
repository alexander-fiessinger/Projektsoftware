using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class ChangePasswordDialog : Window
    {
        private readonly DatabaseService databaseService;
        private readonly User user;

        public ChangePasswordDialog(User user)
        {
            InitializeComponent();
            this.user = user;
            databaseService = new DatabaseService();

            UsernameTextBlock.Text = $"Benutzer: {user.Username}";
            NewPasswordBox.Focus();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            string newPassword = NewPasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            // Validierung
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Bitte geben Sie ein neues Passwort ein.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NewPasswordBox.Focus();
                return;
            }

            if (newPassword.Length < 4)
            {
                MessageBox.Show("Das Passwort muss mindestens 4 Zeichen lang sein.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NewPasswordBox.Focus();
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Die Passwörter stimmen nicht überein!", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ConfirmPasswordBox.Password = "";
                ConfirmPasswordBox.Focus();
                return;
            }

            try
            {
                // Hash das neue Passwort
                string newPasswordHash = AuthenticationService.HashPassword(newPassword);
                
                // Speichere in Datenbank
                await databaseService.ChangePasswordAsync(user.Id, newPasswordHash);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Ändern des Passworts:\n\n{ex.Message}", "Fehler",
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
