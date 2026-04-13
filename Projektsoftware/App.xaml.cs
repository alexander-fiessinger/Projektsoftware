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
