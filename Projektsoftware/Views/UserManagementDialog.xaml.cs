using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class UserManagementDialog : Window
    {
        private readonly DatabaseService databaseService;
        private ObservableCollection<User> users;

        public UserManagementDialog()
        {
            InitializeComponent();
            databaseService = new DatabaseService();
            users = new ObservableCollection<User>();
            UsersDataGrid.ItemsSource = users;

            Loaded += async (s, e) => await LoadUsersAsync();
        }

        private async System.Threading.Tasks.Task LoadUsersAsync()
        {
            try
            {
                var usersList = await databaseService.GetAllUsersAsync();
                
                users.Clear();
                foreach (var user in usersList)
                {
                    users.Add(user);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Benutzer:\n\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserEditDialog();
            if (dialog.ShowDialog() == true)
            {
                await LoadUsersAsync();
            }
        }

        private async void EditUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var user = button?.DataContext as User;
            
            if (user != null)
            {
                var dialog = new UserEditDialog(user);
                if (dialog.ShowDialog() == true)
                {
                    await LoadUsersAsync();
                }
            }
        }

        private async void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var user = button?.DataContext as User;
            
            if (user != null)
            {
                var dialog = new ChangePasswordDialog(user);
                if (dialog.ShowDialog() == true)
                {
                    MessageBox.Show("Passwort erfolgreich geändert!", "Erfolg",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var user = button?.DataContext as User;
            
            if (user == null) return;

            // Prüfe ob letzter Admin
            if (user.Role == "Admin")
            {
                var allUsers = await databaseService.GetAllUsersAsync();
                var activeAdmins = allUsers.FindAll(u => u.Role == "Admin" && u.IsActive && u.Id != user.Id);
                
                if (activeAdmins.Count == 0)
                {
                    MessageBox.Show(
                        "Dieser Benutzer kann nicht gelöscht werden!\n\n" +
                        "Es muss mindestens ein aktiver Administrator vorhanden sein.",
                        "Löschen nicht möglich",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            var result = MessageBox.Show(
                $"Möchten Sie den Benutzer '{user.Username}' wirklich löschen?",
                "Benutzer löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await databaseService.DeleteUserAsync(user.Id);
                    await LoadUsersAsync();
                    
                    MessageBox.Show("Benutzer erfolgreich gelöscht!", "Erfolg",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Löschen:\n\n{ex.Message}", "Fehler",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
