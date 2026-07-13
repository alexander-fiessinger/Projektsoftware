using System;
using System.Linq;
using System.Windows;
using Projektsoftware.Models;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class BudgetTrackingDialog : Window
    {
        private readonly DatabaseService _db = new DatabaseService();

        public BudgetTrackingDialog()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadProjectsAsync();
        }

        private async System.Threading.Tasks.Task LoadProjectsAsync()
        {
            try
            {
                var projects = await _db.GetAllProjectsAsync();
                ProjectCombo.ItemsSource = projects.OrderBy(p => p.Name).ToList();
                if (projects.Any()) ProjectCombo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Show_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectCombo.SelectedItem is not Project project) return;
            try
            {
                var budget = await _db.GetProjectBudgetAsync(project.Id);
                var entries = await _db.GetBudgetEntriesByProjectAsync(project.Id);

                PlannedHoursText.Text = budget?.TotalPlannedHours.ToString("N1") ?? "0";
                ActualHoursText.Text = budget?.TotalActualHours.ToString("N1") ?? "0";
                PlannedBudgetText.Text = budget != null ? $"{budget.TotalPlannedBudget:N2} €" : "0 €";
                UtilizationText.Text = budget != null ? $"{budget.BudgetUtilizationPercentage:N1} %" : "0 %";

                EntriesGrid.ItemsSource = entries;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
