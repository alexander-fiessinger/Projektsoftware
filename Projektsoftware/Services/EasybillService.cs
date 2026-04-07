using Projektsoftware.Models;
using Projektsoftware.Converters;
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
    public class EasybillService
    {
        private readonly HttpClient httpClient;
        private readonly EasybillConfig config;
        private readonly JsonSerializerOptions jsonOptions;

        public EasybillService()
        {
            config = EasybillConfig.Load();
            httpClient = new HttpClient();
            jsonOptions = CreateJsonOptions();

            if (config.IsConfigured)
            {
                ConfigureHttpClient();
            }
        }

        public EasybillService(string email, string apiKey)
        {
            config = new EasybillConfig { Email = email, ApiKey = apiKey };
            httpClient = new HttpClient();
            jsonOptions = CreateJsonOptions();
            ConfigureHttpClient();
        }

        private JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            // Register custom converters for handling problematic values
            options.Converters.Add(new NullableLongConverter());
            options.Converters.Add(new NullableDecimalConverter());
            options.Converters.Add(new DecimalConverter());
            options.Converters.Add(new EasybillPriceConverter());

            return options;
        }

        private void ConfigureHttpClient()
        {
            // Wichtig: BaseAddress MUSS mit "/" enden, sonst wird der Pfad Ã¼berschrieben!
            var baseUrl = config.ApiUrl.TrimEnd('/') + "/";
            httpClient.BaseAddress = new Uri(baseUrl);

            // Easybill Basic Authentication: base64(<email>:<api_key>)
            var credentials = $"{config.Email}:{config.ApiKey}";
            var base64Credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(credentials));
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Basic", base64Credentials);

            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // User-Agent hinzufÃ¼gen (manche APIs benÃ¶tigen das)
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Projektsoftware/2.0");
        }

        public bool IsConfigured => config.IsConfigured;

        /// <summary>
        /// Testet die API-Verbindung
        /// </summary>
        public async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            try
            {
                // Debug: Zeige die URL und Header
                var fullUrl = httpClient.BaseAddress + "customers?limit=1";
                var authHeader = httpClient.DefaultRequestHeaders.Authorization?.ToString();
                System.Diagnostics.Debug.WriteLine($"Testing URL: {fullUrl}");
                System.Diagnostics.Debug.WriteLine($"Auth Header: {authHeader}");

                var response = await httpClient.GetAsync("customers?limit=1");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Success! Response: {content}");
                    return (true, "Verbindung erfolgreich");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var debugInfo = $"URL: {fullUrl}\nAuth: {authHeader}\nResponse: {errorContent}";
                    System.Diagnostics.Debug.WriteLine($"Error! {debugInfo}");
                    return (false, $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}\n{errorContent}\n\nDebug:\n{debugInfo}");
                }
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Netzwerkfehler: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Fehler: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Holt alle Kunden von Easybill
        /// </summary>
        public async Task<List<EasybillCustomer>> GetAllCustomersAsync()
        {
            var allCustomers = new List<EasybillCustomer>();
            int page = 1;
            int totalPages = 1;

            try
            {
                while (page <= totalPages)
                {
                    var response = await httpClient.GetAsync($"customers?page={page}&limit=100");

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"JSON Response (Page {page}): {json.Substring(0, Math.Min(500, json.Length))}...");

                    var result = JsonSerializer.Deserialize<EasybillCustomerList>(json, jsonOptions);

                    if (result?.Items != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Deserialized {result.Items.Length} customers from page {page}");
                        allCustomers.AddRange(result.Items);
                        totalPages = result.Pages;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"WARNING: result or Items is null! result={result}, Items={(result?.Items == null ? "null" : "not null")}");
                    }

                    page++;
                }

                System.Diagnostics.Debug.WriteLine($"Total customers loaded: {allCustomers.Count}");
                return allCustomers;
            }
            catch (JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"JSON Deserialization Error: {jsonEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Path: {jsonEx.Path}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {jsonEx.StackTrace}");
                throw new Exception($"Fehler beim Parsen der Kunden-Daten: {jsonEx.Message} (Path: {jsonEx.Path})", jsonEx);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllCustomersAsync: {ex.Message}");
                throw new Exception($"Fehler beim Abrufen der Kunden: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Holt einen einzelnen Kunden
        /// </summary>
        public async Task<EasybillCustomer> GetCustomerAsync(long customerId)
        {
            try
            {
                var response = await httpClient.GetAsync($"customers/{customerId}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Kunde nicht gefunden: {customerId}");
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillCustomer>(json, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen des Kunden: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt einen neuen Kunden in Easybill
        /// </summary>
        public async Task<EasybillCustomer> CreateCustomerAsync(EasybillCustomer customer)
        {
            try
            {
                var json = JsonSerializer.Serialize(customer, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("customers", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillCustomer>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Kunden: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Aktualisiert einen Kunden in Easybill
        /// </summary>
        public async Task<EasybillCustomer> UpdateCustomerAsync(long customerId, EasybillCustomer customer)
        {
            try
            {
                var json = JsonSerializer.Serialize(customer, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"customers/{customerId}", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillCustomer>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Aktualisieren des Kunden: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// LÃ¶scht einen Kunden in Easybill
        /// </summary>
        public async Task DeleteCustomerAsync(long customerId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"customers/{customerId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim LÃ¶schen des Kunden: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sucht Kunden nach Name/Firma
        /// </summary>
        public async Task<List<EasybillCustomer>> SearchCustomersAsync(string searchTerm)
        {
            try
            {
                var allCustomers = await GetAllCustomersAsync();
                
                return allCustomers.FindAll(c => 
                    c.CompanyName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                    c.FirstName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                    c.LastName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                    c.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler bei der Suche: {ex.Message}", ex);
            }
        }

        #region Position/Abrechnung Methods

        /// <summary>
        /// Erstellt eine Position (Leistung) in Easybill aus einem Zeiteintrag
        /// </summary>
        public async Task<EasybillPosition> CreatePositionFromTimeEntryAsync(TimeEntry timeEntry, decimal hourlyRate)
        {
            try
            {
                // Generiere eine eindeutige Positionsnummer basierend auf Datum und ID
                var positionNumber = $"ZE-{timeEntry.Date:yyyyMMdd}-{timeEntry.Id}";

                var position = new EasybillPosition
                {
                    Type = "SERVICE",
                    Number = positionNumber,
                    Description = $"{timeEntry.Activity ?? "Zeiterfassung"} - {timeEntry.Date:dd.MM.yyyy}\n{timeEntry.Description}",
                    SalePrice = hourlyRate,
                    Quantity = (decimal)timeEntry.Duration.TotalHours,
                    Unit = "Stunden",
                    CustomerId = timeEntry.EasybillCustomerId,
                    Note = $"Mitarbeiter: {timeEntry.EmployeeName}\nProjekt: {timeEntry.ProjectName}",
                    ExportIdentifier = $"TimeEntry-{timeEntry.Id}"
                };

                var json = JsonSerializer.Serialize(position);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("positions", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillPosition>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen der Position: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exportiert mehrere ZeiteintrÃ¤ge als Positionen zu einem Kunden
        /// </summary>
        public async Task<List<EasybillPosition>> ExportTimeEntriesToPositionsAsync(List<TimeEntry> timeEntries, decimal hourlyRate)
        {
            var exportedPositions = new List<EasybillPosition>();

            foreach (var entry in timeEntries)
            {
                if (!entry.EasybillCustomerId.HasValue)
                {
                    throw new Exception($"Zeiteintrag '{entry.Activity}' hat keinen Easybill-Kunden zugewiesen!");
                }

                var position = await CreatePositionFromTimeEntryAsync(entry, hourlyRate);
                exportedPositions.Add(position);
            }

            return exportedPositions;
        }

        /// <summary>
        /// Holt alle Positionen eines Kunden
        /// </summary>
        public async Task<List<EasybillPosition>> GetCustomerPositionsAsync(long customerId)
        {
            try
            {
                var allPositions = new List<EasybillPosition>();
                int page = 1;
                int totalPages = 1;

                while (page <= totalPages)
                {
                    var response = await httpClient.GetAsync($"positions?customer_id={customerId}&page={page}&limit=100");

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<EasybillPositionList>(json, jsonOptions);

                    if (result?.Items != null)
                    {
                        allPositions.AddRange(result.Items);
                        totalPages = result.Pages;
                    }

                    page++;
                }

                return allPositions;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Positionen: {ex.Message}", ex);
            }
        }

        #endregion

        #region Project Methods

        /// <summary>
        /// Holt alle Projekte von Easybill
        /// </summary>
        public async Task<List<EasybillProject>> GetAllProjectsAsync()
        {
            var allProjects = new List<EasybillProject>();
            int page = 1;
            int totalPages = 1;

            try
            {
                while (page <= totalPages)
                {
                    var response = await httpClient.GetAsync($"projects?page={page}&limit=100");

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<EasybillProjectList>(json, jsonOptions);

                    if (result?.Items != null)
                    {
                        allProjects.AddRange(result.Items);
                        totalPages = result.Pages;
                    }

                    page++;
                }

                return allProjects;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Projekte: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt ein Projekt in Easybill
        /// </summary>
        public async Task<EasybillProject> CreateProjectAsync(EasybillProject project)
        {
            try
            {
                var json = JsonSerializer.Serialize(project);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("projects", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillProject>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Projekts: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Holt Projekte eines bestimmten Kunden
        /// </summary>
        public async Task<List<EasybillProject>> GetProjectsByCustomerAsync(long customerId)
        {
            try
            {
                var allProjects = await GetAllProjectsAsync();
                return allProjects.FindAll(p => p.CustomerId == customerId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Kundenprojekte: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Aktualisiert ein Projekt in Easybill
        /// </summary>
        public async Task<EasybillProject> UpdateProjectAsync(long projectId, EasybillProject project)
        {
            try
            {
                var json = JsonSerializer.Serialize(project);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"projects/{projectId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillProject>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Aktualisieren des Projekts: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Löscht ein Projekt in Easybill
        /// </summary>
        public async Task DeleteProjectAsync(long projectId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"projects/{projectId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Löschen des Projekts: {ex.Message}", ex);
            }
        }

        #endregion

        #region Time Tracking Methods

        /// <summary>
        /// Erstellt eine Zeiterfassung in Easybill aus einem Zeiteintrag
        /// </summary>
        public async Task<EasybillTimeTracking> CreateTimeTrackingFromEntryAsync(TimeEntry timeEntry, long easybillProjectId, decimal hourlyRate)
        {
            try
            {
                // Berechne Start- und Endzeit basierend auf Datum und Duration
                var startDateTime = timeEntry.Date.Date.AddHours(9); // Standard: 9:00 Uhr
                var endDateTime = startDateTime.Add(timeEntry.Duration);

                // Dauer in Minuten fÃ¼r timer_value
                var durationInMinutes = (int)timeEntry.Duration.TotalMinutes;

                // Easybill erwartet Stundensatz in CENT (100 â‚¬ = 10000 Cent)
                var hourlyRateInCents = (int)(hourlyRate * 100);

                // DEBUG: Eingabewerte loggen
                System.Diagnostics.Debug.WriteLine($"=== TIME TRACKING EXPORT ===");
                System.Diagnostics.Debug.WriteLine($"Stundensatz: {hourlyRate} â‚¬ â†’ {hourlyRateInCents} Cent");
                System.Diagnostics.Debug.WriteLine($"Dauer: {timeEntry.Duration} ({durationInMinutes} Minuten)");
                System.Diagnostics.Debug.WriteLine($"Von: {startDateTime:yyyy-MM-dd HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine($"Bis: {endDateTime:yyyy-MM-dd HH:mm:ss}");

                var timeTracking = new EasybillTimeTracking
                {
                    DateFrom = startDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    DateThru = endDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    Description = string.IsNullOrWhiteSpace(timeEntry.Activity) ? "Zeiterfassung" : timeEntry.Activity,
                    ProjectId = easybillProjectId,
                    HourlyRate = hourlyRateInCents,
                    TimerValue = durationInMinutes,
                    Note = string.IsNullOrWhiteSpace(timeEntry.Description) 
                        ? $"Mitarbeiter: {timeEntry.EmployeeName}\nProjekt: {timeEntry.ProjectName}\nExport-ID: TimeEntry-{timeEntry.Id}"
                        : $"{timeEntry.Description}\n\nMitarbeiter: {timeEntry.EmployeeName}\nProjekt: {timeEntry.ProjectName}\nExport-ID: TimeEntry-{timeEntry.Id}"
                };

                // Kultur-invariante JSON-Serialisierung (Dezimalpunkt statt Komma)
                var options = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(timeTracking, options);
                System.Diagnostics.Debug.WriteLine($"Sending time tracking JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("time-trackings", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Error response: {error}");
                    throw new Exception($"API-Fehler beim Erstellen der Zeiterfassung: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillTimeTracking>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen der Zeiterfassung: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exportiert mehrere ZeiteintrÃ¤ge als Zeiterfassungen zu einem Easybill-Projekt
        /// </summary>
        public async Task<List<EasybillTimeTracking>> ExportTimeEntriesToProjectAsync(List<TimeEntry> timeEntries, long easybillProjectId, decimal hourlyRate)
        {
            var exportedTrackings = new List<EasybillTimeTracking>();

            foreach (var entry in timeEntries)
            {
                var tracking = await CreateTimeTrackingFromEntryAsync(entry, easybillProjectId, hourlyRate);
                exportedTrackings.Add(tracking);
            }

            return exportedTrackings;
        }

        /// <summary>
        /// Holt alle Zeiterfassungen eines Projekts
        /// </summary>
        public async Task<List<EasybillTimeTracking>> GetProjectTimeTrackingsAsync(long projectId)
        {
            try
            {
                var allTrackings = new List<EasybillTimeTracking>();
                int page = 1;
                int totalPages = 1;

                while (page <= totalPages)
                {
                    var response = await httpClient.GetAsync($"time-trackings?project_id={projectId}&page={page}&limit=100");

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<EasybillTimeTrackingList>(json, jsonOptions);

                    if (result?.Items != null)
                    {
                        allTrackings.AddRange(result.Items);
                        totalPages = result.Pages;
                    }

                    page++;
                }

                return allTrackings;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Zeiterfassungen: {ex.Message}", ex);
            }
        }

        #endregion

        #region Document Methods

        /// <summary>
        /// Holt alle Dokumente (Rechnungen, Angebote, etc.)
        /// </summary>
        public async Task<List<EasybillDocument>> GetAllDocumentsAsync(string type = null)
        {
            var allDocuments = new List<EasybillDocument>();

            try
            {
                int page = 1;
                int totalPages = 1;

                while (page <= totalPages)
                {
                    var url = $"documents?page={page}&limit=100";
                    if (!string.IsNullOrEmpty(type))
                    {
                        url += $"&type={type}";
                    }

                    var response = await httpClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                    }

                    var json = await response.Content.ReadAsStringAsync();

                    var result = JsonSerializer.Deserialize<EasybillDocumentList>(json, jsonOptions);

                    if (result?.Items != null)
                    {
                        allDocuments.AddRange(result.Items);
                        totalPages = result.Pages;
                    }

                    page++;
                }

                return allDocuments;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Dokumente: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Holt ein einzelnes Dokument
        /// </summary>
        public async Task<EasybillDocument> GetDocumentAsync(long documentId)
        {
            try
            {
                var response = await httpClient.GetAsync($"documents/{documentId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillDocument>(json, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen des Dokuments: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt ein Dokument (Rechnung, Angebot, etc.)
        /// </summary>
        public async Task<EasybillDocument> CreateDocumentAsync(EasybillDocument document)
        {
            try
            {
                var json = JsonSerializer.Serialize(document, jsonOptions);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("documents", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillDocument>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Dokuments: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Aktualisiert ein Dokument
        /// </summary>
        public async Task<EasybillDocument> UpdateDocumentAsync(long documentId, EasybillDocument document)
        {
            try
            {
                var json = JsonSerializer.Serialize(document, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"documents/{documentId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillDocument>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Aktualisieren des Dokuments: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// LÃ¶scht ein Dokument
        /// </summary>
        public async Task DeleteDocumentAsync(long documentId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"documents/{documentId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim LÃ¶schen des Dokuments: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Dokument abschlieÃŸen (aus Entwurf entfernen)
        /// </summary>
        public async Task<EasybillDocument> FinalizeDocumentAsync(long documentId)
        {
            try
            {
                var response = await httpClient.PutAsync($"documents/{documentId}/done", null);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillDocument>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim AbschlieÃŸen des Dokuments: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Dokument versenden per E-Mail
        /// </summary>
        public async Task SendDocumentAsync(long documentId, string to, string subject, string message, string cc = null, string bcc = null)
        {
            try
            {
                var emailData = new
                {
                    to,
                    subject,
                    message,
                    cc,
                    bcc
                };

                var json = JsonSerializer.Serialize(emailData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"documents/{documentId}/send/email", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Versenden des Dokuments: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// PDF eines Dokuments herunterladen
        /// </summary>
        public async Task<byte[]> DownloadDocumentPdfAsync(long documentId)
        {
            try
            {
                var response = await httpClient.GetAsync($"documents/{documentId}/pdf");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Download des PDFs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Dokument als bezahlt markieren (erstellt eine Zahlung über den vollen Bruttobetrag)
        /// </summary>
        public async Task<EasybillDocument> MarkDocumentAsPaidAsync(long documentId, string paidAt = null)
        {
            try
            {
                var paidDate = paidAt ?? DateTime.Now.ToString("yyyy-MM-dd");

                var document = await GetDocumentAsync(documentId);

                var payment = new EasybillPayment
                {
                    DocumentId = documentId,
                    Amount = document.TotalGross ?? 0m,
                    Type = "BANK_TRANSFER",
                    PaymentAt = paidDate
                };

                var json = JsonSerializer.Serialize(payment, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("document-payments?paid=true", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                return await GetDocumentAsync(documentId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Markieren als bezahlt: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Dokument stornieren
        /// </summary>
        public async Task<EasybillDocument> CancelDocumentAsync(long documentId)
        {
            try
            {
                var response = await httpClient.PostAsync($"documents/{documentId}/cancel", null);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillDocument>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Stornieren des Dokuments: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Konvertiert ein bestehendes Dokument in einen anderen Typ (z.B. Angebot → Auftragsbestätigung)
        /// </summary>
        public async Task<EasybillDocument> ConvertDocumentAsync(EasybillDocument sourceDocument, string targetType, bool isDraft = true)
        {
            try
            {
                var items = sourceDocument.Items?.Select((item, index) => new EasybillDocumentItem
                {
                    Number = item.Number,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Type = item.Type,
                    Position = index + 1,
                    SinglePriceNet = item.SinglePriceNet,
                    VatPercent = item.VatPercent,
                    Discount = item.Discount,
                    DiscountType = item.DiscountType,
                }).ToArray();

                var newDoc = new EasybillDocument
                {
                    Type = targetType,
                    CustomerId = sourceDocument.CustomerId,
                    DocumentDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    Subject = sourceDocument.Subject,
                    Text = sourceDocument.Text,
                    TextSuffix = sourceDocument.TextSuffix,
                    Discount = sourceDocument.Discount,
                    DiscountType = sourceDocument.DiscountType,
                    IsDraft = isDraft,
                    Items = items,
                };

                if (targetType == "INVOICE")
                    newDoc.DueInDays = 14;

                return await CreateDocumentAsync(newDoc);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Konvertieren des Dokuments: {ex.Message}", ex);
            }
        }

        #endregion

        #region Product/Position Methods

        /// <summary>
        /// Holt alle Produkte/Artikel
        /// </summary>
        public async Task<List<EasybillProduct>> GetAllProductsAsync()
        {
            var allProducts = new List<EasybillProduct>();

            try
            {
                int page = 1;
                int totalPages = 1;

                while (page <= totalPages)
                {
                    var response = await httpClient.GetAsync($"positions?page={page}&limit=100");

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<EasybillProductList>(json, jsonOptions);

                    if (result?.Items != null)
                    {
                        allProducts.AddRange(result.Items);
                        totalPages = result.Pages;
                    }

                    page++;
                }

                return allProducts;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Produkte: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Holt ein einzelnes Produkt
        /// </summary>
        public async Task<EasybillProduct> GetProductAsync(long productId)
        {
            try
            {
                var response = await httpClient.GetAsync($"positions/{productId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillProduct>(json, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen des Produkts: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt ein Produkt/Artikel
        /// </summary>
        public async Task<EasybillProduct> CreateProductAsync(EasybillProduct product)
        {
            try
            {
                var json = JsonSerializer.Serialize(product, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("positions", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillProduct>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Produkts: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Aktualisiert ein Produkt
        /// </summary>
        public async Task<EasybillProduct> UpdateProductAsync(long productId, EasybillProduct product)
        {
            try
            {
                var json = JsonSerializer.Serialize(product, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"positions/{productId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillProduct>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Aktualisieren des Produkts: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// LÃ¶scht ein Produkt
        /// </summary>
        public async Task DeleteProductAsync(long productId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"positions/{productId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim LÃ¶schen des Produkts: {ex.Message}", ex);
            }
        }

        #endregion

        #region Payment Methods

        /// <summary>
        /// Holt alle Zahlungen
        /// </summary>
        public async Task<List<EasybillPayment>> GetAllPaymentsAsync()
        {
            var allPayments = new List<EasybillPayment>();

            try
            {
                int page = 1;
                int totalPages = 1;

                while (page <= totalPages)
                {
                    var response = await httpClient.GetAsync($"document-payments?page={page}&limit=100");

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<EasybillPaymentList>(json, jsonOptions);

                    if (result?.Items != null)
                    {
                        allPayments.AddRange(result.Items);
                        totalPages = result.Pages;
                    }

                    page++;
                }

                return allPayments;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Zahlungen: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Holt Zahlungen fÃ¼r ein bestimmtes Dokument
        /// </summary>
        public async Task<List<EasybillPayment>> GetPaymentsByDocumentAsync(long documentId)
        {
            try
            {
                var allPayments = await GetAllPaymentsAsync();
                return allPayments.FindAll(p => p.DocumentId == documentId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Dokumentzahlungen: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt eine Zahlung
        /// </summary>
        public async Task<EasybillPayment> CreatePaymentAsync(EasybillPayment payment)
        {
            try
            {
                var json = JsonSerializer.Serialize(payment);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("document-payments", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillPayment>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen der Zahlung: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// LÃ¶scht eine Zahlung
        /// </summary>
        public async Task DeletePaymentAsync(long paymentId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"document-payments/{paymentId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim LÃ¶schen der Zahlung: {ex.Message}", ex);
            }
        }

        #endregion

        #region Contact Methods

        /// <summary>
        /// Holt alle Kontakte eines Kunden
        /// </summary>
        public async Task<List<EasybillContact>> GetContactsByCustomerAsync(long customerId)
        {
            try
            {
                var response = await httpClient.GetAsync($"customers/{customerId}/contacts");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var contacts = JsonSerializer.Deserialize<EasybillContact[]>(json, jsonOptions);
                return contacts?.ToList() ?? new List<EasybillContact>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Kontakte: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt einen Kontakt fÃ¼r einen Kunden
        /// </summary>
        public async Task<EasybillContact> CreateContactAsync(EasybillContact contact)
        {
            try
            {
                var json = JsonSerializer.Serialize(contact);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"customers/{contact.CustomerId}/contacts", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillContact>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Kontakts: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Aktualisiert einen Kontakt
        /// </summary>
        public async Task<EasybillContact> UpdateContactAsync(long customerId, long contactId, EasybillContact contact)
        {
            try
            {
                var json = JsonSerializer.Serialize(contact);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"customers/{customerId}/contacts/{contactId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillContact>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Aktualisieren des Kontakts: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// LÃ¶scht einen Kontakt
        /// </summary>
        public async Task DeleteContactAsync(long customerId, long contactId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"customers/{customerId}/contacts/{contactId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim LÃ¶schen des Kontakts: {ex.Message}", ex);
            }
        }

        #endregion

        #region Task Methods

        /// <summary>
        /// Holt alle Aufgaben
        /// </summary>
        public async Task<List<EasybillTask>> GetAllTasksAsync()
        {
            try
            {
                var response = await httpClient.GetAsync("tasks");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var tasks = JsonSerializer.Deserialize<EasybillTask[]>(json, jsonOptions);
                return tasks?.ToList() ?? new List<EasybillTask>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Aufgaben: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt eine Aufgabe
        /// </summary>
        public async Task<EasybillTask> CreateTaskAsync(EasybillTask task)
        {
            try
            {
                var json = JsonSerializer.Serialize(task);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("tasks", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillTask>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen der Aufgabe: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Aktualisiert eine Aufgabe
        /// </summary>
        public async Task<EasybillTask> UpdateTaskAsync(long taskId, EasybillTask task)
        {
            try
            {
                var json = JsonSerializer.Serialize(task);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"tasks/{taskId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillTask>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Aktualisieren der Aufgabe: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// LÃ¶scht eine Aufgabe
        /// </summary>
        public async Task DeleteTaskAsync(long taskId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"tasks/{taskId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim LÃ¶schen der Aufgabe: {ex.Message}", ex);
            }
        }

        #endregion

        #region Attachment Methods

        /// <summary>
        /// LÃ¤dt einen Anhang zu einem Dokument hoch
        /// </summary>
        public async Task<EasybillAttachment> UploadAttachmentAsync(long documentId, string fileName, byte[] fileData)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(fileData);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(fileContent, "file", fileName);

                var response = await httpClient.PostAsync($"documents/{documentId}/attachments", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillAttachment>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Hochladen des Anhangs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Holt alle AnhÃ¤nge eines Dokuments
        /// </summary>
        public async Task<List<EasybillAttachment>> GetAttachmentsByDocumentAsync(long documentId)
        {
            try
            {
                var response = await httpClient.GetAsync($"documents/{documentId}/attachments");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var attachments = JsonSerializer.Deserialize<EasybillAttachment[]>(json, jsonOptions);
                return attachments?.ToList() ?? new List<EasybillAttachment>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der AnhÃ¤nge: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// LÃ¶scht einen Anhang
        /// </summary>
        public async Task DeleteAttachmentAsync(long documentId, long attachmentId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"documents/{documentId}/attachments/{attachmentId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim LÃ¶schen des Anhangs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// LÃ¤dt einen Anhang herunter
        /// </summary>
        public async Task<byte[]> DownloadAttachmentAsync(long documentId, long attachmentId)
        {
            try
            {
                var response = await httpClient.GetAsync($"documents/{documentId}/attachments/{attachmentId}/download");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Herunterladen des Anhangs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lädt eine Datei als globalen Beleg (ohne Kontext) hoch – erscheint in Easybill unter Belege → Uploads
        /// </summary>
        public async Task<EasybillAttachment> UploadGlobalAttachmentAsync(string fileName, byte[] fileData)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(fileData);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                content.Add(fileContent, "file", fileName);

                var response = await httpClient.PostAsync("attachments", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillAttachment>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Hochladen des Belegs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Aktualisiert einen globalen Anhang (z.B. customer_id zuweisen, um ihn einem Lieferanten zuzuordnen)
        /// </summary>
        public async Task<EasybillAttachment> UpdateAttachmentAsync(long attachmentId, long? customerId)
        {
            try
            {
                var attachment = new EasybillAttachment { CustomerId = customerId };
                var json = JsonSerializer.Serialize(attachment, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"attachments/{attachmentId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson2 = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillAttachment>(resultJson2, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Aktualisieren des Anhangs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Löscht einen globalen Anhang
        /// </summary>
        public async Task DeleteGlobalAttachmentAsync(long attachmentId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"attachments/{attachmentId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Löschen des Anhangs: {ex.Message}", ex);
            }
        }

        #endregion

        #region Stock Methods

        /// <summary>
        /// Holt den Lagerbestand fÃ¼r ein Produkt
        /// </summary>
        public async Task<List<EasybillStock>> GetStockByProductAsync(long productId)
        {
            try
            {
                var response = await httpClient.GetAsync($"stocks?position_id={productId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var stocks = JsonSerializer.Deserialize<EasybillStock[]>(json, jsonOptions);
                return stocks?.ToList() ?? new List<EasybillStock>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen des Lagerbestands: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt einen Lagereintrag
        /// </summary>
        public async Task<EasybillStock> CreateStockAsync(EasybillStock stock)
        {
            try
            {
                var json = JsonSerializer.Serialize(stock);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("stocks", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillStock>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Lagereintrags: {ex.Message}", ex);
            }
        }

        #endregion

        #region PDF Template Methods

        /// <summary>
        /// Holt alle PDF-Vorlagen
        /// </summary>
        public async Task<List<EasybillPdfTemplate>> GetAllPdfTemplatesAsync()
        {
            try
            {
                var response = await httpClient.GetAsync("pdf-templates");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var templates = JsonSerializer.Deserialize<EasybillPdfTemplate[]>(json, jsonOptions);
                return templates?.ToList() ?? new List<EasybillPdfTemplate>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der PDF-Vorlagen: {ex.Message}", ex);
            }
        }

        #endregion

        #region Text Template Methods

        /// <summary>
        /// Holt alle Text-Vorlagen
        /// </summary>
        public async Task<List<EasybillTextTemplate>> GetAllTextTemplatesAsync()
        {
            try
            {
                var response = await httpClient.GetAsync("text-templates");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var templates = JsonSerializer.Deserialize<EasybillTextTemplate[]>(json, jsonOptions);
                return templates?.ToList() ?? new List<EasybillTextTemplate>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Text-Vorlagen: {ex.Message}", ex);
            }
        }

        #endregion

        #region Discount Methods

        /// <summary>
        /// Holt alle Rabatte fÃ¼r einen Kunden
        /// </summary>
        public async Task<List<EasybillDiscount>> GetDiscountsByCustomerAsync(long customerId)
        {
            try
            {
                var response = await httpClient.GetAsync($"discounts/position?customer_id={customerId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var discounts = JsonSerializer.Deserialize<EasybillDiscount[]>(json, jsonOptions);
                return discounts?.ToList() ?? new List<EasybillDiscount>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Rabatte: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt einen Rabatt
        /// </summary>
        public async Task<EasybillDiscount> CreateDiscountAsync(EasybillDiscount discount)
        {
            try
            {
                var json = JsonSerializer.Serialize(discount);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("discounts/position", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillDiscount>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Rabatts: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// LÃ¶scht einen Rabatt
        /// </summary>
        public async Task DeleteDiscountAsync(long discountId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"discounts/position/{discountId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim LÃ¶schen des Rabatts: {ex.Message}", ex);
            }
        }

        #endregion

        #region SEPA Mandate Methods

        /// <summary>
        /// Holt alle SEPA-Mandate fÃ¼r einen Kunden
        /// </summary>
        public async Task<List<EasybillSepaMandate>> GetSepaMandatesByCustomerAsync(long customerId)
        {
            try
            {
                var response = await httpClient.GetAsync($"sepa-mandates?customer_id={customerId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var mandates = JsonSerializer.Deserialize<EasybillSepaMandate[]>(json, jsonOptions);
                return mandates?.ToList() ?? new List<EasybillSepaMandate>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der SEPA-Mandate: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt ein SEPA-Mandat
        /// </summary>
        public async Task<EasybillSepaMandate> CreateSepaMandateAsync(EasybillSepaMandate mandate)
        {
            try
            {
                var json = JsonSerializer.Serialize(mandate);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("sepa-mandates", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillSepaMandate>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des SEPA-Mandats: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// LÃ¶scht ein SEPA-Mandat
        /// </summary>
        public async Task DeleteSepaMandateAsync(long mandateId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"sepa-mandates/{mandateId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim LÃ¶schen des SEPA-Mandats: {ex.Message}", ex);
            }
        }

        #endregion

        #region Webhook Methods

        /// <summary>
        /// Holt alle Webhooks
        /// </summary>
        public async Task<List<EasybillWebhook>> GetAllWebhooksAsync()
        {
            try
            {
                var response = await httpClient.GetAsync("webhooks");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var webhooks = JsonSerializer.Deserialize<EasybillWebhook[]>(json, jsonOptions);
                return webhooks?.ToList() ?? new List<EasybillWebhook>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Webhooks: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt einen Webhook
        /// </summary>
        public async Task<EasybillWebhook> CreateWebhookAsync(EasybillWebhook webhook)
        {
            try
            {
                var json = JsonSerializer.Serialize(webhook);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("webhooks", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillWebhook>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Webhooks: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Aktualisiert einen Webhook
        /// </summary>
        public async Task<EasybillWebhook> UpdateWebhookAsync(long webhookId, EasybillWebhook webhook)
        {
            try
            {
                var json = JsonSerializer.Serialize(webhook);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"webhooks/{webhookId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillWebhook>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Aktualisieren des Webhooks: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// LÃ¶scht einen Webhook
        /// </summary>
        public async Task DeleteWebhookAsync(long webhookId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"webhooks/{webhookId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim LÃ¶schen des Webhooks: {ex.Message}", ex);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Konvertiert ein lokales TimeEntry-Projekt zu einem Easybill-Dokument (Rechnung)
        /// </summary>
        public async Task<EasybillDocument> CreateInvoiceFromProjectAsync(
            Project project, 
            List<TimeEntry> timeEntries, 
            decimal hourlyRate,
            string invoiceText = null,
            int dueInDays = 14,
            bool isDraft = false,
            int vatPercent = 19,
            string vatSuffix = null)
        {
            try
            {
                if (!project.EasybillCustomerId.HasValue)
                {
                    throw new Exception("Projekt hat keine Easybill-Kunden-ID!");
                }

                // Erstelle Positionen aus Zeiteinträgen
                var items = new List<EasybillDocumentItem>();
                int position = 1;

                foreach (var entry in timeEntries)
                {
                    var hours = (decimal)entry.Duration.TotalHours;
                    var singlePriceNet = hourlyRate;
                    var totalPriceNet = hours * singlePriceNet;

                    items.Add(new EasybillDocumentItem
                    {
                        Type = "POSITION",
                        Position = position++,
                        Number = $"ZE-{entry.Date:yyyyMMdd}",
                        Description = $"{entry.Activity ?? "Zeiterfassung"} - {entry.Date:dd.MM.yyyy}\n{entry.Description}",
                        Quantity = hours,
                        Unit = "Stunden",
                        SinglePriceNet = singlePriceNet,
                        VatPercent = vatPercent,
                        TotalPriceNet = totalPriceNet,
                        ExportIdentifier = $"TimeEntry-{entry.Id}"
                    });
                }

                // Standard-Schlusstext oder steuerrechtlichen Suffix verwenden
                var defaultSuffix = "Wir bedanken uns für Ihr Vertrauen und freuen uns auf die weitere Zusammenarbeit.";
                var textSuffix = !string.IsNullOrEmpty(vatSuffix)
                    ? $"{vatSuffix}\n\n{defaultSuffix}"
                    : defaultSuffix;

                // Erstelle Rechnung
                var invoice = new EasybillDocument
                {
                    Type = "INVOICE",
                    CustomerId = project.EasybillCustomerId.Value,
                    ProjectId = project.EasybillProjectId,
                    DocumentDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    Title = $"Rechnung für Projekt: {project.Name}",
                    Subject = $"Leistungen {project.Name}",
                    Text = invoiceText ?? $"Vielen Dank für Ihren Auftrag.\n\nHiermit stellen wir Ihnen die erbrachten Leistungen für das Projekt '{project.Name}' in Rechnung:",
                    TextSuffix = textSuffix,
                    Status = "DRAFT",
                    Currency = "EUR",
                    DueInDays = dueInDays,
                    ServiceDate = new ServiceDate
                    {
                        Type = "FROM_TO",
                        DateFrom = timeEntries.Min(e => e.Date).ToString("yyyy-MM-dd"),
                        DateTo = timeEntries.Max(e => e.Date).ToString("yyyy-MM-dd")
                    },
                    Items = items.ToArray()
                };

                var result = await CreateDocumentAsync(invoice);

                // Wenn nicht als Entwurf gewünscht, Dokument abschließen
                if (!isDraft && result.Id.HasValue)
                {
                    result = await FinalizeDocumentAsync(result.Id.Value);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen der Rechnung aus Projekt: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt ein Angebot aus einem Projekt
        /// </summary>
        public async Task<EasybillDocument> CreateOfferFromProjectAsync(
            Project project,
            List<EasybillDocumentItem> items,
            string offerText = null,
            int validityDays = 30)
        {
            try
            {
                if (!project.EasybillCustomerId.HasValue)
                {
                    throw new Exception("Projekt hat keine Easybill-Kunden-ID!");
                }

                var offer = new EasybillDocument
                {
                    Type = "OFFER",
                    CustomerId = project.EasybillCustomerId.Value,
                    ProjectId = project.EasybillProjectId,
                    DocumentDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    Title = $"Angebot fÃ¼r Projekt: {project.Name}",
                    Subject = $"Angebot {project.Name}",
                    Text = offerText ?? $"Vielen Dank fÃ¼r Ihre Anfrage.\n\nGerne unterbreiten wir Ihnen folgendes Angebot fÃ¼r das Projekt '{project.Name}':",
                    TextSuffix = $"Dieses Angebot ist gÃ¼ltig bis {DateTime.Now.AddDays(validityDays):dd.MM.yyyy}.\n\nWir freuen uns auf Ihre Auftragserteilung.",
                    Status = "DRAFT",
                    Currency = "EUR",
                    Items = items.ToArray()
                };

                return await CreateDocumentAsync(offer);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Angebots: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt ein Angebot aus Zeiteinträgen eines Projekts
        /// </summary>
        public async Task<EasybillDocument> CreateOfferFromTimeEntriesAsync(
            Project project,
            List<TimeEntry> timeEntries,
            decimal hourlyRate,
            string offerText = null,
            int validityDays = 30,
            bool groupByDescription = false,
            bool isDraft = false,
            int vatPercent = 19,
            string vatSuffix = null)
        {
            try
            {
                if (!project.EasybillCustomerId.HasValue)
                {
                    throw new Exception("Projekt hat keine Easybill-Kunden-ID!");
                }

                // Erstelle Positionen aus Zeiteinträgen
                var items = new List<EasybillDocumentItem>();
                int position = 1;

                if (groupByDescription)
                {
                    // Gruppiere nach Aktivität/Beschreibung
                    var grouped = timeEntries
                        .GroupBy(e => e.Activity ?? "Zeiterfassung")
                        .ToList();

                    foreach (var group in grouped)
                    {
                        var totalHours = (decimal)group.Sum(e => e.Duration.TotalHours);
                        var singlePriceNet = hourlyRate;
                        var totalPriceNet = totalHours * singlePriceNet;

                        items.Add(new EasybillDocumentItem
                        {
                            Type = "POSITION",
                            Position = position++,
                            Description = group.Key,
                            Quantity = totalHours,
                            Unit = "Stunden",
                            SinglePriceNet = singlePriceNet,
                            VatPercent = vatPercent,
                            TotalPriceNet = totalPriceNet
                        });
                    }
                }
                else
                {
                    // Eine Position mit Gesamtstunden
                    var totalHours = (decimal)timeEntries.Sum(e => e.Duration.TotalHours);
                    var singlePriceNet = hourlyRate;
                    var totalPriceNet = totalHours * singlePriceNet;

                    items.Add(new EasybillDocumentItem
                    {
                        Type = "POSITION",
                        Position = 1,
                        Description = $"Entwicklung / Beratung\n{project.Name}",
                        Quantity = totalHours,
                        Unit = "Stunden",
                        SinglePriceNet = singlePriceNet,
                        VatPercent = vatPercent,
                        TotalPriceNet = totalPriceNet
                    });
                }

                // Standard-Schlusstext oder steuerrechtlichen Suffix verwenden
                var defaultSuffix = $"Dieses Angebot ist g\u00fcltig bis {DateTime.Now.AddDays(validityDays):dd.MM.yyyy}.\n\nWir freuen uns auf Ihre Auftragserteilung.";
                var textSuffix = !string.IsNullOrEmpty(vatSuffix)
                    ? $"{vatSuffix}\n\n{defaultSuffix}"
                    : defaultSuffix;

                // Erstelle Angebot
                var offer = new EasybillDocument
                {
                    Type = "OFFER",
                    CustomerId = project.EasybillCustomerId.Value,
                    ProjectId = project.EasybillProjectId,
                    DocumentDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    Title = $"Angebot f\u00fcr Projekt: {project.Name}",
                    Subject = $"Angebot {project.Name}",
                    Text = offerText ?? $"Vielen Dank f\u00fcr Ihre Anfrage.\n\nGerne unterbreiten wir Ihnen folgendes Angebot f\u00fcr das Projekt '{project.Name}':",
                    TextSuffix = textSuffix,
                    Status = "DRAFT",
                    Currency = "EUR",
                    ServiceDate = new ServiceDate
                    {
                        Type = "FROM_TO",
                        DateFrom = timeEntries.Min(e => e.Date).ToString("yyyy-MM-dd"),
                        DateTo = timeEntries.Max(e => e.Date).ToString("yyyy-MM-dd")
                    },
                    Items = items.ToArray()
                };

                var result = await CreateDocumentAsync(offer);

                // Wenn nicht als Entwurf gewünscht, Dokument abschließen
                if (!isDraft && result.Id.HasValue)
                {
                    result = await FinalizeDocumentAsync(result.Id.Value);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Angebots aus Zeiteintr\u00e4gen: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt eine Proforma-Rechnung aus Zeiteinträgen eines Projekts
        /// </summary>
        public async Task<EasybillDocument> CreateProformaFromTimeEntriesAsync(
            Project project,
            List<TimeEntry> timeEntries,
            decimal hourlyRate,
            string proformaText = null,
            int validityDays = 30,
            bool isDraft = false,
            int vatPercent = 19,
            string vatSuffix = null)
        {
            try
            {
                if (!project.EasybillCustomerId.HasValue)
                {
                    throw new Exception("Projekt hat keine Easybill-Kunden-ID!");
                }

                var items = new List<EasybillDocumentItem>();
                int position = 1;

                foreach (var entry in timeEntries)
                {
                    var hours = (decimal)entry.Duration.TotalHours;
                    var singlePriceNet = hourlyRate;
                    var totalPriceNet = hours * singlePriceNet;

                    items.Add(new EasybillDocumentItem
                    {
                        Type = "POSITION",
                        Position = position++,
                        Number = $"ZE-{entry.Date:yyyyMMdd}",
                        Description = $"{entry.Activity ?? "Zeiterfassung"} - {entry.Date:dd.MM.yyyy}\n{entry.Description}",
                        Quantity = hours,
                        Unit = "Stunden",
                        SinglePriceNet = singlePriceNet,
                        VatPercent = vatPercent,
                        TotalPriceNet = totalPriceNet,
                        ExportIdentifier = $"TimeEntry-{entry.Id}"
                    });
                }

                var defaultSuffix = $"Diese Proforma-Rechnung ist g\u00fcltig bis {DateTime.Now.AddDays(validityDays):dd.MM.yyyy}.\n\nBitte beachten Sie, dass es sich hierbei um keine steuerlich wirksame Rechnung handelt.";
                var textSuffix = !string.IsNullOrEmpty(vatSuffix)
                    ? $"{vatSuffix}\n\n{defaultSuffix}"
                    : defaultSuffix;

                var proforma = new EasybillDocument
                {
                    Type = "PROFORMA_INVOICE",
                    CustomerId = project.EasybillCustomerId.Value,
                    ProjectId = project.EasybillProjectId,
                    DocumentDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    Title = $"Proforma-Rechnung f\u00fcr Projekt: {project.Name}",
                    Subject = $"Proforma-Rechnung {project.Name}",
                    Text = proformaText ?? $"Sehr geehrte Damen und Herren,\n\nhiermit \u00fcbersenden wir Ihnen die Proforma-Rechnung f\u00fcr die erbrachten Leistungen im Rahmen des Projekts '{project.Name}':",
                    TextSuffix = textSuffix,
                    Status = "DRAFT",
                    Currency = "EUR",
                    ServiceDate = new ServiceDate
                    {
                        Type = "FROM_TO",
                        DateFrom = timeEntries.Min(e => e.Date).ToString("yyyy-MM-dd"),
                        DateTo = timeEntries.Max(e => e.Date).ToString("yyyy-MM-dd")
                    },
                    Items = items.ToArray()
                };

                var result = await CreateDocumentAsync(proforma);

                if (!isDraft && result.Id.HasValue)
                {
                    result = await FinalizeDocumentAsync(result.Id.Value);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen der Proforma-Rechnung aus Zeiteintr\u00e4gen: {ex.Message}", ex);
            }
        }

        #endregion

        #region Customer Sync Helper Methods

        /// <summary>
        /// Konvertiert einen lokalen Customer in einen EasybillCustomer
        /// </summary>
        public static EasybillCustomer ConvertToEasybillCustomer(Customer customer)
        {
            return new EasybillCustomer
            {
                CompanyName = customer.CompanyName,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Emails = !string.IsNullOrEmpty(customer.Email) ? new[] { customer.Email } : null,
                Phone1 = customer.Phone,
                Street = customer.Street,
                Zipcode = customer.ZipCode,
                City = customer.City,
                Country = ToIsoCountryCode(customer.Country),
                VatId = customer.VatId,
                Note = customer.Note
            };
        }

        /// <summary>
        /// Aktualisiert einen lokalen Customer mit Daten von Easybill
        /// </summary>
        public static void UpdateCustomerFromEasybill(Customer customer, EasybillCustomer easybillCustomer)
        {
            customer.CompanyName = easybillCustomer.CompanyName;
            customer.FirstName = easybillCustomer.FirstName;
            customer.LastName = easybillCustomer.LastName;
            customer.Email = easybillCustomer.Email;
            customer.Phone = easybillCustomer.Phone1;
            customer.Street = easybillCustomer.Street;
            customer.ZipCode = easybillCustomer.Zipcode;
            customer.City = easybillCustomer.City;
            customer.Country = easybillCustomer.Country;
            customer.VatId = easybillCustomer.VatId;
            customer.Note = easybillCustomer.Note;
            customer.EasybillCustomerId = easybillCustomer.Id;
            customer.LastSyncedAt = DateTime.Now;
            customer.UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Synchronisiert einen lokalen Kunden zu Easybill
        /// </summary>
        public async Task<EasybillCustomer> SyncCustomerToEasybillAsync(Customer customer)
        {
            var easybillCustomer = ConvertToEasybillCustomer(customer);

            if (customer.EasybillCustomerId.HasValue)
            {
                // Kunde existiert bereits in Easybill - aktualisieren
                easybillCustomer.Id = customer.EasybillCustomerId.Value;
                return await UpdateCustomerAsync(customer.EasybillCustomerId.Value, easybillCustomer);
            }
            else
            {
                // Neuer Kunde - erstellen
                return await CreateCustomerAsync(easybillCustomer);
            }
        }

        /// <summary>
        /// Konvertiert einen lokalen Lieferanten in einen EasybillCustomer
        /// </summary>
        public static EasybillCustomer ConvertSupplierToEasybillCustomer(Supplier supplier)
        {
            var noteParts = new System.Collections.Generic.List<string> { "Lieferant" };
            if (!string.IsNullOrWhiteSpace(supplier.TaxNumber))
                noteParts.Add($"USt-IdNr.: {supplier.TaxNumber}");
            if (!string.IsNullOrWhiteSpace(supplier.BankIban))
                noteParts.Add($"IBAN: {supplier.BankIban}");
            if (!string.IsNullOrWhiteSpace(supplier.Notes))
                noteParts.Add(supplier.Notes);

            return new EasybillCustomer
            {
                CompanyName = supplier.Name,
                FirstName = null,
                LastName = null,
                Emails = !string.IsNullOrEmpty(supplier.Email) ? new[] { supplier.Email } : null,
                Phone1 = supplier.Phone,
                Street = supplier.Address,
                Zipcode = supplier.ZipCode,
                City = supplier.City,
                Country = ToIsoCountryCode(supplier.Country),
                Note = string.Join(" | ", noteParts)
            };
        }

        /// <summary>
        /// Synchronisiert einen lokalen Lieferanten zu Easybill (als Kundeneintrag mit Kontakt)
        /// </summary>
        public async Task<EasybillCustomer> SyncSupplierToEasybillAsync(Supplier supplier)
        {
            var customer = ConvertSupplierToEasybillCustomer(supplier);

            EasybillCustomer result;
            if (supplier.EasybillCustomerId.HasValue)
            {
                customer.Id = supplier.EasybillCustomerId.Value;
                result = await UpdateCustomerAsync(supplier.EasybillCustomerId.Value, customer);
            }
            else
            {
                result = await CreateCustomerAsync(customer);
            }

            if (!string.IsNullOrWhiteSpace(supplier.ContactPerson) && result?.Id > 0)
            {
                await SyncSupplierContactAsync(supplier, result.Id);
            }

            return result;
        }

        private async Task SyncSupplierContactAsync(Supplier supplier, long easybillCustomerId)
        {
            try
            {
                var parts = supplier.ContactPerson.Trim().Split(' ', 2);
                var firstName = parts.Length > 1 ? parts[0] : "";
                var lastName = parts.Length > 1 ? parts[1] : parts[0];

                var existingContacts = await GetContactsByCustomerAsync(easybillCustomerId);
                if (existingContacts?.Count > 0)
                {
                    var existing = existingContacts[0];
                    if (existing.Id.HasValue)
                    {
                        await UpdateContactAsync(easybillCustomerId, existing.Id.Value, new EasybillContact
                        {
                            CustomerId = easybillCustomerId,
                            FirstName = firstName,
                            LastName = lastName,
                            Email = supplier.Email,
                            Phone1 = supplier.Phone
                        });
                        return;
                    }
                }

                await CreateContactAsync(new EasybillContact
                {
                    CustomerId = easybillCustomerId,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = supplier.Email,
                    Phone1 = supplier.Phone
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kontakt-Sync Fehler für {supplier.Name}: {ex.Message}");
            }
        }

        private static string ToIsoCountryCode(string country)
        {
            if (string.IsNullOrWhiteSpace(country)) return "DE";
            if (country.Length == 2) return country.ToUpperInvariant();
            return country.Trim().ToLowerInvariant() switch
            {
                "deutschland" or "germany" => "DE",
                "österreich" or "oesterreich" or "austria" => "AT",
                "schweiz" or "switzerland" or "suisse" or "svizzera" => "CH",
                "frankreich" or "france" => "FR",
                "niederlande" or "netherlands" or "holland" => "NL",
                "belgien" or "belgium" or "belgique" => "BE",
                "italien" or "italy" or "italia" => "IT",
                "spanien" or "spain" or "españa" => "ES",
                "vereinigtes königreich" or "united kingdom" or "großbritannien" => "GB",
                "vereinigte staaten" or "united states" or "usa" => "US",
                _ => "DE"
            };
        }

        #endregion

        #region Note Methods

        /// <summary>
        /// Holt alle Notizen (optional gefiltert nach Typ)
        /// </summary>
        public async Task<List<EasybillNote>> GetAllNotesAsync(string type = null)
        {
            var allNotes = new List<EasybillNote>();

            try
            {
                int page = 1;
                int totalPages = 1;

                while (page <= totalPages)
                {
                    var url = $"notes?page={page}&limit=100";
                    if (!string.IsNullOrEmpty(type))
                    {
                        url += $"&type={type}";
                    }

                    var response = await httpClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<EasybillNoteList>(json, jsonOptions);

                    if (result?.Items != null)
                    {
                        allNotes.AddRange(result.Items);
                        totalPages = result.Pages;
                    }

                    page++;
                }

                return allNotes;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Notizen: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Holt Notizen für ein Dokument
        /// </summary>
        public async Task<List<EasybillNote>> GetNotesByDocumentAsync(long documentId)
        {
            try
            {
                var allNotes = await GetAllNotesAsync();
                return allNotes.FindAll(n => n.DocumentId == documentId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Dokument-Notizen: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Holt Notizen für einen Kunden
        /// </summary>
        public async Task<List<EasybillNote>> GetNotesByCustomerAsync(long customerId)
        {
            try
            {
                var allNotes = await GetAllNotesAsync();
                return allNotes.FindAll(n => n.CustomerId == customerId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Kunden-Notizen: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt eine Notiz
        /// </summary>
        public async Task<EasybillNote> CreateNoteAsync(EasybillNote note)
        {
            try
            {
                var json = JsonSerializer.Serialize(note);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("notes", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillNote>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen der Notiz: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Aktualisiert eine Notiz
        /// </summary>
        public async Task<EasybillNote> UpdateNoteAsync(long noteId, EasybillNote note)
        {
            try
            {
                var json = JsonSerializer.Serialize(note);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"notes/{noteId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillNote>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Aktualisieren der Notiz: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Löscht eine Notiz
        /// </summary>
        public async Task DeleteNoteAsync(long noteId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"notes/{noteId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Löschen der Notiz: {ex.Message}", ex);
            }
        }

        #endregion

        #region Activity Methods

        /// <summary>
        /// Holt alle Aktivitäten
        /// </summary>
        public async Task<List<EasybillActivity>> GetAllActivitiesAsync()
        {
            var allActivities = new List<EasybillActivity>();

            try
            {
                int page = 1;
                int totalPages = 1;

                while (page <= totalPages)
                {
                    var response = await httpClient.GetAsync($"activities?page={page}&limit=100");

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<EasybillActivityList>(json, jsonOptions);

                    if (result?.Items != null)
                    {
                        allActivities.AddRange(result.Items);
                        totalPages = result.Pages;
                    }

                    page++;
                }

                return allActivities;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Aktivitäten: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Holt Aktivitäten für einen Kunden
        /// </summary>
        public async Task<List<EasybillActivity>> GetActivitiesByCustomerAsync(long customerId)
        {
            try
            {
                var allActivities = await GetAllActivitiesAsync();
                return allActivities.FindAll(a => a.CustomerId == customerId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Kunden-Aktivitäten: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt eine Aktivität
        /// </summary>
        public async Task<EasybillActivity> CreateActivityAsync(EasybillActivity activity)
        {
            try
            {
                var json = JsonSerializer.Serialize(activity);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("activities", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillActivity>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen der Aktivität: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Aktualisiert eine Aktivität
        /// </summary>
        public async Task<EasybillActivity> UpdateActivityAsync(long activityId, EasybillActivity activity)
        {
            try
            {
                var json = JsonSerializer.Serialize(activity);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"activities/{activityId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillActivity>(resultJson, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Aktualisieren der Aktivität: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Löscht eine Aktivität
        /// </summary>
        public async Task DeleteActivityAsync(long activityId)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"activities/{activityId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Löschen der Aktivität: {ex.Message}", ex);
            }
        }

        #endregion

        #region Tax Rule Methods

        /// <summary>
        /// Holt alle Steuerregeln
        /// </summary>
        public async Task<List<EasybillTaxRule>> GetAllTaxRulesAsync()
        {
            var allTaxRules = new List<EasybillTaxRule>();

            try
            {
                int page = 1;
                int totalPages = 1;

                while (page <= totalPages)
                {
                    var response = await httpClient.GetAsync($"tax-rules?page={page}&limit=100");

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<EasybillTaxRuleList>(json, jsonOptions);

                    if (result?.Items != null)
                    {
                        allTaxRules.AddRange(result.Items);
                        totalPages = result.Pages;
                    }

                    page++;
                }

                return allTaxRules;
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Steuerregeln: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Holt eine einzelne Steuerregel
        /// </summary>
        public async Task<EasybillTaxRule> GetTaxRuleAsync(long taxRuleId)
        {
            try
            {
                var response = await httpClient.GetAsync($"tax-rules/{taxRuleId}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API-Fehler: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EasybillTaxRule>(json, jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der Steuerregel: {ex.Message}", ex);
            }
        }

        #endregion

        #region Document Type Helpers

        /// <summary>
        /// Erstellt eine Mahnung (Dunning/Reminder)
        /// </summary>
        public async Task<EasybillDocument> CreateDunningAsync(long invoiceId, int dunningLevel = 1)
        {
            try
            {
                // Erst die Original-Rechnung abrufen
                var invoice = await GetDocumentAsync(invoiceId);

                var dunning = new EasybillDocument
                {
                    Type = "DUNNING",
                    CustomerId = invoice.CustomerId,
                    DocumentDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    Title = $"Zahlungserinnerung {dunningLevel}. Mahnung",
                    Subject = $"Mahnung zur Rechnung {invoice.Number}",
                    Text = $"Sehr geehrte Damen und Herren,\n\nzu unserer Rechnung {invoice.Number} vom {invoice.DocumentDate} über {invoice.TotalGross:F2} EUR haben wir bisher keinen Zahlungseingang verzeichnet.",
                    Items = invoice.Items,
                    DueInDays = dunningLevel switch
                    {
                        1 => 7,   // 1. Mahnung: 7 Tage
                        2 => 5,   // 2. Mahnung: 5 Tage
                        _ => 3    // 3. Mahnung: 3 Tage
                    }
                };

                return await CreateDocumentAsync(dunning);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen der Mahnung: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt einen Lieferschein
        /// </summary>
        public async Task<EasybillDocument> CreateDeliveryNoteAsync(EasybillDocument deliveryNote)
        {
            try
            {
                deliveryNote.Type = "DELIVERY_NOTE";
                return await CreateDocumentAsync(deliveryNote);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Lieferscheins: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt eine Gutschrift
        /// </summary>
        public async Task<EasybillDocument> CreateCreditNoteAsync(EasybillDocument creditNote)
        {
            try
            {
                creditNote.Type = "CREDIT";
                return await CreateDocumentAsync(creditNote);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen der Gutschrift: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt eine Gutschrift aus einer Rechnung (Storno)
        /// </summary>
        public async Task<EasybillDocument> CreateCreditNoteFromInvoiceAsync(long invoiceId, string reason = null)
        {
            try
            {
                var invoice = await GetDocumentAsync(invoiceId);

                // Invertiere die Mengen/Preise für Gutschrift
                var creditItems = invoice.Items?.Select(item => new EasybillDocumentItem
                {
                    Type = item.Type,
                    Number = item.Number,
                    Description = item.Description,
                    Quantity = -item.Quantity, // Negativ für Gutschrift
                    SinglePriceNet = item.SinglePriceNet,
                    VatPercent = item.VatPercent,
                    Unit = item.Unit
                }).ToArray();

                var creditNote = new EasybillDocument
                {
                    Type = "CREDIT",
                    CustomerId = invoice.CustomerId,
                    DocumentDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    Title = $"Gutschrift zu Rechnung {invoice.Number}",
                    Subject = $"Gutschrift - Storno Rechnung {invoice.Number}",
                    Text = string.IsNullOrEmpty(reason) 
                        ? $"Gutschrift zur Rechnung {invoice.Number} vom {invoice.DocumentDate}"
                        : $"Gutschrift zur Rechnung {invoice.Number} vom {invoice.DocumentDate}\n\nGrund: {reason}",
                    Items = creditItems,
                    BuyerReference = invoice.Number // Referenz zur Original-Rechnung
                };

                return await CreateDocumentAsync(creditNote);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen der Gutschrift: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Erstellt eine Auftragsbestätigung.
        /// Hinweis: Die Easybill REST API v1 bietet keinen Dokumenttyp für Auftragsbestätigungen.
        /// ORDER entspricht einer Bestellung, nicht einer Auftragsbestätigung.
        /// </summary>
        public Task<EasybillDocument> CreateOrderConfirmationAsync(EasybillDocument orderConfirmation)
        {
            throw new NotSupportedException(
                "Die Easybill REST API unterstützt keinen Dokumenttyp für Auftragsbestätigungen.\n" +
                "'ORDER' entspricht einer Bestellung.\n" +
                "Bitte erstellen Sie die Auftragsbestätigung manuell in der Easybill Web-Oberfläche.");
        }

        /// <summary>
        /// Erstellt eine Auftragsbestätigung aus einem Angebot.
        /// Hinweis: Die Easybill REST API v1 bietet keinen Dokumenttyp für Auftragsbestätigungen.
        /// ORDER entspricht einer Bestellung, nicht einer Auftragsbestätigung.
        /// </summary>
        public Task<EasybillDocument> CreateOrderConfirmationFromOfferAsync(long offerId, string confirmationText = null, bool isDraft = false)
        {
            throw new NotSupportedException(
                "Die Easybill REST API unterstützt keinen Dokumenttyp für Auftragsbestätigungen.\n" +
                "'ORDER' entspricht einer Bestellung.\n" +
                "Bitte erstellen Sie die Auftragsbestätigung manuell in der Easybill Web-Oberfläche.");
        }

        #endregion

        #region Purchase Invoice Sync Methods

        /// <summary>
        /// Synchronisiert den Lieferanten einer Eingangsrechnung zu Easybill.
        /// Hinweis: Die Easybill REST API v1 bietet keinen Endpunkt für Eingangsrechnungen/Belegerfassung.
        /// Die PDF-Ablage erfolgt über den Anhang-Endpunkt (POST /attachments + PUT /attachments/{id}).
        /// </summary>
        public async Task<EasybillDocument?> SyncPurchaseInvoiceToEasybillAsync(PurchaseInvoice invoice, Supplier? supplier)
        {
            if (supplier != null && !supplier.EasybillCustomerId.HasValue)
            {
                var syncedCustomer = await SyncSupplierToEasybillAsync(supplier);
                if (syncedCustomer?.Id > 0)
                    supplier.EasybillCustomerId = syncedCustomer.Id;
            }
            return null;
        }

        /// <summary>
        /// Synchronisiert eine Bestellung zu Easybill (erstellt oder aktualisiert das Dokument).
        /// Die Bestellnummer wird von Easybill automatisch vergeben.
        /// </summary>
        public async Task<EasybillDocument> SyncPurchaseOrderToEasybillAsync(PurchaseOrder order, Supplier? supplier, bool isDraft = false)
        {
            // Lieferant zuerst synchronisieren, falls noch keine Easybill-ID vorhanden
            if (supplier != null && !supplier.EasybillCustomerId.HasValue)
            {
                var syncedCustomer = await SyncSupplierToEasybillAsync(supplier);
                if (syncedCustomer?.Id > 0)
                    supplier.EasybillCustomerId = syncedCustomer.Id;
            }

            EasybillDocumentItem[] items;
            if (order.Items != null && order.Items.Count > 0)
            {
                items = order.Items.Select((item, index) => new EasybillDocumentItem
                {
                    Type = "POSITION",
                    Position = index + 1,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? null : item.Unit,
                    SinglePriceNet = item.UnitPriceNet,
                    VatPercent = (int)item.VatPercent
                    // TotalPriceNet wird von Easybill selbst berechnet – NICHT senden
                }).ToArray();
            }
            else
            {
                decimal net = order.TotalNet > 0 ? order.TotalNet : Math.Round(order.TotalGross / 1.19m, 2);
                items =
                [
                    new EasybillDocumentItem
                    {
                        Type = "POSITION",
                        Position = 1,
                        Description = $"Bestellung bei {order.SupplierName}",
                        Quantity = 1,
                        SinglePriceNet = net,
                        VatPercent = 19
                    }
                ];
            }

            var doc = new EasybillDocument
            {
                Type = "ORDER",
                CustomerId = supplier?.EasybillCustomerId,
                DocumentDate = order.OrderDate.ToString("yyyy-MM-dd"),
                Title = $"Bestellung {order.SupplierName}",
                Text = string.IsNullOrWhiteSpace(order.Notes) ? null : order.Notes,
                IsDraft = isDraft,
                Items = items
            };

            if (order.EasybillDocumentId.HasValue)
            {
                doc.Id = order.EasybillDocumentId.Value;
                var result = await UpdateDocumentAsync(order.EasybillDocumentId.Value, doc);

                // Wenn noch ein Entwurf und kein Entwurf gewünscht → Dokument abschließen → Easybill vergibt die Bestellnummer
                if (!isDraft && result.IsDraft && result.Id.HasValue)
                {
                    result = await FinalizeDocumentAsync(result.Id.Value);
                }

                return result;
            }
            else
            {
                var result = await CreateDocumentAsync(doc);

                // Wenn nicht als Entwurf gewünscht, Dokument abschließen → Easybill vergibt die Bestellnummer
                if (!isDraft && result.Id.HasValue)
                {
                    result = await FinalizeDocumentAsync(result.Id.Value);
                }

                return result;
            }
        }

        #endregion
    }
}
