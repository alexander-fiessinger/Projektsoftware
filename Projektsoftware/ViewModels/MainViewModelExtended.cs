using Projektsoftware.Models;
using System;
using System.Windows;

namespace Projektsoftware.ViewModels
{
    /// <summary>
    /// Erweiterte ViewModel-Methoden für Employee, Task, Milestone
    /// </summary>
    public partial class MainViewModel
    {
        #region Employee Methods

        public async System.Threading.Tasks.Task AddEmployeeAsync(Employee employee)
        {
            try
            {
                employee.Id = await dbService.AddEmployeeAsync(employee);
                Employees.Add(employee);
                MessageBox.Show("Mitarbeiter erfolgreich hinzugefügt!", "Erfolg", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAllDataAsync(); // Refresh stats
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Hinzufügen: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async System.Threading.Tasks.Task UpdateEmployeeAsync(Employee employee)
        {
            try
            {
                await dbService.UpdateEmployeeAsync(employee);
                MessageBox.Show("Mitarbeiter erfolgreich aktualisiert!", "Erfolg", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Aktualisieren: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async System.Threading.Tasks.Task DeleteEmployeeAsync(Employee employee)
        {
            try
            {
                var result = MessageBox.Show($"Möchten Sie {employee.FullName} wirklich löschen?", 
                    "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await dbService.DeleteEmployeeAsync(employee.Id);
                    Employees.Remove(employee);
                    MessageBox.Show("Mitarbeiter erfolgreich gelöscht!", "Erfolg", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadAllDataAsync(); // Refresh stats
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region ProjectTask Methods

        public async System.Threading.Tasks.Task AddTaskAsync(ProjectTask task)
        {
            try
            {
                task.Id = await dbService.AddTaskAsync(task);
                Tasks.Insert(0, task);
                MessageBox.Show("Aufgabe erfolgreich hinzugefügt!", "Erfolg", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAllDataAsync(); // Refresh stats
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Hinzufügen: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async System.Threading.Tasks.Task UpdateTaskAsync(ProjectTask task)
        {
            try
            {
                await dbService.UpdateTaskAsync(task);
                MessageBox.Show("Aufgabe erfolgreich aktualisiert!", "Erfolg", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAllDataAsync(); // Refresh stats
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Aktualisieren: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async System.Threading.Tasks.Task DeleteTaskAsync(ProjectTask task)
        {
            try
            {
                var result = MessageBox.Show($"Möchten Sie die Aufgabe '{task.Title}' wirklich löschen?", 
                    "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await dbService.DeleteTaskAsync(task.Id);
                    Tasks.Remove(task);
                    MessageBox.Show("Aufgabe erfolgreich gelöscht!", "Erfolg", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadAllDataAsync(); // Refresh stats
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Milestone Methods

        public async System.Threading.Tasks.Task AddMilestoneAsync(Milestone milestone)
        {
            try
            {
                milestone.Id = await dbService.AddMilestoneAsync(milestone);
                Milestones.Insert(0, milestone);
                MessageBox.Show("Meilenstein erfolgreich hinzugefügt!", "Erfolg", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Hinzufügen: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async System.Threading.Tasks.Task UpdateMilestoneAsync(Milestone milestone)
        {
            try
            {
                await dbService.UpdateMilestoneAsync(milestone);
                MessageBox.Show("Meilenstein erfolgreich aktualisiert!", "Erfolg", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Aktualisieren: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async System.Threading.Tasks.Task DeleteMilestoneAsync(Milestone milestone)
        {
            try
            {
                var result = MessageBox.Show($"Möchten Sie den Meilenstein '{milestone.Title}' wirklich löschen?", 
                    "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await dbService.DeleteMilestoneAsync(milestone.Id);
                    Milestones.Remove(milestone);
                    MessageBox.Show("Meilenstein erfolgreich gelöscht!", "Erfolg", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
