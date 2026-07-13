using System;
using System.Linq;
using System.Windows;
using Projektsoftware.Models;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class EmailHistoryDialog : Window
    {
        private readonly DatabaseService _db = new DatabaseService();

        public EmailHistoryDialog()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var customers = await _db.GetAllCustomersAsync();
                ContactCombo.ItemsSource = customers.OrderBy(c => c.DisplayName).ToList();
                ContactCombo.SelectionChanged += async (_, __) => await LoadEmailsAsync();
                if (customers.Any())
                {
                    ContactCombo.SelectedIndex = 0;
                    await LoadEmailsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadEmailsAsync()
        {
            if (ContactCombo.SelectedItem is not Customer c) return;
            try
            {
                var emails = await _db.GetEmailHistoryByContactAsync(c.Id);
                EmailGrid.ItemsSource = emails.OrderByDescending(em => em.SentDate).ToList();
                if (emails.Count == 0)
                {
                    Title = $"E-Mail-Verlauf – {c.DisplayName} (keine Einträge)";
                }
                else
                {
                    Title = $"E-Mail-Verlauf – {c.DisplayName} ({emails.Count})";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Show_Click(object sender, RoutedEventArgs e)
        {
            await LoadEmailsAsync();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
