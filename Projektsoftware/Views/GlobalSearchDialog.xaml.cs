using System;
using System.Windows;
using System.Windows.Input;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class GlobalSearchDialog : Window
    {
        private readonly GlobalSearchService _searchService;

        public GlobalSearchDialog() : this(null) { }

        public GlobalSearchDialog(string? initialQuery)
        {
            InitializeComponent();
            _searchService = new GlobalSearchService(new DatabaseService());
            Loaded += async (_, __) =>
            {
                if (!string.IsNullOrWhiteSpace(initialQuery))
                {
                    QueryBox.Text = initialQuery;
                    await RunSearchAsync();
                }
                else
                {
                    QueryBox.Focus();
                }
            };
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await RunSearchAsync();
        }

        private async void QueryBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) await RunSearchAsync();
        }

        private async System.Threading.Tasks.Task RunSearchAsync()
        {
            var query = QueryBox.Text?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                StatusText.Text = "Bitte Suchbegriff eingeben.";
                return;
            }

            try
            {
                StatusText.Text = "Suche läuft...";
                SearchButton.IsEnabled = false;
                var results = await _searchService.SearchAllAsync(query);
                ResultsGrid.ItemsSource = results;
                StatusText.Text = $"{results.Count} Treffer gefunden.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Fehler: {ex.Message}";
            }
            finally
            {
                SearchButton.IsEnabled = true;
            }
        }
    }
}
