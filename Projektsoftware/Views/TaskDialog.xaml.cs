using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class TaskDialog : Window
    {
        public ProjectTask Task { get; private set; }
        public bool AssignmentChanged { get; private set; }
        private readonly EasybillService easybillService;
        private ObservableCollection<EasybillCustomer> easybillCustomers;
        private string _originalAssignedTo;

        public TaskDialog(List<Project> availableProjects, List<Employee> availableEmployees = null, ProjectTask existingTask = null)
        {
            InitializeComponent();

            // KRITISCH: Erstelle KOPIEN der Listen, damit externe Änderungen uns nicht betreffen
            var projectsCopy = new List<Project>(availableProjects);
            var employeesCopy = new List<Employee>(availableEmployees ?? new List<Employee>());

            // Setze ItemsSource nur EINMAL mit den KOPIEN
            ProjectComboBox.ItemsSource = projectsCopy;
            AssignedToComboBox.ItemsSource = employeesCopy;
            StatusComboBox.ItemsSource = new[] { "Offen", "In Arbeit", "Erledigt", "Blockiert" };
            PriorityComboBox.ItemsSource = new[] { "Niedrig", "Normal", "Hoch", "Kritisch" };

            // Easybill-Kunden
            easybillService = new EasybillService();
            easybillCustomers = new ObservableCollection<EasybillCustomer>();
            CustomerComboBox.ItemsSource = easybillCustomers;

            if (existingTask != null)
            {
                Task = existingTask;
                _originalAssignedTo = existingTask.AssignedTo ?? "";
                Title = "Aufgabe bearbeiten";

                // Lade Task-Daten
                LoadTaskData(projectsCopy, employeesCopy);
            }
            else
            {
                Task = new ProjectTask { CreatedAt = DateTime.Now, Status = "Offen", Priority = "Normal" };
                _originalAssignedTo = "";
                StatusComboBox.SelectedIndex = 0;
                PriorityComboBox.SelectedIndex = 1;
                Title = "Neue Aufgabe";
            }

            // Lade Easybill-Kunden (fire and forget, da Constructor nicht async sein kann)
            _ = LoadEasybillCustomers();

            // Verhindere, dass die Auswahl beim Öffnen der DropDown verloren geht
            ProjectComboBox.DropDownClosed += PreserveSelection;
            AssignedToComboBox.DropDownClosed += PreserveSelection;
            CustomerComboBox.DropDownClosed += PreserveSelection;
            StatusComboBox.DropDownClosed += PreserveSelection;
            PriorityComboBox.DropDownClosed += PreserveSelection;
        }

        private void PreserveSelection(object sender, EventArgs e)
        {
            // Diese Methode sorgt dafür, dass die Auswahl erhalten bleibt
            // Sie wird nach dem Schließen der DropDown aufgerufen
        }

        private void LoadTaskData(List<Project> projects, List<Employee> employees)
        {
            // Projekt auswählen
            var project = projects.FirstOrDefault(p => p.Id == Task.ProjectId);
            if (project != null)
            {
                ProjectComboBox.SelectedItem = project;
            }

            // Mitarbeiter auswählen oder Text setzen
            var employee = employees.FirstOrDefault(e => e.FullName == Task.AssignedTo);
            if (employee != null)
            {
                AssignedToComboBox.SelectedItem = employee;
            }
            else if (!string.IsNullOrEmpty(Task.AssignedTo))
            {
                AssignedToComboBox.Text = Task.AssignedTo;
            }

            // Andere Felder
            TitleTextBox.Text = Task.Title;
            DescriptionTextBox.Text = Task.Description;
            StatusComboBox.SelectedItem = Task.Status;
            PriorityComboBox.SelectedItem = Task.Priority;
            DueDatePicker.SelectedDate = Task.DueDate;
            EstimatedHoursTextBox.Text = Task.EstimatedHours.ToString();
            ActualHoursTextBox.Text = Task.ActualHours.ToString();

            IsRecurringCheckBox.IsChecked = Task.IsRecurring;
            RecurrenceIntervalTextBox.Text = Task.RecurrenceIntervalDays > 0 ? Task.RecurrenceIntervalDays.ToString() : "7";
            RecurrencePanel.Visibility = Task.IsRecurring ? Visibility.Visible : Visibility.Collapsed;
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
                    var currentSelection = CustomerComboBox.SelectedItem as EasybillCustomer;
                    var currentText = CustomerComboBox.Text;

                    easybillCustomers.Clear();
                    foreach (var customer in customers)
                    {
                        easybillCustomers.Add(customer);
                    }

                    // Kunde auswählen falls vorhanden
                    if (Task?.EasybillCustomerId != null)
                    {
                        var existingCustomer = easybillCustomers.FirstOrDefault(c => c.Id == Task.EasybillCustomerId);
                        if (existingCustomer != null)
                        {
                            CustomerComboBox.SelectedItem = existingCustomer;
                        }
                    }
                    else if (!string.IsNullOrEmpty(Task?.ClientName))
                    {
                        CustomerComboBox.Text = Task.ClientName;
                    }
                    // Stelle vorherige Auswahl wieder her, falls keine Task-Daten vorhanden
                    else if (currentSelection != null)
                    {
                        var customer = easybillCustomers.FirstOrDefault(c => c.Id == currentSelection.Id);
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
            catch
            {
                // Fehler beim Laden ignorieren
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectComboBox.SelectedItem == null || string.IsNullOrWhiteSpace(TitleTextBox.Text) || 
                StatusComboBox.SelectedItem == null || PriorityComboBox.SelectedItem == null)
            {
                MessageBox.Show("Bitte füllen Sie alle Pflichtfelder (*) aus.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(EstimatedHoursTextBox.Text, out int estimatedHours) || estimatedHours < 0)
            {
                MessageBox.Show("Bitte geben Sie einen gültigen Wert für geschätzte Stunden ein.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(ActualHoursTextBox.Text, out int actualHours) || actualHours < 0)
            {
                MessageBox.Show("Bitte geben Sie einen gültigen Wert für tatsächliche Stunden ein.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verwende SelectedItem direkt
            var selectedProject = ProjectComboBox.SelectedItem as Project;
            if (selectedProject != null)
            {
                Task.ProjectId = selectedProject.Id;
                Task.ProjectName = selectedProject.Name;
            }

            Task.Title = TitleTextBox.Text.Trim();
            Task.Description = DescriptionTextBox.Text.Trim();

            // Kunde aus ComboBox oder freier Text
            if (CustomerComboBox.SelectedItem is EasybillCustomer selectedCustomer)
            {
                Task.ClientName = selectedCustomer.DisplayName;
                Task.EasybillCustomerId = selectedCustomer.Id;
            }
            else
            {
                Task.ClientName = CustomerComboBox.Text.Trim();
                Task.EasybillCustomerId = null;
            }

            // Mitarbeiter aus ComboBox oder freier Text
            if (AssignedToComboBox.SelectedItem is Employee selectedEmployee)
            {
                Task.AssignedTo = selectedEmployee.FullName;
            }
            else
            {
                Task.AssignedTo = AssignedToComboBox.Text.Trim();
            }

            // Prüfe ob Zuweisung geändert wurde
            AssignmentChanged = !string.IsNullOrEmpty(Task.AssignedTo)
                && !string.Equals(_originalAssignedTo, Task.AssignedTo, StringComparison.OrdinalIgnoreCase);

            Task.Status = StatusComboBox.SelectedItem.ToString();
            Task.Priority = PriorityComboBox.SelectedItem.ToString();
            Task.DueDate = DueDatePicker.SelectedDate;
            Task.EstimatedHours = estimatedHours;
            Task.ActualHours = actualHours;
            Task.UpdatedAt = DateTime.Now;

            Task.IsRecurring = IsRecurringCheckBox.IsChecked == true;
            if (Task.IsRecurring && int.TryParse(RecurrenceIntervalTextBox.Text, out int interval) && interval > 0)
                Task.RecurrenceIntervalDays = interval;
            else
                Task.RecurrenceIntervalDays = 0;

            if (Task.Status == "Erledigt" && !Task.CompletedDate.HasValue)
                Task.CompletedDate = DateTime.Now;

            DialogResult = true;
            Close();
        }

        private void IsRecurringCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            RecurrencePanel.Visibility = IsRecurringCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void OpenEasybillCustomers_Click(object sender, RoutedEventArgs e)
        {
            // Speichere die aktuelle Auswahl vor dem Öffnen des Dialogs
            var currentSelection = CustomerComboBox.SelectedItem as EasybillCustomer;
            var currentText = CustomerComboBox.Text;

            var dialog = new EasybillCustomersDialog();
            dialog.ShowDialog();

            // Kunden nach dem Schließen neu laden
            await LoadEasybillCustomers();

            // Stelle die Auswahl wieder her, falls sie noch gültig ist
            if (currentSelection != null)
            {
                var customer = easybillCustomers.FirstOrDefault(c => c.Id == currentSelection.Id);
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
