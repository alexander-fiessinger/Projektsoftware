using System;

namespace Projektsoftware.Models
{
    public enum DealStage
    {
        Lead = 0,
        Qualified = 1,
        Proposal = 2,
        Negotiation = 3,
        Won = 4,
        Lost = 5
    }

    public class CrmDeal
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int? ContactId { get; set; }
        public string ContactName { get; set; }
        public string Title { get; set; }
        public decimal Value { get; set; }
        public DealStage Stage { get; set; }
        public int Probability { get; set; }
        public DateTime? ExpectedCloseDate { get; set; }
        public string Notes { get; set; }
        public string AssignedTo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? WonAt { get; set; }
        public DateTime? LostAt { get; set; }
        public string LostReason { get; set; }

        public CrmDeal()
        {
            CreatedAt = DateTime.Now;
            Probability = 50;
        }

        public string StageText => Stage switch
        {
            DealStage.Lead => "🔵 Lead",
            DealStage.Qualified => "🟡 Qualifiziert",
            DealStage.Proposal => "🟠 Angebot",
            DealStage.Negotiation => "🔴 Verhandlung",
            DealStage.Won => "✅ Gewonnen",
            DealStage.Lost => "❌ Verloren",
            _ => "Unbekannt"
        };

        public decimal WeightedValue => Value * Probability / 100m;
    }
}
