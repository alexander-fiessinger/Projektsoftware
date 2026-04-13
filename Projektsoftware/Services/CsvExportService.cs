using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Projektsoftware.Services
{
    public static class CsvExportService
    {
        private static readonly CultureInfo De = new("de-DE");

        public static void ExportCustomers(IEnumerable<Customer> customers, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name;E-Mail;Telefon;Stadt;PLZ;Straße;Land;Easybill-Status");
            foreach (var c in customers)
            {
                sb.AppendLine($"{Esc(c.DisplayName)};{Esc(c.Email)};{Esc(c.Phone)};{Esc(c.City)};{Esc(c.ZipCode)};{Esc(c.Street)};{Esc(c.Country)};{Esc(c.SyncStatus)}");
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public static void ExportProjects(IEnumerable<Project> projects, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Projektname;Kunde;Status;Startdatum;Budget");
            foreach (var p in projects)
            {
                sb.AppendLine($"{Esc(p.Name)};{Esc(p.ClientName)};{Esc(p.Status)};{p.StartDate:dd.MM.yyyy};{p.Budget.ToString("F2", De)}");
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public static void ExportTimeEntries(IEnumerable<TimeEntry> entries, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Projekt;Mitarbeiter;Datum;Stunden;Tätigkeit;Kunde");
            foreach (var t in entries)
            {
                sb.AppendLine($"{Esc(t.ProjectName)};{Esc(t.EmployeeName)};{t.Date:dd.MM.yyyy};{t.Duration.TotalHours.ToString("F2", De)};{Esc(t.Activity)};{Esc(t.ClientName)}");
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public static void ExportTasks(IEnumerable<ProjectTask> tasks, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Titel;Projekt;Zugewiesen;Status;Priorität;Fällig;Beschreibung");
            foreach (var t in tasks)
            {
                sb.AppendLine($"{Esc(t.Title)};{Esc(t.ProjectName)};{Esc(t.AssignedTo)};{Esc(t.Status)};{Esc(t.Priority)};{t.DueDate:dd.MM.yyyy};{Esc(t.Description)}");
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public static void ExportEmployees(IEnumerable<Employee> employees, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name;Position;Abteilung;E-Mail;Stundensatz;Aktiv");
            foreach (var e in employees)
            {
                sb.AppendLine($"{Esc(e.FullName)};{Esc(e.Position)};{Esc(e.Department)};{Esc(e.Email)};{e.HourlyRate.ToString("F2", De)};{(e.IsActive ? "Ja" : "Nein")}");
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static string Esc(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
