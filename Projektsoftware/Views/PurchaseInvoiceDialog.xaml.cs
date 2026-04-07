using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class PurchaseInvoiceDialog : Window
    {
        private static readonly CultureInfo deFormat = new CultureInfo("de-DE");
        public PurchaseInvoice Result { get; private set; }

        public PurchaseInvoiceDialog(List<Supplier> suppliers, List<PurchaseOrder> orders, PurchaseInvoice? existing = null)
        {
            InitializeComponent();
            SupplierCombo.ItemsSource = suppliers;
            PurchaseOrderCombo.ItemsSource = orders;
            InvoiceDatePicker.SelectedDate = DateTime.Today;

            if (existing != null)
            {
                TitleText.Text = "📥 Eingangsrechnung bearbeiten";
                SupplierCombo.SelectedValue = existing.SupplierId;
                InvoiceNumberBox.Text = existing.InvoiceNumber;
                PurchaseOrderCombo.SelectedValue = existing.PurchaseOrderId;
                InvoiceDatePicker.SelectedDate = existing.InvoiceDate;
                DueDatePicker.SelectedDate = existing.DueDate;
                TotalNetBox.Text = existing.TotalNet.ToString("N2", deFormat);
                TotalGrossBox.Text = existing.TotalGross.ToString("N2", deFormat);
                foreach (ComboBoxItem item in StatusCombo.Items)
                    if (item.Content?.ToString() == existing.Status) { StatusCombo.SelectedItem = item; break; }
                PaymentDatePicker.SelectedDate = existing.PaymentDate;
                NotesBox.Text = existing.Notes;
                Result = existing;
            }
            else
            {
                Result = new PurchaseInvoice { InvoiceDate = DateTime.Today };
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (SupplierCombo.SelectedItem == null)
            {
                MessageBox.Show("Bitte wählen Sie einen Lieferanten.", "Pflichtfeld", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TotalNetBox.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal tnet))
                tnet = 0;
            if (!decimal.TryParse(TotalGrossBox.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal tgross))
                tgross = 0;

            var supplier = (Supplier)SupplierCombo.SelectedItem;
            Result.SupplierId = supplier.Id;
            Result.SupplierName = supplier.Name;
            Result.InvoiceNumber = InvoiceNumberBox.Text.Trim();
            Result.PurchaseOrderId = PurchaseOrderCombo.SelectedValue as int?;
            Result.InvoiceDate = InvoiceDatePicker.SelectedDate ?? DateTime.Today;
            Result.DueDate = DueDatePicker.SelectedDate;
            Result.TotalNet = tnet;
            Result.TotalGross = tgross;
            Result.Status = ((ComboBoxItem)StatusCombo.SelectedItem)?.Content?.ToString() ?? "Offen";
            Result.PaymentDate = PaymentDatePicker.SelectedDate;
            Result.Notes = NotesBox.Text.Trim();

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
