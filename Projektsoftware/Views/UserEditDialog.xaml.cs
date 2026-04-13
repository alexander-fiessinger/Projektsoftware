using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class UserEditDialog : Window
    {
        private readonly DatabaseService databaseService;
        private readonly User user;
        private readonly bool isNewUser;
        private ObservableCollection<Employee> employees;
        private readonly Dictionary<string, CheckBox> _permissionCheckBoxes = new();

        public UserEditDialog()
        {
            InitializeComponent();
            databaseService = new DatabaseService();
            user = new User { CreatedAt = DateTime.Now };
            isNewUser = true;

            Title = "Neuer Benutzer";

            BuildPermissionCheckBoxes();

            Loaded += async (s, e) =>
            {
                await LoadEmployeesAsync();
                UpdatePermissionsVisibility();
            };
        }

        public UserEditDialog(User existingUser) : this()
        {
            user = existingUser;
            isNewUser = false;

            Title = "Benutzer bearbeiten";

            // E-Mail-Feld ausblenden bei Bearbeitung
            EmailPanel.Visibility = Visibility.Collapsed;

            LoadUserData();

            Loaded += async (s, e) => await LoadPermissionsAsync();
        }

        private void BuildPermissionCheckBoxes()
        {
            foreach (var (key, displayName) in PermissionService.AllModules)
            {
                var cb = new CheckBox
                {
                    Content = displayName,
                    Tag = key,
                    Margin = new Thickness(0, 3, 0, 3),
                    FontWeight = FontWeights.Normal
                };
                _permissionCheckBoxes[key] = cb;
                PermissionsPanel.Children.Add(cb);
            }
        }

        private async System.Threading.Tasks.Task LoadPermissionsAsync()
        {
            try
            {
                var allowed = await databaseService.GetUserPermissionsAsync(user.Id);
                foreach (var kvp in _permissionCheckBoxes)
                {
                    kvp.Value.IsChecked = allowed.Contains(kvp.Key);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Berechtigungen: {ex.Message}");
            }
        }

        private void UpdatePermissionsVisibility()
        {
            var selectedRole = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            PermissionsGroupBox.Visibility = selectedRole == "Admin" ? Visibility.Collapsed : Visibility.Visible;
        }

        private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PermissionsGroupBox != null)
                UpdatePermissionsVisibility();
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

            // E-Mail-Validierung nur bei Neuanlage
            if (isNewUser && string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie eine E-Mail-Adresse ein, an die das vorübergehende Passwort gesendet wird.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                user.Username = UsernameTextBox.Text.Trim();
                user.Role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "User";
                user.IsActive = IsActiveCheckBox.IsChecked ?? true;

                var selectedEmployee = EmployeeComboBox.SelectedItem as Employee;
                user.EmployeeId = (selectedEmployee != null && selectedEmployee.Id > 0) ? selectedEmployee.Id : null;

                int userId;

                if (isNewUser)
                {
                    // Vorübergehendes Passwort generieren
                    string tempPassword = GenerateTempPassword();
                    user.PasswordHash = AuthenticationService.HashPassword(tempPassword);
                    user.MustChangePassword = true;
                    userId = await databaseService.AddUserAsync(user);

                    // must_change_password in DB setzen
                    await databaseService.SetMustChangePasswordAsync(userId, true);

                    // E-Mail mit vorübergehendem Passwort senden
                    var (emailSent, emailError) = await SendTempPasswordEmailAsync(EmailTextBox.Text.Trim(), user.Username, tempPassword);

                    if (!emailSent)
                    {
                        // Passwort im Dialog anzeigen als Fallback
                        MessageBox.Show(
                            $"Die E-Mail konnte nicht gesendet werden.\n\n" +
                            $"Grund: {emailError}\n\n" +
                            $"Bitte teilen Sie dem Benutzer das vorübergehende Passwort manuell mit:\n\n" +
                            $"Benutzername: {user.Username}\n" +
                            $"Passwort: {tempPassword}\n\n" +
                            $"Der Benutzer muss das Passwort beim ersten Login ändern.",
                            "⚠ E-Mail-Versand fehlgeschlagen",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Benutzer erfolgreich erstellt!\n\n" +
                            $"Das vorübergehende Passwort wurde an {EmailTextBox.Text.Trim()} gesendet.\n" +
                            $"Der Benutzer muss das Passwort beim ersten Login ändern.",
                            "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    await databaseService.UpdateUserAsync(user);
                    userId = user.Id;

                    MessageBox.Show("Benutzer erfolgreich aktualisiert!",
                        "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Berechtigungen speichern (nur für Nicht-Admins)
                if (user.Role != "Admin")
                {
                    var selectedModules = _permissionCheckBoxes
                        .Where(kvp => kvp.Value.IsChecked == true)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    await databaseService.SaveUserPermissionsAsync(userId, selectedModules);
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

        private static string GenerateTempPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var data = new byte[10];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(data);
            var result = new char[10];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = chars[data[i] % chars.Length];
            }
            return new string(result);
        }

        private async System.Threading.Tasks.Task<(bool Success, string? ErrorMessage)> SendTempPasswordEmailAsync(string recipientEmail, string username, string tempPassword)
        {
            try
            {
                var emailService = new ExchangeEmailService();
                if (!emailService.IsConfigured)
                    return (false, "SMTP ist nicht konfiguriert.\n\nBitte konfigurieren Sie zunächst die SMTP-Einstellungen unter\nEinstellungen → SMTP E-Mail → Konfiguration.");

                string subject = "Ihr neues Benutzerkonto — Projektierungssoftware";
                string body = $@"
                    <html><body style='font-family: Arial, sans-serif; color: #333;'>
                    <h2>Willkommen bei der Projektierungssoftware!</h2>
                    <p>Es wurde ein Benutzerkonto für Sie erstellt.</p>
                    <table style='border-collapse: collapse; margin: 15px 0;'>
                        <tr><td style='padding: 8px; font-weight: bold;'>Benutzername:</td><td style='padding: 8px;'>{username}</td></tr>
                        <tr><td style='padding: 8px; font-weight: bold;'>Vorübergehendes Passwort:</td><td style='padding: 8px; font-family: monospace; font-size: 16px; background: #f5f5f5; padding: 8px 12px;'>{tempPassword}</td></tr>
                    </table>
                    <p style='color: #d32f2f; font-weight: bold;'>⚠ Bitte ändern Sie Ihr Passwort beim ersten Login!</p>
                    <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'/>
                    <p style='font-size: 12px; color: #999;'>Diese Nachricht wurde automatisch generiert.</p>
                    </body></html>";

                await emailService.SendEmailAsync(recipientEmail, subject, body);
                return (true, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Senden der Einladungs-E-Mail: {ex.Message}");
                return (false, ex.InnerException?.Message ?? ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
