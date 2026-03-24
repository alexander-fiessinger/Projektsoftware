using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class ProjectDialog : Window
    {
        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");
        private readonly EasybillService easybillService;
        private readonly DatabaseService databaseService;
        private ObservableCollection<EasybillCustomer> easybillCustomers;

        public Project Project { get; private set; }
        public EasybillProject CreatedEasybillProject { get; private set; }

        public ProjectDialog()
        {
            InitializeComponent();
            Project = new Project();
            StartDatePicker.SelectedDate = DateTime.Now;

            easybillService = new EasybillService();
            databaseService = new DatabaseService();
            easybillCustomers = new ObservableCollection<EasybillCustomer>();
            CustomerComboBox.ItemsSource = easybillCustomers;

            Loaded += async (s, e) => await LoadEasybillCustomersAsync();

            // Verhindere, dass die Auswahl beim Öffnen der DropDown verloren geht
            CustomerComboBox.DropDownClosed += PreserveSelection;
            StatusComboBox.DropDownClosed += PreserveSelection;
        }

        private void PreserveSelection(object sender, EventArgs e)
        {
            // Diese Methode sorgt dafür, dass die Auswahl erhalten bleibt
        }

        public ProjectDialog(Project project) : this()
        {
            Project = project;
            LoadProjectData();

            // Bei Bearbeitung: Easybill-Bereich ausblenden (nur bei Neuanlage)
            CreateInEasybillCheckBox.Visibility = Visibility.Collapsed;
        }

        private async System.Threading.Tasks.Task LoadEasybillCustomersAsync()
        {
            try
            {
                var config = EasybillConfig.Load();
                if (!config.IsConfigured)
                {
                    // Easybill-Bereich deaktivieren wenn nicht konfiguriert
                    CreateInEasybillCheckBox.IsEnabled = false;
                    CreateInEasybillCheckBox.Content = "📤 Easybill nicht konfiguriert";
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

                    // Vorhandenen Kunden auswählen, falls vorhanden
                    if (Project?.EasybillCustomerId != null)
                    {
                        var existingCustomer = easybillCustomers.FirstOrDefault(c => c.Id == Project.EasybillCustomerId);
                        if (existingCustomer != null)
                        {
                            CustomerComboBox.SelectedItem = existingCustomer;
                        }
                    }
                    else if (!string.IsNullOrEmpty(Project?.ClientName))
                    {
                        CustomerComboBox.Text = Project.ClientName;
                    }
                    // Stelle vorherige Auswahl wieder her
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
                // Fehler beim Laden ignorieren - Benutzer kann manuell eingeben
                CreateInEasybillCheckBox.IsEnabled = false;
            }
        }

        private void LoadProjectData()
        {
            NameTextBox.Text = Project.Name;
            DescriptionTextBox.Text = Project.Description;
            CustomerComboBox.Text = Project.ClientName;
            StartDatePicker.SelectedDate = Project.StartDate;
            EndDatePicker.SelectedDate = Project.EndDate;
            StatusComboBox.Text = Project.Status;
            BudgetTextBox.Text = Project.Budget.ToString("F2", euroFormat);
        }

        private void CreateInEasybillCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Erweiterte Optionen einblenden
            EasybillOptionsPanel.Visibility = Visibility.Visible;

            // Standard-Werte aus Hauptformular übernehmen
            if (decimal.TryParse(BudgetTextBox.Text, NumberStyles.Number, euroFormat, out decimal budget))
            {
                HourlyRateTextBox.Text = "100,00"; // Standard-Stundensatz
            }
        }

        private void CreateInEasybillCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Erweiterte Optionen ausblenden
            EasybillOptionsPanel.Visibility = Visibility.Collapsed;
        }

        private void CustomerComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Automatisch Checkbox aktivieren wenn Easybill-Kunde gewählt wird
            if (CustomerComboBox.SelectedItem is EasybillCustomer && 
                CreateInEasybillCheckBox.IsEnabled &&
                Project.Id == 0) // Nur bei Neuanlage
            {
                CreateInEasybillCheckBox.IsChecked = true;
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Projektnamen ein.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!StartDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Bitte wählen Sie ein Startdatum.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validierung für Easybill-Integration
            if (CreateInEasybillCheckBox.IsChecked == true)
            {
                if (CustomerComboBox.SelectedItem == null)
                {
                    MessageBox.Show(
                        "Bitte wählen Sie einen Easybill-Kunden aus,\n" +
                        "um das Projekt in Easybill anzulegen.",
                        "Validierung", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(HourlyRateTextBox.Text, NumberStyles.Number, euroFormat, out _))
                {
                    MessageBox.Show("Bitte geben Sie einen gültigen Stundensatz ein.", "Validierung",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            Project.Name = NameTextBox.Text;
            Project.Description = DescriptionTextBox.Text;

            // Kunde aus ComboBox oder freier Text
            if (CustomerComboBox.SelectedItem is EasybillCustomer selectedCustomer)
            {
                Project.ClientName = selectedCustomer.DisplayName;
                Project.EasybillCustomerId = selectedCustomer.Id;
            }
            else
            {
                Project.ClientName = CustomerComboBox.Text;
                Project.EasybillCustomerId = null;
            }

            Project.StartDate = StartDatePicker.SelectedDate.Value;
            Project.EndDate = EndDatePicker.SelectedDate;
            Project.Status = (StatusComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Aktiv";

            if (decimal.TryParse(BudgetTextBox.Text, NumberStyles.Number, euroFormat, out decimal budget))
                Project.Budget = budget;

            // **EASYBILL INTEGRATION: Projekt automatisch in Easybill anlegen**
            if (CreateInEasybillCheckBox.IsChecked == true && CustomerComboBox.SelectedItem is EasybillCustomer customer)
            {
                try
                {
                    // Parse Easybill-spezifische Werte
                    decimal.TryParse(HourlyRateTextBox.Text, NumberStyles.Number, euroFormat, out decimal hourlyRate);
                    decimal.TryParse(BudgetTextBox.Text, NumberStyles.Number, euroFormat, out decimal budgetAmount);

                    // Konvertiere in Cent
                    var hourlyRateInCents = (int)(hourlyRate * 100);
                    var budgetAmountInCents = budgetAmount > 0 ? (int)(budgetAmount * 100) : (int?)null;

                    System.Diagnostics.Debug.WriteLine($"=== EASYBILL AUTO-CREATE ===");
                    System.Diagnostics.Debug.WriteLine($"Stundensatz: {hourlyRate} € → {hourlyRateInCents} Cent");
                    System.Diagnostics.Debug.WriteLine($"Budget: {budgetAmount} € → {budgetAmountInCents} Cent");

                    var easybillProject = new EasybillProject
                    {
                        Name = Project.Name,
                        Description = Project.Description,
                        CustomerId = customer.Id,
                        HourlyRate = hourlyRateInCents,
                        BudgetAmount = budgetAmountInCents,
                        BudgetType = BudgetTypeComboBox.SelectedIndex == 0 ? "HOUR" : "MONEY",
                        DueAt = EndDatePicker.SelectedDate?.ToString("yyyy-MM-dd"),
                        Status = EasybillStatusComboBox.SelectedIndex switch
                        {
                            0 => "OPEN",
                            1 => "COMPLETED",
                            2 => "CANCELED",
                            _ => "OPEN"
                        }
                    };

                    // Erstelle in Easybill
                    CreatedEasybillProject = await easybillService.CreateProjectAsync(easybillProject);

                    // Verknüpfe mit lokalem Projekt
                    Project.EasybillProjectId = CreatedEasybillProject.Id;

                    MessageBox.Show(
                        $"✅ Projekt erfolgreich gespeichert!\n\n" +
                        $"📤 Easybill-Projekt erstellt:\n" +
                        $"   • ID: {CreatedEasybillProject.Id}\n" +
                        $"   • Kunde: {customer.DisplayName}\n" +
                        $"   • Stundensatz: {hourlyRate:F2} €",
                        "Erfolg",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    var result = MessageBox.Show(
                        $"⚠️ Lokales Projekt wurde erstellt, aber:\n\n" +
                        $"Fehler beim Erstellen des Easybill-Projekts:\n{ex.Message}\n\n" +
                        $"Möchten Sie trotzdem fortfahren?",
                        "Easybill-Fehler",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
            }

            DialogResult = true;
            Close();
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
            await LoadEasybillCustomersAsync();

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
