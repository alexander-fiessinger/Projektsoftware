using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using Projektsoftware.Resources;

namespace Projektsoftware
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

#if !DEBUG
            // Generate application icon if it doesn't exist
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app-icon.ico");
            string iconDir = Path.GetDirectoryName(iconPath);

            if (!Directory.Exists(iconDir))
            {
                Directory.CreateDirectory(iconDir);
            }

            if (!File.Exists(iconPath))
            {
                try
                {
                    IconGenerator.SaveIconToFile(iconPath);
                }
                catch
                {
                    // Silently fail if icon generation doesn't work
                }
            }
#endif
        }
    }
}
