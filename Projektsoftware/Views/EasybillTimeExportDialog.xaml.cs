using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class EasybillTimeExportDialog : Window
    {
        private readonly DatabaseService databaseService;
        private readonly EasybillService easybillService;
        private ObservableCollection<EasybillCustomer> customers;
        private ObservableCollection<EasybillProject> projects;
        private ObservableCollection<TimeEntryExportItem> timeEntries;
        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");

        public EasybillTimeExportDialog()
        {
            InitializeComponent();

            databaseService = new DatabaseService();
            easybillService = new EasybillService();
            customers = new ObservableCollection<EasybillCustomer>();
            projects = new ObservableCollection<EasybillProject>();
            timeEntries = new ObservableCollection<TimeEntryExportItem>();

            CustomerComboBox.ItemsSource = customers;
            ProjectComboBox.ItemsSource = projects;
            TimeEntriesDataGrid.ItemsSource = timeEntries;

            Loaded += async (s, e) => await LoadCustomersAsync();
        }

        private async System.Threading.Tasks.Task LoadCustomersAsync()
        {
            try
            {
                var config = EasybillConfig.Load();
                if (!config.IsConfigured)
                {
                    MessageBox.Show(
                        "Easybill ist noch nicht konfiguriert!",
                        "Konfiguration erforderlich",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Close();
                    return;
                }

                var customersList = await easybillService.GetAllCustomersAsync();

                if (customersList != null)
                {
                    customers.Clear();
                    foreach (var customer in customersList)
                    {
                        customers.Add(customer);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Kunden:\n\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CustomerComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CustomerComboBox.SelectedItem is EasybillCustomer customer)
            {
                await LoadProjectsForCustomerAsync(customer.Id);
                await LoadTimeEntriesForCustomerAsync(customer.Id);
            }
        }

        private async System.Threading.Tasks.Task LoadProjectsForCustomerAsync(long customerId)
        {
            try
            {
                var projectsList = await easybillService.GetProjectsByCustomerAsync(customerId);

                projects.Clear();
                foreach (var project in projectsList)
                {
                    projects.Add(project);
                }

                // Wähle erstes Projekt automatisch aus, falls vorhanden
                if (projects.Any())
                {
                    ProjectComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Projekte:\n\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadTimeEntries_Click(object sender, RoutedEventArgs e)
        {
            if (CustomerComboBox.SelectedItem is EasybillCustomer customer)
            {
                await LoadTimeEntriesForCustomerAsync(customer.Id);
            }
            else
            {
                MessageBox.Show("Bitte wählen Sie zuerst einen Kunden aus.", "Hinweis",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async System.Threading.Tasks.Task LoadTimeEntriesForCustomerAsync(long customerId)
        {
            try
            {
                if (!decimal.TryParse(HourlyRateTextBox.Text, NumberStyles.Number, euroFormat, out decimal hourlyRate))
                {
                    MessageBox.Show("Bitte geben Sie einen gültigen Stundensatz ein.", "Validierung",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var entries = await databaseService.GetUnexportedTimeEntriesForCustomerAsync(customerId);

                timeEntries.Clear();
                foreach (var entry in entries)
                {
                    timeEntries.Add(new TimeEntryExportItem
                    {
                        TimeEntry = entry,
                        IsSelected = true,
                        HourlyRate = hourlyRate
                    });
                }

                UpdateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Zeiteinträge:\n\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTotals()
        {
            var selectedEntries = timeEntries.Where(e => e.IsSelected).ToList();
            var totalHours = selectedEntries.Sum(e => e.TimeEntry.Duration.TotalHours);

            if (decimal.TryParse(HourlyRateTextBox.Text, NumberStyles.Number, euroFormat, out decimal hourlyRate))
            {
                var totalAmount = (decimal)totalHours * hourlyRate;
                TotalHoursText.Text = $"{totalHours:F2} Stunden";
                TotalAmountText.Text = totalAmount.ToString("C2", euroFormat);
            }
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            var selectedEntries = timeEntries.Where(e => e.IsSelected).ToList();

            if (!selectedEntries.Any())
            {
                MessageBox.Show("Bitte wählen Sie mindestens einen Zeiteintrag aus.", "Hinweis",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (ProjectComboBox.SelectedItem == null)
            {
                MessageBox.Show("Bitte wählen Sie ein Easybill-Projekt aus.", "Hinweis",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!decimal.TryParse(HourlyRateTextBox.Text, NumberStyles.Number, euroFormat, out decimal hourlyRate))
            {
                MessageBox.Show("Bitte geben Sie einen gültigen Stundensatz ein.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var easybillProject = ProjectComboBox.SelectedItem as EasybillProject;

            var result = MessageBox.Show(
                $"Möchten Sie {selectedEntries.Count} Zeiteinträge ({selectedEntries.Sum(e => e.TimeEntry.Duration.TotalHours):F2} Stunden)\n" +
                $"zum Easybill-Projekt \"{easybillProject.Name}\" exportieren?",
                "Bestätigung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var exportedTrackings = await easybillService.ExportTimeEntriesToProjectAsync(
                    selectedEntries.Select(e => e.TimeEntry).ToList(),
                    easybillProject.Id.Value,
                    hourlyRate);

                var entryIds = selectedEntries.Select(e => e.TimeEntry.Id).ToList();
                var trackingIds = exportedTrackings.Select(t => t.Id.Value).ToList();

                await databaseService.MarkTimeEntriesAsExportedAsync(entryIds, trackingIds);

                MessageBox.Show(
                    $"✅ {exportedTrackings.Count} Zeiterfassungen erfolgreich zum Easybill-Projekt exportiert!",
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                if (CustomerComboBox.SelectedItem is EasybillCustomer customer)
                {
                    await LoadTimeEntriesForCustomerAsync(customer.Id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Export:\n\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class TimeEntryExportItem
    {
        public TimeEntry TimeEntry { get; set; }
        public bool IsSelected { get; set; }
        public decimal HourlyRate { get; set; }

        public DateTime Date => TimeEntry.Date;
        public string EmployeeName => TimeEntry.EmployeeName;
        public string ProjectName => TimeEntry.ProjectName;
        public string Activity => TimeEntry.Activity;
        public TimeSpan Duration => TimeEntry.Duration;
    }
}
