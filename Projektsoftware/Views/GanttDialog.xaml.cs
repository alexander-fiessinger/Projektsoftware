using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Projektsoftware.Models;
using Projektsoftware.Services;

namespace Projektsoftware.Views
{
    public partial class GanttDialog : Window
    {
        private readonly DatabaseService _db = new DatabaseService();
        private const double PixelsPerDay = 16.0;

        public GanttDialog()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadProjectsAsync();
        }

        public class GanttRow
        {
            public string TaskName { get; set; } = "";
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public double OffsetLeft { get; set; }
            public double BarWidth { get; set; }
            public Brush BarColor { get; set; } = Brushes.SteelBlue;
            public string ProgressText { get; set; } = "";
        }

        private async System.Threading.Tasks.Task LoadProjectsAsync()
        {
            try
            {
                var projects = await _db.GetAllProjectsAsync();
                ProjectCombo.ItemsSource = projects.OrderBy(p => p.Name).ToList();
                if (projects.Any()) ProjectCombo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Show_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectCombo.SelectedItem is not Project project) return;
            try
            {
                var tasks = await _db.GetGanttTasksByProjectAsync(project.Id);
                if (tasks.Count == 0)
                {
                    GanttItems.ItemsSource = new List<GanttRow>();
                    MessageBox.Show("Keine Gantt-Aufgaben für dieses Projekt vorhanden.",
                        "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var earliest = tasks.Min(t => t.StartDate).Date;
                var rows = tasks.OrderBy(t => t.StartDate).Select(t => new GanttRow
                {
                    TaskName = t.TaskName,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    OffsetLeft = (t.StartDate.Date - earliest).TotalDays * PixelsPerDay,
                    BarWidth = Math.Max(8, (t.EndDate.Date - t.StartDate.Date).TotalDays * PixelsPerDay),
                    BarColor = GetColor(t),
                    ProgressText = $"{t.ProgressPercentage}%"
                }).ToList();

                GanttItems.ItemsSource = rows;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static Brush GetColor(GanttTask t)
        {
            if (t.IsMilestone) return Brushes.Goldenrod;
            return t.StatusColor switch
            {
                "Green" => Brushes.SeaGreen,
                "Red" => Brushes.IndianRed,
                "Blue" => Brushes.SteelBlue,
                "Gray" => Brushes.Gray,
                _ => Brushes.SteelBlue
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
