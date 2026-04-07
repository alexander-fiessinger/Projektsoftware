using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Projektsoftware.Views
{
    public partial class CreateProformaFromProjectDialog : Window
    {
        private readonly EasybillService easybillService;
        private readonly DatabaseService databaseService;
        private readonly Project project;
        private List<TimeEntry> projectTimeEntries;
        private VatResult? currentVatResult;
        private EasybillCustomer? projectCustomer;

        public EasybillDocument? CreatedDocument { get; private set; }

        public CreateProformaFromProjectDialog(Project project)
        {
            InitializeComponent();
            this.project = project;
            easybillService = new EasybillService();
            databaseService = new DatabaseService();

            ProjectNameTextBlock.Text = project.Name;
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                CreateButton.IsEnabled = false;
                StatusTextBlock.Text = "Lade Zeiteinträge...";

                var allTimeEntries = await databaseService.GetAllTimeEntriesAsync();
                projectTimeEntries = allTimeEntries.FindAll(t => t.ProjectId == project.Id);

                if (projectTimeEntries.Count == 0)
                {
                    MessageBox.Show(
                        $"Es wurden keine Zeiteinträge für das Projekt '{project.Name}' gefunden.\n\n" +
                        "Bitte erfassen Sie zuerst Zeiteinträge für dieses Projekt.",
                        "Keine Zeiteinträge",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Close();
                    return;
                }

                var totalHours = projectTimeEntries.Sum(t => t.Duration.TotalHours);
                TotalHoursTextBlock.Text = $"{totalHours:N2} Stunden";

                var firstEntry = projectTimeEntries.First();
                if (!string.IsNullOrEmpty(firstEntry.EmployeeName))
                {
                    var employees = await databaseService.GetAllEmployeesAsync();
                    var employee = employees.FirstOrDefault(e => $"{e.FirstName} {e.LastName}" == firstEntry.EmployeeName);
                    if (employee != null && employee.HourlyRate > 0)
                    {
                        HourlyRateTextBox.Text = employee.HourlyRate.ToString("N2");
                    }
                }

                await LoadCustomerVatInfoAsync();

                StatusTextBlock.Text = $"{projectTimeEntries.Count} Zeiteinträge geladen";
                CreateButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Laden der Daten:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
            }
        }

        private async System.Threading.Tasks.Task LoadCustomerVatInfoAsync()
        {
            try
            {
                if (project.EasybillCustomerId.HasValue)
                {
                    projectCustomer = await easybillService.GetCustomerAsync(project.EasybillCustomerId.Value);
                    currentVatResult = VatService.DetermineVat(projectCustomer);

                    if (currentVatResult.Scenario != VatScenario.Inland)
                    {
                        VatInfoBorder.Visibility = Visibility.Visible;
                        VatInfoBorder.Background = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString(currentVatResult.InfoColor));
                        VatInfoTextBlock.Text = currentVatResult.DisplayText;
                        VatDetailTextBlock.Text = currentVatResult.LegalNotice;
                    }
                }
                else
                {
                    currentVatResult = VatService.DetermineVat(null);
                }

                UpdatePreview();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Steuerermittlung: {ex.Message}");
                currentVatResult = VatService.DetermineVat(null);
            }
        }

        private void HourlyRate_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (PreviewTextBlock == null || projectTimeEntries == null) return;

            try
            {
                var totalHours = (decimal)projectTimeEntries.Sum(t => t.Duration.TotalHours);
                var hourlyRateText = HourlyRateTextBox.Text.Replace("€", "").Replace(",", ".").Trim();

                if (decimal.TryParse(hourlyRateText, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal hourlyRate))
                {
                    var vatPercent = currentVatResult?.VatPercent ?? 19;
                    var vatRate = vatPercent / 100m;
                    var netTotal = totalHours * hourlyRate;
                    var vatAmount = netTotal * vatRate;
                    var grossTotal = netTotal + vatAmount;

                    var vatLabel = currentVatResult?.Scenario switch
                    {
                        VatScenario.InnergemeinschaftlichB2B => $"MwSt ({vatPercent}% - Reverse Charge)",
                        VatScenario.DrittlandExport => $"MwSt ({vatPercent}% - Drittland-Export)",
                        VatScenario.Kleinunternehmer => $"MwSt ({vatPercent}% - §19 UStG)",
                        _ => $"MwSt ({vatPercent}%)"
                    };

                    PreviewTextBlock.Text =
                        $"Netto: {netTotal:N2} €\n" +
                        $"{vatLabel}: {vatAmount:N2} €\n" +
                        $"Brutto: {grossTotal:N2} €";
                }
                else
                {
                    PreviewTextBlock.Text = "Ungültiger Stundensatz";
                }
            }
            catch
            {
                PreviewTextBlock.Text = "Berechnungsfehler";
            }
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var hourlyRateText = HourlyRateTextBox.Text.Replace("€", "").Replace(",", ".").Trim();
                if (!decimal.TryParse(hourlyRateText, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal hourlyRate) || hourlyRate <= 0)
                {
                    MessageBox.Show(
                        "Bitte geben Sie einen gültigen Stundensatz ein (größer als 0).",
                        "Validierung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                CreateButton.IsEnabled = false;
                StatusTextBlock.Text = "Erstelle Proforma-Rechnung...";

                var isDraft = IsDraftCheckBox.IsChecked == true;
                var vatPercent = currentVatResult?.VatPercent ?? 19;
                var vatSuffix = currentVatResult?.DocumentSuffix;
                int validityDays = int.TryParse(ValidityDaysTextBox.Text, out var days) ? days : 30;

                CreatedDocument = await easybillService.CreateProformaFromTimeEntriesAsync(
                    project,
                    projectTimeEntries,
                    hourlyRate,
                    TextTextBox.Text,
                    validityDays,
                    isDraft,
                    vatPercent,
                    vatSuffix
                );

                StatusTextBlock.Text = "Proforma-Rechnung erfolgreich erstellt!";
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Erstellen der Proforma-Rechnung:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                CreateButton.IsEnabled = true;
                StatusTextBlock.Text = "Fehler";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
