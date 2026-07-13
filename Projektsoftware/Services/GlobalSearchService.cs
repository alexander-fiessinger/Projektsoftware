using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Globaler Such-Service für alle Module (Projekte, Tickets, Aufgaben, Mitarbeiter)
    /// </summary>
    public class GlobalSearchService
    {
        public class SearchResult
        {
            public string Type { get; set; }
            public int Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Icon { get; set; }
            public double Relevance { get; set; }
        }

        private readonly DatabaseService _databaseService;

        public GlobalSearchService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Sucht über alle Module hinweg parallel
        /// </summary>
        public async Task<List<SearchResult>> SearchAllAsync(string searchTerm, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                return new List<SearchResult>();

            var allResults = new List<SearchResult>();

            try
            {
                // Parallele Suche
                var projectSearch = SearchProjectsAsync(searchTerm, maxResults);
                var ticketSearch = SearchTicketsAsync(searchTerm, maxResults);
                var taskSearch = SearchTasksAsync(searchTerm, maxResults);
                var employeeSearch = SearchEmployeesAsync(searchTerm, maxResults);
                var customerSearch = SearchCustomersAsync(searchTerm, maxResults);
                var leadSearch = SearchLeadsAsync(searchTerm, maxResults);

                await Task.WhenAll(projectSearch, ticketSearch, taskSearch, employeeSearch, customerSearch, leadSearch);

                allResults.AddRange(await projectSearch);
                allResults.AddRange(await ticketSearch);
                allResults.AddRange(await taskSearch);
                allResults.AddRange(await employeeSearch);
                allResults.AddRange(await customerSearch);
                allResults.AddRange(await leadSearch);

                return allResults
                    .OrderByDescending(r => r.Relevance)
                    .Take(maxResults)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei globaler Suche: {ex.Message}");
                return allResults;
            }
        }

        private async Task<List<SearchResult>> SearchProjectsAsync(string searchTerm, int maxResults)
        {
            try
            {
                var projects = await _databaseService.GetAllProjectsAsync();
                var term = searchTerm.ToLower();

                return projects
                    .Where(p =>
                        (p.Name != null && p.Name.ToLower().Contains(term)) ||
                        (p.Description != null && p.Description.ToLower().Contains(term)) ||
                        (p.ClientName != null && p.ClientName.ToLower().Contains(term)))
                    .Take(maxResults)
                    .Select(p => new SearchResult
                    {
                        Type = "Projekt",
                        Id = p.Id,
                        Title = p.Name ?? "Unbekannt",
                        Description = $"{p.ClientName ?? "Kein Kunde"} | Status: {p.Status ?? ""}",
                        Icon = "📁",
                        Relevance = CalculateRelevance(searchTerm, p.Name ?? "")
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Projekt-Suche: {ex.Message}");
                return new List<SearchResult>();
            }
        }

        private async Task<List<SearchResult>> SearchTicketsAsync(string searchTerm, int maxResults)
        {
            try
            {
                var tickets = await _databaseService.GetAllTicketsAsync();
                var term = searchTerm.ToLower();

                return tickets
                    .Where(t =>
                        (t.Subject != null && t.Subject.ToLower().Contains(term)) ||
                        (t.Description != null && t.Description.ToLower().Contains(term)) ||
                        (t.CustomerName != null && t.CustomerName.ToLower().Contains(term)) ||
                        (t.CustomerEmail != null && t.CustomerEmail.ToLower().Contains(term)))
                    .Take(maxResults)
                    .Select(t => new SearchResult
                    {
                        Type = "Ticket",
                        Id = t.Id,
                        Title = t.Subject ?? "Unbekannt",
                        Description = $"{t.TicketNumber} | {t.CustomerName ?? ""} | {t.StatusText}",
                        Icon = "🎫",
                        Relevance = CalculateRelevance(searchTerm, t.Subject ?? "")
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Ticket-Suche: {ex.Message}");
                return new List<SearchResult>();
            }
        }

        private async Task<List<SearchResult>> SearchTasksAsync(string searchTerm, int maxResults)
        {
            try
            {
                var tasks = await _databaseService.GetAllTasksAsync();
                var term = searchTerm.ToLower();

                return tasks
                    .Where(t =>
                        (t.Title != null && t.Title.ToLower().Contains(term)) ||
                        (t.Description != null && t.Description.ToLower().Contains(term)) ||
                        (t.ProjectName != null && t.ProjectName.ToLower().Contains(term)) ||
                        (t.AssignedTo != null && t.AssignedTo.ToLower().Contains(term)))
                    .Take(maxResults)
                    .Select(t => new SearchResult
                    {
                        Type = "Aufgabe",
                        Id = t.Id,
                        Title = t.Title ?? "Unbekannt",
                        Description = $"{t.ProjectName ?? "Kein Projekt"} | {t.Status ?? ""} | {t.AssignedTo ?? ""}",
                        Icon = "✓",
                        Relevance = CalculateRelevance(searchTerm, t.Title ?? "")
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Aufgaben-Suche: {ex.Message}");
                return new List<SearchResult>();
            }
        }

        private async Task<List<SearchResult>> SearchEmployeesAsync(string searchTerm, int maxResults)
        {
            try
            {
                var employees = await _databaseService.GetAllEmployeesAsync();
                var term = searchTerm.ToLower();

                return employees
                    .Where(e =>
                        (e.FirstName != null && e.FirstName.ToLower().Contains(term)) ||
                        (e.LastName != null && e.LastName.ToLower().Contains(term)) ||
                        (e.Email != null && e.Email.ToLower().Contains(term)) ||
                        (e.Position != null && e.Position.ToLower().Contains(term)))
                    .Take(maxResults)
                    .Select(e => new SearchResult
                    {
                        Type = "Mitarbeiter",
                        Id = e.Id,
                        Title = $"{e.FirstName} {e.LastName}",
                        Description = $"{e.Position ?? ""} | {e.Email ?? ""}",
                        Icon = "👤",
                        Relevance = CalculateRelevance(searchTerm, $"{e.FirstName} {e.LastName}")
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Mitarbeiter-Suche: {ex.Message}");
                return new List<SearchResult>();
            }
        }

        private async Task<List<SearchResult>> SearchCustomersAsync(string searchTerm, int maxResults)
        {
            try
            {
                var customers = await _databaseService.GetAllCustomersAsync();
                var term = searchTerm.ToLower();

                return customers
                    .Where(c =>
                        (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                        (c.FirstName != null && c.FirstName.ToLower().Contains(term)) ||
                        (c.LastName != null && c.LastName.ToLower().Contains(term)) ||
                        (c.Email != null && c.Email.ToLower().Contains(term)) ||
                        (c.City != null && c.City.ToLower().Contains(term)))
                    .Take(maxResults)
                    .Select(c => new SearchResult
                    {
                        Type = "Kunde",
                        Id = c.Id,
                        Title = c.DisplayName,
                        Description = $"{c.Email ?? ""} | {c.City ?? ""}",
                        Icon = "🏢",
                        Relevance = CalculateRelevance(searchTerm, c.DisplayName)
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Kunden-Suche: {ex.Message}");
                return new List<SearchResult>();
            }
        }

        private async Task<List<SearchResult>> SearchLeadsAsync(string searchTerm, int maxResults)
        {
            try
            {
                var leads = await _databaseService.GetSalesLeadsAsync();
                var term = searchTerm.ToLower();

                return leads
                    .Where(l =>
                        (l.Title != null && l.Title.ToLower().Contains(term)) ||
                        (l.ContactName != null && l.ContactName.ToLower().Contains(term)) ||
                        (l.ContactCompany != null && l.ContactCompany.ToLower().Contains(term)) ||
                        (l.ContactEmail != null && l.ContactEmail.ToLower().Contains(term)))
                    .Take(maxResults)
                    .Select(l => new SearchResult
                    {
                        Type = "Lead",
                        Id = l.Id,
                        Title = l.Title ?? l.ContactName ?? "Lead",
                        Description = $"{l.ContactCompany ?? ""} | {l.Status}",
                        Icon = "💼",
                        Relevance = CalculateRelevance(searchTerm, l.Title ?? l.ContactName ?? "")
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei Lead-Suche: {ex.Message}");
                return new List<SearchResult>();
            }
        }

        private double CalculateRelevance(string searchTerm, string targetText)
        {
            if (string.IsNullOrEmpty(targetText))
                return 0;

            var term = searchTerm.ToLower();
            var text = targetText.ToLower();

            if (text == term) return 100;
            if (text.StartsWith(term)) return 80;
            if (text.Contains($" {term}")) return 60;
            if (text.Contains(term)) return 40;

            return 20;
        }
    }
}
