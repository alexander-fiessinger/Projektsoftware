using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class PurchaseOrderDialog : Window
    {
        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");
        public PurchaseOrder Result { get; private set; }
        public bool SaveAsDraft => EasybillDraftCheckBox.IsChecked == true;
        private readonly ObservableCollection<PurchaseOrderItem> _items = new();

        public PurchaseOrderDialog(List<Supplier> suppliers, PurchaseOrder? existing = null)
        {
            InitializeComponent();
            SupplierCombo.ItemsSource = suppliers;
            ItemsGrid.ItemsSource = _items;
            OrderDatePicker.SelectedDate = DateTime.Today;

            if (existing != null)
            {
                TitleText.Text = "🛒 Bestellung bearbeiten";
                SupplierCombo.SelectedValue = existing.SupplierId;
                OrderNumberBox.Text = existing.OrderNumber;
                OrderDatePicker.SelectedDate = existing.OrderDate;
                DeliveryDatePicker.SelectedDate = existing.DeliveryDateExpected;
                foreach (ComboBoxItem item in StatusCombo.Items)
                    if (item.Content?.ToString() == existing.Status) { StatusCombo.SelectedItem = item; break; }
                NotesBox.Text = existing.Notes;
                Result = existing;
                foreach (var it in existing.Items) _items.Add(it);
            }
            else
            {
                Result = new PurchaseOrder { OrderDate = DateTime.Today };
            }
            _items.CollectionChanged += (_, _) => RecalcTotals();
            RecalcTotals();
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            _items.Add(new PurchaseOrderItem { Quantity = 1, VatPercent = 19 });
            RecalcTotals();
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is PurchaseOrderItem item)
                _items.Remove(item);
            RecalcTotals();
        }

        private void RecalcTotals()
        {
            foreach (var item in _items)
                item.TotalNet = Math.Round(item.Quantity * item.UnitPriceNet, 2);

            decimal totalNet = 0;
            decimal totalGross = 0;
            foreach (var item in _items)
            {
                totalNet += item.TotalNet;
                totalGross += item.TotalNet * (1 + item.VatPercent / 100m);
            }
            TotalNetText.Text = $"Netto: {totalNet.ToString("C2", euroFormat)}";
            TotalGrossText.Text = $"Brutto: {totalGross.ToString("C2", euroFormat)}";
            Result.TotalNet = Math.Round(totalNet, 2);
            Result.TotalGross = Math.Round(totalGross, 2);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (SupplierCombo.SelectedItem == null)
            {
                MessageBox.Show("Bitte wählen Sie einen Lieferanten.", "Pflichtfeld", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ItemsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            RecalcTotals();

            var supplier = (Supplier)SupplierCombo.SelectedItem;
            Result.SupplierId = supplier.Id;
            Result.SupplierName = supplier.Name;
            Result.OrderNumber = OrderNumberBox.Text.Trim();
            Result.OrderDate = OrderDatePicker.SelectedDate ?? DateTime.Today;
            Result.DeliveryDateExpected = DeliveryDatePicker.SelectedDate;
            Result.Status = ((ComboBoxItem)StatusCombo.SelectedItem)?.Content?.ToString() ?? "Offen";
            Result.Notes = NotesBox.Text.Trim();
            Result.Items = new List<PurchaseOrderItem>(_items);

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
