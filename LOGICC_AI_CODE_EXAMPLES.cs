using Projektsoftware.Models;
using Projektsoftware.Services;
using Projektsoftware.Views;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Projektsoftware.Examples
{
    /// <summary>
    /// Einfache Code-Beispiele für die LogicC AI Integration
    /// Diese Datei ist nur zur Demonstration und wird NICHT kompiliert
    /// </summary>
    public static class LogicCAiUsageExamples
    {
        // ============================================
        // BEISPIEL 1: Ticket automatisch kategorisieren
        // ============================================
        /*
        public async Task<Ticket> CategorizeTicketExample(Ticket ticket)
        {
            var aiService = new LogicCAiService();

            if (!aiService.IsConfigured)
            {
                MessageBox.Show("Bitte LogicC AI konfigurieren.");
                return ticket;
            }

            try
            {
                var result = await aiService.CategorizeTicketAsync(
                    ticket.Subject, 
                    ticket.Description
                );

                MessageBox.Show(
                    $"KI-Vorschlag:\n\n" +
                    $"Kategorie: {result.Category}\n" +
                    $"Priorität: {result.Priority}\n" +
                    $"Konfidenz: {result.Confidence:P0}\n\n" +
                    $"{result.Reasoning}",
                    "Kategorisierung"
                );

                return ticket;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler: {ex.Message}");
                return ticket;
            }
        }
        */

        // ============================================
        // BEISPIEL 2: Antwort-Vorschläge generieren
        // ============================================
        /*
        public async Task GenerateResponseSuggestions(Ticket ticket)
        {
            var aiService = new LogicCAiService();

            var suggestions = await aiService.GenerateTicketResponseSuggestionsAsync(
                ticket.Subject,
                ticket.Description,
                null // oder List<string> mit bisherigen Kommentaren
            );

            // Zeige Vorschläge dem Benutzer
            foreach (var suggestion in suggestions)
            {
                Console.WriteLine($"Vorschlag: {suggestion}\n");
            }
        }
        */

        // ============================================
        // BEISPIEL 3: E-Mail zusammenfassen
        // ============================================
        /*
        public async Task SummarizeEmail(string subject, string body)
        {
            var aiService = new LogicCAiService();

            var summary = await aiService.SummarizeEmailAsync(subject, body);

            MessageBox.Show(
                $"Zusammenfassung:\n{summary.Summary}\n\n" +
                $"Action Items:\n{string.Join("\n", summary.ActionItems)}\n\n" +
                $"Sentiment: {summary.Sentiment}",
                "E-Mail-Zusammenfassung"
            );
        }
        */

        // ============================================
        // BEISPIEL 4: Lead bewerten
        // ============================================
        /*
        public async Task ScoreLead(CrmDeal deal)
        {
            var aiService = new LogicCAiService();

            var score = await aiService.ScoreLeadAsync(
                deal.Title,
                deal.Notes ?? "",
                deal.Value,
                deal.ContactName ?? "Unbekannt"
            );

            MessageBox.Show(
                $"Lead-Score: {score.Score}/100\n\n" +
                $"Begründung: {score.Reasoning}\n\n" +
                $"Nächste Aktion: {score.NextBestAction}",
                "Lead-Scoring"
            );
        }
        */

        // ============================================
        // BEISPIEL 5: Projektaufwand schätzen
        // ============================================
        /*
        public async Task EstimateProject(Project project)
        {
            var aiService = new LogicCAiService();

            var estimation = await aiService.EstimateProjectEffortAsync(
                project.Name,
                project.Description ?? "",
                null // oder List<string> mit Tasks
            );

            MessageBox.Show(
                $"Geschätzte Stunden: {estimation.EstimatedHours:F1}h\n" +
                $"Konfidenz: {estimation.ConfidenceLevel:P0}\n\n" +
                $"Begründung: {estimation.Reasoning}",
                "Aufwandsschätzung"
            );
        }
        */

        // ============================================
        // BEISPIEL 6: AI-Button in Dialog hinzufügen
        // ============================================
        /*
        // In TicketDetailsDialog.xaml:
        // <Button Content="🤖 KI-Assistent" Click="AiButton_Click"/>

        // In TicketDetailsDialog.xaml.cs:
        private void AiButton_Click(object sender, RoutedEventArgs e)
        {
            var aiDialog = new TicketAiAssistantDialog(ticket, comments);
            aiDialog.Owner = this;
            aiDialog.ShowDialog();
        }
        */

        // ============================================
        // BEISPIEL 7: Text verbessern
        // ============================================
        /*
        public async Task<string> ImproveText(string originalText)
        {
            var aiService = new LogicCAiService();

            var improvedText = await aiService.ImproveTextAsync(originalText);

            return improvedText;
        }
        */

        // ============================================
        // BEISPIEL 8: Sentiment analysieren
        // ============================================
        /*
        public async Task CheckSentiment(string text)
        {
            var aiService = new LogicCAiService();

            var sentiment = await aiService.AnalyzeSentimentAsync(text);

            Console.WriteLine($"Sentiment: {sentiment}");
        }
        */
    }
}
