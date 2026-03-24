using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class CustomersListDialog : Window
    {
        private readonly DatabaseService databaseService;
        private readonly EasybillService easybillService;
        private ObservableCollection<Customer> customers;

        public CustomersListDialog()
        {
            InitializeComponent();
            
            databaseService = new DatabaseService();
            easybillService = new EasybillService();
            customers = new ObservableCollection<Customer>();
            CustomersDataGrid.ItemsSource = customers;

            Loaded += async (s, e) => await LoadCustomersAsync();
        }

        private async System.Threading.Tasks.Task LoadCustomersAsync()
        {
            try
            {
                StatusTextBlock.Text = "⏳ Lade Kunden...";
                customers.Clear();

                var loadedCustomers = await databaseService.GetAllCustomersAsync();
                
                foreach (var customer in loadedCustomers.OrderBy(c => c.DisplayName))
                {
                    customers.Add(customer);
                }

                CountTextBlock.Text = $"📊 {customers.Count} Kunden geladen";
                StatusTextBlock.Text = "✅ Bereit";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "❌ Fehler beim Laden";
                MessageBox.Show(
                    $"Fehler beim Laden der Kunden:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomerDialog();
            if (dialog.ShowDialog() == true)
            {
                await LoadCustomersAsync();
            }
        }

        private async void EditCustomer_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var customer = button?.DataContext as Customer;
            
            if (customer != null)
            {
                var dialog = new CustomerDialog(customer);
                if (dialog.ShowDialog() == true)
                {
                    await LoadCustomersAsync();
                }
            }
        }

        private async void DeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var customer = button?.DataContext as Customer;
            
            if (customer != null)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie den Kunden '{customer.DisplayName}' wirklich löschen?\n\n" +
                    "Hinweis: Dies löscht den Kunden nur lokal. Der Kunde bleibt in Easybill erhalten.",
                    "Bestätigung",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await databaseService.DeleteCustomerAsync(customer);
                        customers.Remove(customer);
                        
                        CountTextBlock.Text = $"📊 {customers.Count} Kunden geladen";
                        StatusTextBlock.Text = "✅ Kunde gelöscht";

                        MessageBox.Show(
                            "Kunde erfolgreich gelöscht!",
                            "Erfolg",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Fehler beim Löschen:\n\n{ex.Message}",
                            "Fehler",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void SyncCustomer_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var customer = button?.DataContext as Customer;
            
            if (customer != null)
            {
                if (!easybillService.IsConfigured)
                {
                    MessageBox.Show(
                        "Easybill ist nicht konfiguriert!\n\n" +
                        "Bitte konfigurieren Sie die API-Verbindung über:\n" +
                        "Einstellungen → Easybill-Konfiguration",
                        "Konfiguration erforderlich",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    StatusTextBlock.Text = $"⏳ Synchronisiere '{customer.DisplayName}' zu Easybill...";
                    
                    var easybillCustomer = await easybillService.SyncCustomerToEasybillAsync(customer);
                    
                    customer.EasybillCustomerId = easybillCustomer.Id;
                    customer.LastSyncedAt = DateTime.Now;
                    await databaseService.UpdateCustomerAsync(customer);

                    // Refresh display
                    await LoadCustomersAsync();

                    StatusTextBlock.Text = "✅ Synchronisation erfolgreich";
                    var customerNumber = easybillCustomer.Number ?? "(wird automatisch vergeben)";
                    MessageBox.Show(
                        $"✅ Kunde erfolgreich zu Easybill synchronisiert!\n\n" +
                        $"Easybill-ID: {easybillCustomer.Id}\n" +
                        $"Kundennummer: {customerNumber}",
                        "Erfolg",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    StatusTextBlock.Text = "❌ Synchronisation fehlgeschlagen";
                    MessageBox.Show(
                        $"Fehler bei der Easybill-Synchronisation:\n\n{ex.Message}",
                        "Fehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private async void ImportFromEasybill_Click(object sender, RoutedEventArgs e)
        {
            if (!easybillService.IsConfigured)
            {
                MessageBox.Show(
                    "Easybill ist nicht konfiguriert!\n\n" +
                    "Bitte konfigurieren Sie die API-Verbindung über:\n" +
                    "Einstellungen → Easybill-Konfiguration",
                    "Konfiguration erforderlich",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Möchten Sie alle Kunden von Easybill importieren?\n\n" +
                "Bereits vorhandene Kunden (mit Easybill-ID) werden übersprungen.",
                "Import bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusTextBlock.Text = "⏳ Importiere Kunden von Easybill...";
                    
                    var easybillCustomers = await easybillService.GetAllCustomersAsync();
                    int importedCount = 0;
                    int skippedCount = 0;

                    foreach (var easybillCustomer in easybillCustomers)
                    {
                        // Check if customer already exists
                        var existing = await databaseService.GetCustomerByEasybillIdAsync(easybillCustomer.Id);
                        
                        if (existing == null)
                        {
                            // Create new customer
                            var newCustomer = new Customer
                            {
                                CompanyName = easybillCustomer.CompanyName,
                                FirstName = easybillCustomer.FirstName,
                                LastName = easybillCustomer.LastName,
                                Email = easybillCustomer.Email,
                                Phone = easybillCustomer.Phone1,
                                Street = easybillCustomer.Street,
                                ZipCode = easybillCustomer.Zipcode,
                                City = easybillCustomer.City,
                                Country = easybillCustomer.Country,
                                VatId = easybillCustomer.VatId,
                                Note = easybillCustomer.Note,
                                EasybillCustomerId = easybillCustomer.Id,
                                LastSyncedAt = DateTime.Now,
                                CreatedAt = DateTime.Now,
                                IsActive = !easybillCustomer.Archived
                            };

                            await databaseService.AddCustomerAsync(newCustomer);
                            importedCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }

                    await LoadCustomersAsync();

                    StatusTextBlock.Text = "✅ Import abgeschlossen";
                    MessageBox.Show(
                        $"✅ Easybill-Import abgeschlossen!\n\n" +
                        $"Importiert: {importedCount} Kunden\n" +
                        $"Übersprungen (bereits vorhanden): {skippedCount} Kunden\n" +
                        $"Gesamt: {easybillCustomers.Count} Kunden in Easybill",
                        "Import erfolgreich",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    StatusTextBlock.Text = "❌ Import fehlgeschlagen";
                    MessageBox.Show(
                        $"Fehler beim Import:\n\n{ex.Message}",
                        "Fehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadCustomersAsync();
        }

        private async void CustomersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CustomersDataGrid.SelectedItem is Customer customer)
            {
                var dialog = new CustomerDialog(customer);
                if (dialog.ShowDialog() == true)
                {
                    await LoadCustomersAsync();
                }
            }
        }
    }
}
