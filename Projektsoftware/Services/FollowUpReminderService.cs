using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Projektsoftware.Models;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Lokale JSON-Persistierung für Follow-up-Erinnerungen
    /// </summary>
    public static class FollowUpReminderService
    {
        private static readonly string _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Projektsoftware", "followups.json");

        public static List<FollowUpReminder> Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return new List<FollowUpReminder>();
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<FollowUpReminder>>(json) ?? new();
            }
            catch
            {
                return new List<FollowUpReminder>();
            }
        }

        public static void Save(List<FollowUpReminder> items)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
                var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FollowUp save error: {ex.Message}");
            }
        }

        public static void Add(FollowUpReminder reminder)
        {
            var items = Load();
            reminder.Id = items.Count == 0 ? 1 : items.Max(r => r.Id) + 1;
            items.Add(reminder);
            Save(items);
        }

        public static void Update(FollowUpReminder reminder)
        {
            var items = Load();
            var idx = items.FindIndex(r => r.Id == reminder.Id);
            if (idx >= 0) items[idx] = reminder;
            Save(items);
        }

        public static void Delete(int id)
        {
            var items = Load();
            items.RemoveAll(r => r.Id == id);
            Save(items);
        }
    }
}
