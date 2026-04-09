using Projektsoftware.Models;
using Projektsoftware.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class CrmContactDialog : Window
    {
        public CrmContact Contact { get; private set; }
        private readonly List<Customer> _customers;

        public CrmContactDialog(List<Customer> customers, CrmContact existing = null)
        {
            InitializeComponent();
            _customers = customers;

            var allItems = new List<Customer> { new Customer { Id = 0, CompanyName = "— Kein Kunde —" } };
            allItems.AddRange(customers);
            CustomerCombo.ItemsSource = allItems;
            CustomerCombo.SelectedIndex = 0;

            if (existing != null)
            {
                TitleText.Text = "Kontakt bearbeiten";
                Contact = existing;
                FirstNameBox.Text = existing.FirstName;
                LastNameBox.Text = existing.LastName;
                PositionBox.Text = existing.Position;
                EmailBox.Text = existing.Email;
                PhoneBox.Text = existing.Phone;
                MobileBox.Text = existing.Mobile;
                NotesBox.Text = existing.Notes;
                IsActiveCheck.IsChecked = existing.IsActive;

                if (existing.CustomerId.HasValue)
                    CustomerCombo.SelectedItem = allItems.FirstOrDefault(c => c.Id == existing.CustomerId.Value);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FirstNameBox.Text) && string.IsNullOrWhiteSpace(LastNameBox.Text))
            {
                MessageBox.Show("Bitte mindestens Vor- oder Nachname angeben.", "Pflichtfeld", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedCustomer = CustomerCombo.SelectedItem as Customer;

            if (Contact == null)
                Contact = new CrmContact();

            Contact.FirstName = FirstNameBox.Text.Trim();
            Contact.LastName = LastNameBox.Text.Trim();
            Contact.Position = PositionBox.Text.Trim();
            Contact.Email = EmailBox.Text.Trim();
            Contact.Phone = PhoneBox.Text.Trim();
            Contact.Mobile = MobileBox.Text.Trim();
            Contact.Notes = NotesBox.Text.Trim();
            Contact.IsActive = IsActiveCheck.IsChecked == true;
            Contact.CustomerId = selectedCustomer?.Id > 0 ? selectedCustomer.Id : null;

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
