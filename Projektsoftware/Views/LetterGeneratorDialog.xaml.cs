using Microsoft.Win32;
using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class LetterGeneratorDialog : Window
    {
        private readonly List<Customer> _customers;
        private Customer? _selectedCustomer;
        private ContractorConfig _contractorConfig = new();

        public LetterGeneratorDialog(List<Customer> customers, Customer? preselectedCustomer = null)
        {
            InitializeComponent();

            _customers = customers;
            CustomerCombo.ItemsSource = _customers;

            LoadSenderDefaults();

            DatePicker.SelectedDate = DateTime.Today;

            if (preselectedCustomer != null)
            {
                var match = _customers.Find(c => c.Id == preselectedCustomer.Id)
                            ?? preselectedCustomer;
                CustomerCombo.SelectedItem = match;
                _selectedCustomer = match;
                ApplyCustomerToFields(match);
            }
        }

        private void LoadSenderDefaults()
        {
            _contractorConfig = ContractorConfig.Load();
            SenderCompanyBox.Text = _contractorConfig.Company;
            SenderNameBox.Text = _contractorConfig.Name;
            SenderStreetBox.Text = _contractorConfig.Street;
            SenderZipCityBox.Text = _contractorConfig.ZipCity;
            SenderEmailBox.Text = _contractorConfig.Email;
            SenderPhoneBox.Text = _contractorConfig.Phone;
        }

        private void CustomerCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CustomerCombo.SelectedItem is Customer customer)
            {
                _selectedCustomer = customer;
                ApplyCustomerToFields(customer);
            }
        }

        private void ApplyCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (CustomerCombo.SelectedItem is Customer customer)
            {
                _selectedCustomer = customer;
                ApplyCustomerToFields(customer);
            }
        }

        private void ApplyCustomerToFields(Customer customer)
        {
            RecipientCompanyBox.Text = customer.CompanyName ?? string.Empty;

            var name = $"{customer.FirstName} {customer.LastName}".Trim();
            RecipientNameBox.Text = name;
            RecipientStreetBox.Text = customer.Street ?? string.Empty;

            var zipCity = $"{customer.ZipCode} {customer.City}".Trim();
            RecipientZipCityBox.Text = zipCity;

            // Anrede anpassen
            if (!string.IsNullOrWhiteSpace(customer.LastName))
                SalutationBox.Text = $"Sehr geehrte(r) {customer.FirstName} {customer.LastName},";
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BrowseLogo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Bilder (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Firmenlogo auswählen"
            };
            if (dlg.ShowDialog() == true)
                LogoPathBox.Text = dlg.FileName;
        }

        private void ClearLogo_Click(object sender, RoutedEventArgs e)
        {
            LogoPathBox.Text = string.Empty;
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(BodyBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Brieftext ein.",
                    "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var data = new LetterData
            {
                SenderCompany = SenderCompanyBox.Text.Trim(),
                SenderName = SenderNameBox.Text.Trim(),
                SenderStreet = SenderStreetBox.Text.Trim(),
                SenderZipCity = SenderZipCityBox.Text.Trim(),
                SenderEmail = SenderEmailBox.Text.Trim(),
                SenderPhone = SenderPhoneBox.Text.Trim(),
                SenderVatId = _contractorConfig.VatId,
                SenderTaxNumber = _contractorConfig.TaxNumber,
                RecipientCompany = RecipientCompanyBox.Text.Trim(),
                RecipientName = RecipientNameBox.Text.Trim(),
                RecipientStreet = RecipientStreetBox.Text.Trim(),
                RecipientZipCity = RecipientZipCityBox.Text.Trim(),
                Subject = SubjectBox.Text.Trim(),
                Date = DatePicker.SelectedDate ?? DateTime.Today,
                Place = PlaceBox.Text.Trim(),
                Reference = ReferenceBox.Text.Trim(),
                Salutation = SalutationBox.Text.Trim(),
                Body = BodyBox.Text.Trim(),
                Closing = ClosingBox.Text.Trim(),
                SignatureName = SenderNameBox.Text.Trim(),
                LogoPath = string.IsNullOrWhiteSpace(LogoPathBox.Text) ? null : LogoPathBox.Text.Trim()
            };

            var saveDialog = new SaveFileDialog
            {
                Filter = "PDF-Datei (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                FileName = $"Brief_{data.Date:yyyy-MM-dd}_{(string.IsNullOrWhiteSpace(data.RecipientCompany) ? data.RecipientName : data.RecipientCompany).Replace(" ", "_")}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var service = new LetterGeneratorService();
                    service.GenerateLetter(data, saveDialog.FileName);

                    var result = MessageBox.Show(
                        "Brief wurde erfolgreich als PDF generiert!\n\nMöchten Sie die Datei jetzt öffnen?",
                        "Erfolg", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                    }

                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Generieren des Briefs:\n{ex.Message}",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
