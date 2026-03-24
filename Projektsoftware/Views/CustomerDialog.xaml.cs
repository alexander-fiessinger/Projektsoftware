using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
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
            NoteTextBox.Text = customer.Note;

            if (customer.IsSyncedToEasybill)
            {
                SyncStatusTextBlock.Text = $"✓ Bereits synchronisiert (Easybill ID: {customer.EasybillCustomerId})";
                SyncStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
            }
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
                            MessageBox.Show(
                                $"⚠ Kunde wurde lokal gespeichert, aber die Easybill-Synchronisation ist fehlgeschlagen:\n\n{ex.Message}\n\n" +
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

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
