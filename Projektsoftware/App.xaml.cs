using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Projektsoftware.Resources;

namespace Projektsoftware
{
    public partial class App : Application
    {
        private static BitmapFrame? _appIcon;

        // Automatische Artikel-Synchronisation (Easybill -> lokaler Portal-Katalog)
        private System.Windows.Threading.DispatcherTimer? _productSyncTimer;
        private bool _productSyncRunning;

        // Automatischer Zahlungsabgleich (BANKSapi -> Easybill) im Hintergrund
        private System.Windows.Threading.DispatcherTimer? _reconciliationTimer;
        private bool _reconciliationRunning;

        /// <summary>
        /// Wird nach jedem automatischen Artikel-Sync ausgelöst (z. B. zur Statusanzeige in offenen Dialogen).
        /// </summary>
        public static event Action<Services.ProductSyncService.SyncResult>? ProductSyncCompleted;

        /// <summary>
        /// Wird nach jedem automatischen Zahlungsabgleich ausgelöst (z. B. zur Aktualisierung des Dashboards).
        /// </summary>
        public static event Action<Services.AutoReconciliationService.AutoReconciliationResult>? AutoReconciliationCompleted;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            static string BuildFullExceptionLog(Exception? root)
            {
                var sb = new System.Text.StringBuilder();
                int level = 0;
                var current = root;
                while (current != null)
                {
                    sb.AppendLine($"=== [Level {level}] {current.GetType().FullName} ===");
                    sb.AppendLine($"Message: {current.Message}");
                    sb.AppendLine($"StackTrace:\n{current.StackTrace}");
                    sb.AppendLine();
                    current = current.InnerException;
                    level++;
                }
                return sb.ToString();
            }

            static void WriteCrashLog(string content)
            {
                try
                {
                    System.IO.File.WriteAllText(
                        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"),
                        content);
                }
                catch { /* ignore log write failures */ }
            }

            // Globaler Ausnahme-Handler für nicht abgefangene Ausnahmen
            DispatcherUnhandledException += (sender, args) =>
            {
                var log = BuildFullExceptionLog(args.Exception);
                WriteCrashLog(log);
                System.Diagnostics.Debug.WriteLine(log);
                var preview = log.Length > 2000 ? log.Substring(0, 2000) + "\n\n...[vollständig in crash.log]" : log;
                MessageBox.Show(preview, "Kritischer Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    var log = BuildFullExceptionLog(ex);
                    WriteCrashLog(log);
                    System.Diagnostics.Debug.WriteLine(log);
                }
            };

            // Theme initialisieren (Dark Mode Einstellungen laden)
            Services.ThemeService.Initialize();

            // Icon einmalig laden/generieren
            _appIcon = LoadOrCreateAppIcon();

            // Automatisch auf alle Fenster anwenden, sobald sie geladen werden
            EventManager.RegisterClassHandler(
                typeof(Window),
                Window.LoadedEvent,
                new RoutedEventHandler(OnAnyWindowLoaded));

            // Automatische Artikel-Synchronisation alle 2 Minuten starten
            StartProductSyncTimer();

            // Automatischen Zahlungsabgleich (BANKSapi -> Easybill) starten:
            // einmal beim Programmstart, danach stündlich ein kompletter Durchlauf.
            StartReconciliationTimer();
        }

        private void StartProductSyncTimer()
        {
            _productSyncTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(2)
            };
            _productSyncTimer.Tick += async (s, e) => await RunProductSyncAsync();
            _productSyncTimer.Start();

            // Einmaliger initialer Lauf, damit der Katalog nicht erst nach 2 Minuten befüllt wird
            _ = RunProductSyncAsync();
        }

        private async System.Threading.Tasks.Task RunProductSyncAsync()
        {
            // Überlappende Läufe verhindern (z. B. bei langsamer API)
            if (_productSyncRunning)
                return;

            _productSyncRunning = true;
            try
            {
                var result = await new Services.ProductSyncService().SyncAsync();
                ProductSyncCompleted?.Invoke(result);
            }
            catch (Exception ex)
            {
                // Hintergrund-Sync darf die App nie zum Absturz bringen
                System.Diagnostics.Debug.WriteLine($"Artikel-Sync-Fehler: {ex.Message}");
            }
            finally
            {
                _productSyncRunning = false;
            }
        }

        private void StartReconciliationTimer()
        {
            _reconciliationTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromHours(1)
            };
            _reconciliationTimer.Tick += async (s, e) => await RunReconciliationAsync();
            _reconciliationTimer.Start();

            // Einmaliger initialer Lauf beim Programmstart.
            _ = RunReconciliationAsync();
        }

        private async System.Threading.Tasks.Task RunReconciliationAsync()
        {
            // Überlappende Läufe verhindern (z. B. bei langsamer Bank-/Easybill-API).
            if (_reconciliationRunning)
                return;

            _reconciliationRunning = true;
            try
            {
                var result = await new Services.AutoReconciliationService().RunAsync();
                AutoReconciliationCompleted?.Invoke(result);
            }
            catch (Exception ex)
            {
                // Hintergrund-Abgleich darf die App nie zum Absturz bringen.
                System.Diagnostics.Debug.WriteLine($"Auto-Zahlungsabgleich-Fehler: {ex.Message}");
            }
            finally
            {
                _reconciliationRunning = false;
            }
        }

        private static BitmapFrame? LoadOrCreateAppIcon()
        {
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (!File.Exists(iconPath))
                    IconGenerator.SaveIconToFile(iconPath);
                
                return BitmapFrame.Create(
                    new Uri(iconPath, UriKind.Absolute),
                    BitmapCreateOptions.None,
                    BitmapCacheOption.OnLoad);
            }
            catch
            {
                return null;
            }
        }

        private static void OnAnyWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window && _appIcon != null)
                window.Icon = _appIcon;
        }
    }
}
