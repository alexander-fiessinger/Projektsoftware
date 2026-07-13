using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Linq;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class CustomerDialog : Window
    {
        private readonly Customer customer;
        private readonly bool isEditMode;

        public Customer Customer => customer;

        public CustomerDialog(Customer existingCustomer = null)
        {
            InitializeComponent();
            
            isEditMode = existingCustomer != null;
            customer = existingCustomer ?? new Customer();

            if (isEditMode)
            {
                Title = "Kunde bearbeiten";
                LoadCustomerData();
            }
            else
            {
                Title = "Neuer Kunde";
            }
        }

        private void LoadCustomerData()
        {
            CompanyNameTextBox.Text = customer.CompanyName;
            FirstNameTextBox.Text = customer.FirstName;
            LastNameTextBox.Text = customer.LastName;
            EmailTextBox.Text = customer.Email;
            PhoneTextBox.Text = customer.Phone;
            StreetTextBox.Text = customer.Street;
            ZipCodeTextBox.Text = customer.ZipCode;
            CityTextBox.Text = customer.City;
            CountryTextBox.Text = customer.Country ?? "Deutschland";
            VatIdTextBox.Text = customer.VatId;
            DiscountPercentTextBox.Text = customer.DiscountPercent.ToString(System.Globalization.CultureInfo.CurrentCulture);
            InvoiceAllowedCheckBox.IsChecked = customer.InvoiceAllowed;
            NoteTextBox.Text = customer.Note;

            if (customer.IsSyncedToEasybill)
            {
                SyncStatusTextBlock.Text = $"✓ Bereits synchronisiert (Easybill ID: {customer.EasybillCustomerId})";
                SyncStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
            }

            PortalSection.Visibility = Visibility.Visible;
            OpenRegistrationsSection.Visibility = Visibility.Visible;
            _ = LoadPortalUsersAsync();
            _ = LoadOpenRegistrationsAsync();
        }

        private async System.Threading.Tasks.Task LoadPortalUsersAsync()
        {
            try
            {
                var dbService = new DatabaseService();
                var users = await dbService.GetPortalUsersAsync(customer.Id);
                PortalUsersItems.ItemsSource = users;
                NoPortalUsersText.Visibility = users.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Portal-Konten konnten nicht geladen werden:\n\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadOpenRegistrationsAsync()
        {
            try
            {
                var dbService = new DatabaseService();
                var allUsers = await dbService.GetPortalUsersAsync(null);
                var openRegistrations = allUsers
                    .Where(u => u.CustomerId == null)
                    .ToList();
                OpenRegistrationsItems.ItemsSource = openRegistrations;
                NoOpenRegistrationsText.Visibility = openRegistrations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Offene Registrierungen konnten nicht geladen werden:\n\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private CustomerPortalUser GetPortalUser(object sender)
            => (sender as System.Windows.Controls.Button)?.DataContext as CustomerPortalUser;

        private async void ApprovePortal_Click(object sender, RoutedEventArgs e)
        {
            var user = GetPortalUser(sender);
            if (user == null) return;
            try
            {
                await new DatabaseService().ApprovePortalUserAsync(user.Id, customer.Id);
                await LoadPortalUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Freischalten fehlgeschlagen:\n\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AssignAndApprovePortal_Click(object sender, RoutedEventArgs e)
        {
            var user = GetPortalUser(sender);
            if (user == null) return;

            var confirm = MessageBox.Show(
                $"Portal-Konto '{user.Email}' diesem Kunden zuordnen und freischalten?",
                "Zuordnen & Freischalten", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                await new DatabaseService().ApprovePortalUserAsync(user.Id, customer.Id);
                await LoadPortalUsersAsync();
                await LoadOpenRegistrationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Zuordnen & Freischalten fehlgeschlagen:\n\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LockPortal_Click(object sender, RoutedEventArgs e)
        {
            var user = GetPortalUser(sender);
            if (user == null) return;
            try
            {
                await new DatabaseService().SetPortalUserActiveAsync(user.Id, false);
                await LoadPortalUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sperren fehlgeschlagen:\n\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UnlockPortal_Click(object sender, RoutedEventArgs e)
        {
            var user = GetPortalUser(sender);
            if (user == null) return;
            try
            {
                await new DatabaseService().SetPortalUserActiveAsync(user.Id, true);
                await LoadPortalUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Entsperren fehlgeschlagen:\n\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ResetPortalPw_Click(object sender, RoutedEventArgs e)
        {
            var user = GetPortalUser(sender);
            if (user == null) return;

            var newPassword = PromptForPassword($"Neues Passwort für {user.Email}:");
            if (string.IsNullOrWhiteSpace(newPassword))
                return;
            if (newPassword.Length < 6)
            {
                MessageBox.Show("Das Passwort muss mindestens 6 Zeichen lang sein.",
                    "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await new DatabaseService().ResetPortalUserPasswordAsync(user.Id, newPassword);
                MessageBox.Show("✅ Passwort wurde zurückgesetzt.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Passwort-Reset fehlgeschlagen:\n\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeletePortal_Click(object sender, RoutedEventArgs e)
        {
            var user = GetPortalUser(sender);
            if (user == null) return;

            var confirm = MessageBox.Show(
                $"Portal-Konto '{user.Email}' wirklich endgültig löschen?",
                "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                await new DatabaseService().DeletePortalUserAsync(user.Id);
                await LoadPortalUsersAsync();
                await LoadOpenRegistrationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Löschen fehlgeschlagen:\n\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string PromptForPassword(string message)
        {
            var dialog = new Window
            {
                Title = "Passwort zurücksetzen",
                Width = 360,
                Height = 170,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = message,
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            });

            var passwordBox = new System.Windows.Controls.PasswordBox { Height = 30 };
            panel.Children.Add(passwordBox);

            var buttons = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };
            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true
            };
            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Abbrechen",
                Width = 80,
                Height = 30,
                IsCancel = true
            };
            okButton.Click += (_, _) => { dialog.DialogResult = true; dialog.Close(); };
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);
            panel.Children.Add(buttons);

            dialog.Content = panel;
            passwordBox.Focus();

            return dialog.ShowDialog() == true ? passwordBox.Password : null;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(CompanyNameTextBox.Text) && 
                string.IsNullOrWhiteSpace(FirstNameTextBox.Text) && 
                string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show(
                    "Bitte geben Sie mindestens einen Firmennamen ODER Vor- und Nachnamen ein!",
                    "Validierung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Update customer object
                customer.CompanyName = CompanyNameTextBox.Text;
                customer.FirstName = FirstNameTextBox.Text;
                customer.LastName = LastNameTextBox.Text;
                customer.Email = EmailTextBox.Text;
                customer.Phone = PhoneTextBox.Text;
                customer.Street = StreetTextBox.Text;
                customer.ZipCode = ZipCodeTextBox.Text;
                customer.City = CityTextBox.Text;
                customer.Country = CountryTextBox.Text;
                customer.VatId = VatIdTextBox.Text;
                customer.DiscountPercent = ParseDiscount(DiscountPercentTextBox.Text);
                customer.InvoiceAllowed = InvoiceAllowedCheckBox.IsChecked == true;
                customer.Note = NoteTextBox.Text;

                if (!isEditMode)
                {
                    customer.CreatedAt = DateTime.Now;
                }
                customer.UpdatedAt = DateTime.Now;

                // Save to database
                var dbService = new DatabaseService();
                if (isEditMode)
                {
                    await dbService.UpdateCustomerAsync(customer);
                }
                else
                {
                    await dbService.AddCustomerAsync(customer);
                }

                // Sync to Easybill if requested
                if (SyncToEasybillCheckBox.IsChecked == true)
                {
                    var easybillService = new EasybillService();
                    
                    if (easybillService.IsConfigured)
                    {
                        try
                        {
                            var easybillCustomer = await easybillService.SyncCustomerToEasybillAsync(customer);
                            
                            // Update customer with Easybill ID
                            customer.EasybillCustomerId = easybillCustomer.Id;
                            customer.LastSyncedAt = DateTime.Now;
                            await dbService.UpdateCustomerAsync(customer);

                            var customerNumber = easybillCustomer.Number ?? "(wird automatisch vergeben)";
                            MessageBox.Show(
                                $"✅ Kunde erfolgreich gespeichert und zu Easybill synchronisiert!\n\n" +
                                $"Easybill-ID: {easybillCustomer.Id}\n" +
                                $"Kundennummer: {customerNumber}",
                                "Erfolg",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            var isRateLimit = ex.Message.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase)
                                || ex.Message.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase)
                                || ex.Message.Contains("21000");

                            var detail = isRateLimit
                                ? "Das Easybill-Anfragelimit (Rate-Limit) wurde erreicht. Bitte warten Sie einen Moment und wiederholen Sie die Synchronisation später."
                                : ex.Message;

                            MessageBox.Show(
                                $"⚠ Kunde wurde lokal gespeichert, aber die Easybill-Synchronisation ist fehlgeschlagen:\n\n{detail}\n\n" +
                                "Sie können die Synchronisation später über die Kundenverwaltung wiederholen.",
                                "Warnung",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            "⚠ Kunde wurde lokal gespeichert.\n\n" +
                            "Easybill ist nicht konfiguriert. Bitte konfigurieren Sie Easybill unter:\n" +
                            "Einstellungen → Easybill-Konfiguration",
                            "Warnung",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show(
                        "✅ Kunde erfolgreich gespeichert!",
                        "Erfolg",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Speichern des Kunden:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static decimal ParseDiscount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            var normalized = text.Trim().Replace("%", "").Replace(',', '.');
            if (!decimal.TryParse(normalized, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var value))
                return 0;

            if (value < 0) value = 0;
            if (value > 100) value = 100;
            return value;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
