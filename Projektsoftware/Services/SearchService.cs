using Projektsoftware.Models;
using System.Collections.Generic;
using System.Linq;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Such- und Filterfunktionen
    /// </summary>
    public class SearchService
    {
        public static List<Project> SearchProjects(List<Project> projects, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return projects;

            searchTerm = searchTerm.ToLower();
            return projects.Where(p =>
                p.Name.ToLower().Contains(searchTerm) ||
                (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                (p.ClientName != null && p.ClientName.ToLower().Contains(searchTerm)) ||
                (p.Status != null && p.Status.ToLower().Contains(searchTerm))
            ).ToList();
        }

        public static List<Project> FilterProjectsByStatus(List<Project> projects, string status)
        {
            if (string.IsNullOrWhiteSpace(status) || status == "Alle")
                return projects;

            return projects.Where(p => p.Status == status).ToList();
        }

        public static List<ProjectTask> SearchTasks(List<ProjectTask> tasks, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return tasks;

            searchTerm = searchTerm.ToLower();
            return tasks.Where(t =>
                t.Title.ToLower().Contains(searchTerm) ||
                (t.Description != null && t.Description.ToLower().Contains(searchTerm)) ||
                (t.AssignedTo != null && t.AssignedTo.ToLower().Contains(searchTerm)) ||
                (t.ProjectName != null && t.ProjectName.ToLower().Contains(searchTerm))
            ).ToList();
        }

        public static List<ProjectTask> FilterTasksByStatus(List<ProjectTask> tasks, string status)
        {
            if (string.IsNullOrWhiteSpace(status) || status == "Alle")
                return tasks;

            return tasks.Where(t => t.Status == status).ToList();
        }

        public static List<ProjectTask> FilterTasksByPriority(List<ProjectTask> tasks, string priority)
        {
            if (string.IsNullOrWhiteSpace(priority) || priority == "Alle")
                return tasks;

            return tasks.Where(t => t.Priority == priority).ToList();
        }

        public static List<TimeEntry> SearchTimeEntries(List<TimeEntry> entries, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return entries;

            searchTerm = searchTerm.ToLower();
            return entries.Where(e =>
                (e.EmployeeName != null && e.EmployeeName.ToLower().Contains(searchTerm)) ||
                (e.ProjectName != null && e.ProjectName.ToLower().Contains(searchTerm)) ||
                (e.Activity != null && e.Activity.ToLower().Contains(searchTerm)) ||
                (e.Description != null && e.Description.ToLower().Contains(searchTerm))
            ).ToList();
        }

        public static List<Employee> SearchEmployees(List<Employee> employees, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return employees;

            searchTerm = searchTerm.ToLower();
            return employees.Where(e =>
                e.FirstName.ToLower().Contains(searchTerm) ||
                e.LastName.ToLower().Contains(searchTerm) ||
                (e.Email != null && e.Email.ToLower().Contains(searchTerm)) ||
                (e.Position != null && e.Position.ToLower().Contains(searchTerm)) ||
                (e.Department != null && e.Department.ToLower().Contains(searchTerm))
            ).ToList();
        }

        public static List<MeetingProtocol> SearchMeetingProtocols(List<MeetingProtocol> protocols, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return protocols;

            searchTerm = searchTerm.ToLower();
            return protocols.Where(p =>
                p.Title.ToLower().Contains(searchTerm) ||
                (p.ProjectName != null && p.ProjectName.ToLower().Contains(searchTerm)) ||
                (p.Participants != null && p.Participants.ToLower().Contains(searchTerm)) ||
                (p.Agenda != null && p.Agenda.ToLower().Contains(searchTerm))
            ).ToList();
        }
    }
}
