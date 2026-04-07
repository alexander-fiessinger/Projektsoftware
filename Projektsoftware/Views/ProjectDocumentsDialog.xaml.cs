using Microsoft.Win32;
using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Projektsoftware.Views
{
    public partial class ProjectDocumentsDialog : Window
    {
        private readonly Project project;
        private readonly DatabaseService databaseService;
        private readonly ObservableCollection<ProjectDocument> documents;
        private readonly string documentsBasePath;

        public ProjectDocumentsDialog(Project project)
        {
            InitializeComponent();

            this.project = project;
            databaseService = new DatabaseService();
            documents = new ObservableCollection<ProjectDocument>();
            DocumentsDataGrid.ItemsSource = documents;

            documentsBasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Projektsoftware", "Documents", "Projects", project.Id.ToString());

            ProjectNameTextBlock.Text = $"Projekt: {project.Name}";

            Loaded += async (s, e) => await LoadDocumentsAsync();
        }

        private async System.Threading.Tasks.Task LoadDocumentsAsync()
        {
            try
            {
                StatusTextBlock.Text = "⏳ Lade Dokumente...";
                documents.Clear();

                var loadedDocs = await databaseService.GetProjectDocumentsAsync(project.Id);
                foreach (var doc in loadedDocs)
                {
                    documents.Add(doc);
                }

                CountTextBlock.Text = $"📊 {documents.Count} Dokument(e)";
                StatusTextBlock.Text = "✅ Bereit";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "❌ Fehler beim Laden";
                MessageBox.Show(
                    $"Fehler beim Laden der Dokumente:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void AddDocument_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Title = "Dokument(e) auswählen",
                Filter = "Alle Dateien (*.*)|*.*|PDF-Dateien (*.pdf)|*.pdf|Word-Dokumente (*.docx;*.doc)|*.docx;*.doc|Excel-Tabellen (*.xlsx;*.xls)|*.xlsx;*.xls|Bilder (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Multiselect = true
            };

            if (openDialog.ShowDialog() != true)
                return;

            try
            {
                Directory.CreateDirectory(documentsBasePath);

                foreach (var sourceFile in openDialog.FileNames)
                {
                    var fileName = Path.GetFileName(sourceFile);
                    var destFile = GetUniqueFilePath(Path.Combine(documentsBasePath, fileName));

                    File.Copy(sourceFile, destFile);

                    var fileInfo = new FileInfo(destFile);
                    var doc = new ProjectDocument
                    {
                        ProjectId = project.Id,
                        FileName = Path.GetFileName(destFile),
                        FilePath = destFile,
                        FileType = Path.GetExtension(destFile).TrimStart('.').ToUpper(),
                        FileSize = fileInfo.Length,
                        Description = "",
                        UploadedBy = AuthenticationService.CurrentUser?.Username ?? "",
                        UploadedAt = DateTime.Now
                    };

                    await databaseService.AddProjectDocumentAsync(doc);
                }

                await LoadDocumentsAsync();
                StatusTextBlock.Text = $"✅ {openDialog.FileNames.Length} Dokument(e) hinzugefügt";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Hinzufügen des Dokuments:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenDocument_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var doc = button?.DataContext as ProjectDocument;
            OpenDocument(doc);
        }

        private void ShowInExplorer_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var doc = button?.DataContext as ProjectDocument;

            if (doc == null) return;

            if (!File.Exists(doc.FilePath))
            {
                MessageBox.Show(
                    "Die Datei wurde nicht gefunden.",
                    "Datei nicht gefunden",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{doc.FilePath}\"");
        }

        private async void DeleteDocument_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var doc = button?.DataContext as ProjectDocument;

            if (doc == null) return;

            var result = MessageBox.Show(
                $"Möchten Sie das Dokument '{doc.FileName}' wirklich löschen?\n\nDie Datei wird ebenfalls vom Datenträger entfernt.",
                "Bestätigung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await databaseService.DeleteProjectDocumentAsync(doc.Id);

                if (File.Exists(doc.FilePath))
                {
                    File.Delete(doc.FilePath);
                }

                documents.Remove(doc);
                CountTextBlock.Text = $"📊 {documents.Count} Dokument(e)";
                StatusTextBlock.Text = "✅ Dokument gelöscht";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Löschen:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDocumentsAsync();
        }

        private void DocumentsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DocumentsDataGrid.SelectedItem is ProjectDocument doc)
            {
                OpenDocument(doc);
            }
        }

        private void OpenDocument(ProjectDocument doc)
        {
            if (doc == null) return;

            if (!File.Exists(doc.FilePath))
            {
                MessageBox.Show(
                    "Die Datei wurde nicht gefunden.",
                    "Datei nicht gefunden",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = doc.FilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Öffnen der Datei:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string GetUniqueFilePath(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;

            var dir = Path.GetDirectoryName(filePath);
            var name = Path.GetFileNameWithoutExtension(filePath);
            var ext = Path.GetExtension(filePath);
            int counter = 1;
            string newPath;

            do
            {
                newPath = Path.Combine(dir, $"{name}_{counter}{ext}");
                counter++;
            } while (File.Exists(newPath));

            return newPath;
        }
    }
}
