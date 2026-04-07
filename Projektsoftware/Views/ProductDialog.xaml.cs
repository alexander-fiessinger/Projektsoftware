using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class ProductDialog : Window
    {
        private readonly EasybillProduct product;
        private readonly bool isEditMode;

        public EasybillProduct Product => product;

        public ProductDialog(EasybillProduct existingProduct = null)
        {
            InitializeComponent();
            
            isEditMode = existingProduct != null;
            product = existingProduct ?? new EasybillProduct();

            if (isEditMode)
            {
                Title = "Produkt bearbeiten";
                LoadProductData();
            }
            else
            {
                Title = "Neues Produkt";
                Loaded += async (s, e) => await GenerateArticleNumberAsync();
            }
        }

        private async System.Threading.Tasks.Task GenerateArticleNumberAsync()
        {
            try
            {
                NumberTextBox.IsEnabled = false;
                NumberTextBox.Text = "Wird generiert...";

                var easybillService = new EasybillService();
                if (easybillService.IsConfigured)
                {
                    var products = await easybillService.GetAllProductsAsync();

                    int maxNumber = products
                        .Select(p => Regex.Match(p.Number ?? string.Empty, @"\d+$"))
                        .Where(m => m.Success)
                        .Select(m => int.Parse(m.Value))
                        .DefaultIfEmpty(0)
                        .Max();

                    NumberTextBox.Text = $"ART-{maxNumber + 1:D4}";
                }
                else
                {
                    NumberTextBox.Text = "ART-0001";
                }
            }
            catch
            {
                NumberTextBox.Text = "ART-0001";
            }
            finally
            {
                NumberTextBox.IsEnabled = true;
                NumberTextBox.SelectAll();
                NumberTextBox.Focus();
            }
        }

        private void LoadProductData()
        {
            // Set Type
            if (product.Type == "PRODUCT")
            {
                TypeComboBox.SelectedIndex = 0;
            }
            else if (product.Type == "SERVICE")
            {
                TypeComboBox.SelectedIndex = 1;
            }

            NumberTextBox.Text = product.Number;
            DescriptionTextBox.Text = product.Description;
            SalePriceTextBox.Text = product.SalePrice.ToString("F2", CultureInfo.InvariantCulture);

            // Set VAT Percent
            switch (product.VatPercent)
            {
                case 0:
                    VatPercentComboBox.SelectedIndex = 0;
                    break;
                case 7:
                    VatPercentComboBox.SelectedIndex = 1;
                    break;
                case 19:
                    VatPercentComboBox.SelectedIndex = 2;
                    break;
                default:
                    VatPercentComboBox.SelectedIndex = 2; // Default to 19%
                    break;
            }

            UnitComboBox.Text = product.Unit;
            NoteTextBox.Text = product.Note;

            if (product.Id != null)
            {
                SyncStatusTextBlock.Text = $"✓ Bereits in Easybill (ID: {product.Id})";
                SyncStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                SyncStatusTextBlock.Visibility = Visibility.Visible;
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(NumberTextBox.Text))
            {
                MessageBox.Show(
                    "Bitte geben Sie eine Artikelnummer ein!",
                    "Validierung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show(
                    "Bitte geben Sie eine Beschreibung ein!",
                    "Validierung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                DescriptionTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(SalePriceTextBox.Text))
            {
                MessageBox.Show(
                    "Bitte geben Sie einen Verkaufspreis ein!",
                    "Validierung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                SalePriceTextBox.Focus();
                return;
            }

            // Parse Sale Price
            if (!decimal.TryParse(SalePriceTextBox.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal salePrice))
            {
                MessageBox.Show(
                    "Ungültiger Verkaufspreis! Bitte geben Sie eine gültige Zahl ein (z.B. 100.00 oder 99.99).",
                    "Validierung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                SalePriceTextBox.Focus();
                return;
            }

            if (salePrice < 0)
            {
                MessageBox.Show(
                    "Der Verkaufspreis darf nicht negativ sein!",
                    "Validierung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                SalePriceTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(UnitComboBox.Text))
            {
                MessageBox.Show(
                    "Bitte geben Sie eine Einheit ein!",
                    "Validierung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                UnitComboBox.Focus();
                return;
            }

            try
            {
                // Update product object
                var typeItem = TypeComboBox.SelectedItem as ComboBoxItem;
                product.Type = typeItem?.Tag?.ToString() ?? "SERVICE";
                
                product.Number = NumberTextBox.Text.Trim();
                product.Description = DescriptionTextBox.Text.Trim();
                product.SalePrice = salePrice;
                
                var vatItem = VatPercentComboBox.SelectedItem as ComboBoxItem;
                product.VatPercent = int.Parse(vatItem?.Tag?.ToString() ?? "19");
                
                product.Unit = UnitComboBox.Text.Trim();
                product.Note = NoteTextBox.Text?.Trim();

                // Save to Easybill
                var easybillService = new EasybillService();

                if (!easybillService.IsConfigured)
                {
                    MessageBox.Show(
                        "Easybill ist nicht konfiguriert!\n\nBitte konfigurieren Sie zuerst die Easybill-API unter:\nEinstellungen → Easybill-Konfiguration",
                        "Nicht konfiguriert",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                EasybillProduct savedProduct;
                
                if (isEditMode && product.Id != null)
                {
                    savedProduct = await easybillService.UpdateProductAsync(product.Id.Value, product);
                    MessageBox.Show(
                        $"Produkt '{savedProduct.Number}' wurde erfolgreich aktualisiert!",
                        "Erfolg",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    savedProduct = await easybillService.CreateProductAsync(product);
                    MessageBox.Show(
                        $"Produkt '{savedProduct.Number}' wurde erfolgreich in Easybill angelegt!\n\nEasybill ID: {savedProduct.Id}",
                        "Erfolg",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                // Update the product with server response
                product.Id = savedProduct.Id;
                product.CreatedAt = savedProduct.CreatedAt;
                product.UpdatedAt = savedProduct.UpdatedAt;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Speichern des Produkts:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
