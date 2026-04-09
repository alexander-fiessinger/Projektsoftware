using Microsoft.Win32;
using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class ContractGeneratorDialog : Window
    {
        private readonly List<Customer> _customers;
        private Customer? _selectedCustomer;

        public ContractGeneratorDialog(List<Customer> customers, Customer? preselectedCustomer = null)
        {
            InitializeComponent();

            _customers = customers;
            CustomerCombo.ItemsSource = _customers;

            // Auftragnehmer-Daten aus gespeicherter Konfiguration laden
            LoadContractorDefaults();

            // Vorausgewählten Kunden über ID in der Liste finden
            if (preselectedCustomer != null)
            {
                var match = _customers.Find(c => c.Id == preselectedCustomer.Id)
                            ?? preselectedCustomer;
                CustomerCombo.SelectedItem = match;
                _selectedCustomer = match;
                ApplyCustomerToFields(match);
            }

            StartDatePicker.SelectedDate = DateTime.Today;
        }

        private ContractorConfig _contractorConfig = new();

        private void LoadContractorDefaults()
        {
            _contractorConfig = ContractorConfig.Load();
            ContractorCompanyBox.Text = _contractorConfig.Company;
            ContractorNameBox.Text = _contractorConfig.Name;
            ContractorStreetBox.Text = _contractorConfig.Street;
            ContractorZipCityBox.Text = _contractorConfig.ZipCity;
            ContractorEmailBox.Text = _contractorConfig.Email;
            ContractorPhoneBox.Text = _contractorConfig.Phone;
        }

        private void SaveContractorDefaults()
        {
            var cfg = new ContractorConfig
            {
                Company = ContractorCompanyBox.Text.Trim(),
                Name = ContractorNameBox.Text.Trim(),
                Street = ContractorStreetBox.Text.Trim(),
                ZipCity = ContractorZipCityBox.Text.Trim(),
                Email = ContractorEmailBox.Text.Trim(),
                Phone = ContractorPhoneBox.Text.Trim(),
                VatId = _contractorConfig.VatId,
                TaxNumber = _contractorConfig.TaxNumber
            };
            cfg.Save();
        }

        private void ContractType_Changed(object sender, RoutedEventArgs e)
        {
            if (ServicePanel == null || WorkPanel == null) return;

            if (RbDienstleistung.IsChecked == true)
            {
                ServicePanel.Visibility = Visibility.Visible;
                WorkPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                ServicePanel.Visibility = Visibility.Collapsed;
                WorkPanel.Visibility = Visibility.Visible;
            }
        }

        private void CustomerCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Direkt übernehmen wenn ein Kunde gewählt wird
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
                ApplyCustomerToFields(customer);
            }
        }

        private void ApplyCustomerToFields(Customer customer)
        {
            ClientCompanyBox.Text = customer.CompanyName ?? string.Empty;
            ClientNameBox.Text = $"{customer.FirstName} {customer.LastName}".Trim();
            ClientStreetBox.Text = customer.Street ?? string.Empty;
            ClientZipCityBox.Text = $"{customer.ZipCode} {customer.City}".Trim();
            ClientEmailBox.Text = customer.Email ?? string.Empty;
            ClientPhoneBox.Text = customer.Phone ?? string.Empty;
        }

        private async void ImportFromOffer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var easybillService = new EasybillService();
                if (!easybillService.IsConfigured)
                {
                    MessageBox.Show(
                        "Easybill ist nicht konfiguriert. Bitte konfigurieren Sie Easybill unter:\n" +
                        "Einstellungen \u2192 Easybill \u2192 Konfiguration",
                        "Hinweis", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsEnabled = false;
                var offers = await easybillService.GetAllDocumentsAsync("OFFER");
                IsEnabled = true;

                if (offers == null || offers.Count == 0)
                {
                    MessageBox.Show("Es wurden keine Angebote in Easybill gefunden.",
                        "Keine Angebote", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Kundennamen f\u00fcr Anzeige erg\u00e4nzen
                foreach (var offer in offers)
                {
                    if (offer.CustomerSnapshot != null)
                    {
                        offer.CustomerDisplay = !string.IsNullOrWhiteSpace(offer.CustomerSnapshot.CompanyName)
                            ? offer.CustomerSnapshot.CompanyName
                            : $"{offer.CustomerSnapshot.FirstName} {offer.CustomerSnapshot.LastName}".Trim();
                    }
                }

                var dialog = new OfferSelectionDialog(offers);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true && dialog.SelectedOffer != null)
                {
                    ApplyOfferData(dialog.SelectedOffer);
                }
            }
            catch (Exception ex)
            {
                IsEnabled = true;
                MessageBox.Show($"Fehler beim Laden der Angebote:\n\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyOfferData(EasybillDocument offer)
        {
            // Vertragsgegenstand aus Titel/Betreff
            if (!string.IsNullOrWhiteSpace(offer.Title))
                SubjectBox.Text = offer.Title;
            else if (!string.IsNullOrWhiteSpace(offer.Subject))
                SubjectBox.Text = offer.Subject;

            // Leistungsbeschreibung aus Positionen zusammensetzen
            if (offer.Items != null && offer.Items.Length > 0)
            {
                var sb = new StringBuilder();
                decimal totalNet = 0;

                foreach (var item in offer.Items.Where(i =>
                    i.Type is "POSITION" or "ITEM" or null))
                {
                    var desc = item.Description?.Trim();
                    if (string.IsNullOrWhiteSpace(desc)) continue;

                    var qty = item.Quantity;
                    var unit = item.Unit ?? "Stk.";
                    var price = item.SinglePriceNet ?? 0m;
                    var lineTotal = item.TotalPriceNet ?? (qty * price);
                    totalNet += lineTotal;

                    sb.AppendLine($"\u2022 {desc}");
                    sb.AppendLine($"  Menge: {qty.ToString("N2", new CultureInfo("de-DE"))} {unit} " +
                        $"\u00e0 {price.ToString("N2", new CultureInfo("de-DE"))} \u20ac (netto) = " +
                        $"{lineTotal.ToString("N2", new CultureInfo("de-DE"))} \u20ac");
                }

                if (sb.Length > 0)
                    DescriptionBox.Text = sb.ToString().TrimEnd();

                if (totalNet > 0)
                    NetAmountBox.Text = totalNet.ToString("N2", new CultureInfo("de-DE"));
            }

            // Freitext \u00fcbernehmen
            if (!string.IsNullOrWhiteSpace(offer.Text))
            {
                if (string.IsNullOrWhiteSpace(DescriptionBox.Text))
                    DescriptionBox.Text = offer.Text;
            }

            // Nettobetrag aus Dokument falls nicht aus Positionen
            if (string.IsNullOrWhiteSpace(NetAmountBox.Text) && offer.TotalNet.HasValue && offer.TotalNet.Value > 0)
                NetAmountBox.Text = offer.TotalNet.Value.ToString("N2", new CultureInfo("de-DE"));

            // Kundendaten aus Snapshot \u00fcbernehmen falls Felder leer
            if (offer.CustomerSnapshot != null && string.IsNullOrWhiteSpace(ClientCompanyBox.Text))
            {
                var snap = offer.CustomerSnapshot;
                ClientCompanyBox.Text = snap.CompanyName ?? string.Empty;
                ClientNameBox.Text = $"{snap.FirstName} {snap.LastName}".Trim();
                ClientStreetBox.Text = snap.Address ?? string.Empty;
                ClientZipCityBox.Text = $"{snap.ZipCode} {snap.City}".Trim();
                ClientEmailBox.Text = snap.Email ?? string.Empty;
                ClientPhoneBox.Text = snap.Phone1 ?? string.Empty;
            }

            MessageBox.Show(
                $"\u2705 Daten aus Angebot {offer.Number ?? offer.Id?.ToString() ?? ""} wurden \u00fcbernommen.\n\n" +
                "Bitte pr\u00fcfen und ggf. anpassen.",
                "Angebot importiert",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void GenerateContract_Click(object sender, RoutedEventArgs e)
        {
            // Validierung
            if (string.IsNullOrWhiteSpace(ContractorCompanyBox.Text))
            {
                MessageBox.Show("Bitte geben Sie den Firmennamen des Auftragnehmers ein.",
                    "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                ContractorCompanyBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(ClientCompanyBox.Text) && string.IsNullOrWhiteSpace(ClientNameBox.Text))
            {
                MessageBox.Show("Bitte geben Sie mindestens einen Firmennamen oder Ansprechpartner des Auftraggebers ein.",
                    "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                ClientCompanyBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(DescriptionBox.Text))
            {
                MessageBox.Show("Bitte geben Sie eine Leistungsbeschreibung ein.",
                    "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                DescriptionBox.Focus();
                return;
            }

            // Daten zusammenstellen
            var data = new ContractData
            {
                ContractType = RbDienstleistung.IsChecked == true
                    ? ContractType.Dienstleistungsvertrag
                    : ContractType.Werkvertrag,

                ContractorCompany = ContractorCompanyBox.Text.Trim(),
                ContractorName = ContractorNameBox.Text.Trim(),
                ContractorStreet = ContractorStreetBox.Text.Trim(),
                ContractorZipCity = ContractorZipCityBox.Text.Trim(),
                ContractorEmail = ContractorEmailBox.Text.Trim(),
                ContractorPhone = ContractorPhoneBox.Text.Trim(),
                ContractorVatId = _contractorConfig.VatId,
                ContractorTaxNumber = _contractorConfig.TaxNumber,

                ClientCompany = ClientCompanyBox.Text.Trim(),
                ClientName = ClientNameBox.Text.Trim(),
                ClientStreet = ClientStreetBox.Text.Trim(),
                ClientZipCity = ClientZipCityBox.Text.Trim(),
                ClientEmail = ClientEmailBox.Text.Trim(),
                ClientPhone = ClientPhoneBox.Text.Trim(),

                ContractSubject = SubjectBox.Text.Trim(),
                ServiceDescription = DescriptionBox.Text.Trim(),
                PaymentTerms = PaymentTermsBox.Text.Trim(),

                ContractStart = StartDatePicker.SelectedDate ?? DateTime.Today,
                ContractEnd = EndDatePicker.SelectedDate,
                NoticePeriod = NoticePeriodBox.Text.Trim(),

                AdditionalClauses = AdditionalClausesBox.Text.Trim(),
                Jurisdiction = JurisdictionBox.Text.Trim()
            };

            // Nettobetrag parsen
            if (decimal.TryParse(NetAmountBox.Text.Trim().Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var netAmount))
            {
                data.NetAmount = netAmount;
            }

            // USt-Satz parsen
            if (decimal.TryParse(VatRateBox.Text.Trim().Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var vatRate))
            {
                data.VatRate = vatRate;
            }

            // Typspezifische Felder
            if (data.ContractType == ContractType.Dienstleistungsvertrag)
            {
                data.ServiceLocation = ServiceLocationBox.Text.Trim();
                data.WorkingHours = WorkingHoursBox.Text.Trim();

                if (decimal.TryParse(HourlyRateBox.Text.Trim().Replace(",", "."),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out var hourlyRate))
                {
                    data.HourlyRate = hourlyRate;
                }
            }
            else
            {
                data.DeliveryDate = DeliveryDatePicker.SelectedDate;
                data.AcceptanceCriteria = AcceptanceCriteriaBox.Text.Trim();
                data.WarrantyPeriod = WarrantyPeriodBox.Text.Trim();
                data.WorkPaymentSchedule = WorkPaymentScheduleBox.Text.Trim();
            }

            // Speicherort wählen
            var saveDialog = new SaveFileDialog
            {
                Filter = "PDF-Dateien (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                FileName = $"{data.ContractTypeDisplay.Replace(" ", "_")}_{(string.IsNullOrWhiteSpace(data.ClientCompany) ? data.ClientName : data.ClientCompany).Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf"
            };

            if (saveDialog.ShowDialog() != true) return;

            try
            {
                // Auftragnehmer-Daten für nächstes Mal speichern
                SaveContractorDefaults();

                var service = new ContractGeneratorService();
                service.GenerateContract(data, saveDialog.FileName);

                // Vertrag dem Kunden in der Datenbank zuordnen
                if (_selectedCustomer != null)
                {
                    await SaveContractToDatabase(data, saveDialog.FileName);
                }

                var result = MessageBox.Show(
                    $"\u2705 Vertrag wurde erfolgreich erstellt:\n\n{saveDialog.FileName}" +
                    (_selectedCustomer != null ? $"\n\nDer Vertrag wurde dem Kunden \u201E{_selectedCustomer.DisplayName}\u201C zugeordnet." : "") +
                    "\n\nM\u00f6chten Sie die Datei jetzt \u00f6ffnen?",
                    "Vertrag erstellt",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Erstellen des Vertrages:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task SaveContractToDatabase(ContractData data, string filePath)
        {
            try
            {
                var dbService = new DatabaseService();
                var fileData = await System.IO.File.ReadAllBytesAsync(filePath);
                var fileInfo = new System.IO.FileInfo(filePath);

                var doc = new CustomerDocument
                {
                    CustomerId = _selectedCustomer!.Id,
                    FileName = System.IO.Path.GetFileName(filePath),
                    FilePath = filePath,
                    FileType = "PDF",
                    FileSize = fileInfo.Length,
                    Description = $"{data.ContractTypeDisplay} \u2013 {data.ContractSubject}".Trim().TrimEnd('\u2013').Trim(),
                    UploadedBy = AuthenticationService.CurrentUser?.Username ?? "",
                    UploadedAt = DateTime.Now,
                    FileData = fileData
                };

                await dbService.AddCustomerDocumentAsync(doc);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Der Vertrag wurde lokal gespeichert, konnte aber nicht in der Datenbank abgelegt werden:\n\n{ex.Message}",
                    "Datenbankfehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
