using Projektsoftware.Services;
using System;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class UpdateSettingsDialog : Window
    {
        public UpdateSettingsDialog()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            var config = UpdateConfig.Load();
            ManifestUrlBox.Text = config.ManifestUrl;
            AutoCheckCheckBox.IsChecked = config.AutoCheckOnStartup;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var config = new UpdateConfig
            {
                ManifestUrl = ManifestUrlBox.Text.Trim(),
                AutoCheckOnStartup = AutoCheckCheckBox.IsChecked == true
            };

            try
            {
                config.Save();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern:\n\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
