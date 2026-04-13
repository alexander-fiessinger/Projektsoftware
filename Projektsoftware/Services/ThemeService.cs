using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace Projektsoftware.Services
{
    public static class ThemeService
    {
        private static bool _isDarkMode;
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Projektsoftware", "theme.json");

        public static bool IsDarkMode => _isDarkMode;

        private static readonly (string Key, string Light, string Dark)[] BrushMap =
        [
            ("PrimaryBrush",       "#2563EB", "#60A5FA"),
            ("PrimaryDarkBrush",   "#1D4ED8", "#3B82F6"),
            ("PrimaryLightBrush",  "#DBEAFE", "#1E3A5F"),
            ("AccentBrush",        "#2563EB", "#60A5FA"),
            ("SuccessBrush",       "#16A34A", "#4ADE80"),
            ("SuccessLightBrush",  "#DCFCE7", "#14532D"),
            ("WarningBrush",       "#D97706", "#FBBF24"),
            ("WarningLightBrush",  "#FEF3C7", "#78350F"),
            ("DangerBrush",        "#DC2626", "#F87171"),
            ("DangerLightBrush",   "#FEE2E2", "#7F1D1D"),
            ("NeutralBrush",       "#475569", "#94A3B8"),
            ("NeutralLightBrush",  "#F1F5F9", "#1E293B"),
            ("BackgroundBrush",    "#F8FAFC", "#0F172A"),
            ("SurfaceBrush",       "#FFFFFF", "#1E293B"),
            ("TextPrimaryBrush",   "#0F172A", "#F1F5F9"),
            ("TextSecondaryBrush", "#64748B", "#94A3B8"),
            ("BorderBrush",        "#E2E8F0", "#334155"),
        ];

        public static void Initialize()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var doc = JsonDocument.Parse(json);
                    _isDarkMode = doc.RootElement.GetProperty("isDarkMode").GetBoolean();
                }
            }
            catch
            {
                _isDarkMode = false;
            }

            ApplyTheme();
        }

        public static void Toggle()
        {
            _isDarkMode = !_isDarkMode;
            ApplyTheme();
            Save();
        }

        private static void ApplyTheme()
        {
            var resources = Application.Current.Resources;

            foreach (var (key, light, dark) in BrushMap)
            {
                var colorHex = _isDarkMode ? dark : light;
                var color = (Color)ColorConverter.ConvertFromString(colorHex);

                if (resources[key] is SolidColorBrush existing)
                {
                    if (existing.IsFrozen)
                    {
                        resources[key] = new SolidColorBrush(color);
                    }
                    else
                    {
                        existing.Color = color;
                    }
                }
                else
                {
                    resources[key] = new SolidColorBrush(color);
                }
            }
        }

        private static void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(new { isDarkMode = _isDarkMode });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme-Einstellungen speichern fehlgeschlagen: {ex.Message}");
            }
        }
    }
}
