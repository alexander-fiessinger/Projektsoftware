using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Projektsoftware.Views
{
    public partial class TimeEntryTemplatesDialog : Window
    {
        private readonly DatabaseService _db;
        private readonly List<Project> _projects;
        private List<TimeEntryTemplate> _templates = new();
        private TimeEntryTemplate? _editing;

        /// <summary>Set when the user clicks "Vorlage verwenden".</summary>
        public TimeEntryTemplate? SelectedTemplate { get; private set; }

        public TimeEntryTemplatesDialog(List<Project> projects)
        {
            InitializeComponent();
            _db = new DatabaseService();
            _projects = projects;
            Loaded += async (_, _) => await RefreshAsync();
        }

        private async System.Threading.Tasks.Task RefreshAsync()
        {
            _templates = await _db.GetAllTimeEntryTemplatesAsync();
            TemplatesGrid.ItemsSource = null;
            TemplatesGrid.ItemsSource = _templates;
        }

        private void NewTemplate_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            NameBox.Text = string.Empty;
            ActivityBox.Text = string.Empty;
            DurationBox.Text = "01:00";
            EditForm.Visibility = Visibility.Visible;
            NameBox.Focus();
        }

        private void EditTemplate_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not TimeEntryTemplate t) return;
            _editing = t;
            NameBox.Text = t.Name;
            ActivityBox.Text = t.Activity ?? string.Empty;
            DurationBox.Text = $"{(int)t.DefaultDuration.TotalHours:D2}:{t.DefaultDuration.Minutes:D2}";
            EditForm.Visibility = Visibility.Visible;
            NameBox.Focus();
        }

        private async void DeleteTemplate_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not TimeEntryTemplate t) return;
            if (MessageBox.Show($"Vorlage '{t.Name}' wirklich löschen?", "Löschen bestätigen",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            await _db.DeleteTimeEntryTemplateAsync(t.Id);
            await RefreshAsync();
        }

        private async void SaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Namen ein.", "Pflichtfeld",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParseExact(DurationBox.Text.Trim(), @"hh\:mm", null, out var duration)
                && !TimeSpan.TryParse(DurationBox.Text.Trim(), out duration))
            {
                MessageBox.Show("Bitte geben Sie die Dauer im Format HH:mm ein.", "Ungültige Eingabe",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_editing == null)
            {
                var t = new TimeEntryTemplate
                {
                    Name            = NameBox.Text.Trim(),
                    Activity        = string.IsNullOrWhiteSpace(ActivityBox.Text) ? null : ActivityBox.Text.Trim(),
                    DefaultDuration = duration,
                    CreatedAt       = DateTime.Now
                };
                await _db.AddTimeEntryTemplateAsync(t);
            }
            else
            {
                _editing.Name            = NameBox.Text.Trim();
                _editing.Activity        = string.IsNullOrWhiteSpace(ActivityBox.Text) ? null : ActivityBox.Text.Trim();
                _editing.DefaultDuration = duration;
                await _db.UpdateTimeEntryTemplateAsync(_editing);
            }

            EditForm.Visibility = Visibility.Collapsed;
            await RefreshAsync();
        }

        private void UseTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatesGrid.SelectedItem is not TimeEntryTemplate t)
            {
                MessageBox.Show("Bitte wählen Sie eine Vorlage aus.", "Keine Auswahl",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SelectedTemplate = t;
            DialogResult = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
