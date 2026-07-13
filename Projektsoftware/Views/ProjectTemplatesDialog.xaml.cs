using System;
using System.Windows;
using System.Windows.Controls;
using Projektsoftware.Models;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class ProjectTemplatesDialog : Window
    {
        private readonly DatabaseService _db = new DatabaseService();

        public ProjectTemplatesDialog()
        {
            InitializeComponent();
            Load();
        }

        private void Load()
        {
            TemplatesList.ItemsSource = ProjectTemplateService.Load();
            if (TemplatesList.Items.Count > 0) TemplatesList.SelectedIndex = 0;
        }

        private void TemplatesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TemplatesList.SelectedItem is ProjectTemplate t)
                TasksGrid.ItemsSource = t.Tasks;
            else
                TasksGrid.ItemsSource = null;
        }

        private async void CreateProject_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatesList.SelectedItem is not ProjectTemplate tmpl)
            {
                MessageBox.Show("Bitte eine Vorlage auswählen.", "Hinweis",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(NewProjectName.Text))
            {
                MessageBox.Show("Bitte Projektnamen eingeben.", "Hinweis",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var project = new Project
                {
                    Name = NewProjectName.Text.Trim(),
                    Description = tmpl.Description,
                    ClientName = NewClientName.Text?.Trim() ?? "",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(tmpl.DefaultDurationDays),
                    Status = "Aktiv"
                };
                var projectId = await _db.AddProjectAsync(project);

                foreach (var tt in tmpl.Tasks)
                {
                    await _db.AddTaskAsync(new ProjectTask
                    {
                        ProjectId = projectId,
                        Title = tt.Title,
                        Description = tt.Description,
                        Priority = tt.Priority,
                        Status = "Offen",
                        DueDate = DateTime.Today.AddDays(tt.DueAfterDays),
                        ProjectName = project.Name
                    });
                }

                MessageBox.Show($"Projekt „{project.Name}\" mit {tmpl.Tasks.Count} Aufgaben angelegt.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                NewProjectName.Clear();
                NewClientName.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatesList.SelectedItem is ProjectTemplate t)
            {
                if (MessageBox.Show($"Vorlage „{t.Name}\" löschen?", "Bestätigen",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    ProjectTemplateService.Delete(t.Id);
                    Load();
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
