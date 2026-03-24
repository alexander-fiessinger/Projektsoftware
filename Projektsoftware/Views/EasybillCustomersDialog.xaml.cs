using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class EasybillCustomersDialog : Window
    {
        private readonly EasybillService easybillService;
        private ObservableCollection<EasybillCustomer> customers;
        private ObservableCollection<EasybillCustomer> allCustomers;

        public EasybillCustomersDialog()
        {
            InitializeComponent();

            var config = EasybillConfig.Load();
            
            if (!config.IsConfigured)
            {
                MessageBox.Show(
                    "Easybill ist noch nicht konfiguriert!\n\n" +
                    "Bitte konfigurieren Sie die API-Verbindung über:\n" +
                    "Einstellungen → Easybill-Konfiguration",
                    "Konfiguration erforderlich",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                
                Close();
                return;
            }

            easybillService = new EasybillService();
            customers = new ObservableCollection<EasybillCustomer>();
            allCustomers = new ObservableCollection<EasybillCustomer>();
            CustomersDataGrid.ItemsSource = customers;

            Loaded += async (s, e) => await LoadCustomersAsync();
        }

        private async void LoadCustomers_Click(object sender, RoutedEventArgs e)
        {
            await LoadCustomersAsync();
        }

        private async System.Threading.Tasks.Task LoadCustomersAsync()
        {
            try
            {
                StatusText.Text = "⏳ Lade Kunden...";
                StatusText.Foreground = System.Windows.Media.Brushes.Orange;

                var customersList = await easybillService.GetAllCustomersAsync();

                if (customersList == null)
                {
                    throw new Exception("GetAllCustomersAsync() hat null zurückgegeben!");
                }

                allCustomers.Clear();
                customers.Clear();

                foreach (var customer in customersList)
                {
                    allCustomers.Add(customer);
                    customers.Add(customer);
                }

                StatusText.Text = $"✅ {customers.Count} Kunden geladen";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                StatusText.Text = "❌ Fehler";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;

                var errorMessage = $"Fehler beim Laden der Kunden:\n\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
                }

                System.Diagnostics.Debug.WriteLine($"LoadCustomersAsync ERROR: {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                MessageBox.Show(errorMessage, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EasybillCustomerDialog();
            if (dialog.ShowDialog() == true)
            {
                _ = CreateCustomerAsync(dialog.Customer);
            }
        }

        private async System.Threading.Tasks.Task CreateCustomerAsync(EasybillCustomer customer)
        {
            try
            {
                StatusText.Text = "⏳ Erstelle Kunde...";
                StatusText.Foreground = System.Windows.Media.Brushes.Orange;

                var newCustomer = await easybillService.CreateCustomerAsync(customer);
                
                allCustomers.Add(newCustomer);
                customers.Add(newCustomer);

                StatusText.Text = "✅ Kunde erstellt";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;

                MessageBox.Show("Kunde erfolgreich in Easybill erstellt!", "Erfolg",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = "❌ Fehler";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                
                MessageBox.Show($"Fehler beim Erstellen:\n\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditCustomer_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var customer = button?.DataContext as EasybillCustomer;
            
            if (customer != null)
            {
                var dialog = new EasybillCustomerDialog(customer);
                if (dialog.ShowDialog() == true)
                {
                    _ = UpdateCustomerAsync(dialog.Customer);
                }
            }
        }

        private async System.Threading.Tasks.Task UpdateCustomerAsync(EasybillCustomer customer)
        {
            try
            {
                StatusText.Text = "⏳ Aktualisiere Kunde...";
                StatusText.Foreground = System.Windows.Media.Brushes.Orange;

                var updatedCustomer = await easybillService.UpdateCustomerAsync(customer.Id, customer);

                var index = customers.IndexOf(customers.First(c => c.Id == customer.Id));
                if (index >= 0)
                {
                    customers[index] = updatedCustomer;
                }

                StatusText.Text = "✅ Kunde aktualisiert";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;

                MessageBox.Show("Kunde erfolgreich aktualisiert!", "Erfolg",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = "❌ Fehler";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                
                MessageBox.Show($"Fehler beim Aktualisieren:\n\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var customer = button?.DataContext as EasybillCustomer;
            
            if (customer != null)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie den Kunden '{customer.DisplayName}' wirklich in Easybill löschen?",
                    "Bestätigung",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        StatusText.Text = "⏳ Lösche Kunde...";
                        StatusText.Foreground = System.Windows.Media.Brushes.Orange;

                        await easybillService.DeleteCustomerAsync(customer.Id);
                        
                        customers.Remove(customer);
                        allCustomers.Remove(customer);

                        StatusText.Text = "✅ Kunde gelöscht";
                        StatusText.Foreground = System.Windows.Media.Brushes.Green;

                        MessageBox.Show("Kunde erfolgreich gelöscht!", "Erfolg",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        StatusText.Text = "❌ Fehler";
                        StatusText.Foreground = System.Windows.Media.Brushes.Red;
                        
                        MessageBox.Show($"Fehler beim Löschen:\n\n{ex.Message}", "Fehler",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void CustomersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CustomersDataGrid.SelectedItem is EasybillCustomer customer)
            {
                var dialog = new EasybillCustomerDialog(customer);
                if (dialog.ShowDialog() == true)
                {
                    _ = UpdateCustomerAsync(dialog.Customer);
                }
            }
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (customers == null || allCustomers == null)
            {
                return;
            }

            var searchText = SearchBox.Text;

            if (string.IsNullOrWhiteSpace(searchText) || searchText == "Suchen...")
            {
                customers.Clear();
                foreach (var customer in allCustomers)
                {
                    customers.Add(customer);
                }
            }
            else
            {
                var filtered = allCustomers.Where(c =>
                    c.DisplayName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true ||
                    c.Email?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true ||
                    c.City?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true ||
                    c.Number?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true
                ).ToList();

                customers.Clear();
                foreach (var customer in filtered)
                {
                    customers.Add(customer);
                }
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Suchen...")
            {
                SearchBox.Text = "";
                SearchBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Suchen...";
                SearchBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
