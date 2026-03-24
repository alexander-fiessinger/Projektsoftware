using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class UserEditDialog : Window
    {
        private readonly DatabaseService databaseService;
        private readonly User user;
        private readonly bool isNewUser;
        private ObservableCollection<Employee> employees;

        public UserEditDialog()
        {
            InitializeComponent();
            databaseService = new DatabaseService();
            user = new User { CreatedAt = DateTime.Now };
            isNewUser = true;
            
            Title = "Neuer Benutzer";
            
            Loaded += async (s, e) => await LoadEmployeesAsync();
        }

        public UserEditDialog(User existingUser) : this()
        {
            user = existingUser;
            isNewUser = false;
            
            Title = "Benutzer bearbeiten";
            
            // Passwortfelder ausblenden bei Bearbeitung
            PasswordPanel.Visibility = Visibility.Collapsed;
            ConfirmPasswordPanel.Visibility = Visibility.Collapsed;
            
            LoadUserData();
        }

        private async System.Threading.Tasks.Task LoadEmployeesAsync()
        {
            try
            {
                var employeesList = await databaseService.GetAllEmployeesAsync();
                employees = new ObservableCollection<Employee>(employeesList);
                
                // Leeren Eintrag hinzufügen
                employees.Insert(0, new Employee { Id = 0, FirstName = "(Kein", LastName = "Mitarbeiter)" });
                
                EmployeeComboBox.ItemsSource = employees;
                EmployeeComboBox.SelectedIndex = 0;

                // Vorhandenen Mitarbeiter auswählen
                if (user.EmployeeId.HasValue)
                {
                    var employee = employees.FirstOrDefault(e => e.Id == user.EmployeeId.Value);
                    if (employee != null)
                    {
                        EmployeeComboBox.SelectedItem = employee;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Mitarbeiter:\n\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserData()
        {
            UsernameTextBox.Text = user.Username;
            RoleComboBox.Text = user.Role;
            IsActiveCheckBox.IsChecked = user.IsActive;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validierung
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Benutzernamen ein.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Passwortvalidierung nur bei Neuanlage
            if (isNewUser)
            {
                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    MessageBox.Show("Bitte geben Sie ein Passwort ein.", "Validierung",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    MessageBox.Show("Die Passwörter stimmen nicht überein!", "Validierung",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PasswordBox.Password.Length < 4)
                {
                    MessageBox.Show("Das Passwort muss mindestens 4 Zeichen lang sein.", "Validierung",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                user.Username = UsernameTextBox.Text.Trim();
                user.Role = (RoleComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "User";
                user.IsActive = IsActiveCheckBox.IsChecked ?? true;

                var selectedEmployee = EmployeeComboBox.SelectedItem as Employee;
                user.EmployeeId = (selectedEmployee != null && selectedEmployee.Id > 0) ? selectedEmployee.Id : null;

                if (isNewUser)
                {
                    // Hash das Passwort
                    user.PasswordHash = AuthenticationService.HashPassword(PasswordBox.Password);
                    await databaseService.AddUserAsync(user);
                    
                    MessageBox.Show("Benutzer erfolgreich erstellt!", "Erfolg",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await databaseService.UpdateUserAsync(user);
                    
                    MessageBox.Show("Benutzer erfolgreich aktualisiert!", "Erfolg",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern:\n\n{ex.Message}", "Fehler",
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
