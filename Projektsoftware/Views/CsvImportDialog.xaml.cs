using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Projektsoftware.Models;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class CsvImportDialog : Window
    {
        private readonly DatabaseService _db = new DatabaseService();
        private DataTable? _preview;

        public CsvImportDialog()
        {
            InitializeComponent();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "CSV-Dateien (*.csv)|*.csv|Alle Dateien|*.*" };
            if (ofd.ShowDialog() != true) return;

            FilePathBox.Text = ofd.FileName;
            try
            {
                _preview = LoadCsv(ofd.FileName);
                PreviewGrid.ItemsSource = _preview.DefaultView;
                StatusText.Text = $"{_preview.Rows.Count} Zeilen geladen.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Lesen:\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static DataTable LoadCsv(string path)
        {
            var table = new DataTable();
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) return table;

            char sep = lines[0].Contains(';') ? ';' : ',';
            var headers = lines[0].Split(sep);
            foreach (var h in headers) table.Columns.Add(h.Trim());

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var parts = lines[i].Split(sep);
                var row = table.NewRow();
                for (int c = 0; c < Math.Min(parts.Length, headers.Length); c++)
                    row[c] = parts[c].Trim().Trim('"');
                table.Rows.Add(row);
            }
            return table;
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            if (_preview == null || _preview.Rows.Count == 0)
            {
                MessageBox.Show("Bitte zuerst eine CSV-Datei laden.", "Hinweis",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int success = 0, failed = 0;
            foreach (DataRow row in _preview.Rows)
            {
                try
                {
                    var customer = new Customer
                    {
                        CompanyName = GetVal(row, "CompanyName"),
                        FirstName = GetVal(row, "FirstName"),
                        LastName = GetVal(row, "LastName"),
                        Email = GetVal(row, "Email"),
                        Phone = GetVal(row, "Phone"),
                        Street = GetVal(row, "Street"),
                        ZipCode = GetVal(row, "ZipCode"),
                        City = GetVal(row, "City"),
                        Country = GetVal(row, "Country", "Deutschland")
                    };
                    if (string.IsNullOrWhiteSpace(customer.CompanyName) &&
                        string.IsNullOrWhiteSpace(customer.LastName)) { failed++; continue; }

                    await _db.AddCustomerAsync(customer);
                    success++;
                }
                catch
                {
                    failed++;
                }
            }

            StatusText.Text = $"Import abgeschlossen: {success} OK, {failed} Fehler.";
            MessageBox.Show($"{success} Kontakte importiert, {failed} fehlgeschlagen.",
                "Import-Ergebnis", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string GetVal(DataRow row, string columnName, string defaultValue = "")
        {
            if (!row.Table.Columns.Contains(columnName)) return defaultValue;
            var v = row[columnName]?.ToString();
            return string.IsNullOrWhiteSpace(v) ? defaultValue : v;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
