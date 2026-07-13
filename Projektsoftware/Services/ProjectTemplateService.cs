using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Projektsoftware.Models;

namespace Projektsoftware.Services
{
    public static class ProjectTemplateService
    {
        private static readonly string _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Projektsoftware", "project_templates.json");

        public static List<ProjectTemplate> Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return DefaultTemplates();
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<ProjectTemplate>>(json) ?? DefaultTemplates();
            }
            catch { return DefaultTemplates(); }
        }

        public static void Save(List<ProjectTemplate> templates)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
                File.WriteAllText(_filePath,
                    JsonSerializer.Serialize(templates, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Template save error: {ex.Message}");
            }
        }

        public static void Add(ProjectTemplate template)
        {
            var items = Load();
            template.Id = items.Count == 0 ? 1 : items.Max(t => t.Id) + 1;
            items.Add(template);
            Save(items);
        }

        public static void Delete(int id)
        {
            var items = Load();
            items.RemoveAll(t => t.Id == id);
            Save(items);
        }

        private static List<ProjectTemplate> DefaultTemplates() => new()
        {
            new ProjectTemplate
            {
                Id = 1,
                Name = "Standard Web-Projekt",
                Description = "Vorlage für ein einfaches Webprojekt",
                DefaultDurationDays = 60,
                Tasks = new List<TemplateTask>
                {
                    new() { Title = "Anforderungsanalyse", Priority = "Hoch", DueAfterDays = 3 },
                    new() { Title = "Konzept & Design", Priority = "Hoch", DueAfterDays = 10 },
                    new() { Title = "Entwicklung", Priority = "Normal", DueAfterDays = 40 },
                    new() { Title = "Testing", Priority = "Normal", DueAfterDays = 50 },
                    new() { Title = "Deployment & Übergabe", Priority = "Hoch", DueAfterDays = 60 },
                }
            },
            new ProjectTemplate
            {
                Id = 2,
                Name = "Beratungsprojekt",
                Description = "Workshop & Beratung",
                DefaultDurationDays = 21,
                Tasks = new List<TemplateTask>
                {
                    new() { Title = "Kick-off-Termin", Priority = "Hoch", DueAfterDays = 1 },
                    new() { Title = "Workshop durchführen", Priority = "Hoch", DueAfterDays = 7 },
                    new() { Title = "Abschlussbericht", Priority = "Normal", DueAfterDays = 21 },
                }
            }
        };
    }
}
