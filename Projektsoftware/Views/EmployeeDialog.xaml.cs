using Projektsoftware.Models;
using System;
using System.Globalization;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class EmployeeDialog : Window
    {
        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");
        public Employee Employee { get; private set; }

        public EmployeeDialog(Employee existingEmployee = null)
        {
            InitializeComponent();

            if (existingEmployee != null)
            {
                Employee = existingEmployee;
                Title = "Mitarbeiter bearbeiten";
                LoadEmployeeData();
            }
            else
            {
                Employee = new Employee { CreatedAt = DateTime.Now, HireDate = DateTime.Today };
                Title = "Neuer Mitarbeiter";
                HourlyRateTextBox.Text = "0,00";
                VacationTotalTextBox.Text = "30";
                VacationUsedTextBox.Text = "0";
                UpdateVacationRemaining();
            }
        }

        private void LoadEmployeeData()
        {
            FirstNameTextBox.Text = Employee.FirstName;
            LastNameTextBox.Text = Employee.LastName;
            EmailTextBox.Text = Employee.Email;
            PhoneTextBox.Text = Employee.Phone;
            PositionTextBox.Text = Employee.Position;
            DepartmentTextBox.Text = Employee.Department;
            HourlyRateTextBox.Text = Employee.HourlyRate.ToString("F2", euroFormat);
            HireDatePicker.SelectedDate = Employee.HireDate;
            IsActiveCheckBox.IsChecked = Employee.IsActive;
            VacationTotalTextBox.Text = Employee.VacationDaysTotal.ToString();
            VacationUsedTextBox.Text = Employee.VacationDaysUsed.ToString();
            UpdateVacationRemaining();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) || string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Bitte füllen Sie alle Pflichtfelder (*) aus.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!HireDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Bitte wählen Sie ein Einstellungsdatum.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(HourlyRateTextBox.Text, NumberStyles.Number, euroFormat, out decimal hourlyRate))
            {
                MessageBox.Show("Bitte geben Sie einen gültigen Stundensatz ein.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Employee.FirstName = FirstNameTextBox.Text.Trim();
            Employee.LastName = LastNameTextBox.Text.Trim();
            Employee.Email = EmailTextBox.Text.Trim();
            Employee.Phone = PhoneTextBox.Text.Trim();
            Employee.Position = PositionTextBox.Text.Trim();
            Employee.Department = DepartmentTextBox.Text.Trim();
            Employee.HourlyRate = hourlyRate;
            Employee.HireDate = HireDatePicker.SelectedDate.Value;
            Employee.IsActive = IsActiveCheckBox.IsChecked == true;

            if (int.TryParse(VacationTotalTextBox.Text, out int vacTotal))
                Employee.VacationDaysTotal = vacTotal;
            if (int.TryParse(VacationUsedTextBox.Text, out int vacUsed))
                Employee.VacationDaysUsed = vacUsed;

            DialogResult = true;
            Close();
        }

        private void UpdateVacationRemaining()
        {
            int total = int.TryParse(VacationTotalTextBox.Text, out int t) ? t : 0;
            int used = int.TryParse(VacationUsedTextBox.Text, out int u) ? u : 0;
            VacationRemainingText.Text = $"Verbleibend: {total - used} Tage";
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
