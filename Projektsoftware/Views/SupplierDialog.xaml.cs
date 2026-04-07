using Projektsoftware.Models;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class SupplierDialog : Window
    {
        public Supplier Result { get; private set; }

        public SupplierDialog(Supplier? existing = null)
        {
            InitializeComponent();
            if (existing != null)
            {
                TitleText.Text = "🏭 Lieferant bearbeiten";
                NameBox.Text = existing.Name;
                ContactPersonBox.Text = existing.ContactPerson;
                EmailBox.Text = existing.Email;
                PhoneBox.Text = existing.Phone;
                AddressBox.Text = existing.Address;
                ZipCodeBox.Text = existing.ZipCode;
                CityBox.Text = existing.City;
                CountryBox.Text = existing.Country;
                TaxNumberBox.Text = existing.TaxNumber;
                IbanBox.Text = existing.BankIban;
                BicBox.Text = existing.BankBic;
                NotesBox.Text = existing.Notes;
                Result = existing;
            }
            else
            {
                Result = new Supplier();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Namen an.", "Pflichtfeld", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Result.Name = NameBox.Text.Trim();
            Result.ContactPerson = ContactPersonBox.Text.Trim();
            Result.Email = EmailBox.Text.Trim();
            Result.Phone = PhoneBox.Text.Trim();
            Result.Address = AddressBox.Text.Trim();
            Result.ZipCode = ZipCodeBox.Text.Trim();
            Result.City = CityBox.Text.Trim();
            Result.Country = CountryBox.Text.Trim();
            Result.TaxNumber = TaxNumberBox.Text.Trim();
            Result.BankIban = IbanBox.Text.Trim();
            Result.BankBic = BicBox.Text.Trim();
            Result.Notes = NotesBox.Text.Trim();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
