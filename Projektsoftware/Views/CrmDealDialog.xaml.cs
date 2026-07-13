using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class CrmDealDialog : Window
    {
        public CrmDeal Deal { get; private set; }

        public CrmDealDialog(List<Customer> customers, List<CrmContact> contacts, List<Employee> employees, CrmDeal existing = null)
        {
            InitializeComponent();

            var allCustomers = new List<Customer> { new Customer { Id = 0, CompanyName = "— Kein Kunde —" } };
            allCustomers.AddRange(customers);
            CustomerCombo.ItemsSource = allCustomers;
            CustomerCombo.SelectedIndex = 0;

            var allContacts = new List<CrmContact> { new CrmContact { Id = 0, FirstName = "—", LastName = "Kein Kontakt —" } };
            allContacts.AddRange(contacts);
            ContactCombo.ItemsSource = allContacts;
            ContactCombo.SelectedIndex = 0;

            var allEmployees = new List<Employee> { new Employee { Id = 0, FirstName = "—", LastName = "Kein Mitarbeiter —" } };
            allEmployees.AddRange(employees.Where(e => e.IsActive));
            AssignedToCombo.ItemsSource = allEmployees;
            AssignedToCombo.SelectedIndex = 0;

            if (existing != null)
            {
                TitleText.Text = "Deal bearbeiten";
                Deal = existing;
                TitleBox.Text = existing.Title;
                ValueBox.Text = existing.Value.ToString("F2");
                ProbabilityBox.Text = existing.Probability.ToString();
                CloseDatePicker.SelectedDate = existing.ExpectedCloseDate;
                var matchedEmployee = allEmployees.FirstOrDefault(emp => emp.FullName == existing.AssignedTo);
                if (matchedEmployee != null)
                    AssignedToCombo.SelectedItem = matchedEmployee;
                else if (!string.IsNullOrWhiteSpace(existing.AssignedTo))
                    AssignedToCombo.SelectedIndex = 0;
                NotesBox.Text = existing.Notes;
                LostReasonBox.Text = existing.LostReason;

                foreach (ComboBoxItem item in StageCombo.Items)
                    if (item.Tag?.ToString() == ((int)existing.Stage).ToString())
                    { item.IsSelected = true; break; }

                if (existing.CustomerId.HasValue)
                    CustomerCombo.SelectedItem = allCustomers.FirstOrDefault(c => c.Id == existing.CustomerId.Value);
                if (existing.ContactId.HasValue)
                    ContactCombo.SelectedItem = allContacts.FirstOrDefault(c => c.Id == existing.ContactId.Value);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                MessageBox.Show("Bitte einen Titel angeben.", "Pflichtfeld", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(ValueBox.Text.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal value))
            {
                MessageBox.Show("Bitte einen gültigen Wert eingeben.", "Ungültige Eingabe", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(ProbabilityBox.Text, out int probability) || probability < 0 || probability > 100)
            {
                MessageBox.Show("Wahrscheinlichkeit muss zwischen 0 und 100 liegen.", "Ungültige Eingabe", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedStageTag = (StageCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            var selectedCustomer = CustomerCombo.SelectedItem as Customer;
            var selectedContact = ContactCombo.SelectedItem as CrmContact;
            var stage = int.TryParse(selectedStageTag, out int s) ? (DealStage)s : DealStage.Lead;

            if (Deal == null)
                Deal = new CrmDeal();

            Deal.Title = TitleBox.Text.Trim();
            Deal.Value = value;
            Deal.Probability = probability;
            Deal.Stage = stage;
            Deal.ExpectedCloseDate = CloseDatePicker.SelectedDate;
            var selectedEmployee = AssignedToCombo.SelectedItem as Employee;
            Deal.AssignedTo = selectedEmployee?.Id > 0 ? selectedEmployee.FullName : string.Empty;
            Deal.Notes = NotesBox.Text.Trim();
            Deal.LostReason = LostReasonBox.Text.Trim();
            Deal.CustomerId = selectedCustomer?.Id > 0 ? selectedCustomer.Id : null;
            Deal.ContactId = selectedContact?.Id > 0 ? selectedContact.Id : null;

            if (stage == DealStage.Won && !Deal.WonAt.HasValue)
                Deal.WonAt = DateTime.Now;
            else if (stage == DealStage.Lost && !Deal.LostAt.HasValue)
                Deal.LostAt = DateTime.Now;

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private async void AiScore_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                MessageBox.Show("Bitte geben Sie mindestens einen Titel ein, bevor Sie das KI-Scoring verwenden.", 
                    "Titel erforderlich", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var aiService = new LogicCAiService();
            if (!aiService.IsConfigured)
            {
                MessageBox.Show("LogicC AI ist nicht konfiguriert.\n\nBitte konfigurieren Sie die API im Menü:\nEinstellungen → 🤖 KI-Integration → Konfiguration",
                    "KI nicht konfiguriert", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AiScoreButton.IsEnabled = false;
            AiScoreResultText.Visibility = Visibility.Visible;
            AiScoreResultText.Text = "⏳ KI analysiert Lead...";
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                var title = TitleBox.Text.Trim();
                var description = NotesBox.Text.Trim();
                decimal.TryParse(ValueBox.Text.Replace(",", "."), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal value);

                var customerInfo = "";
                var selectedCustomer = CustomerCombo.SelectedItem as Customer;
                if (selectedCustomer != null && selectedCustomer.Id > 0)
                {
                    customerInfo = $"Kunde: {selectedCustomer.CompanyName}";
                }

                var result = await aiService.ScoreLeadAsync(title, description, value, customerInfo);

                if (result != null)
                {
                    // Ergebnis anzeigen
                    AiScoreResultText.Text = $"✅ KI-Scoring:\n\n" +
                        $"📊 Score: {result.Score}/100\n" +
                        $"🎯 Erfolgswahrscheinlichkeit: {result.SuccessProbability:F0}%\n" +
                        $"💡 Begründung: {result.Reasoning}\n\n" +
                        $"📋 Empfohlene Maßnahmen:\n{string.Join("\n", result.RecommendedActions.Select(a => $"  • {a}"))}\n\n" +
                        $"⚠️ Risikofaktoren:\n{string.Join("\n", result.RiskFactors.Select(r => $"  • {r}"))}";

                    // Wahrscheinlichkeit übernehmen (optional)
                    if (result.SuccessProbability > 0)
                    {
                        var applyResult = MessageBox.Show(
                            $"Die KI empfiehlt eine Erfolgswahrscheinlichkeit von {result.SuccessProbability:F0}%.\n\n" +
                            $"Möchten Sie diesen Wert übernehmen?",
                            "Wahrscheinlichkeit übernehmen?",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (applyResult == MessageBoxResult.Yes)
                        {
                            ProbabilityBox.Text = result.SuccessProbability.ToString("F0");
                        }
                    }
                }
                else
                {
                    AiScoreResultText.Text = "❌ Keine Antwort von der KI erhalten.";
                }
            }
            catch (Exception ex)
            {
                AiScoreResultText.Text = $"❌ Fehler beim KI-Scoring:\n{ex.Message}";
                MessageBox.Show($"Fehler beim KI-Scoring:\n\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                AiScoreButton.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }
    }
}
