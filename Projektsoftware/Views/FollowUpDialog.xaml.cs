using System;
using System.Linq;
using System.Windows;
using Projektsoftware.Models;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class FollowUpDialog : Window
    {
        private readonly DatabaseService _db = new DatabaseService();

        public FollowUpDialog()
        {
            InitializeComponent();
            DueDatePicker.SelectedDate = DateTime.Today.AddDays(1);
            Loaded += async (_, __) => await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var leads = await _db.GetSalesLeadsAsync();
                LeadCombo.ItemsSource = leads.OrderBy(l => l.Title).ToList();

                RemindersGrid.ItemsSource = FollowUpReminderService.Load()
                    .OrderBy(r => r.Completed)
                    .ThenBy(r => r.DueDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (LeadCombo.SelectedItem is not SalesLead lead)
            {
                MessageBox.Show("Bitte einen Lead auswählen.", "Hinweis",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!DueDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Bitte ein Fälligkeitsdatum wählen.", "Hinweis",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            FollowUpReminderService.Add(new FollowUpReminder
            {
                LeadId = lead.Id,
                LeadTitle = lead.Title ?? "Lead",
                ContactName = lead.ContactName ?? "",
                DueDate = DueDatePicker.SelectedDate.Value,
                Note = NoteBox.Text ?? ""
            });

            NoteBox.Clear();
            _ = LoadAsync();
        }

        private void SaveCompleted_Click(object sender, RoutedEventArgs e)
        {
            if (RemindersGrid.ItemsSource is not System.Collections.Generic.IEnumerable<FollowUpReminder> items) return;
            foreach (var r in items) FollowUpReminderService.Update(r);
            MessageBox.Show("Status gespeichert.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            _ = LoadAsync();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (RemindersGrid.SelectedItem is FollowUpReminder r)
            {
                FollowUpReminderService.Delete(r.Id);
                _ = LoadAsync();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
