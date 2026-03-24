using MySql.Data.MySqlClient;
using Projektsoftware.Services;
using System;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class DatabaseConfigDialog : Window
    {
        private DatabaseConfig config;

        public DatabaseConfigDialog()
        {
            InitializeComponent();
            LoadCurrentConfig();
        }

        private void LoadCurrentConfig()
        {
            config = DatabaseConfig.Load();

            ServerTextBox.Text = config.Server;
            PortTextBox.Text = config.Port;
            DatabaseTextBox.Text = config.Database;
            UserTextBox.Text = config.User;
            PasswordBox.Password = config.Password;

            SslModeComboBox.SelectedIndex = config.SslMode switch
            {
                "Required" => 1,
                "None" => 2,
                _ => 0 // Preferred
            };
        }

        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;

            string connectionString = BuildConnectionString();

            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();
                
                MessageBox.Show("✅ Verbindung erfolgreich!\n\nDie Verbindung zur Datenbank wurde erfolgreich hergestellt.", 
                    "Verbindungstest erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string errorMessage = "❌ Verbindung fehlgeschlagen!\n\n";
                errorMessage += $"Fehler: {ex.Message}\n\n";
                
                if (ex.Message.Contains("Unable to connect"))
                {
                    errorMessage += "Mögliche Ursachen:\n";
                    errorMessage += "• SSH-Tunnel nicht aktiv (bei 127.0.0.1:3307)\n";
                    errorMessage += "• Falsche Server-Adresse\n";
                    errorMessage += "• Firewall blockiert die Verbindung\n";
                    errorMessage += "• Remote-Zugriff nicht aktiviert (bei Direktverbindung)";
                }
                else if (ex.Message.Contains("Access denied"))
                {
                    errorMessage += "Benutzername oder Passwort ist falsch!";
                }
                else if (ex.Message.Contains("Unknown database"))
                {
                    errorMessage += "Die Datenbank existiert nicht!\nBitte erstellen Sie sie im Dogado Control Panel.";
                }

                MessageBox.Show(errorMessage, "Verbindungsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;

            try
            {
                config.Server = ServerTextBox.Text;
                config.Port = PortTextBox.Text;
                config.Database = DatabaseTextBox.Text;
                config.User = UserTextBox.Text;
                config.Password = PasswordBox.Password;
                config.SslMode = (SslModeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Preferred";

                config.Save();

                MessageBox.Show(
                    $"✅ Konfiguration wurde erfolgreich gespeichert!\n\n" +
                    $"Speicherort:\n{DatabaseConfig.GetConfigFilePath()}\n\n" +
                    $"Die Einstellungen bleiben auch nach Neustarts erhalten.\n" +
                    $"Bitte starten Sie die Anwendung neu.", 
                    "Erfolgreich gespeichert", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern der Konfiguration:\n{ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(ServerTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie eine Server-Adresse ein.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(PortTextBox.Text) || !int.TryParse(PortTextBox.Text, out _))
            {
                MessageBox.Show("Bitte geben Sie einen gültigen Port ein.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(DatabaseTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Datenbanknamen ein.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(UserTextBox.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Benutzernamen ein.", "Validierung", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Passwort nur bei Remote-Servern erforderlich
            bool isLocalhost = ServerTextBox.Text == "localhost" || ServerTextBox.Text == "127.0.0.1";
            if (!isLocalhost && string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                var result = MessageBox.Show(
                    "Kein Passwort eingegeben!\n\nFür Remote-Server wird normalerweise ein Passwort benötigt.\nTrotzdem fortfahren?", 
                    "Warnung", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return false;
            }

            return true;
        }

        private string BuildConnectionString()
        {
            string sslMode = (SslModeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Preferred";
            
            return $"Server={ServerTextBox.Text};Port={PortTextBox.Text};Database={DatabaseTextBox.Text};" +
                   $"Uid={UserTextBox.Text};Pwd={PasswordBox.Password};SslMode={sslMode};" +
                   $"CharSet=utf8mb4;AllowPublicKeyRetrieval=True;";
        }
    }
}
