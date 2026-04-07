using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class TicketEditDialog : Window
    {
        private Ticket ticket;
        private DatabaseService db;

        // Zustand vor dem Speichern merken
        private TicketStatus _previousStatus;
        private string _previousResolution = string.Empty;

        public TicketEditDialog(Ticket ticket)
        {
            InitializeComponent();
            this.ticket = ticket;
            db = new DatabaseService();

            // Aktuellen Zustand als Vergleichsbasis speichern
            _previousStatus = ticket.Status;
            _previousResolution = ticket.Resolution ?? string.Empty;

            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                TicketNumberText.Text = $"Ticket {ticket.TicketNumber}";
                CreatedAtText.Text = $"Erstellt am: {ticket.CreatedAt:dd.MM.yyyy HH:mm}";

                CustomerNameTextBox.Text = ticket.CustomerName;
                CustomerEmailTextBox.Text = ticket.CustomerEmail;
                CustomerPhoneTextBox.Text = ticket.CustomerPhone;

                SubjectTextBox.Text = ticket.Subject;
                DescriptionTextBox.Text = ticket.Description;
                CategoryTextBox.Text = ticket.CategoryText;

                PriorityComboBox.SelectedIndex = (int)ticket.Priority;
                StatusComboBox.SelectedIndex = (int)ticket.Status;
                ResolutionTextBox.Text = ticket.Resolution;

                var employees = await db.GetAllEmployeesAsync();
                employees.Insert(0, new Employee { Id = 0, FirstName = "Nicht", LastName = "zugewiesen" });
                AssignedToComboBox.ItemsSource = employees;

                if (ticket.AssignedToEmployeeId.HasValue)
                    AssignedToComboBox.SelectedValue = ticket.AssignedToEmployeeId.Value;
                else
                    AssignedToComboBox.SelectedIndex = 0;

                var projects = await db.GetAllProjectsAsync();
                projects.Insert(0, new Project { Id = 0, Name = "— kein Projekt —" });
                ProjectComboBox.ItemsSource = projects;

                if (ticket.ProjectId.HasValue)
                    ProjectComboBox.SelectedValue = ticket.ProjectId.Value;
                else
                    ProjectComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Laden der Daten:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ticket.Priority = (TicketPriority)PriorityComboBox.SelectedIndex;
                ticket.Status = (TicketStatus)StatusComboBox.SelectedIndex;
                ticket.Resolution = ResolutionTextBox.Text.Trim();

                var selectedEmployeeId = (int)AssignedToComboBox.SelectedValue;
                ticket.AssignedToEmployeeId = selectedEmployeeId == 0 ? null : selectedEmployeeId;

                var selectedProjectId = ProjectComboBox.SelectedValue is int pid ? pid : 0;
                ticket.ProjectId = selectedProjectId > 0 ? selectedProjectId : (int?)null;

                if (ticket.Status == TicketStatus.Resolved && !ticket.ResolvedAt.HasValue)
                {
                    ticket.ResolvedAt = DateTime.Now;
                }
                else if (ticket.Status != TicketStatus.Resolved)
                {
                    ticket.ResolvedAt = null;
                }

                ticket.UpdatedAt = DateTime.Now;

                await db.UpdateTicketAsync(ticket);

                // Kundenbenachrichtigung bei Status- oder Lösungsänderung (im Hintergrund)
                _ = TicketNotificationService.SendUpdateNotificationAsync(
                    ticket, _previousStatus, _previousResolution);

                MessageBox.Show(
                    "Ticket wurde erfolgreich aktualisiert.",
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Speichern:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
