using MySql.Data.MySqlClient;
using Projektsoftware.Services;
using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Projektsoftware.Views
{
    public partial class ConnectionDiagnosticDialog : Window
    {
        private DatabaseConfig config;
        private string server;
        private string port;
        private string database;
        private string user;
        private string password;
        private string sslMode;

        public ConnectionDiagnosticDialog()
        {
            InitializeComponent();
            LoadConfiguration();
            RunDiagnostics();
        }

        private void LoadConfiguration()
        {
            config = DatabaseConfig.Load();

            server = config.Server;
            port = config.Port;
            database = config.Database;
            user = config.User;
            password = config.Password;
            sslMode = config.SslMode;

            var configText = new StringBuilder();
            configText.AppendLine($"Server:     {server}");
            configText.AppendLine($"Port:       {port}");
            configText.AppendLine($"Datenbank:  {database}");
            configText.AppendLine($"Benutzer:   {user}");
            configText.AppendLine($"Passwort:   {(string.IsNullOrEmpty(password) ? "❌ Nicht gesetzt" : password.Length > 4 ? "✅ " + new string('*', password.Length - 4) + password.Substring(password.Length - 4) : "✅ Gesetzt")}");
            configText.AppendLine($"SSL-Modus:  {sslMode}");
            configText.AppendLine();
            configText.AppendLine($"📁 Konfigurationsdatei:");
            configText.AppendLine(DatabaseConfig.GetConfigFilePath());

            ConfigInfoTextBlock.Text = configText.ToString();
        }

        private void RunDiagnostics()
        {
            ChecklistPanel.Children.Clear();
            var problems = new StringBuilder();
            var solutions = new StringBuilder();
            bool hasErrors = false;

            // Test 1: Konfiguration vorhanden
            AddChecklistItem("Konfiguration vorhanden", 
                !string.IsNullOrEmpty(password) && !password.Contains("PASSWORT") && !password.Contains("...."));

            // Test 2: Passwort gültig
            bool passwordValid = !string.IsNullOrEmpty(password) && 
                                !password.Contains("PASSWORT") && 
                                !password.Contains("....") &&
                                password.Length > 5;
            AddChecklistItem("Passwort gültig", passwordValid);
            if (!passwordValid)
            {
                problems.AppendLine("❌ Passwort ist ungültig oder nicht gesetzt");
                solutions.AppendLine("1️⃣ Öffnen Sie die Einstellungen und geben Sie Ihr vollständiges Dogado-Passwort ein.\n");
                hasErrors = true;
            }

            // Test 3: Netzwerkverbindung
            bool isLocalhost = server == "127.0.0.1" || server == "localhost";
            bool networkOk = false;
            
            if (isLocalhost)
            {
                networkOk = true;
                AddChecklistItem("Localhost-Verbindung", true);
                
                // Test SSH-Tunnel
                if (port == "3307")
                {
                    bool tunnelActive = TestPort("127.0.0.1", 3307);
                    AddChecklistItem("SSH-Tunnel aktiv (Port 3307)", tunnelActive);
                    if (!tunnelActive)
                    {
                        problems.AppendLine("❌ SSH-Tunnel ist nicht aktiv auf Port 3307");
                        solutions.AppendLine("2️⃣ SSH-Tunnel starten:");
                        solutions.AppendLine("   Mit PuTTY: Connection → SSH → Tunnels");
                        solutions.AppendLine("   - Source Port: 3307, Destination: 127.0.0.1:3306");
                        solutions.AppendLine("   Mit OpenSSH (Terminal/CMD):");
                        solutions.AppendLine("   ssh -L 3307:127.0.0.1:3306 benutzer@ihr-server.de\n");
                        hasErrors = true;
                    }
                }
            }
            else
            {
                // Test externe Verbindung
                try
                {
                    using var ping = new Ping();
                    var reply = ping.Send(server, 2000);
                    networkOk = reply.Status == IPStatus.Success;
                    AddChecklistItem($"Server erreichbar ({server})", networkOk);
                    
                    if (!networkOk)
                    {
                        problems.AppendLine($"❌ Server {server} ist nicht erreichbar");
                        solutions.AppendLine("3️⃣ Überprüfen Sie:");
                        solutions.AppendLine("   - Ist die Server-Adresse korrekt?");
                        solutions.AppendLine("   - Internetverbindung aktiv?");
                        solutions.AppendLine("   - Firewall blockiert die Verbindung?\n");
                        hasErrors = true;
                    }
                }
                catch
                {
                    AddChecklistItem($"Server erreichbar ({server})", false);
                    problems.AppendLine($"❌ Kann Server {server} nicht erreichen");
                    hasErrors = true;
                }
            }

            // Test 4: MySQL-Verbindung
            if (passwordValid && (networkOk || isLocalhost))
            {
                var connectionString = $"Server={server};Port={port};Database={database};Uid={user};Pwd={password};SslMode={sslMode};CharSet=utf8mb4;AllowPublicKeyRetrieval=True;ConnectionTimeout=5;";
                
                try
                {
                    using var connection = new MySqlConnection(connectionString);
                    connection.Open();
                    AddChecklistItem("MySQL-Verbindung erfolgreich", true);
                    
                    ResultBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                    ResultBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    ResultTextBlock.Text = "✅ Verbindung erfolgreich hergestellt!\n\nDie Datenbank ist erreichbar und einsatzbereit.";
                    
                    solutions.AppendLine("✅ Alles in Ordnung! Die Verbindung funktioniert.");
                    return;
                }
                catch (MySqlException ex)
                {
                    AddChecklistItem("MySQL-Verbindung", false);
                    problems.AppendLine($"❌ MySQL-Fehler: {ex.Message}");
                    
                    if (ex.Message.Contains("Access denied"))
                    {
                        solutions.AppendLine("4️⃣ Benutzername oder Passwort falsch:");
                        solutions.AppendLine("   - Überprüfen Sie Ihre Zugangsdaten im Dogado Control Panel");
                        solutions.AppendLine("   - Benutzer: " + user);
                        solutions.AppendLine("   - Passwort vollständig eingegeben?\n");
                    }
                    else if (ex.Message.Contains("Unknown database"))
                    {
                        solutions.AppendLine("5️⃣ Datenbank existiert nicht:");
                        solutions.AppendLine("   - Loggen Sie sich ins Dogado Control Panel ein");
                        solutions.AppendLine($"   - Erstellen Sie die Datenbank: {database}\n");
                    }
                    else if (ex.Message.Contains("Unable to connect"))
                    {
                        solutions.AppendLine("6️⃣ Verbindung fehlgeschlagen:");
                        solutions.AppendLine("   - Bei SSH-Tunnel: Stellen Sie sicher, dass der Tunnel aktiv ist");
                        solutions.AppendLine("   - Bei Direktverbindung: Aktivieren Sie Remote-Zugriff bei Dogado");
                        solutions.AppendLine("   - Überprüfen Sie Server-Adresse und Port\n");
                    }
                    
                    hasErrors = true;
                }
                catch (Exception ex)
                {
                    AddChecklistItem("MySQL-Verbindung", false);
                    problems.AppendLine($"❌ Fehler: {ex.Message}");
                    hasErrors = true;
                }
            }

            // Zusammenfassung
            if (hasErrors)
            {
                ResultTextBlock.Text = problems.ToString();
                SolutionsTextBlock.Text = solutions.ToString();
            }
        }

        private void AddChecklistItem(string text, bool success)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            
            var icon = new TextBlock
            {
                Text = success ? "✅" : "❌",
                FontSize = 16,
                Margin = new Thickness(0, 0, 10, 0)
            };
            
            var label = new TextBlock
            {
                Text = text,
                FontSize = 13,
                Foreground = success ? Brushes.Green : Brushes.Red
            };
            
            panel.Children.Add(icon);
            panel.Children.Add(label);
            ChecklistPanel.Children.Add(panel);
        }

        private bool TestPort(string host, int portNumber)
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                var result = client.BeginConnect(host, portNumber, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                client.EndConnect(result);
                return success;
            }
            catch
            {
                return false;
            }
        }

        private void Retest_Click(object sender, RoutedEventArgs e)
        {
            LoadConfiguration();
            RunDiagnostics();
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DatabaseConfigDialog();
            dialog.ShowDialog();
            LoadConfiguration();
            RunDiagnostics();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
