using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class ExpensesAnalyticsDialog : Window
    {
        private readonly DatabaseService _db = new DatabaseService();
        private const double MaxBarWidth = 500;

        public ExpensesAnalyticsDialog()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        public class BarItem
        {
            public string Label { get; set; } = "";
            public decimal Value { get; set; }
            public double BarWidth { get; set; }
            public string ValueText => $"{Value:N2} €";
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var expenses = await _db.GetMonthlyExpensesAsync();
                if (expenses.Count == 0)
                {
                    BarsControl.ItemsSource = new List<BarItem>();
                    TotalText.Text = "Keine Ausgabendaten vorhanden.";
                    return;
                }

                var grouped = expenses
                    .GroupBy(e => e.CategoryId?.ToString() ?? "Unkategorisiert")
                    .Select(g => new { Label = g.Key, Value = g.Sum(x => x.TotalAmount) })
                    .OrderByDescending(g => g.Value)
                    .ToList();

                decimal max = grouped.Max(g => g.Value);
                decimal total = grouped.Sum(g => g.Value);

                var items = grouped.Select(g => new BarItem
                {
                    Label = $"Kategorie {g.Label}",
                    Value = g.Value,
                    BarWidth = max > 0 ? (double)(g.Value / max) * MaxBarWidth : 0
                }).ToList();

                BarsControl.ItemsSource = items;
                TotalText.Text = $"Gesamtausgaben: {total:N2} €  |  Kategorien: {items.Count}";
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
