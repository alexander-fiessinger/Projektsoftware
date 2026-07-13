using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class PortalOrdersDialog : Window
    {
        private readonly DatabaseService _db = new DatabaseService();

        public PortalOrdersDialog()
        {
            InitializeComponent();
            Loaded += async (_, _) => await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                // 0 = nur neu, 1 = alle, 2 = in Bearbeitung, 3 = erledigt
                int? statusFilter = StatusFilter.SelectedIndex switch
                {
                    0 => 0,
                    2 => 1,
                    3 => 2,
                    _ => (int?)null
                };

                var orders = await _db.GetPortalOrdersAsync(statusFilter);

                if (orders.Count == 0)
                {
                    OrdersList.Visibility = Visibility.Collapsed;
                    EmptyText.Visibility = Visibility.Visible;
                }
                else
                {
                    OrdersList.Visibility = Visibility.Visible;
                    EmptyText.Visibility = Visibility.Collapsed;
                    OrdersList.ItemsSource = orders;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bestellungen konnten nicht geladen werden:\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadOrdersAsync();

        private async void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                await LoadOrdersAsync();
        }

        private async void SetInProgress_Click(object sender, RoutedEventArgs e) => await ChangeStatusAsync(sender, 1);

        private async void SetDone_Click(object sender, RoutedEventArgs e) => await ChangeStatusAsync(sender, 2);

        private async void SetCancelled_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Diese Bestellung wirklich stornieren?",
                "Bestellung stornieren", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                await ChangeStatusAsync(sender, 3);
        }

        private async Task ChangeStatusAsync(object sender, int status)
        {
            if (sender is Button btn && btn.Tag is int orderId)
            {
                try
                {
                    await _db.UpdatePortalOrderStatusAsync(orderId, status);
                    await LoadOrdersAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Status konnte nicht geändert werden:\n{ex.Message}",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
