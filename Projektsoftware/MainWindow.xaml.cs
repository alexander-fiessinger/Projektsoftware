using Projektsoftware.Models;
using Projektsoftware.ViewModels;
using Projektsoftware.Views;
using Projektsoftware.Services;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Projektsoftware
{
    public partial class MainWindow : Window
    {
        private MainViewModel? viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Set Window Icon
            SetWindowIcon();

            // **AUTHENTIFIZIERUNG: Login-Dialog beim Laden anzeigen (async)**
            Loaded += async (s, e) => await InitializeWithLoginAsync();
        }

        /// <summary>
        /// Initialisiert die Anwendung nach erfolgreicher Anmeldung
        /// </summary>
        private async System.Threading.Tasks.Task InitializeWithLoginAsync()
        {
            try
            {
                // Login-Dialog anzeigen
                if (!await ShowLoginDialogAsync())
                {
                    // Benutzer hat Anmeldung abgebrochen
                    Application.Current.Shutdown();
                    return;
                }

                // Nach erfolgreichem Login: ViewModel initialisieren
                viewModel = new MainViewModel();

                if (viewModel == null)
                {
                    throw new InvalidOperationException("ViewModel konnte nicht initialisiert werden.");
                }

                DataContext = viewModel;

                // Zeige Benutzernamen in der Titelleiste
                Title = $"Projektierungssoftware Professional - Angemeldet als: {AuthenticationService.CurrentUser.Username}";

                // Subscribe to PropertyChanged to update dashboard when stats change
                viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MainViewModel.DashboardStats) && 
                        viewModel?.DashboardStats != null && 
                        DashboardControl != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                DashboardControl.UpdateStats(viewModel.DashboardStats);
                            }
                            catch (Exception updateEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Fehler beim Dashboard-Update: {updateEx.Message}");
                            }
                        });
                    }
                };

                // Initial Dashboard Update
                if (viewModel?.DashboardStats != null && DashboardControl != null)
                {
                    DashboardControl.UpdateStats(viewModel.DashboardStats);
                }

                // Kalender nach DB-Initialisierung laden
                if (MeetingCalendarView != null)
                {
                    _ = MeetingCalendarView.LoadAsync();
                }

                // Auf Updates prüfen (im Hintergrund, nach kurzer Verzögerung)
                _ = CheckForUpdatesInBackgroundAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initialisierungsfehler: {ex.Message}\n{ex.StackTrace}");

                if (ex.Message.Contains("nicht konfiguriert"))
                {
                    var result = MessageBox.Show(
                        "Die Datenbankverbindung ist noch nicht konfiguriert.\n\n" +
                        "Möchten Sie die Verbindung jetzt einrichten?",
                        "Datenbankverbindung erforderlich",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        ConfigureDatabase_Click(null, null);
                    }
                    else
                    {
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    var result = MessageBox.Show(
                        $"Fehler beim Verbinden mit der Datenbank:\n\n{ex.Message}\n\n" +
                        "Möchten Sie die Verbindungsdiagnose starten?",
                        "Datenbankfehler",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error);

                    if (result == MessageBoxResult.Yes)
                    {
                        var diagnostic = new ConnectionDiagnosticDialog();
                        diagnostic.ShowDialog();
                    }

                    Application.Current.Shutdown();
                }
            }
        }

        /// <summary>
        /// Zeigt den Login-Dialog und initialisiert ggf. den ersten Admin-Benutzer
        /// </summary>
        private async System.Threading.Tasks.Task<bool> ShowLoginDialogAsync()
        {
            try
            {
                var databaseService = new DatabaseService();

                // **WICHTIG: Datenbank initialisieren (erstellt users Tabelle)**
                try
                {
                    System.Diagnostics.Debug.WriteLine("🔧 Rufe InitializeDatabaseAsync() auf...");
                    await databaseService.InitializeDatabaseAsync();
                    System.Diagnostics.Debug.WriteLine("✅ InitializeDatabaseAsync() abgeschlossen");
                }
                catch (Exception initEx)
                {
                    var errorMsg = $"Fehler beim Initialisieren der Datenbank:\n\n{initEx.Message}";
                    if (initEx.InnerException != null)
                    {
                        errorMsg += $"\n\nInnerException:\n{initEx.InnerException.Message}";
                    }
                    errorMsg += $"\n\nStackTrace:\n{initEx.StackTrace}";

                    MessageBox.Show(
                        errorMsg + "\n\nDie Anwendung wird beendet.",
                        "Datenbankfehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }

                // Prüfe, ob überhaupt Benutzer existieren
                System.Diagnostics.Debug.WriteLine("🔧 Rufe HasAdminUserAsync() auf...");
                var hasAdmin = await databaseService.HasAdminUserAsync();
                System.Diagnostics.Debug.WriteLine($"✅ HasAdminUserAsync() ergebnis: {hasAdmin}");

                if (!hasAdmin)
                {
                    // Keine Benutzer vorhanden - Erstelle Standard-Admin
                    var result = MessageBox.Show(
                        "Es wurde noch kein Benutzer angelegt.\n\n" +
                        "Es wird jetzt ein Standard-Administrator erstellt:\n\n" +
                        "Benutzername: admin\n" +
                        "Passwort: admin\n\n" +
                        "Bitte ändern Sie das Passwort nach dem ersten Login!",
                        "Erster Start",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Erstelle Admin-Benutzer
                    var adminUser = new User
                    {
                        Username = "admin",
                        PasswordHash = AuthenticationService.HashPassword("admin"),
                        Role = "Admin",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    await databaseService.AddUserAsync(adminUser);

                    MessageBox.Show(
                        "✅ Administrator-Konto wurde erstellt!\n\n" +
                        "Sie können sich jetzt anmelden mit:\n" +
                        "Benutzername: admin\n" +
                        "Passwort: admin",
                        "Erfolgreich",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                // Zeige Login-Dialog
                var loginDialog = new LoginDialog();
                return loginDialog.ShowDialog() == true;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Fehler beim Initialisieren des Anmeldesystems:\n\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMsg += $"\n\nInnerException:\n{ex.InnerException.Message}";
                }
                errorMsg += $"\n\nStackTrace:\n{ex.StackTrace}";

                MessageBox.Show(
                    errorMsg,
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        private void ConfigureDatabase_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DatabaseConfigDialog();
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show(
                    "Bitte starten Sie die Anwendung neu, damit die neuen Verbindungseinstellungen wirksam werden.",
                    "Neustart erforderlich",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
        }

        private void DiagnoseConnection_Click(object sender, RoutedEventArgs e)
        {
            var diagnostic = new ConnectionDiagnosticDialog();
            diagnostic.ShowDialog();
        }

        private async void ResetDatabase_Click(object sender, RoutedEventArgs e)
        {
            // SCHRITT 1: Passwort-Authentifizierung
            var passwordDialog = new PasswordDialog();
            if (passwordDialog.ShowDialog() != true || !passwordDialog.IsAuthenticated)
            {
                MessageBox.Show(
                    "❌ Authentifizierung fehlgeschlagen!\n\n" +
                    "Zugriff verweigert. Sie benötigen das Administrator-Passwort,\n" +
                    "um die Datenbank zurückzusetzen.",
                    "Zugriff verweigert",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // SCHRITT 2: Erste Sicherheitsabfrage
            var result = MessageBox.Show(
                "⚠️ WARNUNG: Datenbank zurücksetzen ⚠️\n\n" +
                "Diese Aktion wird ALLE Daten unwiderruflich löschen:\n" +
                "• Projekte\n" +
                "• Zeiteinträge\n" +
                "• Aufgaben\n" +
                "• Mitarbeiter\n" +
                "• Besprechungsprotokolle\n\n" +
                "Alle AUTO_INCREMENT-Werte werden auf 1 zurückgesetzt.\n\n" +
                "Möchten Sie wirklich ALLE Daten löschen?",
                "⚠️ Datenbank zurücksetzen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // SCHRITT 3: Zweite Sicherheitsabfrage (finale Bestätigung)
            var finalConfirm = MessageBox.Show(
                "🚨 LETZTE WARNUNG! 🚨\n\n" +
                "Sind Sie ABSOLUT SICHER, dass Sie alle Daten unwiderruflich löschen möchten?\n\n" +
                "Diese Aktion kann NICHT rückgängig gemacht werden!",
                "🚨 Finale Bestätigung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop,
                MessageBoxResult.No);

            if (finalConfirm != MessageBoxResult.Yes)
            {
                MessageBox.Show("Vorgang abgebrochen. Keine Daten wurden gelöscht.", "Abgebrochen", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var databaseService = new Services.DatabaseService();

                // Alle Daten löschen und AUTO_INCREMENT zurücksetzen
                await databaseService.ResetDatabaseAsync(deleteAllData: true);

                MessageBox.Show(
                    "✅ Datenbank erfolgreich zurückgesetzt!\n\n" +
                    "• Alle Daten wurden gelöscht\n" +
                    "• Alle AUTO_INCREMENT-Werte sind auf 1 gesetzt\n\n" +
                    "Die Anwendung wird jetzt neu geladen.",
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Lade die Ansicht neu
                if (viewModel != null)
                {
                    await viewModel.LoadAllDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Fehler beim Zurücksetzen der Datenbank:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ConfigureEasybill_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EasybillConfigDialog();
            dialog.ShowDialog();
        }

        private void ConfigureWebex_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WebexConfigDialog { Owner = this };
            dialog.ShowDialog();
        }

        private void ManageCustomers_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomersListDialog();
            dialog.ShowDialog();

            if (viewModel != null)
            {
                _ = viewModel.LoadAllDataAsync();
            }
        }

        private async void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;

            var dialog = new CustomerDialog();
            if (dialog.ShowDialog() == true && dialog.Customer != null)
            {
                await viewModel.LoadAllDataAsync();
            }
        }

        private async void EditCustomer_Click(object sender, RoutedEventArgs e)
        {
            Customer? customer = null;

            if (sender is Button button)
            {
                customer = button.DataContext as Customer;
            }
            else if (sender is MenuItem menuItem)
            {
                customer = CustomersDataGrid?.SelectedItem as Customer;
            }

            if (customer != null)
            {
                var dialog = new CustomerDialog(customer);
                if (dialog.ShowDialog() == true && dialog.Customer != null)
                {
                    await viewModel?.LoadAllDataAsync();
                }
            }
        }

        private async void SyncCustomer_Click(object sender, RoutedEventArgs e)
        {
            Customer? customer = null;

            if (sender is Button button)
            {
                customer = button.DataContext as Customer;
            }
            else if (sender is MenuItem menuItem)
            {
                customer = CustomersDataGrid?.SelectedItem as Customer;
            }

            if (customer != null)
            {
                try
                {
                    var easybillService = new EasybillService();
                    var dbService = new DatabaseService();

                    var easybillCustomer = await easybillService.SyncCustomerToEasybillAsync(customer);

                    if (easybillCustomer != null)
                    {
                        EasybillService.UpdateCustomerFromEasybill(customer, easybillCustomer);
                        await dbService.UpdateCustomerAsync(customer);
                        await viewModel?.LoadAllDataAsync();

                        var statusMsg = customer.IsSyncedToEasybill 
                            ? $"Kunde '{customer.DisplayName}' wurde erfolgreich mit Easybill synchronisiert!" 
                            : "Kunde wurde lokal aktualisiert.";
                        MessageBox.Show(statusMsg, "Synchronisation", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Synchronisieren: {ex.Message}", "Fehler", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ImportCustomersFromEasybill_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Möchten Sie alle Kunden von Easybill importieren?\n\n" +
                    "Bestehende Kunden werden aktualisiert, neue werden hinzugefügt.",
                    "Kunden importieren",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var easybillService = new EasybillService();
                    var dbService = new DatabaseService();

                    var easybillCustomers = await easybillService.GetAllCustomersAsync();
                    int addedCount = 0;
                    int updatedCount = 0;

                    foreach (var ebCustomer in easybillCustomers)
                    {
                        var existingCustomer = await dbService.GetCustomerByEasybillIdAsync(ebCustomer.Id);

                        if (existingCustomer != null)
                        {
                            EasybillService.UpdateCustomerFromEasybill(existingCustomer, ebCustomer);
                            await dbService.UpdateCustomerAsync(existingCustomer);
                            updatedCount++;
                        }
                        else
                        {
                            var newCustomer = new Customer
                            {
                                CompanyName = ebCustomer.CompanyName ?? "",
                                FirstName = ebCustomer.FirstName ?? "",
                                LastName = ebCustomer.LastName ?? "",
                                Email = ebCustomer.Emails?.FirstOrDefault() ?? "",
                                Phone = ebCustomer.Phone1 ?? "",
                                Street = ebCustomer.Street ?? "",
                                ZipCode = ebCustomer.Zipcode ?? "",
                                City = ebCustomer.City ?? "",
                                Country = ebCustomer.Country ?? "DE",
                                EasybillCustomerId = ebCustomer.Id,
                                LastSyncedAt = DateTime.Now
                            };
                            await dbService.AddCustomerAsync(newCustomer);
                            addedCount++;
                        }
                    }

                    await viewModel?.LoadAllDataAsync();
                    MessageBox.Show(
                        $"Import abgeschlossen!\n\n" +
                        $"Neu hinzugefügt: {addedCount}\n" +
                        $"Aktualisiert: {updatedCount}",
                        "Import erfolgreich",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Import: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CustomersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CustomersDataGrid?.SelectedItem is Customer customer)
            {
                var dialog = new CustomerDialog(customer);
                if (dialog.ShowDialog() == true && dialog.Customer != null)
                {
                    await viewModel?.LoadAllDataAsync();
                }
            }
        }

        private async void DeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            Customer? customer = null;

            if (sender is Button button)
            {
                customer = button.DataContext as Customer;
            }
            else if (sender is MenuItem menuItem)
            {
                customer = CustomersDataGrid?.SelectedItem as Customer;
            }

            if (customer != null)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie den Kunden '{customer.DisplayName}' wirklich löschen?",
                    "Kunde löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var dbService = new DatabaseService();
                        await dbService.DeleteCustomerAsync(customer);
                        await viewModel?.LoadAllDataAsync();
                        MessageBox.Show("Kunde wurde erfolgreich gelöscht.", "Erfolg", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Fehler", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ShowEasybillCustomers_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EasybillCustomersDialog();
            dialog.ShowDialog();
        }

        private void ExportTimesToEasybill_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EasybillTimeExportDialog();
            dialog.ShowDialog();
        }

        private void ShowEasybillDocuments_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EasybillDocumentsDialog();
            dialog.ShowDialog();
        }

        private void ShowEasybillProducts_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EasybillProductsDialog();
            dialog.ShowDialog();
        }

        private void ManageUsers_Click(object sender, RoutedEventArgs e)
        {
            // Nur Admins dürfen Benutzer verwalten
            if (!AuthenticationService.IsAdmin)
            {
                MessageBox.Show(
                    "Diese Funktion ist nur für Administratoren verfügbar!",
                    "Zugriff verweigert",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var dialog = new UserManagementDialog();
            dialog.ShowDialog();
        }

        private void ChangeOwnPassword_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ChangePasswordDialog(AuthenticationService.CurrentUser);
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show(
                    "Ihr Passwort wurde erfolgreich geändert!",
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Möchten Sie sich wirklich abmelden?",
                "Abmelden",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AuthenticationService.Logout();

                MessageBox.Show(
                    "Sie wurden abgemeldet.\n\nDie Anwendung wird beendet.",
                    "Abgemeldet",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Application.Current.Shutdown();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Projektierungssoftware v2.0 Professional\n\n" +
                "Eine umfassende Lösung für:\n" +
                "• 📁 Projektverwaltung mit Budgets und Status\n" +
                "• ⏱ Zeiterfassung und Stundentracking\n" +
                "• 📋 Besprechungsprotokolle\n" +
                "• ✓ Aufgabenverwaltung mit Prioritäten\n" +
                "• 👥 Mitarbeiterverwaltung\n" +
                "• 📊 Dashboard mit Live-Statistiken\n" +
                "• 🔍 Erweiterte Such- und Filterfunktionen\n" +
                "• 💾 Export zu CSV und Text\n" +
                "• 💳 Easybill API-Integration für Kundenverwaltung\n\n" +
                "Mit MySQL-Datenbankanbindung und Easybill-Synchronisation",
                "Über diese Software",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void AddProject_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;

            var dialog = new ProjectDialog();
            if (dialog.ShowDialog() == true)
            {
                await viewModel.AddProjectAsync(dialog.Project);
            }
        }

        private async void EditProject_Click(object sender, RoutedEventArgs e)
        {
            Project project = null;

            if (sender is Button button)
            {
                project = button.DataContext as Project;
            }
            else if (sender is MenuItem menuItem)
            {
                project = ProjectsDataGrid.SelectedItem as Project;
            }

            if (project != null)
            {
                var dialog = new ProjectDialog(project);
                if (dialog.ShowDialog() == true)
                {
                    await viewModel.UpdateProjectAsync(dialog.Project);
                }
            }
        }

        private async void DeleteProject_Click(object sender, RoutedEventArgs e)
        {
            Project project = null;

            if (sender is Button button)
            {
                project = button.DataContext as Project;
            }
            else if (sender is MenuItem menuItem)
            {
                project = ProjectsDataGrid.SelectedItem as Project;
            }

            if (project != null)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie das Projekt '{project.Name}' wirklich löschen?",
                    "Projekt löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await viewModel.DeleteProjectAsync(project);
                }
            }
        }

        private void CreateOfferFromProject_Click(object sender, RoutedEventArgs e)
        {
            Project project = null;

            if (sender is Button button)
            {
                project = button.DataContext as Project;
            }
            else if (sender is MenuItem menuItem)
            {
                project = ProjectsDataGrid.SelectedItem as Project;
            }

            if (project != null)
            {
                var dialog = new CreateInvoiceFromProjectDialog(project, isOffer: true);
                if (dialog.ShowDialog() == true && dialog.CreatedDocument != null)
                {
                    ShowOfferCreatedMessage(dialog.CreatedDocument, sender, e);
                }
            }
            else
            {
                // Kein Projekt ausgewählt - Angebot ohne Projekt erstellen
                var dialog = new CreateEasybillDocumentDialog("OFFER");
                if (dialog.ShowDialog() == true && dialog.CreatedDocument != null)
                {
                    ShowOfferCreatedMessage(dialog.CreatedDocument, sender, e);
                }
            }
        }

        private void ShowOfferCreatedMessage(EasybillDocument doc, object sender, RoutedEventArgs e)
        {
            var docNumber = !string.IsNullOrEmpty(doc.Number) ? doc.Number : "(Entwurf - wird beim Abschließen vergeben)";

            var result = MessageBox.Show(
                $"Angebot erfolgreich in Easybill erstellt!\n\n" +
                $"Dokumentnummer: {docNumber}\n" +
                $"Gesamtbetrag: {doc.TotalGross:N2} EUR\n" +
                $"Status: {doc.DisplayStatus}\n" +
                $"Datum: {doc.DocumentDate}\n\n" +
                $"Möchten Sie alle Easybill-Dokumente jetzt anzeigen?",
                "Angebot erstellt",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                ShowEasybillDocuments_Click(sender, e);
            }
        }

        private void CreateInvoiceFromProject_Click(object sender, RoutedEventArgs e)
        {
            Project project = null;

            if (sender is Button button)
            {
                project = button.DataContext as Project;
            }
            else if (sender is MenuItem menuItem)
            {
                project = ProjectsDataGrid.SelectedItem as Project;
            }

            if (project != null)
            {
                var dialog = new CreateInvoiceFromProjectDialog(project, isOffer: false);
                if (dialog.ShowDialog() == true && dialog.CreatedDocument != null)
                {
                    var doc = dialog.CreatedDocument;
                    var docNumber = !string.IsNullOrEmpty(doc.Number) ? doc.Number : "(Entwurf - wird beim Abschließen vergeben)";

                    var result = MessageBox.Show(
                        $"✅ Rechnung erfolgreich in Easybill erstellt!\n\n" +
                        $"📄 Rechnungsnummer: {docNumber}\n" +
                        $"💰 Gesamtbetrag: {doc.TotalGross:N2} €\n" +
                        $"📊 Status: {doc.DisplayStatus}\n" +
                        $"📅 Datum: {doc.DocumentDate}\n\n" +
                        $"Möchten Sie alle Easybill-Dokumente jetzt anzeigen?",
                        "Rechnung erstellt",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        ShowEasybillDocuments_Click(sender, e);
                    }
                }
            }
        }

        private async void ProjectsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProjectsDataGrid.SelectedItem is Project project)
            {
                var dialog = new ProjectDialog(project);
                if (dialog.ShowDialog() == true)
                {
                    await viewModel.UpdateProjectAsync(dialog.Project);
                }
            }
        }

        private async void AddTimeEntry_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;

            var dialog = new TimeEntryDialog(viewModel.Projects, viewModel.Employees);
            if (dialog.ShowDialog() == true)
            {
                await viewModel.AddTimeEntryAsync(dialog.TimeEntry);
            }
        }

        private async void DeleteTimeEntry_Click(object sender, RoutedEventArgs e)
        {
            TimeEntry entry = null;

            if (sender is Button button)
            {
                entry = button.DataContext as TimeEntry;
            }
            else if (sender is MenuItem menuItem)
            {
                entry = TimeEntriesDataGrid.SelectedItem as TimeEntry;
            }

            if (entry != null)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie die Zeiterfassung für '{entry.ProjectName}' wirklich löschen?",
                    "Zeiterfassung löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await viewModel.DeleteTimeEntryAsync(entry);
                }
            }
        }

        private async void AddMeetingProtocol_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;

            var dialog = new MeetingProtocolDialog(viewModel.Projects);
            if (dialog.ShowDialog() == true)
            {
                await viewModel.AddMeetingProtocolAsync(dialog.Protocol);
            }
        }

        private async void EditMeetingProtocol_Click(object sender, RoutedEventArgs e)
        {
            MeetingProtocol protocol = null;

            if (sender is Button button)
            {
                protocol = button.DataContext as MeetingProtocol;
            }
            else if (sender is MenuItem menuItem)
            {
                protocol = MeetingProtocolsDataGrid.SelectedItem as MeetingProtocol;
            }

            if (protocol != null)
            {
                var dialog = new MeetingProtocolDialog(viewModel.Projects, protocol);
                if (dialog.ShowDialog() == true)
                {
                    await viewModel.LoadAllDataAsync();
                }
            }
        }

        private async void DeleteMeetingProtocol_Click(object sender, RoutedEventArgs e)
        {
            MeetingProtocol protocol = null;

            if (sender is Button button)
            {
                protocol = button.DataContext as MeetingProtocol;
            }
            else if (sender is MenuItem menuItem)
            {
                protocol = MeetingProtocolsDataGrid.SelectedItem as MeetingProtocol;
            }

            if (protocol != null)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie das Protokoll '{protocol.Title}' wirklich löschen?",
                    "Protokoll löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await viewModel.DeleteMeetingProtocolAsync(protocol);
                }
            }
        }

        private void MeetingProtocolsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MeetingProtocolsDataGrid.SelectedItem is MeetingProtocol protocol)
            {
                var dialog = new MeetingProtocolDialog(viewModel.Projects, protocol);
                dialog.ShowDialog();
            }
        }

        // Dashboard Quick Actions
        private void DashboardNewProject_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 2; // Projekte Tab
            AddProject_Click(sender, e);
        }

        private void DashboardNewTask_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 5; // Aufgaben Tab
            AddTask_Click(sender, e);
        }

        private void DashboardNewCustomer_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 1; // Kunden Tab
            AddCustomer_Click(sender, e);
        }

        private void DashboardTimeTracking_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 3; // Zeiterfassung Tab
        }

        private void DashboardCreateOffer_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;

            // Frage ob mit oder ohne Projekt
            var choice = MessageBox.Show(
                "Möchten Sie das Angebot aus einem bestehenden Projekt erstellen?\n\n" +
                "Ja = Aus Projekt (Zeiteinträge werden übernommen)\n" +
                "Nein = Ohne Projekt (freie Positionen eingeben)",
                "Angebot erstellen",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (choice == MessageBoxResult.Cancel)
                return;

            if (choice == MessageBoxResult.Yes)
            {
                // Bestehender Flow: Projekt auswählen
                var projectSelectionDialog = new ProjectSelectionDialog(viewModel.Projects.ToList(), isForOffer: true);
                if (projectSelectionDialog.ShowDialog() == true && projectSelectionDialog.SelectedProject != null)
                {
                    var project = projectSelectionDialog.SelectedProject;
                    var dialog = new CreateInvoiceFromProjectDialog(project, isOffer: true);
                    if (dialog.ShowDialog() == true && dialog.CreatedDocument != null)
                    {
                        var doc = dialog.CreatedDocument;
                        var docNumber = !string.IsNullOrEmpty(doc.Number) ? doc.Number : "(Entwurf - wird beim Abschließen vergeben)";

                        var result = MessageBox.Show(
                            $"Angebot erfolgreich in Easybill erstellt!\n\n" +
                            $"Angebotsnummer: {docNumber}\n" +
                            $"Gesamtbetrag: {doc.TotalGross:N2} EUR\n" +
                            $"Status: {doc.DisplayStatus}\n" +
                            $"Datum: {doc.DocumentDate}\n\n" +
                            $"Möchten Sie alle Easybill-Dokumente jetzt anzeigen?",
                            "Angebot erstellt",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            ShowEasybillDocuments_Click(sender, e);
                        }
                    }
                }
            }
            else
            {
                // Neuer Flow: Ohne Projekt direkt Angebot erstellen
                var dialog = new CreateEasybillDocumentDialog("OFFER");
                if (dialog.ShowDialog() == true && dialog.CreatedDocument != null)
                {
                    var doc = dialog.CreatedDocument;
                    var docNumber = !string.IsNullOrEmpty(doc.Number) ? doc.Number : "(Entwurf - wird beim Abschließen vergeben)";

                    var result = MessageBox.Show(
                        $"Angebot erfolgreich in Easybill erstellt!\n\n" +
                        $"Angebotsnummer: {docNumber}\n" +
                        $"Gesamtbetrag: {doc.TotalGross:N2} EUR\n" +
                        $"Status: {doc.DisplayStatus}\n" +
                        $"Datum: {doc.DocumentDate}\n\n" +
                        $"Möchten Sie alle Easybill-Dokumente jetzt anzeigen?",
                        "Angebot erstellt",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        ShowEasybillDocuments_Click(sender, e);
                    }
                }
            }
        }

        private void DashboardCreateInvoice_Click(object sender, RoutedEventArgs e)
        {
            // Öffne Dialog zur Auswahl eines Projekts für Rechnung
            if (viewModel == null) return;

            // Zeige Projekt-Auswahl-Dialog
            var projectSelectionDialog = new ProjectSelectionDialog(viewModel.Projects.ToList(), isForOffer: false);
            if (projectSelectionDialog.ShowDialog() == true && projectSelectionDialog.SelectedProject != null)
            {
                var project = projectSelectionDialog.SelectedProject;
                var dialog = new CreateInvoiceFromProjectDialog(project, isOffer: false);
                if (dialog.ShowDialog() == true && dialog.CreatedDocument != null)
                {
                    var doc = dialog.CreatedDocument;
                    var docNumber = !string.IsNullOrEmpty(doc.Number) ? doc.Number : "(Entwurf - wird beim Abschließen vergeben)";

                    var result = MessageBox.Show(
                        $"✅ Rechnung erfolgreich in Easybill erstellt!\n\n" +
                        $"📄 Rechnungsnummer: {docNumber}\n" +
                        $"💰 Gesamtbetrag: {doc.TotalGross:N2} €\n" +
                        $"📊 Status: {doc.DisplayStatus}\n" +
                        $"📅 Datum: {doc.DocumentDate}\n\n" +
                        $"Möchten Sie alle Easybill-Dokumente jetzt anzeigen?",
                        "Rechnung erstellt",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        ShowEasybillDocuments_Click(sender, e);
                    }
                }
            }
        }

        // View Menu Navigation (Not needed anymore - removed from menu)
        private void ShowDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 0;
        }

        private void ShowProjects_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 1;
        }

        private void ShowTasks_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 4;
        }

        private void ShowEmployees_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 5;
        }

        // Task Management
        private async void AddTask_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;

            var dialog = new TaskDialog(viewModel.Projects.ToList(), viewModel.Employees.ToList());
            if (dialog.ShowDialog() == true)
            {
                await viewModel.AddTaskAsync(dialog.Task);
            }
        }

        private async void EditTask_Click(object sender, RoutedEventArgs e)
        {
            ProjectTask task = null;

            if (sender is Button button)
            {
                task = button.DataContext as ProjectTask;
            }
            else if (sender is MenuItem menuItem)
            {
                task = TasksDataGrid.SelectedItem as ProjectTask;
            }

            if (task != null)
            {
                var dialog = new TaskDialog(viewModel.Projects.ToList(), viewModel.Employees.ToList(), task);
                if (dialog.ShowDialog() == true)
                {
                    await viewModel.UpdateTaskAsync(dialog.Task);
                }
            }
        }

        private async void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            ProjectTask task = null;

            if (sender is Button button)
            {
                task = button.DataContext as ProjectTask;
            }
            else if (sender is MenuItem menuItem)
            {
                task = TasksDataGrid.SelectedItem as ProjectTask;
            }

            if (task != null)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie die Aufgabe '{task.Title}' wirklich löschen?",
                    "Aufgabe löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await viewModel.DeleteTaskAsync(task);
                }
            }
        }

        private async void TasksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TasksDataGrid.SelectedItem is ProjectTask task)
            {
                var dialog = new TaskDialog(viewModel.Projects.ToList(), viewModel.Employees.ToList(), task);
                if (dialog.ShowDialog() == true)
                {
                    await viewModel.UpdateTaskAsync(dialog.Task);
                }
            }
        }

        private void TaskSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (viewModel == null || TasksDataGrid == null) return;

            var searchText = TaskSearchBox.Text;
            if (string.IsNullOrWhiteSpace(searchText) || searchText == "Suchen...")
            {
                TasksDataGrid.ItemsSource = viewModel.Tasks;
            }
            else
            {
                var filtered = viewModel.Tasks.Where(t =>
                    t.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(t.Description) && t.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(t.AssignedTo) && t.AssignedTo.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                ).ToList();
                TasksDataGrid.ItemsSource = filtered;
            }
        }

        private void TaskSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TaskSearchBox.Text == "Suchen...")
            {
                TaskSearchBox.Text = "";
                TaskSearchBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void TaskSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TaskSearchBox.Text))
            {
                TaskSearchBox.Text = "Suchen...";
                TaskSearchBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        // Employee Management
        private async void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;

            var dialog = new EmployeeDialog();
            if (dialog.ShowDialog() == true)
            {
                await viewModel.AddEmployeeAsync(dialog.Employee);
            }
        }

        private async void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            Employee employee = null;

            if (sender is Button button)
            {
                employee = button.DataContext as Employee;
            }
            else if (sender is MenuItem menuItem)
            {
                employee = EmployeesDataGrid.SelectedItem as Employee;
            }

            if (employee != null)
            {
                var dialog = new EmployeeDialog(employee);
                if (dialog.ShowDialog() == true)
                {
                    await viewModel.UpdateEmployeeAsync(dialog.Employee);
                }
            }
        }

        private async void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            Employee employee = null;

            if (sender is Button button)
            {
                employee = button.DataContext as Employee;
            }
            else if (sender is MenuItem menuItem)
            {
                employee = EmployeesDataGrid.SelectedItem as Employee;
            }

            if (employee != null)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie den Mitarbeiter '{employee.FullName}' wirklich löschen?",
                    "Mitarbeiter löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await viewModel.DeleteEmployeeAsync(employee);
                }
            }
        }

        private async void EmployeesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EmployeesDataGrid.SelectedItem is Employee employee)
            {
                var dialog = new EmployeeDialog(employee);
                if (dialog.ShowDialog() == true)
                {
                    await viewModel.UpdateEmployeeAsync(dialog.Employee);
                }
            }
        }

        private void SetWindowIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app-icon.ico");

                if (File.Exists(iconPath))
                {
                    this.Icon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Icon konnte nicht geladen werden: {ex.Message}");
            }
        }

        #region Dashboard Event Handlers

        private async void DashboardCreateDeliveryNote_Click(object sender, RoutedEventArgs e)
        {
            await CreateDeliveryNoteAsync();
        }

        private async void DashboardCreateCreditNote_Click(object sender, RoutedEventArgs e)
        {
            await CreateCreditNoteAsync();
        }

        private async void DashboardCreateDunning_Click(object sender, RoutedEventArgs e)
        {
            await CreateDunningAsync();
        }

        /// <summary>
        /// Erstellt eine Gutschrift aus einer bestehenden Rechnung
        /// </summary>
        private async System.Threading.Tasks.Task CreateCreditNoteAsync()
        {
            try
            {
                var easybillService = new EasybillService();

                if (!easybillService.IsConfigured)
                {
                    MessageBox.Show(
                        "Easybill ist nicht konfiguriert!",
                        "Nicht konfiguriert",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Hole alle Rechnungen
                var invoices = await easybillService.GetAllDocumentsAsync("INVOICE");

                if (!invoices.Any())
                {
                    MessageBox.Show(
                        "Es wurden noch keine Rechnungen in Easybill gefunden!",
                        "Keine Rechnungen",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Zeige Auswahl-Dialog (vereinfacht - könnte durch einen richtigen Dialog ersetzt werden)
                var invoiceSelection = string.Join("\n", invoices.Take(10).Select((inv, idx) => 
                    $"{idx + 1}. {inv.Number} - {inv.CustomerSnapshot?.CompanyName} - {inv.TotalGross:F2} €"));

                var input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Wählen Sie eine Rechnung (1-{Math.Min(10, invoices.Count)}):\n\n{invoiceSelection}",
                    "Rechnung auswählen",
                    "1");

                if (string.IsNullOrWhiteSpace(input) || !int.TryParse(input, out int selection) || 
                    selection < 1 || selection > invoices.Count)
                {
                    return;
                }

                var selectedInvoice = invoices[selection - 1];

                // Grund für Gutschrift erfragen
                var reason = Microsoft.VisualBasic.Interaction.InputBox(
                    "Bitte geben Sie den Grund für die Gutschrift ein:",
                    "Grund für Gutschrift",
                    "Storno / Rechnungskorrektur");

                if (string.IsNullOrWhiteSpace(reason))
                {
                    return;
                }

                var creditNote = await easybillService.CreateCreditNoteFromInvoiceAsync(
                    selectedInvoice.Id.Value,
                    reason);

                MessageBox.Show(
                    $"Gutschrift wurde erfolgreich erstellt!\n\n" +
                    $"Gutschrift-Nr: {creditNote.Number}\n" +
                    $"Zu Rechnung: {selectedInvoice.Number}\n" +
                    $"Betrag: {creditNote.TotalGross:F2} €",
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Erstellen der Gutschrift:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Erstellt eine Mahnung für eine unbezahlte Rechnung
        /// </summary>
        private async System.Threading.Tasks.Task CreateDunningAsync()
        {
            try
            {
                var easybillService = new EasybillService();

                if (!easybillService.IsConfigured)
                {
                    MessageBox.Show(
                        "Easybill ist nicht konfiguriert!",
                        "Nicht konfiguriert",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Hole alle unbezahlten Rechnungen
                var invoices = await easybillService.GetAllDocumentsAsync("INVOICE");
                var unpaidInvoices = invoices.Where(i => 
                    string.IsNullOrEmpty(i.PaidAt) && 
                    i.Status != "CANCELLED" &&
                    !i.IsArchive).ToList();

                if (!unpaidInvoices.Any())
                {
                    MessageBox.Show(
                        "Es wurden keine unbezahlten Rechnungen gefunden!",
                        "Keine offenen Rechnungen",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Zeige Auswahl-Dialog
                var invoiceSelection = string.Join("\n", unpaidInvoices.Take(10).Select((inv, idx) => 
                    $"{idx + 1}. {inv.Number} - {inv.CustomerSnapshot?.CompanyName} - {inv.TotalGross:F2} € (Fällig: {inv.DueDate})"));

                var input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Wählen Sie eine Rechnung zum Mahnen (1-{Math.Min(10, unpaidInvoices.Count)}):\n\n{invoiceSelection}",
                    "Rechnung auswählen",
                    "1");

                if (string.IsNullOrWhiteSpace(input) || !int.TryParse(input, out int selection) || 
                    selection < 1 || selection > unpaidInvoices.Count)
                {
                    return;
                }

                var selectedInvoice = unpaidInvoices[selection - 1];

                // Mahnstufe erfragen
                var levelInput = Microsoft.VisualBasic.Interaction.InputBox(
                    "Welche Mahnstufe?\n\n1 = 1. Mahnung (7 Tage)\n2 = 2. Mahnung (5 Tage)\n3 = 3. Mahnung (3 Tage)",
                    "Mahnstufe",
                    "1");

                if (!int.TryParse(levelInput, out int dunningLevel) || dunningLevel < 1 || dunningLevel > 3)
                {
                    return;
                }

                var dunning = await easybillService.CreateDunningAsync(
                    selectedInvoice.Id.Value,
                    dunningLevel);

                MessageBox.Show(
                    $"{dunningLevel}. Mahnung wurde erfolgreich erstellt!\n\n" +
                    $"Mahnung-Nr: {dunning.Number}\n" +
                    $"Zu Rechnung: {selectedInvoice.Number}\n" +
                    $"Betrag: {dunning.TotalGross:F2} €\n" +
                    $"Zahlungsfrist: {dunning.DueInDays} Tage",
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Erstellen der Mahnung:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Erstellt einen Lieferschein aus einer bestehenden Rechnung/Bestellung
        /// </summary>
        private async System.Threading.Tasks.Task CreateDeliveryNoteAsync()
        {
            try
            {
                var easybillService = new EasybillService();

                if (!easybillService.IsConfigured)
                {
                    MessageBox.Show(
                        "Easybill ist nicht konfiguriert!",
                        "Nicht konfiguriert",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Hole alle Rechnungen (bezahlt oder unbezahlt)
                var invoices = await easybillService.GetAllDocumentsAsync("INVOICE");
                var activeInvoices = invoices.Where(i => 
                    i.Status != "CANCELLED" && 
                    !i.IsArchive)
                    .OrderByDescending(i => i.DocumentDate)
                    .ToList();

                if (!activeInvoices.Any())
                {
                    MessageBox.Show(
                        "Es wurden keine Rechnungen gefunden!\n\n" +
                        "Erstellen Sie zuerst eine Rechnung, aus der ein Lieferschein generiert werden kann.",
                        "Keine Rechnungen",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Zeige Auswahl-Dialog
                var invoiceSelection = string.Join("\n", activeInvoices.Take(10).Select((inv, idx) => 
                    $"{idx + 1}. {inv.Number} - {inv.CustomerSnapshot?.CompanyName ?? inv.CustomerSnapshot?.FirstName + " " + inv.CustomerSnapshot?.LastName} - {inv.TotalGross:F2} € ({inv.DocumentDate})"));

                var input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Wählen Sie eine Rechnung für den Lieferschein (1-{Math.Min(10, activeInvoices.Count)}):\n\n{invoiceSelection}",
                    "Rechnung auswählen",
                    "1");

                if (string.IsNullOrWhiteSpace(input) || !int.TryParse(input, out int selection) || 
                    selection < 1 || selection > activeInvoices.Count)
                {
                    return;
                }

                var selectedInvoice = activeInvoices[selection - 1];

                // Erstelle Lieferschein basierend auf der Rechnung
                var deliveryNote = new EasybillDocument
                {
                    Type = "DELIVERY_NOTE",
                    CustomerId = selectedInvoice.CustomerId,
                    ProjectId = selectedInvoice.ProjectId,
                    DocumentDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    Title = $"Lieferschein zu Rechnung {selectedInvoice.Number}",
                    Subject = selectedInvoice.Subject,
                    Text = $"Lieferung gemäß Rechnung {selectedInvoice.Number} vom {selectedInvoice.DocumentDate}",
                    TextSuffix = "Bitte prüfen Sie die Lieferung auf Vollständigkeit und Unversehrtheit.",
                    Items = selectedInvoice.Items,
                    BuyerReference = selectedInvoice.Number,
                    ServiceDate = selectedInvoice.ServiceDate
                };

                var createdDeliveryNote = await easybillService.CreateDeliveryNoteAsync(deliveryNote);

                MessageBox.Show(
                    $"✅ Lieferschein erfolgreich erstellt!\n\n" +
                    $"📦 Lieferschein-Nr: {createdDeliveryNote.Number ?? "(Entwurf)"}\n" +
                    $"📄 Zu Rechnung: {selectedInvoice.Number}\n" +
                    $"👤 Kunde: {selectedInvoice.CustomerSnapshot?.CompanyName ?? selectedInvoice.CustomerSnapshot?.FirstName + " " + selectedInvoice.CustomerSnapshot?.LastName}\n" +
                    $"📅 Datum: {createdDeliveryNote.DocumentDate}",
                    "Lieferschein erstellt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Erstellen des Lieferscheins:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handler für Dashboard-Button "Auftrag erstellen"
        /// </summary>
        private void DashboardCreateOrderConfirmation_Click(object sender, RoutedEventArgs e)
        {
            CreateOrderConfirmation_Click(sender, e);
        }

        /// <summary>
        /// Erstellt eine Auftragsbestätigung aus einem Angebot
        /// </summary>
        private void CreateOrderConfirmation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CreateOrderConfirmationDialog();
                if (dialog.ShowDialog() == true && dialog.CreatedDocument != null)
                {
                    var doc = dialog.CreatedDocument;
                    var docNumber = !string.IsNullOrEmpty(doc.Number) ? doc.Number : "(Entwurf - wird beim Abschließen vergeben)";
                    var selectedOffer = dialog.SelectedOffer;

                    var result = MessageBox.Show(
                        $"✅ Auftragsbestätigung erfolgreich in Easybill erstellt!\n\n" +
                        $"📋 Dokumentnummer: {docNumber}\n" +
                        $"👤 Kunde: {doc.CustomerSnapshot?.CompanyName ?? doc.CustomerSnapshot?.FirstName + " " + doc.CustomerSnapshot?.LastName}\n" +
                        $"💰 Gesamtbetrag: {doc.TotalGross:N2} €\n" +
                        $"📊 Status: {doc.DisplayStatus}\n" +
                        $"📅 Datum: {doc.DocumentDate}\n" +
                        $"🔗 Referenz: {selectedOffer?.Number}\n\n" +
                        $"Möchten Sie alle Easybill-Dokumente jetzt anzeigen?",
                        "Auftragsbestätigung erstellt",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        ShowEasybillDocuments_Click(sender, e);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Erstellen der Auftragsbestätigung:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Ticket Management

        /// <summary>
        /// Öffnet die Ticketverwaltung
        /// </summary>
        private void ManageTickets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new TicketManagementDialog();
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Öffnen der Ticketverwaltung:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Öffnet das Ticket-Dashboard
        /// </summary>
        private void ShowTicketDashboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dashboard = new TicketDashboard();
                dashboard.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Öffnen des Dashboards:\n\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Update

        private async System.Threading.Tasks.Task CheckForUpdatesInBackgroundAsync()
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(4000);

                var service = new UpdateService();
                var info = await service.CheckForUpdateAsync();
                if (info != null)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var dialog = new UpdateDialog(info) { Owner = this };
                        dialog.ShowDialog();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hintergrund-Update-Check fehlgeschlagen: {ex.Message}");
                await Dispatcher.InvokeAsync(() =>
                    MessageBox.Show($"Update-Check fehlgeschlagen:\n\n{ex.Message}",
                        "Update-Fehler", MessageBoxButton.OK, MessageBoxImage.Warning));
            }
        }

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var service = new UpdateService();
                var info = await service.CheckForUpdateAsync();
                Mouse.OverrideCursor = null;

                if (info != null)
                {
                    var dialog = new UpdateDialog(info) { Owner = this };
                    dialog.ShowDialog();
                }
                else
                {
                    MessageBox.Show(
                        $"Sie verwenden bereits die aktuelle Version ({UpdateService.CurrentVersion}).",
                        "Kein Update verfügbar",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show(
                    $"Fehler beim Prüfen auf Updates:\n\n{ex.Message}",
                    "Update-Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
