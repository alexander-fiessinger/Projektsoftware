using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Projektsoftware.Views
{
    public partial class KanbanBoardView : UserControl
    {
        private List<ProjectTask> allTasks = new();
        private List<Project> allProjects = new();
        private bool _isLoaded;

        public event Action TaskUpdated;

        public KanbanBoardView()
        {
            InitializeComponent();
        }

        public async void LoadData()
        {
            try
            {
                var db = new DatabaseService();
                allTasks = await db.GetAllTasksAsync();
                allProjects = await db.GetAllProjectsAsync();

                // Populate project filter with strings
                _isLoaded = false;
                var items = new List<string> { "Alle Projekte" };
                items.AddRange(allProjects.Select(p => p.Name));
                ProjectFilterCombo.ItemsSource = items;
                ProjectFilterCombo.SelectedIndex = 0;
                _isLoaded = true;

                UpdateBoard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateBoard()
        {
            var tasks = allTasks;

            // Apply project filter
            if (ProjectFilterCombo.SelectedItem is string selectedName
                && selectedName != "Alle Projekte")
            {
                var project = allProjects.FirstOrDefault(p => p.Name == selectedName);
                if (project != null)
                    tasks = tasks.Where(t => t.ProjectId == project.Id).ToList();
            }

            var kanbanItems = tasks.Select(t => new KanbanTaskItem(t)).ToList();

            var open = kanbanItems.Where(t => t.Status == "Offen").ToList();
            var inProgress = kanbanItems.Where(t => t.Status == "In Arbeit").ToList();
            var blocked = kanbanItems.Where(t => t.Status == "Blockiert").ToList();
            var done = kanbanItems.Where(t => t.Status == "Erledigt").ToList();

            OpenTasksList.ItemsSource = open;
            InProgressTasksList.ItemsSource = inProgress;
            BlockedTasksList.ItemsSource = blocked;
            DoneTasksList.ItemsSource = done;

            OpenCount.Text = open.Count.ToString();
            InProgressCount.Text = inProgress.Count.ToString();
            BlockedCount.Text = blocked.Count.ToString();
            DoneCount.Text = done.Count.ToString();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void ProjectFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoaded)
                UpdateBoard();
        }

        private async void TaskCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.DataContext is KanbanTaskItem item)
                await OpenTaskDialogAsync(item.Id);
        }

        private async void EditTaskFromMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is KanbanTaskItem item)
                await OpenTaskDialogAsync(item.Id);
        }

        private async void MoveTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem mi) return;
            var newStatus = mi.Tag?.ToString();
            if (string.IsNullOrEmpty(newStatus)) return;

            if (mi.DataContext is not KanbanTaskItem item) return;
            if (item.Status == newStatus) return;

            var task = allTasks.FirstOrDefault(t => t.Id == item.Id);
            if (task == null) return;

            task.Status = newStatus;
            try
            {
                var db = new DatabaseService();
                await db.UpdateTaskAsync(task);
                TaskUpdated?.Invoke();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Statuswechsel: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task OpenTaskDialogAsync(int taskId)
        {
            var task = allTasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) return;

            var db = new DatabaseService();
            List<Employee> employees;
            try
            {
                employees = await db.GetAllEmployeesAsync();
            }
            catch
            {
                employees = new List<Employee>();
            }

            var dialog = new TaskDialog(allProjects, employees, task);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await db.UpdateTaskAsync(dialog.Task);
                    TaskUpdated?.Invoke();
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class KanbanTaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ProjectName { get; set; }
        public string AssignedTo { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public string DueDateDisplay => DueDate.HasValue ? $"Fällig: {DueDate.Value:dd.MM.yyyy}" : "";
        public string CompletedDateDisplay => CompletedDate.HasValue ? $"Erledigt: {CompletedDate.Value:dd.MM.yyyy}" : "";

        public SolidColorBrush PriorityColor => Priority switch
        {
            "Kritisch" => new SolidColorBrush(Color.FromRgb(198, 40, 40)),
            "Hoch" => new SolidColorBrush(Color.FromRgb(230, 81, 0)),
            "Normal" => new SolidColorBrush(Color.FromRgb(21, 101, 192)),
            "Niedrig" => new SolidColorBrush(Color.FromRgb(46, 125, 50)),
            _ => new SolidColorBrush(Color.FromRgb(117, 117, 117))
        };

        public KanbanTaskItem(ProjectTask task)
        {
            Id = task.Id;
            Title = task.Title;
            ProjectName = task.ProjectName;
            AssignedTo = task.AssignedTo;
            Status = task.Status;
            Priority = task.Priority;
            DueDate = task.DueDate;
            CompletedDate = task.CompletedDate;
        }
    }
}
