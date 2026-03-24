using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class EasybillProjectDialog : Window
    {
        private readonly DatabaseService databaseService;
        private readonly EasybillService easybillService;
        private ObservableCollection<Project> localProjects;
        private ObservableCollection<EasybillCustomer> customers;
        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");

        public EasybillProject CreatedProject { get; private set; }
        public Project LinkedLocalProject { get; private set; }

        public EasybillProjectDialog()
        {
            InitializeComponent();

            databaseService = new DatabaseService();
            easybillService = new EasybillService();
            localProjects = new ObservableCollection<Project>();
            customers = new ObservableCollection<EasybillCustomer>();

            LocalProjectComboBox.ItemsSource = localProjects;
            CustomerComboBox.ItemsSource = customers;

            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
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

                // Lade lokale Projekte
                var projects = await databaseService.GetAllProjectsAsync();
                foreach (var project in projects)
                {
                    localProjects.Add(project);
                }

                // Lade Easybill-Kunden
                var customersList = await easybillService.GetAllCustomersAsync();
                foreach (var customer in customersList)
                {
                    customers.Add(customer);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden:\n\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LocalProject_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LocalProjectComboBox.SelectedItem is Project project)
            {
                // Fülle die Felder automatisch mit Daten aus dem lokalen Projekt
                ProjectNameTextBox.Text = project.Name;
                DescriptionTextBox.Text = project.Description;

                // Wähle den Kunden aus, falls bereits verknüpft
                if (project.EasybillCustomerId.HasValue)
                {
                    var customer = customers.FirstOrDefault(c => c.Id == project.EasybillCustomerId.Value);
                    if (customer != null)
                    {
                        CustomerComboBox.SelectedItem = customer;
                    }
                }

                // Setze Budget
                if (project.Budget > 0)
                {
                    BudgetAmountTextBox.Text = project.Budget.ToString("F2", euroFormat);
                }

                // Setze Fälligkeitsdatum
                if (project.EndDate.HasValue)
                {
                    DueDatePicker.SelectedDate = project.EndDate.Value;
                }
            }
        }

        private void Customer_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Aktualisiere auch das lokale Projekt mit dem gewählten Kunden
            if (LocalProjectComboBox.SelectedItem is Project project && 
                CustomerComboBox.SelectedItem is EasybillCustomer customer)
            {
                project.EasybillCustomerId = customer.Id;
                project.ClientName = customer.DisplayName;
            }
        }

        private async void CreateInEasybill_Click(object sender, RoutedEventArgs e)
        {
            // Validierung
            if (CustomerComboBox.SelectedItem == null)
            {
                MessageBox.Show("Bitte wählen Sie einen Easybill-Kunden aus.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Projektnamen ein.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(HourlyRateTextBox.Text, NumberStyles.Number, euroFormat, out decimal hourlyRate))
            {
                MessageBox.Show("Bitte geben Sie einen gültigen Stundensatz ein.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(BudgetAmountTextBox.Text, NumberStyles.Number, euroFormat, out decimal budgetAmount))
            {
                MessageBox.Show("Bitte geben Sie einen gültigen Budget-Betrag ein.", "Validierung",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var customer = CustomerComboBox.SelectedItem as EasybillCustomer;

                // Easybill erwartet Geldbeträge in CENT (100 € = 10000 Cent)
                var hourlyRateInCents = (int)(hourlyRate * 100);
                var budgetAmountInCents = budgetAmount > 0 ? (int)(budgetAmount * 100) : (int?)null;

                System.Diagnostics.Debug.WriteLine($"=== PROJEKT EXPORT ===");
                System.Diagnostics.Debug.WriteLine($"Stundensatz: {hourlyRate} € → {hourlyRateInCents} Cent");
                System.Diagnostics.Debug.WriteLine($"Budget: {budgetAmount} € → {budgetAmountInCents} Cent");

                // Erstelle Easybill-Projekt
                var easybillProject = new EasybillProject
                {
                    Name = ProjectNameTextBox.Text.Trim(),
                    Description = DescriptionTextBox.Text?.Trim(),
                    CustomerId = customer.Id,
                    HourlyRate = hourlyRateInCents,
                    BudgetAmount = budgetAmountInCents,
                    BudgetType = BudgetTypeComboBox.SelectedIndex == 0 ? "HOUR" : "MONEY",
                    DueAt = DueDatePicker.SelectedDate?.ToString("yyyy-MM-dd"),
                    Status = StatusComboBox.SelectedIndex switch
                    {
                        0 => "OPEN",
                        1 => "COMPLETED",
                        2 => "CANCELED",
                        _ => "OPEN"
                    }
                };

                // Erstelle in Easybill
                CreatedProject = await easybillService.CreateProjectAsync(easybillProject);

                // Verknüpfe mit lokalem Projekt, falls ausgewählt
                if (LocalProjectComboBox.SelectedItem is Project localProject)
                {
                    localProject.EasybillProjectId = CreatedProject.Id;
                    localProject.EasybillCustomerId = customer.Id;
                    localProject.ClientName = customer.DisplayName;
                    
                    await databaseService.UpdateProjectAsync(localProject);
                    LinkedLocalProject = localProject;
                }

                MessageBox.Show(
                    $"✅ Easybill-Projekt '{CreatedProject.Name}' erfolgreich erstellt!\n\n" +
                    $"Projekt-ID: {CreatedProject.Id}\n" +
                    (LinkedLocalProject != null ? $"Verknüpft mit: {LinkedLocalProject.Name}" : ""),
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Erstellen des Projekts:\n\n{ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
