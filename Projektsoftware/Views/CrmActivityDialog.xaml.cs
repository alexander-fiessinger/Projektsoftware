using Projektsoftware.Models;
using Projektsoftware.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class CrmActivityDialog : Window
    {
        public CrmActivity Activity { get; private set; }

        public CrmActivityDialog(List<Customer> customers, List<CrmContact> contacts, CrmActivity existing = null)
        {
            InitializeComponent();

            var allContacts = new List<CrmContact> { new CrmContact { Id = 0, FirstName = "—", LastName = "Kein Kontakt —" } };
            allContacts.AddRange(contacts);
            ContactCombo.ItemsSource = allContacts;
            ContactCombo.SelectedIndex = 0;

            var allCustomers = new List<Customer> { new Customer { Id = 0, CompanyName = "— Kein Kunde —" } };
            allCustomers.AddRange(customers);
            CustomerCombo.ItemsSource = allCustomers;
            CustomerCombo.SelectedIndex = 0;

            if (existing != null)
            {
                TitleText.Text = "Aktivität bearbeiten";
                Activity = existing;

                foreach (ComboBoxItem item in TypeCombo.Items)
                    if (item.Tag?.ToString() == ((int)existing.Type).ToString())
                    { item.IsSelected = true; break; }

                SubjectBox.Text = existing.Subject;
                NotesBox.Text = existing.Notes;
                DueDatePicker.SelectedDate = existing.DueDate;
                IsCompletedCheck.IsChecked = existing.IsCompleted;

                if (existing.ContactId.HasValue)
                    ContactCombo.SelectedItem = allContacts.FirstOrDefault(c => c.Id == existing.ContactId.Value);
                if (existing.CustomerId.HasValue)
                    CustomerCombo.SelectedItem = allCustomers.FirstOrDefault(c => c.Id == existing.CustomerId.Value);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SubjectBox.Text))
            {
                MessageBox.Show("Bitte einen Betreff angeben.", "Pflichtfeld", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedContact = ContactCombo.SelectedItem as CrmContact;
            var selectedCustomer = CustomerCombo.SelectedItem as Customer;
            var selectedType = (TypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            if (Activity == null)
                Activity = new CrmActivity();

            Activity.Type = int.TryParse(selectedType, out int t) ? (CrmActivityType)t : CrmActivityType.Note;
            Activity.Subject = SubjectBox.Text.Trim();
            Activity.Notes = NotesBox.Text.Trim();
            Activity.DueDate = DueDatePicker.SelectedDate;
            Activity.IsCompleted = IsCompletedCheck.IsChecked == true;
            Activity.ContactId = selectedContact?.Id > 0 ? selectedContact.Id : null;
            Activity.CustomerId = selectedCustomer?.Id > 0 ? selectedCustomer.Id : null;

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
