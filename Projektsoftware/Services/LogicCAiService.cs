using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    public class LogicCAiService
    {
        private readonly HttpClient httpClient;
        private readonly LogicCConfig config;
        private readonly JsonSerializerOptions jsonOptions;

        public LogicCAiService()
        {
            config = LogicCConfig.Load();
            httpClient = new HttpClient();
            jsonOptions = CreateJsonOptions();

            if (config.IsConfigured)
            {
                ConfigureHttpClient();
            }
        }

        public LogicCAiService(string apiKey)
        {
            config = new LogicCConfig { ApiKey = apiKey };
            httpClient = new HttpClient();
            jsonOptions = CreateJsonOptions();
            ConfigureHttpClient();
        }

        private JsonSerializerOptions CreateJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        private void ConfigureHttpClient()
        {
            httpClient.BaseAddress = new Uri(config.ApiUrl);
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Projektsoftware/1.0");
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Bereinigt JSON-Antworten von Markdown-Code-Blöcken (```json ... ```)
        /// </summary>
        private string CleanJsonResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return response;

            var cleaned = response.Trim();

            // Entferne führende Markdown-Code-Blöcke
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(7);
            }
            else if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Substring(3);
            }

            // Entferne abschließende Code-Block-Marker
            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }

            return cleaned.Trim();
        }

        public bool IsConfigured => config.IsConfigured;

        // Generic Chat Completion Method
        private async Task<string> GetChatCompletionAsync(string systemPrompt, string userPrompt)
        {
            if (!config.IsConfigured)
            {
                throw new InvalidOperationException("LogicC API ist nicht konfiguriert. Bitte API-Key in den Einstellungen hinterlegen.");
            }

            var request = new LogicCChatRequest
            {
                Model = config.Model,
                MaxTokens = config.MaxTokens,
                Temperature = config.Temperature,
                Messages = new List<LogicCMessage>
                {
                    new LogicCMessage { Role = "system", Content = systemPrompt },
                    new LogicCMessage { Role = "user", Content = userPrompt }
                }
            };

            var json = JsonSerializer.Serialize(request, jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/chat/completions", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Detaillierte Fehlermeldung für besseres Debugging
                var errorMessage = $"LogicC API Fehler:\n" +
                                 $"Status: {response.StatusCode}\n" +
                                 $"URL: {httpClient.BaseAddress}/chat/completions\n" +
                                 $"Antwort: {responseContent}";
                throw new HttpRequestException(errorMessage);
            }

            // Prüfe ob die Antwort JSON ist
            if (!responseContent.TrimStart().StartsWith("{") && !responseContent.TrimStart().StartsWith("["))
            {
                throw new InvalidOperationException($"API hat kein JSON zurückgegeben. Antwort beginnt mit: {responseContent.Substring(0, Math.Min(100, responseContent.Length))}");
            }

            var chatResponse = JsonSerializer.Deserialize<LogicCChatResponse>(responseContent, jsonOptions);
            return chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }

        #region Ticket Management AI Features

        /// <summary>
        /// Kategorisiert ein Ticket automatisch basierend auf Titel und Beschreibung
        /// </summary>
        public async Task<TicketCategorizationResult> CategorizeTicketAsync(string title, string description)
        {
            var systemPrompt = @"Du bist ein Experte für IT-Support-Ticket-Klassifizierung.
Deine Aufgabe ist es, Tickets in Kategorien einzuordnen und die Priorität zu bestimmen.

Kategorien: Bug, Feature Request, Support, Configuration, Performance, Security, Documentation
Prioritäten: Low, Normal, High, Critical

Antworte im JSON-Format:
{
    ""category"": ""kategorie"",
    ""priority"": ""priorität"",
    ""confidence"": 0.95,
    ""reasoning"": ""kurze Begründung""
}";

            var userPrompt = $"Titel: {title}\n\nBeschreibung: {description}";

            var response = await GetChatCompletionAsync(systemPrompt, userPrompt);

            try
            {
                var cleanedResponse = CleanJsonResponse(response);
                return JsonSerializer.Deserialize<TicketCategorizationResult>(cleanedResponse, jsonOptions);
            }
            catch
            {
                // Fallback if JSON parsing fails
                return new TicketCategorizationResult
                {
                    Category = "Support",
                    Priority = "Normal",
                    Confidence = 0.5,
                    Reasoning = "Automatische Kategorisierung fehlgeschlagen"
                };
            }
        }

        /// <summary>
        /// Generiert Antwortvorschläge für Tickets
        /// </summary>
        public async Task<List<string>> GenerateTicketResponseSuggestionsAsync(string ticketTitle, string ticketDescription, List<string> comments = null)
        {
            var systemPrompt = @"Du bist ein hilfreicher IT-Support-Mitarbeiter.
Generiere 3 mögliche Antworten auf das Ticket - von kurz/prägnant bis ausführlich/detailliert.
Antworte als JSON-Array: [""antwort1"", ""antwort2"", ""antwort3""]";

            var conversationHistory = comments != null && comments.Any() 
                ? $"\n\nBisherige Kommentare:\n{string.Join("\n---\n", comments)}" 
                : "";

            var userPrompt = $"Ticket: {ticketTitle}\n\nBeschreibung: {ticketDescription}{conversationHistory}";

            var response = await GetChatCompletionAsync(systemPrompt, userPrompt);

            try
            {
                var cleanedResponse = CleanJsonResponse(response);
                return JsonSerializer.Deserialize<List<string>>(cleanedResponse, jsonOptions) ?? new List<string>();
            }
            catch
            {
                return new List<string> { response };
            }
        }

        /// <summary>
        /// Sucht ähnliche bereits gelöste Tickets
        /// </summary>
        public async Task<string> FindSimilarTicketSolutionsAsync(string currentTicketDescription, List<(string Title, string Solution)> historicalTickets)
        {
            if (!historicalTickets.Any()) return "Keine historischen Tickets verfügbar.";

            var systemPrompt = @"Du bist ein Experte darin, Muster in Support-Tickets zu erkennen.
Analysiere das aktuelle Ticket und finde ähnliche gelöste Tickets.
Gib konkrete Lösungsvorschläge basierend auf den historischen Daten.";

            var historicalData = string.Join("\n\n", historicalTickets.Select((t, i) => 
                $"Ticket {i + 1}:\nTitel: {t.Title}\nLösung: {t.Solution}"));

            var userPrompt = $"Aktuelles Ticket:\n{currentTicketDescription}\n\n---\n\nHistorische Tickets:\n{historicalData}";

            return await GetChatCompletionAsync(systemPrompt, userPrompt);
        }

        #endregion

        #region CRM & Lead Management AI Features

        /// <summary>
        /// Bewertet einen Lead/Deal basierend auf verschiedenen Faktoren
        /// </summary>
        public async Task<LeadScoringResult> ScoreLeadAsync(string dealTitle, string dealDescription, decimal? dealValue, string customerInfo)
        {
            var systemPrompt = @"Du bist ein Experte für Sales und Lead-Qualifizierung.
Bewerte den Lead auf einer Skala von 0-100 basierend auf:
- Klarheit der Anforderungen
- Budget/Deal-Wert
- Kundeninformationen
- Kaufbereitschaft

Antworte im JSON-Format:
{
    ""score"": 85,
    ""reasoning"": ""Begründung"",
    ""positiveFactors"": [""Faktor 1"", ""Faktor 2""],
    ""negativeFactors"": [""Faktor 1""],
    ""nextBestAction"": ""Empfohlene nächste Aktion""
}";

            var userPrompt = $@"Deal: {dealTitle}
Beschreibung: {dealDescription}
Wert: {dealValue?.ToString("C") ?? "Unbekannt"}
Kunde: {customerInfo}";

            var response = await GetChatCompletionAsync(systemPrompt, userPrompt);

            try
            {
                var cleanedResponse = CleanJsonResponse(response);
                return JsonSerializer.Deserialize<LeadScoringResult>(cleanedResponse, jsonOptions);
            }
            catch
            {
                return new LeadScoringResult
                {
                    Score = 50,
                    Reasoning = "Automatisches Scoring fehlgeschlagen",
                    PositiveFactors = new List<string>(),
                    NegativeFactors = new List<string>(),
                    NextBestAction = "Manuelle Überprüfung erforderlich"
                };
            }
        }

        /// <summary>
        /// Analysiert das Sentiment von Kundennotizen oder E-Mails
        /// </summary>
        public async Task<string> AnalyzeSentimentAsync(string text)
        {
            var systemPrompt = @"Analysiere das Sentiment des folgenden Textes.
Antworte mit einem der folgenden Werte: Positiv, Neutral, Negativ, Sehr Negativ
Füge eine kurze Begründung hinzu (max. 1 Satz).";

            return await GetChatCompletionAsync(systemPrompt, text);
        }

        #endregion

        #region Email & Communication AI Features

        /// <summary>
        /// Erstellt eine Zusammenfassung einer E-Mail mit Action Items
        /// </summary>
        public async Task<EmailSummary> SummarizeEmailAsync(string subject, string body)
        {
            var systemPrompt = @"Fasse die E-Mail zusammen und extrahiere Action Items.
Bestimme auch die Kategorie und Priorität der E-Mail.

Kategorien: Anfrage, Beschwerde, Information, Angebot, Bestellung, Support, Sonstiges
Prioritäten: Low, Normal, High, Urgent

Antworte im JSON-Format:
{
    ""summary"": ""Kurze Zusammenfassung (2-3 Sätze)"",
    ""actionItems"": [""Aufgabe 1"", ""Aufgabe 2""],
    ""sentiment"": ""Positiv/Neutral/Negativ"",
    ""category"": ""Kategorie"",
    ""priority"": ""Priorität""
}";

            var userPrompt = $"Betreff: {subject}\n\n{body}";

            var response = await GetChatCompletionAsync(systemPrompt, userPrompt);

            try
            {
                // Bereinige die Antwort von Markdown-Code-Blöcken
                var cleanedResponse = CleanJsonResponse(response);

                var summary = JsonSerializer.Deserialize<EmailSummary>(cleanedResponse, jsonOptions);

                // Fallback-Werte wenn Felder leer sind
                if (summary != null)
                {
                    if (string.IsNullOrWhiteSpace(summary.Category))
                        summary.Category = "Information";
                    if (string.IsNullOrWhiteSpace(summary.Priority))
                        summary.Priority = "Normal";
                    if (summary.ActionItems == null || !summary.ActionItems.Any())
                        summary.ActionItems = new List<string> { "Keine spezifischen Aktionen erforderlich" };
                }

                return summary;
            }
            catch (Exception ex)
            {
                return new EmailSummary
                {
                    Summary = $"Fehler bei der Zusammenfassung: {ex.Message}\n\nRoh-Antwort: {response.Substring(0, Math.Min(200, response.Length))}",
                    ActionItems = new List<string> { "Fehler bei der Verarbeitung" },
                    Sentiment = "Neutral",
                    Category = "Sonstiges",
                    Priority = "Normal"
                };
            }
        }

        /// <summary>
        /// Generiert professionelle E-Mail-Antworten
        /// </summary>
        public async Task<string> GenerateEmailResponseAsync(string originalEmail, string responseContext)
        {
            var systemPrompt = @"Generiere eine professionelle E-Mail-Antwort auf Deutsch.
Sei höflich, präzise und hilfsbereit. Verwende eine angemessene Geschäftssprache.";

            var userPrompt = $"Ursprüngliche E-Mail:\n{originalEmail}\n\nKontext für Antwort:\n{responseContext}";

            return await GetChatCompletionAsync(systemPrompt, userPrompt);
        }

        #endregion

        #region Project Management AI Features

        /// <summary>
        /// Schätzt den Zeitaufwand für ein Projekt basierend auf Beschreibung
        /// </summary>
        public async Task<ProjectEstimationResult> EstimateProjectEffortAsync(string projectTitle, string projectDescription, List<string> tasks = null)
        {
            var systemPrompt = @"Du bist ein erfahrener Projektmanager mit über 15 Jahren Erfahrung in der Zeitschätzung von IT-Projekten.

WICHTIG: Berücksichtige ALLE versteckten Aufwände:
- Planung und Konzeption (10-15% der Entwicklungszeit)
- Testing und Qualitätssicherung (20-30% der Entwicklungszeit)
- Dokumentation (10-15%)
- Meetings, Abstimmungen, Reviews (10-20%)
- Bugfixing und Nacharbeiten (15-25%)
- Integration und Deployment
- Unvorhergesehene Probleme (Buffer 20-30%)

Bei Hardware-Integration (CNC, Roboter, etc.):
- Deutlich MEHR Zeit für Tests und Validierung
- Hardware-Inkompatibilitäten und Treiberprobleme
- Kalibrierung und Feinabstimmung
- Sicherheitstests und Zertifizierungen

Schätze REALISTISCH und eher konservativ. Unterschätze NICHT!

Antworte im JSON-Format:
{
    ""estimatedHours"": 250.0,
    ""confidenceLevel"": 0.75,
    ""reasoning"": ""Detaillierte Begründung mit Aufschlüsselung"",
    ""riskFactors"": [""Risiko 1"", ""Risiko 2"", ""Risiko 3""]
}";

            var taskList = tasks != null && tasks.Any() 
                ? $"\n\nAufgaben:\n{string.Join("\n", tasks.Select((t, i) => $"{i + 1}. {t}"))}" 
                : "";

            var userPrompt = $"Projekt: {projectTitle}\n\nBeschreibung: {projectDescription}{taskList}";

            var response = await GetChatCompletionAsync(systemPrompt, userPrompt);

            try
            {
                var cleanedResponse = CleanJsonResponse(response);
                return JsonSerializer.Deserialize<ProjectEstimationResult>(cleanedResponse, jsonOptions);
            }
            catch
            {
                return new ProjectEstimationResult
                {
                    EstimatedHours = 0,
                    ConfidenceLevel = 0,
                    Reasoning = "Automatische Schätzung fehlgeschlagen",
                    RiskFactors = new List<string>()
                };
            }
        }

        /// <summary>
        /// Analysiert Meeting-Protokolle und extrahiert Zeitaufwände
        /// </summary>
        public async Task<Dictionary<string, double>> ExtractTimeEntriesFromMeetingAsync(string meetingProtocol)
        {
            var systemPrompt = @"Analysiere das Meeting-Protokoll und extrahiere erwähnte Zeitaufwände.
Antworte als JSON-Object mit Aufgabe als Key und Stunden als Value:
{
    ""Feature X entwickeln"": 8.5,
    ""Code Review"": 2.0
}";

            var response = await GetChatCompletionAsync(systemPrompt, meetingProtocol);

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, double>>(response, jsonOptions) ?? new Dictionary<string, double>();
            }
            catch
            {
                return new Dictionary<string, double>();
            }
        }

        #endregion

        #region Document Generation AI Features

        /// <summary>
        /// Verbessert/Generiert Text für Briefe und Verträge
        /// </summary>
        public async Task<string> GenerateDocumentTextAsync(DocumentGenerationRequest request)
        {
            var systemPrompt = request.DocumentType.ToLower() switch
            {
                "letter" => "Du bist ein Experte für professionelle Geschäftsbriefe. Generiere einen formellen Brief auf Deutsch.",
                "contract" => "Du bist ein Experte für Vertragsformulierungen. Generiere einen rechtssicheren Vertragstext auf Deutsch.",
                "offer" => "Du bist ein Experte für Angebotserstellung. Generiere ein professionelles Angebot auf Deutsch.",
                _ => "Generiere einen professionellen Text auf Deutsch."
            };

            var variables = request.Variables != null && request.Variables.Any()
                ? $"\n\nVariablen:\n{string.Join("\n", request.Variables.Select(kv => $"{kv.Key}: {kv.Value}"))}"
                : "";

            var userPrompt = $"Kontext:\n{request.Context}{variables}";

            return await GetChatCompletionAsync(systemPrompt, userPrompt);
        }

        /// <summary>
        /// Verbessert vorhandenen Text (Rechtschreibung, Stil, Klarheit)
        /// </summary>
        public async Task<string> ImproveTextAsync(string text)
        {
            var systemPrompt = @"Verbessere den folgenden Text:
- Korrigiere Rechtschreibung und Grammatik
- Verbessere Klarheit und Professionalität
- Behalte den ursprünglichen Sinn bei
- Antworte NUR mit dem verbesserten Text, ohne Erklärungen.";

            return await GetChatCompletionAsync(systemPrompt, text);
        }

        #endregion

        #region Generic AI Helper

        /// <summary>
        /// Generischer AI-Assistent für beliebige Fragen
        /// </summary>
        public async Task<string> AskAssistantAsync(string question, string context = null)
        {
            var systemPrompt = "Du bist ein hilfreicher Assistent für Business-Software. Antworte präzise und professionell auf Deutsch.";
            var userPrompt = context != null 
                ? $"Kontext:\n{context}\n\nFrage:\n{question}" 
                : question;

            return await GetChatCompletionAsync(systemPrompt, userPrompt);
        }

        /// <summary>
        /// Testet die Verbindung zur LogicC API
        /// </summary>
        public async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            try
            {
                if (!config.IsConfigured)
                {
                    return (false, "Konfiguration unvollständig. Bitte API-Key eingeben.");
                }

                ConfigureHttpClient();

                // Einfacher Test mit minimalem Request
                var testRequest = new LogicCChatRequest
                {
                    Model = config.Model,
                    MaxTokens = 10,
                    Temperature = 0.0,
                    Messages = new List<LogicCMessage>
                    {
                        new LogicCMessage { Role = "user", Content = "Test" }
                    }
                };

                var json = JsonSerializer.Serialize(testRequest, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Teste die Verbindung
                var response = await httpClient.PostAsync("/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"API-Fehler {response.StatusCode}: {responseContent}");
                }

                // Prüfe ob JSON zurückkommt
                if (!responseContent.TrimStart().StartsWith("{") && !responseContent.TrimStart().StartsWith("["))
                {
                    return (false, $"API gibt kein JSON zurück. URL möglicherweise falsch.\nAntwort: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}");
                }

                var chatResponse = JsonSerializer.Deserialize<LogicCChatResponse>(responseContent, jsonOptions);

                if (chatResponse?.Choices != null && chatResponse.Choices.Any())
                {
                    return (true, $"✅ Verbindung erfolgreich!\nModell: {config.Model}\nAntwort erhalten.");
                }
                else
                {
                    return (false, "API antwortet, aber keine Choices in der Antwort.");
                }
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Netzwerkfehler: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return (false, $"JSON Parse-Fehler: {ex.Message}\nDie API-URL ist möglicherweise falsch.");
            }
            catch (Exception ex)
            {
                return (false, $"Unerwarteter Fehler: {ex.Message}");
            }
        }

        #endregion
    }
}
