using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Projektsoftware.Views
{
    public partial class AddDocumentItemDialog : Window
    {
        public EasybillDocumentItem? Item { get; private set; }
        private readonly VatResult? vatResult;

        public AddDocumentItemDialog()
        {
            InitializeComponent();
            UpdatePreview();
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
