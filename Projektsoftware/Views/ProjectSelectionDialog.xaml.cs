using Projektsoftware.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class ProjectSelectionDialog : Window
    {
        public Project? SelectedProject { get; private set; }
        private bool shouldCloseImmediately = false;

        public ProjectSelectionDialog(List<Project> projects, bool isForOffer)
        {
            InitializeComponent();

            // Set title and description based on document type
            if (isForOffer)
            {
                TitleTextBlock.Text = "📋 Projekt für Angebot auswählen";
                DescriptionTextBlock.Text = "Wählen Sie ein Projekt aus, für das Sie ein Angebot erstellen möchten. " +
                    "Die erfassten Zeiten und Projektdaten werden automatisch übernommen.";
            }
            else
            {
                TitleTextBlock.Text = "📄 Projekt für Rechnung auswählen";
                DescriptionTextBlock.Text = "Wählen Sie ein Projekt aus, für das Sie eine Rechnung erstellen möchten. " +
                    "Die erfassten Zeiten werden automatisch in Rechnungspositionen umgewandelt.";
            }

            // Filter active projects only
            var activeProjects = projects.Where(p => p.Status != "Abgeschlossen").OrderByDescending(p => p.StartDate).ToList();

            if (activeProjects.Count == 0)
            {
                MessageBox.Show(
                    "Es sind keine aktiven Projekte vorhanden.\n\n" +
                    "Bitte erstellen Sie zuerst ein Projekt, bevor Sie Dokumente erstellen.",
                    "Keine Projekte",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                shouldCloseImmediately = true;
                Loaded += (s, e) =>
                {
                    DialogResult = false;
                    Close();
                };
                return;
            }

            ProjectsListBox.ItemsSource = activeProjects;

            // Auto-select first item
            if (activeProjects.Count > 0)
            {
                ProjectsListBox.SelectedIndex = 0;
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsListBox.SelectedItem is Project project)
            {
                SelectedProject = project;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(
                    "Bitte wählen Sie ein Projekt aus der Liste aus.",
                    "Kein Projekt ausgewählt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ProjectsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProjectsListBox.SelectedItem is Project project)
            {
                SelectedProject = project;
                DialogResult = true;
                Close();
            }
        }
    }
}
