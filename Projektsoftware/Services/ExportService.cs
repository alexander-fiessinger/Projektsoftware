using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Export-Service für CSV/Excel-Export (PDF benötigt externe Bibliothek)
    /// </summary>
    public class ExportService
    {
        private static readonly CultureInfo euroFormat = new CultureInfo("de-DE");

        public static string ExportProjectsToCsv(List<Project> projects, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID,Name,Beschreibung,Kunde,Status,Startdatum,Enddatum,Budget,Erstellt am");

            foreach (var project in projects)
            {
                csv.AppendLine($"{project.Id}," +
                             $"\"{EscapeCsv(project.Name)}\"," +
                             $"\"{EscapeCsv(project.Description)}\"," +
                             $"\"{EscapeCsv(project.ClientName)}\"," +
                             $"\"{project.Status}\"," +
                             $"{project.StartDate:yyyy-MM-dd}," +
                             $"{(project.EndDate?.ToString("yyyy-MM-dd") ?? "")}," +
                             $"{project.Budget.ToString("F2", euroFormat)}," +
                             $"{project.CreatedAt:yyyy-MM-dd HH:mm}");
            }

            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
            return filePath;
        }

        public static string ExportTimeEntriesToCsv(List<TimeEntry> entries, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID,Projekt,Mitarbeiter,Datum,Dauer,Aktivität,Beschreibung");

            foreach (var entry in entries)
            {
                csv.AppendLine($"{entry.Id}," +
                             $"\"{EscapeCsv(entry.ProjectName)}\"," +
                             $"\"{EscapeCsv(entry.EmployeeName)}\"," +
                             $"{entry.Date:yyyy-MM-dd}," +
                             $"{entry.Duration}," +
                             $"\"{EscapeCsv(entry.Activity)}\"," +
                             $"\"{EscapeCsv(entry.Description)}\"");
            }

            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
            return filePath;
        }

        public static string ExportTasksToCsv(List<ProjectTask> tasks, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID,Projekt,Titel,Zugewiesen an,Status,Priorität,Fälligkeitsdatum,Geschätzte Stunden,Tatsächliche Stunden");

            foreach (var task in tasks)
            {
                csv.AppendLine($"{task.Id}," +
                             $"\"{EscapeCsv(task.ProjectName)}\"," +
                             $"\"{EscapeCsv(task.Title)}\"," +
                             $"\"{EscapeCsv(task.AssignedTo)}\"," +
                             $"\"{task.Status}\"," +
                             $"\"{task.Priority}\"," +
                             $"{(task.DueDate?.ToString("yyyy-MM-dd") ?? "")}," +
                             $"{task.EstimatedHours}," +
                             $"{task.ActualHours}");
            }

            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
            return filePath;
        }

        public static string ExportTimeEntriesReport(List<TimeEntry> entries, DateTime from, DateTime to, string filePath)
        {
            var filteredEntries = entries.Where(e => e.Date >= from && e.Date <= to).ToList();
            
            var report = new StringBuilder();
            report.AppendLine($"Zeiterfassungsbericht: {from:dd.MM.yyyy} - {to:dd.MM.yyyy}");
            report.AppendLine(new string('=', 80));
            report.AppendLine();

            var groupedByEmployee = filteredEntries.GroupBy(e => e.EmployeeName);
            
            foreach (var group in groupedByEmployee)
            {
                report.AppendLine($"Mitarbeiter: {group.Key}");
                report.AppendLine(new string('-', 80));
                
                decimal totalHours = 0;
                foreach (var entry in group.OrderBy(e => e.Date))
                {
                    totalHours += (decimal)entry.Duration.TotalHours;
                    report.AppendLine($"  {entry.Date:dd.MM.yyyy} | {entry.Duration} | {entry.ProjectName} | {entry.Activity}");
                }
                
                report.AppendLine($"  Gesamt: {totalHours:F2} Stunden");
                report.AppendLine();
            }

            var grandTotal = filteredEntries.Sum(e => e.Duration.TotalHours);
            report.AppendLine(new string('=', 80));
            report.AppendLine($"Gesamtstunden: {grandTotal:F2}");

            File.WriteAllText(filePath, report.ToString(), Encoding.UTF8);
            return filePath;
        }

        public static string ExportMeetingProtocolToText(MeetingProtocol protocol, string filePath)
        {
            var text = new StringBuilder();
            text.AppendLine("BESPRECHUNGSPROTOKOLL");
            text.AppendLine(new string('=', 80));
            text.AppendLine();
            text.AppendLine($"Titel: {protocol.Title}");
            text.AppendLine($"Projekt: {protocol.ProjectName}");
            text.AppendLine($"Datum: {protocol.MeetingDate:dd.MM.yyyy HH:mm}");
            text.AppendLine($"Ort: {protocol.Location}");
            text.AppendLine();
            text.AppendLine("TEILNEHMER:");
            text.AppendLine(new string('-', 80));
            text.AppendLine(protocol.Participants);
            text.AppendLine();
            text.AppendLine("TAGESORDNUNG:");
            text.AppendLine(new string('-', 80));
            text.AppendLine(protocol.Agenda);
            text.AppendLine();
            text.AppendLine("DISKUSSION:");
            text.AppendLine(new string('-', 80));
            text.AppendLine(protocol.Discussion);
            text.AppendLine();
            text.AppendLine("BESCHLÜSSE:");
            text.AppendLine(new string('-', 80));
            text.AppendLine(protocol.Decisions);
            text.AppendLine();
            text.AppendLine("MASSNAHMEN:");
            text.AppendLine(new string('-', 80));
            text.AppendLine(protocol.ActionItems);
            text.AppendLine();
            text.AppendLine($"Nächstes Treffen: {protocol.NextMeetingDate}");

            File.WriteAllText(filePath, text.ToString(), Encoding.UTF8);
            return filePath;
        }

        private static string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            
            return text.Replace("\"", "\"\"");
        }
    }
}
