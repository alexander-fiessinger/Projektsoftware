using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Projektsoftware.Views
{
    public partial class AddDocumentItemDialog : Window
    {
        public EasybillDocumentItem? Item { get; private set; }
        private readonly VatResult? vatResult;
        private List<EasybillProduct> availableProducts = new();
        private bool suppressProductChange;

        public AddDocumentItemDialog()
        {
            InitializeComponent();
            UpdatePreview();
            Loaded += async (_, _) => await LoadProductsAsync();
        }

        /// <summary>
        /// Konstruktor mit steuerrechtlichem Kontext vom ausgewählten Kunden
        /// </summary>
        public AddDocumentItemDialog(VatResult? vatInfo) : this()
        {
            vatResult = vatInfo;
            if (vatInfo != null)
            {
                ApplyVatResult(vatInfo);
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var service = new EasybillService();
                if (!service.IsConfigured)
                {
                    ProductStatusTextBlock.Text = "Easybill nicht konfiguriert – nur Freiposten möglich.";
                    ProductComboBox.IsEnabled = false;
                    return;
                }

                ProductStatusTextBlock.Text = "Lade Artikel...";
                ProductComboBox.IsEnabled = false;

                var products = await service.GetAllProductsAsync();
                availableProducts = products
                    .Where(p => !p.IsArchived)
                    .OrderBy(p => p.Number)
                    .ToList();

                ProductComboBox.ItemsSource = availableProducts;
                ProductComboBox.DisplayMemberPath = "DisplayInfo";
                ProductComboBox.IsEnabled = true;

                ProductStatusTextBlock.Text = availableProducts.Count == 0
                    ? "Keine Artikel in Easybill vorhanden – Sie können einen Freiposten erfassen."
                    : $"{availableProducts.Count} Artikel verfügbar. Auswahl füllt die Felder unten automatisch aus.";
            }
            catch (Exception ex)
            {
                ProductStatusTextBlock.Text = $"Artikel konnten nicht geladen werden: {ex.Message}";
                ProductComboBox.IsEnabled = false;
            }
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressProductChange) return;
            if (ProductComboBox.SelectedItem is not EasybillProduct product) return;

            DescriptionTextBox.Text = string.IsNullOrWhiteSpace(product.Number)
                ? product.Description
                : $"{product.Number} – {product.Description}";

            SinglePriceNetTextBox.Text = product.SalePrice.ToString("F2", CultureInfo.GetCultureInfo("de-DE"));

            if (!string.IsNullOrWhiteSpace(product.Unit))
            {
                UnitComboBox.Text = product.Unit;
            }

            // Typ vorbelegen
            if (string.Equals(product.Type, "SERVICE", StringComparison.OrdinalIgnoreCase))
                SelectComboBoxTag(TypeComboBox, "POSITION");
            else
                SelectComboBoxTag(TypeComboBox, "POSITION");

            // MwSt nur überschreiben, wenn der Kunde keinen abweichenden Steuerkontext erzwingt
            if (vatResult == null || vatResult.Scenario == VatScenario.Inland)
            {
                SelectComboBoxTag(VatPercentComboBox, product.VatPercent.ToString(CultureInfo.InvariantCulture));
            }

            UpdatePreview();
        }

        private void ClearProduct_Click(object sender, RoutedEventArgs e)
        {
            suppressProductChange = true;
            try
            {
                ProductComboBox.SelectedItem = null;
                ProductComboBox.Text = string.Empty;
            }
            finally
            {
                suppressProductChange = false;
            }
        }

        private static void SelectComboBoxTag(ComboBox box, string tag)
        {
            foreach (ComboBoxItem item in box.Items)
            {
                if (string.Equals(item.Tag?.ToString(), tag, StringComparison.Ordinal))
                {
                    box.SelectedItem = item;
                    return;
                }
            }
        }

        private void ApplyVatResult(VatResult vatInfo)
        {
            // MwSt-Satz vorauswählen
            foreach (ComboBoxItem item in VatPercentComboBox.Items)
            {
                if (item.Tag?.ToString() == vatInfo.VatPercent.ToString())
                {
                    VatPercentComboBox.SelectedItem = item;
                    break;
                }
            }

            // Info-Banner anzeigen
            if (vatInfo.Scenario != VatScenario.Inland)
            {
                VatInfoBorder.Visibility = Visibility.Visible;
                VatInfoBorder.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(vatInfo.InfoColor));
                VatInfoTextBlock.Text = vatInfo.DisplayText;
            }
        }

        private void Price_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void VatPercent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (PreviewNetTextBlock == null) return;

            try
            {
                var quantity = ParseDecimal(QuantityTextBox?.Text ?? "1");
                var singlePrice = ParseDecimal(SinglePriceNetTextBox?.Text ?? "0");
                var vatPercent = int.Parse((VatPercentComboBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "19");

                var netTotal = quantity * singlePrice;
                var vatAmount = netTotal * (vatPercent / 100m);
                var grossTotal = netTotal + vatAmount;

                PreviewNetTextBlock.Text = $"{netTotal:N2} €";
                PreviewVatTextBlock.Text = $"{vatAmount:N2} €";
                PreviewGrossTextBlock.Text = $"{grossTotal:N2} €";
            }
            catch
            {
                PreviewNetTextBlock.Text = "0,00 €";
                PreviewVatTextBlock.Text = "0,00 €";
                PreviewGrossTextBlock.Text = "0,00 €";
            }
        }

        private decimal ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            text = text.Replace("€", "").Trim();

            // Versuche zuerst CurrentCulture (z.B. Deutsch: 1.200,50)
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal result))
                return result;

            // Falls das fehlschlägt, versuche InvariantCulture (Englisch: 1200.50)
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return result;

            return 0;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validierung
                if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
                {
                    MessageBox.Show(
                        "Bitte geben Sie eine Beschreibung ein.",
                        "Validierung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var quantity = ParseDecimal(QuantityTextBox.Text);
                if (quantity <= 0)
                {
                    MessageBox.Show(
                        "Bitte geben Sie eine gültige Menge ein (größer als 0).",
                        "Validierung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var singlePrice = ParseDecimal(SinglePriceNetTextBox.Text);
                if (singlePrice < 0)
                {
                    MessageBox.Show(
                        "Der Einzelpreis darf nicht negativ sein.",
                        "Validierung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var selectedType = (TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "POSITION";
                var vatPercent = int.Parse((VatPercentComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "19");
                var unit = UnitComboBox.Text;

                var netTotal = quantity * singlePrice;
                var vatAmount = netTotal * (vatPercent / 100m);
                var grossTotal = netTotal + vatAmount;

                Item = new EasybillDocumentItem
                {
                    Type = selectedType,
                    Description = DescriptionTextBox.Text,
                    Quantity = quantity,
                    Unit = string.IsNullOrWhiteSpace(unit) ? null : unit,
                    SinglePriceNet = singlePrice,
                    SinglePriceGross = singlePrice + (singlePrice * vatPercent / 100m),
                    VatPercent = vatPercent,
                    TotalPriceNet = netTotal,
                    TotalPriceGross = grossTotal
                };

                // Verknüpfung mit Easybill-Artikel übernehmen, falls ausgewählt
                if (ProductComboBox.SelectedItem is EasybillProduct selectedProduct)
                {
                    Item.Number = selectedProduct.Number;
                    Item.PositionId = selectedProduct.Id;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Hinzufügen der Position:\n\n{ex.Message}",
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
