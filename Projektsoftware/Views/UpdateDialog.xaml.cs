using Projektsoftware.Services;
using System;
using System.Threading;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class UpdateDialog : Window
    {
        private readonly UpdateInfo updateInfo;
        private readonly UpdateService updateService;
        private CancellationTokenSource? cts;

        public UpdateDialog(UpdateInfo info)
        {
            InitializeComponent();
            updateInfo = info;
            updateService = new UpdateService();

            CurrentVersionText.Text = UpdateService.CurrentVersion.ToString();
            NewVersionText.Text = info.Version;
            ReleaseNotesText.Text = string.IsNullOrWhiteSpace(info.ReleaseNotes)
                ? "Keine Versionshinweise verfügbar."
                : info.ReleaseNotes.Replace("\\n", "\n");
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            UpdateButton.IsEnabled = false;
            CancelButton.Content = "Abbrechen";
            ProgressPanel.Visibility = Visibility.Visible;
            ProgressStatusText.Text = "⬇ Wird heruntergeladen...";

            cts = new CancellationTokenSource();
            var progress = new Progress<int>(p =>
            {
                DownloadProgressBar.Value = p;
                ProgressPercentText.Text = $"{p} %";
            });

            try
            {
                var filePath = await updateService.DownloadUpdateAsync(updateInfo, progress, cts.Token);
                ProgressStatusText.Text = "✅ Download abgeschlossen – Installation wird gestartet...";
                await System.Threading.Tasks.Task.Delay(800);
                updateService.InstallUpdate(filePath);
            }
            catch (OperationCanceledException)
            {
                ProgressPanel.Visibility = Visibility.Collapsed;
                DownloadProgressBar.Value = 0;
                ProgressPercentText.Text = "0 %";
                UpdateButton.IsEnabled = true;
                CancelButton.Content = "Später";
            }
            catch (Exception ex)
            {
                ProgressStatusText.Text = $"❌ Fehler: {ex.Message}";
                CancelButton.Content = "Schließen";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            cts?.Cancel();
            cts?.Dispose();
        }
    }
}
