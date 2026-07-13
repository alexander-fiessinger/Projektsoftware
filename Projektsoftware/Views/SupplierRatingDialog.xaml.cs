using System;
using System.Linq;
using System.Windows;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class SupplierRatingDialog : Window
    {
        private readonly DatabaseService _db = new DatabaseService();

        public SupplierRatingDialog()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var ratings = await _db.GetSupplierRatingsAsync();
                RatingsGrid.ItemsSource = ratings.OrderByDescending(r => r.RatingDate).ToList();
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
