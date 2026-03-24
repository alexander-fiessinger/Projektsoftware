using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
    }

    public class UpdateService
    {
        private readonly HttpClient httpClient;
        private readonly UpdateConfig config;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public UpdateService()
        {
            config = UpdateConfig.Load();
            httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Projektsoftware-Updater/1.0");
        }

        /// <summary>
        /// Returns the current application version (Major.Minor.Build).
        /// </summary>
        public static Version CurrentVersion
        {
            get
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
                return new Version(v.Major, v.Minor, Math.Max(v.Build, 0));
            }
        }

        public bool IsConfigured => config.IsConfigured;
        public bool AutoCheckOnStartup => config.AutoCheckOnStartup;

        /// <summary>
        /// Fetches the update manifest and returns UpdateInfo if a newer version is available,
        /// or null if the current version is already up-to-date.
        /// </summary>
        public async Task<UpdateInfo?> CheckForUpdateAsync()
        {
            if (!config.IsConfigured)
                return null;

            var json = await httpClient.GetStringAsync(config.ManifestUrl);
            var info = JsonSerializer.Deserialize<UpdateInfo>(json, JsonOptions)
                ?? throw new Exception("Ungültiges Update-Manifest.");

            if (string.IsNullOrWhiteSpace(info.Version) || string.IsNullOrWhiteSpace(info.DownloadUrl))
                throw new Exception("Update-Manifest ist unvollständig (version oder downloadUrl fehlt).");

            if (!Version.TryParse(info.Version, out var available))
                throw new Exception($"Ungültige Versionsnummer im Manifest: '{info.Version}'");

            var availableNorm = new Version(available.Major, available.Minor, Math.Max(available.Build, 0));
            return availableNorm > CurrentVersion ? info : null;
        }

        /// <summary>
        /// Downloads the update installer to %TEMP% and reports progress (0–100).
        /// Returns the local file path.
        /// </summary>
        public async Task<string> DownloadUpdateAsync(
            UpdateInfo info,
            IProgress<int> progress,
            CancellationToken cancellationToken = default)
        {
            var ext = Path.GetExtension(new Uri(info.DownloadUrl).LocalPath);
            if (string.IsNullOrEmpty(ext)) ext = ".exe";
            var destPath = Path.Combine(Path.GetTempPath(), $"Projektsoftware_update_{info.Version}{ext}");

            using var response = await httpClient.GetAsync(
                info.DownloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            using var src = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var dst = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await src.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;
                if (totalBytes > 0)
                    progress.Report((int)(totalRead * 100 / totalBytes));
            }

            progress.Report(100);
            return destPath;
        }

        /// <summary>
        /// Launches the downloaded installer and shuts down the current application.
        /// </summary>
        public void InstallUpdate(string installerPath)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            });
            System.Windows.Application.Current.Shutdown();
        }
    }
}
