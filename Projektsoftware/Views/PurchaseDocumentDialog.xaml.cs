using Microsoft.Win32;
using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class PurchaseDocumentDialog : Window
    {
        public PurchaseDocument Result { get; private set; }
        public byte[] SelectedFileBytes { get; private set; }
        private string _selectedFilePath = "";

        public PurchaseDocumentDialog(List<Supplier> suppliers, PurchaseDocument? existing = null)
        {
            InitializeComponent();
            SupplierCombo.ItemsSource = suppliers;
            DocumentDatePicker.SelectedDate = DateTime.Today;

            if (existing != null)
            {
                TitleText.Text = "📁 Beleg bearbeiten";
                SupplierCombo.SelectedValue = existing.SupplierId;
                DocumentNameBox.Text = existing.DocumentName;
                foreach (ComboBoxItem item in DocumentTypeCombo.Items)
                    if (item.Content?.ToString() == existing.DocumentType)
                    {
                        DocumentTypeCombo.SelectedItem = item;
                        break;
                    }
                DocumentDatePicker.SelectedDate = existing.DocumentDate;
                NotesBox.Text = existing.Notes;
                if (!string.IsNullOrEmpty(existing.OriginalFileName))
                {
                    SelectedFileText.Text = $"📎 {existing.OriginalFileName} (bereits gespeichert)";
                    SelectedFileText.FontStyle = System.Windows.FontStyles.Normal;
                }
                Result = existing;
            }
            else
            {
                Result = new PurchaseDocument { DocumentDate = DateTime.Today };
            }
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Beleg auswählen",
                Filter = "Alle Belege|*.pdf;*.jpg;*.jpeg;*.png;*.tiff;*.bmp|" +
                         "PDF-Dateien|*.pdf|" +
                         "Bilder|*.jpg;*.jpeg;*.png;*.tiff;*.bmp|" +
                         "Alle Dateien|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                _selectedFilePath = dlg.FileName;
                SelectedFileBytes = File.ReadAllBytes(_selectedFilePath);

                var fileName = Path.GetFileName(_selectedFilePath);
                SelectedFileText.Text = $"📎 {fileName}";
                SelectedFileText.FontStyle = System.Windows.FontStyles.Normal;

                var sizeKb = SelectedFileBytes.Length / 1024.0;
                FileSizeText.Text = sizeKb < 1024
                    ? $"{sizeKb:F1} KB"
                    : $"{sizeKb / 1024.0:F2} MB";
                FileSizeText.Visibility = Visibility.Visible;

                if (string.IsNullOrWhiteSpace(DocumentNameBox.Text))
                    DocumentNameBox.Text = Path.GetFileNameWithoutExtension(_selectedFilePath);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DocumentNameBox.Text))
            {
                MessageBox.Show("Bitte geben Sie eine Bezeichnung ein.", "Pflichtfeld",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var supplier = SupplierCombo.SelectedItem as Supplier;
            Result.SupplierId = supplier?.Id;
            Result.SupplierName = supplier?.Name ?? "";
            Result.DocumentName = DocumentNameBox.Text.Trim();
            Result.DocumentType = ((ComboBoxItem)DocumentTypeCombo.SelectedItem)?.Content?.ToString() ?? "Rechnung";
            Result.DocumentDate = DocumentDatePicker.SelectedDate ?? DateTime.Today;
            Result.Notes = NotesBox.Text.Trim();

            if (!string.IsNullOrEmpty(_selectedFilePath))
                Result.OriginalFileName = Path.GetFileName(_selectedFilePath);

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
