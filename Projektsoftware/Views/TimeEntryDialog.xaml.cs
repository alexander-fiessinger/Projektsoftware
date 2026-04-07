using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class TimeEntryDialog : Window
    {
        public TimeEntry TimeEntry { get; private set; }
        private readonly EasybillService easybillService;
        private ObservableCollection<EasybillCustomer> easybillCustomers;

        public TimeEntryDialog(ObservableCollection<Project> projects, ObservableCollection<Employee> employees)
        {
            InitializeComponent();
            TimeEntry = new TimeEntry();

            // KRITISCH: Erstelle KOPIEN der Collections, damit externe Änderungen uns nicht betreffen!
            var projectsCopy = new ObservableCollection<Project>(projects);
            var employeesCopy = new ObservableCollection<Employee>(employees);

            // Setze ItemsSource nur EINMAL mit den KOPIEN
            ProjectComboBox.ItemsSource = projectsCopy;
            EmployeeComboBox.ItemsSource = employeesCopy;
            DatePicker.SelectedDate = DateTime.Now;

            // Easybill-Kunden: Separate ObservableCollection
            easybillService = new EasybillService();
            easybillCustomers = new ObservableCollection<EasybillCustomer>();
            CustomerComboBox.ItemsSource = easybillCustomers;

            // WICHTIG: Lade Easybill NICHT automatisch!
            // Nur wenn der Benutzer in die CustomerComboBox klickt
            bool easybillLoaded = false;
            CustomerComboBox.DropDownOpened += async (s, e) =>
            {
                if (!easybillLoaded)
                {
                    easybillLoaded = true;
                    await LoadEasybillCustomers();
                }
            };

            // Verhindere, dass die Auswahl beim Öffnen der DropDown verloren geht
            ProjectComboBox.DropDownClosed += PreserveSelection;
            EmployeeComboBox.DropDownClosed += PreserveSelection;
            CustomerComboBox.DropDownClosed += PreserveSelection;
        }

        public TimeEntryDialog(ObservableCollection<Project> projects, ObservableCollection<Employee> employees, TimeEntry prefilledEntry)
            : this(projects, employees)
        {
            if (prefilledEntry == null) return;

            DatePicker.SelectedDate = prefilledEntry.Date;
            DurationTextBox.Text = prefilledEntry.Duration.TotalHours.ToString("F2");
            ActivityTextBox.Text = prefilledEntry.Activity ?? "";
            DescriptionTextBox.Text = prefilledEntry.Description ?? "";
            EmployeeComboBox.Text = prefilledEntry.EmployeeName ?? "";

            var project = (ProjectComboBox.ItemsSource as ObservableCollection<Project>)
                ?.FirstOrDefault(p => p.Id == prefilledEntry.ProjectId);
            if (project != null)
                ProjectComboBox.SelectedItem = project;
        }

        private void PreserveSelection(object sender, EventArgs e)
        {
            // Diese Methode sorgt dafür, dass die Auswahl erhalten bleibt
            // Sie wird nach dem Schließen der DropDown aufgerufen
        }

        private async System.Threading.Tasks.Task LoadEasybillCustomers()
        {
            try
            {
                var config = EasybillConfig.Load();
                if (!config.IsConfigured)
                {
                    return;
                }

                var customers = await easybillService.GetAllCustomersAsync();

                if (customers != null)
                {
                    // Speichere die aktuelle Auswahl
                    var currentSelection = CustomerComboBox.SelectedItem;
                    var currentText = CustomerComboBox.Text;

                    // WICHTIG: Clear und Add OHNE die anderen ComboBoxen zu beeinflussen
                    easybillCustomers.Clear();
                    foreach (var customer in customers)
                    {
                        easybillCustomers.Add(customer);
                    }

                    // Stelle die Auswahl wieder her
                    if (currentSelection != null)
                    {
                        CustomerComboBox.SelectedItem = currentSelection;
                    }
                    else if (!string.IsNullOrEmpty(currentText))
                    {
                        CustomerComboBox.Text = currentText;
                    }
                }
            }
            catch
            {
                // Fehler beim Laden ignorieren
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectComboBox.SelectedItem == null)
            {
                MessageBox.Show("Bitte wählen Sie ein Projekt.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EmployeeComboBox.SelectedItem == null && string.IsNullOrWhiteSpace(EmployeeComboBox.Text))
            {
                MessageBox.Show("Bitte wählen Sie einen Mitarbeiter aus oder geben Sie einen Namen ein.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Bitte wählen Sie ein Datum.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(DurationTextBox.Text, out double hours) || hours <= 0)
            {
                MessageBox.Show("Bitte geben Sie eine gültige Dauer ein.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verwende SelectedItem direkt (EINFACHER)
            var selectedProject = ProjectComboBox.SelectedItem as Project;
            if (selectedProject != null)
            {
                TimeEntry.ProjectId = selectedProject.Id;
                TimeEntry.ProjectName = selectedProject.Name;
            }

            // Verwende ausgewählten Mitarbeiter oder eingegebenen Text
            if (EmployeeComboBox.SelectedItem is Employee selectedEmployee)
            {
                TimeEntry.EmployeeName = selectedEmployee.FullName;
            }
            else
            {
                TimeEntry.EmployeeName = EmployeeComboBox.Text;
            }

            TimeEntry.Date = DatePicker.SelectedDate.Value;
            TimeEntry.Duration = TimeSpan.FromHours(hours);
            TimeEntry.Activity = ActivityTextBox.Text;
            TimeEntry.Description = DescriptionTextBox.Text;

            // Kunde aus ComboBox oder freier Text
            if (CustomerComboBox.SelectedItem is EasybillCustomer selectedCustomer)
            {
                TimeEntry.ClientName = selectedCustomer.DisplayName;
                TimeEntry.EasybillCustomerId = selectedCustomer.Id;
            }
            else
            {
                TimeEntry.ClientName = CustomerComboBox.Text;
                TimeEntry.EasybillCustomerId = null;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void LoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TimeEntryTemplatesDialog(
                new System.Collections.Generic.List<Project>(
                    ProjectComboBox.ItemsSource as ObservableCollection<Project> ?? new ObservableCollection<Project>()))
            { Owner = this };

            if (dialog.ShowDialog() != true || dialog.SelectedTemplate is not TimeEntryTemplate t)
                return;

            // Apply template values to form fields
            ActivityTextBox.Text = t.Activity ?? string.Empty;
            DescriptionTextBox.Text = t.Description ?? string.Empty;

            DurationTextBox.Text = t.DefaultDuration.TotalHours.ToString("F2");

            if (t.ProjectId.HasValue)
            {
                foreach (var item in ProjectComboBox.Items)
                {
                    if (item is Project p && p.Id == t.ProjectId.Value)
                    {
                        ProjectComboBox.SelectedItem = p;
                        break;
                    }
                }
            }
        }

        private async void OpenEasybillCustomers_Click(object sender, RoutedEventArgs e)
        {
            // Speichere die aktuelle Auswahl vor dem Öffnen des Dialogs
            var currentSelection = CustomerComboBox.SelectedItem;
            var currentText = CustomerComboBox.Text;

            var dialog = new EasybillCustomersDialog();
            dialog.ShowDialog();

            // Kunden neu laden
            await LoadEasybillCustomers();

            // Stelle die Auswahl wieder her, falls sie noch gültig ist
            if (currentSelection != null)
            {
                var customer = easybillCustomers.FirstOrDefault(c => 
                    c.Id == (currentSelection as EasybillCustomer)?.Id);
                if (customer != null)
                {
                    CustomerComboBox.SelectedItem = customer;
                }
            }
            else if (!string.IsNullOrEmpty(currentText))
            {
                CustomerComboBox.Text = currentText;
            }
        }
    }
}
