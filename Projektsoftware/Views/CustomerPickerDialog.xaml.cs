using Projektsoftware.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class CustomerPickerDialog : Window
    {
        private readonly List<Customer> _allCustomers;
        public Customer? SelectedCustomer { get; private set; }

        public CustomerPickerDialog(List<Customer> customers)
        {
            InitializeComponent();
            _allCustomers = customers;
            CustomerListBox.ItemsSource = _allCustomers;
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var search = SearchBox.Text.ToLower();
            CustomerListBox.ItemsSource = string.IsNullOrWhiteSpace(search)
                ? _allCustomers
                : _allCustomers.Where(c =>
                    c.DisplayName.ToLower().Contains(search) ||
                    (c.Email ?? string.Empty).ToLower().Contains(search)).ToList();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SelectedCustomer = CustomerListBox.SelectedItem as Customer;
            if (SelectedCustomer == null)
            {
                MessageBox.Show("Bitte wählen Sie einen Kunden aus.", "Hinweis",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void CustomerListBox_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CustomerListBox.SelectedItem != null)
                OK_Click(sender, e);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
