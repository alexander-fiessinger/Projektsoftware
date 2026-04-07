using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class AuditLogDialog : Window
    {
        private List<AuditLogEntry> _allEntries = new();

        public AuditLogDialog()
        {
            InitializeComponent();
            Loaded += AuditLogDialog_Loaded;
        }

        private async void AuditLogDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var dbConfig = DatabaseConfig.Load();
                if (!dbConfig.IsConfigured())
                {
                    MessageBox.Show("Datenbankverbindung nicht konfiguriert.", "Fehler",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _allEntries = await new AuditLogService(dbConfig.GetConnectionString()).GetAllAsync(1000);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden des Audit-Logs:\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var term = FilterTextBox.Text?.Trim() ?? string.Empty;

            var filtered = string.IsNullOrEmpty(term)
                ? _allEntries
                : _allEntries.FindAll(entry =>
                    entry.UserName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    entry.EntityType.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    entry.EntityId.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    entry.Action.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    entry.Details.Contains(term, StringComparison.OrdinalIgnoreCase));

            AuditGrid.ItemsSource = filtered;
            CountText.Text = $"{filtered.Count} Einträge";
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
