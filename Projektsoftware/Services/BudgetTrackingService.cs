using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Service für Projekt-Budget-Tracking - delegiert an DatabaseService.
    /// </summary>
    public class BudgetTrackingService
    {
        private readonly DatabaseService _databaseService;

        public BudgetTrackingService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Erstellt oder aktualisiert Projekt-Budget
        /// </summary>
        public async Task<ProjectBudget> InitializeProjectBudgetAsync(int projectId, decimal plannedBudget, decimal plannedHours)
        {
            var existing = await _databaseService.GetProjectBudgetAsync(projectId) ?? new ProjectBudget { ProjectId = projectId };
            existing.ProjectId = projectId;
            existing.TotalPlannedBudget = plannedBudget;
            existing.TotalPlannedHours = plannedHours;
            existing.LastUpdated = DateTime.Now;
            await _databaseService.SaveProjectBudgetAsync(existing);
            return existing;
        }

        /// <summary>
        /// Aktualisiert Budget-Ist-Werte aus aktuellen Zeiterfassungen
        /// </summary>
        public async Task UpdateActualBudgetAsync(int projectId)
        {
            var budget = await _databaseService.GetProjectBudgetAsync(projectId);
            if (budget == null) return;
            await _databaseService.SaveProjectBudgetAsync(budget);
        }

        /// <summary>
        /// Fügt Budget-Eintrag hinzu
        /// </summary>
        public async Task AddBudgetEntryAsync(int projectId, string category, string description, decimal plannedAmount, decimal actualAmount)
        {
            var entry = new BudgetEntry
            {
                ProjectId = projectId,
                Category = category,
                Description = description,
                PlannedAmount = plannedAmount,
                ActualAmount = actualAmount,
                EntryDate = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            await _databaseService.SaveBudgetEntryAsync(entry);
        }

        /// <summary>
        /// Gibt Budget-Übersicht zurück
        /// </summary>
        public async Task<BudgetOverview?> GetBudgetOverviewAsync(int projectId)
        {
            var budget = await _databaseService.GetProjectBudgetAsync(projectId);
            if (budget == null) return null;

            var entries = await _databaseService.GetBudgetEntriesByProjectAsync(projectId);

            var breakdown = entries
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Category) ? "Sonstiges" : e.Category)
                .Select(g => new CategoryBreakdown
                {
                    Category = g.Key,
                    PlannedAmount = g.Sum(x => x.PlannedAmount),
                    ActualAmount = g.Sum(x => x.ActualAmount)
                })
                .ToList();

            decimal variance = budget.TotalActualBudget - budget.TotalPlannedBudget;
            decimal variancePct = budget.TotalPlannedBudget > 0
                ? Math.Round(variance / budget.TotalPlannedBudget * 100m, 2)
                : 0m;

            return new BudgetOverview
            {
                ProjectId = projectId,
                Budget = budget,
                CategoryBreakdown = breakdown,
                TotalVariance = variance,
                VariancePercentage = variancePct,
                IsOverBudget = budget.TotalActualBudget > budget.TotalPlannedBudget
            };
        }

        public class BudgetOverview
        {
            public int ProjectId { get; set; }
            public ProjectBudget Budget { get; set; } = new();
            public List<CategoryBreakdown> CategoryBreakdown { get; set; } = new();
            public decimal TotalVariance { get; set; }
            public decimal VariancePercentage { get; set; }
            public bool IsOverBudget { get; set; }
        }

        public class CategoryBreakdown
        {
            public string Category { get; set; } = string.Empty;
            public decimal PlannedAmount { get; set; }
            public decimal ActualAmount { get; set; }
            public decimal Variance => ActualAmount - PlannedAmount;
        }
    }
}
