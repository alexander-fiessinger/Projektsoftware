using Projektsoftware.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class InvoiceSelectionDialog : Window
    {
        public EasybillDocument? SelectedInvoice { get; private set; }
        public string InputValue { get; private set; } = string.Empty;

        private readonly List<InvoiceDisplayItem> _allItems;
        private readonly CollectionViewSource _viewSource = new();

        public InvoiceSelectionDialog(
            List<EasybillDocument> invoices,
            string title,
            string description,
            string? inputLabel = null,
            string? inputDefault = null)
        {
            InitializeComponent();

            TitleTextBlock.Text = title;
            DescriptionTextBlock.Text = description;

            if (inputLabel != null)
            {
                InputLabelBlock.Text = inputLabel;
                InputTextBox.Text = inputDefault ?? string.Empty;
                InputPanel.Visibility = Visibility.Visible;
            }

            _allItems = invoices.Select(inv => new InvoiceDisplayItem(inv)).ToList();
            _viewSource.Source = _allItems;
            InvoicesListBox.ItemsSource = _viewSource.View;

            if (_allItems.Count > 0)
                InvoicesListBox.SelectedIndex = 0;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var term = SearchBox.Text.Trim();
            _viewSource.View.Filter = string.IsNullOrEmpty(term)
                ? null
                : obj => obj is InvoiceDisplayItem item &&
                    (item.Number?.Contains(term, System.StringComparison.OrdinalIgnoreCase) == true ||
                     item.CustomerName.Contains(term, System.StringComparison.OrdinalIgnoreCase));

            if (InvoicesListBox.Items.Count > 0)
                InvoicesListBox.SelectedIndex = 0;
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            Confirm();
        }

        private void InvoicesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Confirm();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Confirm()
        {
            if (InvoicesListBox.SelectedItem is InvoiceDisplayItem item)
            {
                SelectedInvoice = item.Document;
                InputValue = InputTextBox?.Text?.Trim() ?? string.Empty;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(
                    "Bitte wählen Sie eine Rechnung aus der Liste aus.",
                    "Keine Auswahl",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    /// <summary>
    /// View-model wrapper for displaying an EasybillDocument in the ListBox.
    /// </summary>
    internal class InvoiceDisplayItem
    {
        public EasybillDocument Document { get; }
        public string? Number => Document.Number;
        public string? DocumentDate => Document.DocumentDate;
        public string? DisplayStatus => Document.DisplayStatus;

        public string CustomerName
        {
            get
            {
                var snap = Document.CustomerSnapshot;
                if (snap == null) return "-";
                if (!string.IsNullOrWhiteSpace(snap.CompanyName)) return snap.CompanyName;
                return $"{snap.FirstName} {snap.LastName}".Trim();
            }
        }

        public string AmountDisplay
        {
            get
            {
                var amount = Document.TotalGross
                    ?? Document.Items?.Sum(i => i.TotalPriceGross ?? 0m)
                    ?? 0m;
                return $"{amount:N2} €";
            }
        }

        public InvoiceDisplayItem(EasybillDocument document)
        {
            Document = document;
        }
    }
}
