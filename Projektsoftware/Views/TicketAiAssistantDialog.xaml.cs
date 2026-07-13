using Projektsoftware.Models;
using Projektsoftware.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Projektsoftware.Views
{
    public partial class TicketAiAssistantDialog : Window
    {
        private readonly Ticket ticket;
        private readonly List<TicketComment> comments;
        private readonly LogicCAiService aiService;
        private string lastFunction = string.Empty;

        public event EventHandler<TicketCategorizationResult> CategorySuggested;
        public event EventHandler<string> ResponseGenerated;

        public TicketAiAssistantDialog(Ticket ticket, List<TicketComment> comments = null)
        {
            InitializeComponent();
            this.ticket = ticket;
            this.comments = comments ?? new List<TicketComment>();

            aiService = new LogicCAiService();

            HeaderText.Text = $"🤖 KI-Assistent für Ticket #{ticket.TicketNumber}";
            SubHeaderText.Text = $"Betreff: {ticket.Subject}";

            if (!aiService.IsConfigured)
            {
                ShowNotConfiguredMessage();
            }
        }

        private void ShowNotConfiguredMessage()
        {
            ResultTitleText.Text = "⚠️ LogicC AI nicht konfiguriert";
            ResultTextBox.Text = "Die LogicC AI-Integration ist noch nicht konfiguriert.\n\n" +
                                 "Bitte klicken Sie auf 'Einstellungen' und geben Sie Ihren API-Key ein.";
            ResultTextBox.Foreground = System.Windows.Media.Brushes.Orange;

            // Disable AI buttons
            CategorizeButton.IsEnabled = false;
            GenerateResponseButton.IsEnabled = false;
            FindSimilarButton.IsEnabled = false;
            EmailSuggestionButton.IsEnabled = false;
            SummarizeButton.IsEnabled = false;
        }

        private void ShowLoading(bool isLoading)
        {
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            ResultPanel.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;

            CategorizeButton.IsEnabled = !isLoading;
            GenerateResponseButton.IsEnabled = !isLoading;
            FindSimilarButton.IsEnabled = !isLoading;
            EmailSuggestionButton.IsEnabled = !isLoading;
            SummarizeButton.IsEnabled = !isLoading;
        }

        private async void CategorizeButton_Click(object sender, RoutedEventArgs e)
        {
            lastFunction = "categorize";
            ShowLoading(true);

            try
            {
                var result = await aiService.CategorizeTicketAsync(ticket.Subject, ticket.Description);

                ResultTitleText.Text = "🏷️ Kategorisierungs-Vorschlag";
                ResultTitleText.Foreground = System.Windows.Media.Brushes.LightGreen;

                var sb = new StringBuilder();
                sb.AppendLine($"Kategorie: {result.Category}");
                sb.AppendLine($"Priorität: {result.Priority}");
                sb.AppendLine($"Konfidenz: {result.Confidence:P0}");
                sb.AppendLine();
                sb.AppendLine("Begründung:");
                sb.AppendLine(result.Reasoning);

                ResultTextBox.Text = sb.ToString();
                ResultTextBox.Foreground = System.Windows.Media.Brushes.White;

                ApplyButton.Visibility = Visibility.Visible;
                ApplyButton.Tag = result; // Store result for later use
                RegenerateButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowError("Kategorisierung fehlgeschlagen", ex.Message);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async void GenerateResponseButton_Click(object sender, RoutedEventArgs e)
        {
            lastFunction = "responses";
            ShowLoading(true);

            try
            {
                var commentTexts = comments.Select(c => c.Comment).ToList();
                var suggestions = await aiService.GenerateTicketResponseSuggestionsAsync(
                    ticket.Subject, 
                    ticket.Description, 
                    commentTexts);

                ResultTitleText.Text = "💬 Antwort-Vorschläge";
                ResultTitleText.Foreground = System.Windows.Media.Brushes.LightBlue;

                var sb = new StringBuilder();
                for (int i = 0; i < suggestions.Count; i++)
                {
                    sb.AppendLine($"═══ Vorschlag {i + 1} ═══");
                    sb.AppendLine(suggestions[i]);
                    sb.AppendLine();
                }

                ResultTextBox.Text = sb.ToString();
                ResultTextBox.Foreground = System.Windows.Media.Brushes.White;

                ApplyButton.Visibility = Visibility.Collapsed;
                RegenerateButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowError("Antwortgenerierung fehlgeschlagen", ex.Message);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async void FindSimilarButton_Click(object sender, RoutedEventArgs e)
        {
            lastFunction = "similar";
            ShowLoading(true);

            try
            {
                // In a real implementation, you would fetch historical tickets from the database
                // For now, we'll use a simplified approach
                var db = new DatabaseService();
                var historicalTickets = new List<(string Title, string Solution)>();

                // You would implement a method to get resolved tickets
                // For demonstration, we'll ask the AI with empty history

                var result = await aiService.FindSimilarTicketSolutionsAsync(
                    $"{ticket.Subject}\n\n{ticket.Description}", 
                    historicalTickets);

                ResultTitleText.Text = "🔍 Ähnliche Tickets & Lösungsvorschläge";
                ResultTitleText.Foreground = System.Windows.Media.Brushes.Yellow;

                if (historicalTickets.Count == 0)
                {
                    ResultTextBox.Text = "Keine historischen Tickets gefunden.\n\n" +
                                        "Tipp: Diese Funktion wird leistungsfähiger, je mehr gelöste Tickets vorhanden sind.";
                }
                else
                {
                    ResultTextBox.Text = result;
                }

                ResultTextBox.Foreground = System.Windows.Media.Brushes.White;

                ApplyButton.Visibility = Visibility.Collapsed;
                RegenerateButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowError("Suche fehlgeschlagen", ex.Message);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async void EmailSuggestionButton_Click(object sender, RoutedEventArgs e)
        {
            lastFunction = "email";
            ShowLoading(true);

            try
            {
                var conversationContext = new StringBuilder();
                conversationContext.AppendLine($"Original-Ticket von {ticket.CustomerName}:");
                conversationContext.AppendLine(ticket.Description);

                if (comments.Any())
                {
                    conversationContext.AppendLine("\nBisherige Konversation:");
                    foreach (var comment in comments.Take(5))
                    {
                        conversationContext.AppendLine($"- {comment.Comment}");
                    }
                }

                var emailResponse = await aiService.GenerateEmailResponseAsync(
                    $"Betreff: {ticket.Subject}\n\n{ticket.Description}",
                    "Bitte erstelle eine professionelle, höfliche E-Mail-Antwort auf Deutsch.");

                ResultTitleText.Text = "📧 E-Mail-Vorschlag";
                ResultTitleText.Foreground = System.Windows.Media.Brushes.LightCoral;

                ResultTextBox.Text = emailResponse;
                ResultTextBox.Foreground = System.Windows.Media.Brushes.White;

                ApplyButton.Visibility = Visibility.Collapsed;
                RegenerateButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowError("E-Mail-Generierung fehlgeschlagen", ex.Message);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async void SummarizeButton_Click(object sender, RoutedEventArgs e)
        {
            lastFunction = "summarize";
            ShowLoading(true);

            try
            {
                var fullContext = new StringBuilder();
                fullContext.AppendLine($"Ticket: {ticket.Subject}");
                fullContext.AppendLine($"Beschreibung: {ticket.Description}");

                if (comments.Any())
                {
                    fullContext.AppendLine("\nKommentare:");
                    foreach (var comment in comments)
                    {
                        fullContext.AppendLine($"- {comment.Comment}");
                    }
                }

                var summary = await aiService.AskAssistantAsync(
                    "Fasse dieses Support-Ticket kurz und prägnant zusammen. " +
                    "Extrahiere die wichtigsten Punkte und offenen Fragen.",
                    fullContext.ToString());

                ResultTitleText.Text = "📝 Ticket-Zusammenfassung";
                ResultTitleText.Foreground = System.Windows.Media.Brushes.Cyan;

                ResultTextBox.Text = summary;
                ResultTextBox.Foreground = System.Windows.Media.Brushes.White;

                ApplyButton.Visibility = Visibility.Collapsed;
                RegenerateButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowError("Zusammenfassung fehlgeschlagen", ex.Message);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void RegenerateButton_Click(object sender, RoutedEventArgs e)
        {
            // Re-run the last function
            switch (lastFunction)
            {
                case "categorize":
                    CategorizeButton_Click(sender, e);
                    break;
                case "responses":
                    GenerateResponseButton_Click(sender, e);
                    break;
                case "similar":
                    FindSimilarButton_Click(sender, e);
                    break;
                case "email":
                    EmailSuggestionButton_Click(sender, e);
                    break;
                case "summarize":
                    SummarizeButton_Click(sender, e);
                    break;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplyButton.Tag is TicketCategorizationResult result)
            {
                CategorySuggested?.Invoke(this, result);
                MessageBox.Show("Kategorisierung wurde übernommen!", "Erfolg", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ResultTextBox.Text);
                MessageBox.Show("Text wurde in die Zwischenablage kopiert!", "Erfolg", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Kopieren: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var configDialog = new LogicCConfigDialog { Owner = this };
            if (configDialog.ShowDialog() == true)
            {
                // Reload service with new config
                var newService = new LogicCAiService();
                if (newService.IsConfigured)
                {
                    // Re-enable buttons
                    CategorizeButton.IsEnabled = true;
                    GenerateResponseButton.IsEnabled = true;
                    FindSimilarButton.IsEnabled = true;
                    EmailSuggestionButton.IsEnabled = true;
                    SummarizeButton.IsEnabled = true;

                    ResultTitleText.Text = "✅ Konfiguration aktualisiert";
                    ResultTextBox.Text = "LogicC AI ist jetzt einsatzbereit!";
                    ResultTextBox.Foreground = System.Windows.Media.Brushes.LightGreen;
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowError(string title, string message)
        {
            ResultTitleText.Text = $"❌ {title}";
            ResultTitleText.Foreground = System.Windows.Media.Brushes.Red;
            ResultTextBox.Text = message;
            ResultTextBox.Foreground = System.Windows.Media.Brushes.Red;
            ApplyButton.Visibility = Visibility.Collapsed;
            RegenerateButton.Visibility = Visibility.Collapsed;
        }
    }
}
