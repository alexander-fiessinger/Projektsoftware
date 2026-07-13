using System;
using System.Windows;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class ActivityFeedDialog : Window
    {
        private readonly DatabaseService _db = new DatabaseService();

        public ActivityFeedDialog()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var items = await _db.GetActivityFeedAsync(100);
                FeedGrid.ItemsSource = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
