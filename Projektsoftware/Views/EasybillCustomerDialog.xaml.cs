using Projektsoftware.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class EasybillCustomerDialog : Window
    {
        public EasybillCustomer Customer { get; private set; }

        public EasybillCustomerDialog(EasybillCustomer existingCustomer = null)
        {
            InitializeComponent();

            if (existingCustomer != null)
            {
                Customer = existingCustomer;
                Title = "Easybill Kunde bearbeiten";
                LoadCustomerData();
            }
            else
            {
                Customer = new EasybillCustomer();
                Title = "Neuer Easybill Kunde";
            }
        }

        private void LoadCustomerData()
        {
            CompanyNameTextBox.Text = Customer.CompanyName;
            FirstNameTextBox.Text = Customer.FirstName;
            LastNameTextBox.Text = Customer.LastName;
            EmailTextBox.Text = Customer.Email;
            Phone1TextBox.Text = Customer.Phone1;
            Phone2TextBox.Text = Customer.Phone2;
            StreetTextBox.Text = Customer.Street;
            ZipcodeTextBox.Text = Customer.Zipcode;
            CityTextBox.Text = Customer.City;
            VatIdTextBox.Text = Customer.VatId;
            NoteTextBox.Text = Customer.Note;

            if (!string.IsNullOrEmpty(Customer.Country))
            {
                CountryComboBox.Text = Customer.Country;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie eine E-Mail-Adresse ein.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CompanyNameTextBox.Text) &&
                string.IsNullOrWhiteSpace(FirstNameTextBox.Text) &&
                string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie entweder einen Firmennamen oder Vor-/Nachnamen ein.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Customer.CompanyName = CompanyNameTextBox.Text.Trim();
            Customer.FirstName = FirstNameTextBox.Text.Trim();
            Customer.LastName = LastNameTextBox.Text.Trim();
            Customer.Emails = new[] { EmailTextBox.Text.Trim() };
            Customer.Phone1 = Phone1TextBox.Text.Trim();
            Customer.Phone2 = Phone2TextBox.Text.Trim();
            Customer.Street = StreetTextBox.Text.Trim();
            Customer.Zipcode = ZipcodeTextBox.Text.Trim();
            Customer.City = CityTextBox.Text.Trim();
            Customer.Country = (CountryComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "DE";
            Customer.VatId = VatIdTextBox.Text.Trim();
            Customer.Note = NoteTextBox.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
