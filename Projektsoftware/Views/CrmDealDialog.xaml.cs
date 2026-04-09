using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class CrmDealDialog : Window
    {
        public CrmDeal Deal { get; private set; }

        public CrmDealDialog(List<Customer> customers, List<CrmContact> contacts, CrmDeal existing = null)
        {
            InitializeComponent();

            var allCustomers = new List<Customer> { new Customer { Id = 0, CompanyName = "— Kein Kunde —" } };
            allCustomers.AddRange(customers);
            CustomerCombo.ItemsSource = allCustomers;
            CustomerCombo.SelectedIndex = 0;

            var allContacts = new List<CrmContact> { new CrmContact { Id = 0, FirstName = "—", LastName = "Kein Kontakt —" } };
            allContacts.AddRange(contacts);
            ContactCombo.ItemsSource = allContacts;
            ContactCombo.SelectedIndex = 0;

            if (existing != null)
            {
                TitleText.Text = "Deal bearbeiten";
                Deal = existing;
                TitleBox.Text = existing.Title;
                ValueBox.Text = existing.Value.ToString("F2");
                ProbabilityBox.Text = existing.Probability.ToString();
                CloseDatePicker.SelectedDate = existing.ExpectedCloseDate;
                AssignedToBox.Text = existing.AssignedTo;
                NotesBox.Text = existing.Notes;
                LostReasonBox.Text = existing.LostReason;

                foreach (ComboBoxItem item in StageCombo.Items)
                    if (item.Tag?.ToString() == ((int)existing.Stage).ToString())
                    { item.IsSelected = true; break; }

                if (existing.CustomerId.HasValue)
                    CustomerCombo.SelectedItem = allCustomers.FirstOrDefault(c => c.Id == existing.CustomerId.Value);
                if (existing.ContactId.HasValue)
                    ContactCombo.SelectedItem = allContacts.FirstOrDefault(c => c.Id == existing.ContactId.Value);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                MessageBox.Show("Bitte einen Titel angeben.", "Pflichtfeld", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(ValueBox.Text.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal value))
            {
                MessageBox.Show("Bitte einen gültigen Wert eingeben.", "Ungültige Eingabe", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(ProbabilityBox.Text, out int probability) || probability < 0 || probability > 100)
            {
                MessageBox.Show("Wahrscheinlichkeit muss zwischen 0 und 100 liegen.", "Ungültige Eingabe", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedStageTag = (StageCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            var selectedCustomer = CustomerCombo.SelectedItem as Customer;
            var selectedContact = ContactCombo.SelectedItem as CrmContact;
            var stage = int.TryParse(selectedStageTag, out int s) ? (DealStage)s : DealStage.Lead;

            if (Deal == null)
                Deal = new CrmDeal();

            Deal.Title = TitleBox.Text.Trim();
            Deal.Value = value;
            Deal.Probability = probability;
            Deal.Stage = stage;
            Deal.ExpectedCloseDate = CloseDatePicker.SelectedDate;
            Deal.AssignedTo = AssignedToBox.Text.Trim();
            Deal.Notes = NotesBox.Text.Trim();
            Deal.LostReason = LostReasonBox.Text.Trim();
            Deal.CustomerId = selectedCustomer?.Id > 0 ? selectedCustomer.Id : null;
            Deal.ContactId = selectedContact?.Id > 0 ? selectedContact.Id : null;

            if (stage == DealStage.Won && !Deal.WonAt.HasValue)
                Deal.WonAt = DateTime.Now;
            else if (stage == DealStage.Lost && !Deal.LostAt.HasValue)
                Deal.LostAt = DateTime.Now;

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
