using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace Projektsoftware.ViewModels
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService dbService;
        private readonly DispatcherTimer syncTimer;
        private bool isRefreshing = false;

        public ObservableCollection<Project> Projects { get; set; }
        public ObservableCollection<TimeEntry> TimeEntries { get; set; }
        public ObservableCollection<MeetingProtocol> MeetingProtocols { get; set; }
        public ObservableCollection<Employee> Employees { get; set; }
        public ObservableCollection<ProjectTask> Tasks { get; set; }
        public ObservableCollection<Milestone> Milestones { get; set; }
        public ObservableCollection<Customer> Customers { get; set; }

        private DashboardStats dashboardStats = new DashboardStats();
        public DashboardStats DashboardStats
        {
            get => dashboardStats;
            set
            {
                dashboardStats = value;
                OnPropertyChanged();
            }
        }

        private int selectedTabIndex;
        public int SelectedTabIndex
        {
            get => selectedTabIndex;
            set
            {
                selectedTabIndex = value;
                OnPropertyChanged();
            }
        }

        private DateTime lastSyncTime;
        public DateTime LastSyncTime
        {
            get => lastSyncTime;
            set
            {
                lastSyncTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastSyncTimeText));
            }
        }

        public string LastSyncTimeText => LastSyncTime == DateTime.MinValue 
            ? "Noch nicht synchronisiert" 
            : $"Letzte Synchronisation: {LastSyncTime:HH:mm:ss}";

        public MainViewModel()
        {
            dbService = new DatabaseService();
            Projects = new ObservableCollection<Project>();
            TimeEntries = new ObservableCollection<TimeEntry>();
            MeetingProtocols = new ObservableCollection<MeetingProtocol>();
            Employees = new ObservableCollection<Employee>();
            Tasks = new ObservableCollection<ProjectTask>();
            Milestones = new ObservableCollection<Milestone>();
            Customers = new ObservableCollection<Customer>();

            // Auto-Sync Timer (alle 60 Sekunden)
            syncTimer = new DispatcherTimer();
            syncTimer.Interval = TimeSpan.FromSeconds(60);
            syncTimer.Tick += async (s, e) => await RefreshDataAsync();
            syncTimer.Start();

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                await dbService.InitializeDatabaseAsync();
                await LoadAllDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Datenbankfehler: {ex.Message}\n\nBitte überprüfen Sie Ihre MySQL-Verbindung.", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async System.Threading.Tasks.Task LoadAllDataAsync()
        {
            try
            {
                var projects = await dbService.GetAllProjectsAsync();
                var timeEntries = await dbService.GetAllTimeEntriesAsync();
                var protocols = await dbService.GetAllMeetingProtocolsAsync();
                var employees = await dbService.GetAllEmployeesAsync();
                var tasks = await dbService.GetAllTasksAsync();
                var milestones = await dbService.GetAllMilestonesAsync();
                var customers = await dbService.GetAllCustomersAsync();
                var stats = await dbService.GetDashboardStatsAsync();

                Projects.Clear();
                foreach (var project in projects)
                    Projects.Add(project);

                TimeEntries.Clear();
                foreach (var entry in timeEntries)
                    TimeEntries.Add(entry);

                MeetingProtocols.Clear();
                foreach (var protocol in protocols)
                    MeetingProtocols.Add(protocol);

                Employees.Clear();
                foreach (var employee in employees)
                    Employees.Add(employee);

                Tasks.Clear();
                foreach (var task in tasks)
                    Tasks.Add(task);

                Milestones.Clear();
                foreach (var milestone in milestones)
                    Milestones.Add(milestone);

                Customers.Clear();
                foreach (var customer in customers)
                    Customers.Add(customer);

                // Finanzdaten werden separat (async) geladen – beim Refresh nicht überschreiben
                var prevStats = dashboardStats;
                if (prevStats?.IsFinancialDataLoaded == true)
                {
                    stats.IsFinancialDataLoaded = true;
                    stats.EasybillConfigured = prevStats.EasybillConfigured;
                    stats.TotalRevenuePaid = prevStats.TotalRevenuePaid;
                    stats.ThisMonthRevenue = prevStats.ThisMonthRevenue;
                    stats.OpenInvoicesCount = prevStats.OpenInvoicesCount;
                    stats.OpenInvoicesAmount = prevStats.OpenInvoicesAmount;
                    stats.OverdueInvoicesCount = prevStats.OverdueInvoicesCount;
                    stats.OverdueInvoicesAmount = prevStats.OverdueInvoicesAmount;
                    stats.DraftInvoicesCount = prevStats.DraftInvoicesCount;
                    stats.OpenPurchaseOrdersCount = prevStats.OpenPurchaseOrdersCount;
                    stats.TotalPurchaseDocumentsCount = prevStats.TotalPurchaseDocumentsCount;
                    stats.SyncedPurchaseDocumentsCount = prevStats.SyncedPurchaseDocumentsCount;
                    stats.TopBudgetProjects = prevStats.TopBudgetProjects;
                }
                DashboardStats = stats;
                LastSyncTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Daten: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task RefreshDataAsync()
        {
            if (isRefreshing) return;

            try
            {
                isRefreshing = true;
                await LoadAllDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-Sync Fehler: {ex.Message}");
            }
            finally
            {
                isRefreshing = false;
            }
        }

        public async System.Threading.Tasks.Task AddProjectAsync(Project project)
        {
            try
            {
                project.Id = await dbService.AddProjectAsync(project);
                Projects.Insert(0, project);
                MessageBox.Show("Projekt erfolgreich gespeichert!", "Erfolg", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAllDataAsync(); // Echtzeit-DB-Sync
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async System.Threading.Tasks.Task UpdateProjectAsync(Project project)
        {
            try
            {
                await dbService.UpdateProjectAsync(project);
                MessageBox.Show("Projekt erfolgreich aktualisiert!", "Erfolg", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAllDataAsync(); // Echtzeit-DB-Sync
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Aktualisieren: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async System.Threading.Tasks.Task DeleteProjectAsync(Project project)
        {
            try
            {
                var result = MessageBox.Show($"Möchten Sie das Projekt '{project.Name}' wirklich löschen?", 
                    "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Lösche verknüpftes Easybill-Projekt und zugehörige Dokumente, falls vorhanden
                    if (project.EasybillProjectId.HasValue)
                    {
                        try
                        {
                            var easybillService = new EasybillService();

                            var deleteEasybillResult = MessageBox.Show(
                                $"Das Projekt ist mit einem Easybill-Projekt verknüpft (ID: {project.EasybillProjectId.Value}).\n\n" +
                                $"Möchten Sie das Projekt auch in Easybill löschen?",
                                "Easybill-Projekt löschen",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (deleteEasybillResult == MessageBoxResult.Yes)
                            {
                                // Hole alle Dokumente des Projekts
                                var allDocuments = await easybillService.GetAllDocumentsAsync();
                                var projectDocuments = allDocuments.FindAll(d => d.ProjectId == project.EasybillProjectId.Value);

                                if (projectDocuments.Count > 0)
                                {
                                    var deleteDocsResult = MessageBox.Show(
                                        $"Es wurden {projectDocuments.Count} Easybill-Dokument(e) zu diesem Projekt gefunden.\n\n" +
                                        $"Möchten Sie diese auch in Easybill löschen?",
                                        "Easybill-Dokumente löschen",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question);

                                    if (deleteDocsResult == MessageBoxResult.Yes)
                                    {
                                        foreach (var doc in projectDocuments)
                                        {
                                            if (doc.Id.HasValue)
                                            {
                                                await easybillService.DeleteDocumentAsync(doc.Id.Value);
                                            }
                                        }
                                    }
                                }

                                await easybillService.DeleteProjectAsync(project.EasybillProjectId.Value);
                            }
                        }
                        catch (Exception easybillEx)
                        {
                            MessageBox.Show(
                                $"Hinweis: Easybill-Projekt konnte nicht gelöscht werden:\n\n{easybillEx.Message}\n\n" +
                                "Das Projekt wird trotzdem lokal gelöscht.",
                                "Warnung",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }

                    await dbService.DeleteProjectAsync(project.Id);
                    Projects.Remove(project);
                    MessageBox.Show("Projekt erfolgreich gelöscht!", "Erfolg", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadAllDataAsync(); // Echtzeit-DB-Sync
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async System.Threading.Tasks.Task AddTimeEntryAsync(TimeEntry entry)
        {
            try
            {
                entry.Id = await dbService.AddTimeEntryAsync(entry);
                TimeEntries.Insert(0, entry);
                MessageBox.Show("Zeiteintrag erfolgreich gespeichert!", "Erfolg", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAllDataAsync(); // Echtzeit-DB-Sync
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async System.Threading.Tasks.Task DeleteTimeEntryAsync(TimeEntry entry)
        {
            try
            {
                var result = MessageBox.Show("Möchten Sie diesen Zeiteintrag wirklich löschen?", 
                    "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await dbService.DeleteTimeEntryAsync(entry.Id);
                    TimeEntries.Remove(entry);
                    MessageBox.Show("Zeiteintrag erfolgreich gelöscht!", "Erfolg", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadAllDataAsync(); // Echtzeit-DB-Sync
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async System.Threading.Tasks.Task AddMeetingProtocolAsync(MeetingProtocol protocol)
        {
            try
            {
                protocol.Id = await dbService.AddMeetingProtocolAsync(protocol);
                MeetingProtocols.Insert(0, protocol);
                MessageBox.Show("Besprechungsprotokoll erfolgreich gespeichert!", "Erfolg", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAllDataAsync(); // Echtzeit-DB-Sync
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async System.Threading.Tasks.Task DeleteMeetingProtocolAsync(MeetingProtocol protocol)
        {
            try
            {
                var result = MessageBox.Show($"Möchten Sie das Protokoll '{protocol.Title}' wirklich löschen?", 
                    "Bestätigung", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await dbService.DeleteMeetingProtocolAsync(protocol.Id);
                    MeetingProtocols.Remove(protocol);
                    MessageBox.Show("Protokoll erfolgreich gelöscht!", "Erfolg", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadAllDataAsync(); // Echtzeit-DB-Sync
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void StopSync()
        {
            syncTimer?.Stop();
        }

        public void StartSync()
        {
            syncTimer?.Start();
        }

        public void SetSyncInterval(int seconds)
        {
            if (seconds < 1) seconds = 1;
            syncTimer.Stop();
            syncTimer.Interval = TimeSpan.FromSeconds(seconds);
            syncTimer.Start();
        }
    }
}
