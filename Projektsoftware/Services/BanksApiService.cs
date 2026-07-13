using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Client für die BANKSapi (BANKS/Connect, https://banksapi.io) zum automatischen
    /// Kontoumsatz-Abruf. Ersetzt den früheren FinTS/HBCI-Zugang.
    ///
    /// Ablauf (REG/Protect, non-regulated):
    ///   1. Client-Token per Basic-Auth ("{Tenant}/{ClientId}":ClientSecret).
    ///   2. Technischen User anlegen (einmalig) und lokal speichern.
    ///   3. User-Token per password-grant.
    ///   4. Bankzugang anlegen -> HTTP 451 + Location = BANKSapi-WebForm (Bank-Login + SCA im Browser).
    ///   5. Nach Einrichtung: Kontoumsätze je Produkt abrufen und auf <see cref="BankTransaction"/> mappen.
    /// </summary>
    public class BanksApiService
    {
        private readonly BankConfig config;
        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions jsonOptions;

        private string? clientToken;
        private string? userToken;

        public BanksApiService(BankConfig config)
        {
            this.config = config;
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(config.ApiBaseUrl.TrimEnd('/') + "/")
            };
            jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };
        }

        // ── Token-Handling ─────────────────────────────────────────────

        private AuthenticationHeaderValue BasicAuthHeader()
        {
            var raw = $"{config.BasicAuthUser}:{config.ClientSecret}";
            var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
            return new AuthenticationHeaderValue("Basic", b64);
        }

        /// <summary>Holt (und cached) einen Client-Token per client_credentials.</summary>
        private async Task<string> GetClientTokenAsync()
        {
            if (!string.IsNullOrEmpty(clientToken))
                return clientToken!;

            using var request = new HttpRequestMessage(HttpMethod.Post, "auth/oauth2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials"
                })
            };
            request.Headers.Authorization = BasicAuthHeader();

            using var response = await httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new BanksApiException(DescribeHttpError("Client-Token", response.StatusCode, body));

            clientToken = ParseToken(body);
            return clientToken!;
        }

        /// <summary>Holt (und cached) einen User-Token per password-grant.</summary>
        private async Task<string> GetUserTokenAsync()
        {
            if (!string.IsNullOrEmpty(userToken))
                return userToken!;

            if (string.IsNullOrWhiteSpace(config.BanksApiUsername) ||
                string.IsNullOrWhiteSpace(config.BanksApiPassword))
                throw new BanksApiException(
                    "Es wurde noch kein BANKSapi-Benutzer eingerichtet. Bitte zuerst 'Bankverbindung einrichten' ausführen.");

            using var request = new HttpRequestMessage(HttpMethod.Post, "auth/oauth2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["username"] = config.BanksApiUsername,
                    ["password"] = config.BanksApiPassword
                })
            };
            request.Headers.Authorization = BasicAuthHeader();

            using var response = await httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new BanksApiException(DescribeHttpError("User-Token", response.StatusCode, body));

            userToken = ParseToken(body);
            return userToken!;
        }

        private string ParseToken(string body)
        {
            var token = JsonSerializer.Deserialize<TokenResponse>(body, jsonOptions);
            if (token?.access_token == null)
                throw new BanksApiException("Die BANKSapi-Antwort enthielt keinen Access-Token.");
            return token.access_token;
        }

        // ── Verbindungstest ────────────────────────────────────────────

        /// <summary>Prüft die BANKSapi-Zugangsdaten, indem ein Client-Token angefordert wird.</summary>
        public async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            try
            {
                await GetClientTokenAsync();
                return (true, "BANKSapi-Zugangsdaten gültig – Verbindung erfolgreich.");
            }
            catch (BanksApiException ex)
            {
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                return (false, DescribeException(ex));
            }
        }

        // ── Benutzer + Bankzugang einrichten ──────────────────────────

        /// <summary>
        /// Stellt sicher, dass ein technischer BANKSapi-User existiert (legt ihn bei Bedarf an)
        /// und speichert dessen Zugangsdaten in der <see cref="BankConfig"/>.
        /// </summary>
        public async Task EnsureUserAsync()
        {
            if (!string.IsNullOrWhiteSpace(config.BanksApiUsername) &&
                !string.IsNullOrWhiteSpace(config.BanksApiPassword))
                return;

            var token = await GetClientTokenAsync();

            var username = "app_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var password = "Pw!" + Guid.NewGuid().ToString("N").Substring(0, 20);

            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"auth/mgmt/v1/tenants/{config.Tenant}/users")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { username, password }),
                    Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new BanksApiException(DescribeHttpError("Benutzeranlage", response.StatusCode, body));

            config.BanksApiUsername = username;
            config.BanksApiPassword = password;
            config.Save();

            // Neuer User -> vorhandenen User-Token verwerfen.
            userToken = null;
        }

        /// <summary>
        /// Verwirft den aktuellen technischen BANKSapi-User (samt gespeichertem Bankzugang)
        /// und legt einen frischen an. Wird zur Wiederherstellung genutzt, wenn ein User durch
        /// abgebrochene WebForm-Versuche in einen inkonsistenten Zustand geraten ist und die
        /// API deshalb keinen neuen Bankzugang mehr anlegt.
        /// </summary>
        public async Task ResetTechnicalUserAsync()
        {
            config.BanksApiUsername = "";
            config.BanksApiPassword = "";
            config.AccessId = "";
            config.Save();
            userToken = null;

            await EnsureUserAsync();
        }

        /// <summary>
        /// Legt einen neuen Bankzugang an. Für REG/Protect-Mandanten antwortet die API mit
        /// HTTP 451 und einer Location-URL zur BANKSapi-WebForm (Bank-Login + SCA im Browser).
        /// Gibt diese WebForm-URL sowie die selbst vergebene Access-ID zurück.
        ///
        /// Antwortet die API mit HTTP 400 ("credentials"), befindet sich der technische User
        /// in einem inkonsistenten Zustand (hängender Zugang aus einem abgebrochenen
        /// WebForm-Versuch). In diesem Fall wird einmalig ein frischer technischer User
        /// angelegt und der Vorgang wiederholt – ein frischer User liefert zuverlässig 451.
        /// </summary>
        /// <param name="callbackUrl">
        /// URL, zu der der Browser nach Abschluss der WebForm zurückgeleitet wird. Für
        /// nicht-regulierte Mandanten (REG/Protect) zwingend erforderlich – wird an die
        /// WebForm-URL angehängt. Ist der Parameter leer, wird <see cref="BankConfig.CallbackUrl"/>
        /// verwendet.
        /// </param>
        public async Task<(string WebFormUrl, string AccessId)> StartBankAccessSetupAsync(string? callbackUrl = null)
        {
            await EnsureUserAsync();

            try
            {
                return await TryStartBankAccessSetupAsync(callbackUrl);
            }
            catch (BanksApiCredentialsConflictException)
            {
                // Technischer User in inkonsistentem Zustand -> frischen User anlegen und
                // genau einmal erneut versuchen.
                await ResetTechnicalUserAsync();
                return await TryStartBankAccessSetupAsync(callbackUrl);
            }
        }

        private async Task<(string WebFormUrl, string AccessId)> TryStartBankAccessSetupAsync(string? callbackUrl)
        {
            var token = await GetUserTokenAsync();

            var effectiveCallbackUrl = string.IsNullOrWhiteSpace(callbackUrl)
                ? config.CallbackUrl
                : callbackUrl;

            var accessId = Guid.NewGuid().ToString();
            var url = "customer/v2/bankzugaenge";
            // Callback-URL zusätzlich als Query-Parameter senden (Fallback, in der
            // REG/Protect-Session gespeichert). Maßgeblich ist die an die Location-URL
            // angehängte Callback-URL (siehe unten).
            if (!string.IsNullOrWhiteSpace(effectiveCallbackUrl))
                url += "?callbackUrl=" + Uri.EscapeDataString(effectiveCallbackUrl);

            var payload = new Dictionary<string, object> { [accessId] = new { } };

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.SendAsync(request);

            // Erwartet: 451 Unavailable For Legal Reasons + Location-Header (WebForm).
            if ((int)response.StatusCode == 451 || response.Headers.Location != null)
            {
                var location = response.Headers.Location?.ToString();
                if (string.IsNullOrWhiteSpace(location))
                {
                    var body = await response.Content.ReadAsStringAsync();
                    throw new BanksApiException(
                        "BANKSapi hat keine WebForm-URL zurückgegeben. Antwort: " + Truncate(body, 300));
                }

                // Für nicht-regulierte Mandanten (REG/Protect) muss die Callback-URL
                // URL-kodiert an die WebForm-Location angehängt werden, sonst zeigt die
                // WebForm "No Callback URL / Callback-URL must not be blank".
                var webFormUrl = AppendCallbackUrl(location!, effectiveCallbackUrl);
                return (webFormUrl, accessId);
            }

            if (response.IsSuccessStatusCode)
            {
                // Regulierter Mandant o. ä.: kein WebForm-Schritt nötig.
                return ("", accessId);
            }

            var errorBody = await response.Content.ReadAsStringAsync();

            // HTTP 400 mit "credentials"-Hinweis: Der technische User ist in einem
            // inkonsistenten Zustand. Als speziellen Konflikt melden, damit der Aufrufer
            // mit einem frischen User erneut versuchen kann.
            if ((int)response.StatusCode == 400 &&
                errorBody.IndexOf("credentials", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new BanksApiCredentialsConflictException(
                    DescribeHttpError("Bankzugang anlegen", response.StatusCode, errorBody));
            }

            throw new BanksApiException(DescribeHttpError("Bankzugang anlegen", response.StatusCode, errorBody));
        }

        /// <summary>
        /// Hängt die (URL-kodierte) Callback-URL als Query-Parameter an die WebForm-URL an,
        /// sofern noch keine vorhanden ist. Für REG/Protect-Mandanten zwingend erforderlich.
        /// </summary>
        private static string AppendCallbackUrl(string webFormUrl, string? callbackUrl)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                return webFormUrl;

            // Bereits vorhandene callbackUrl nicht überschreiben.
            if (webFormUrl.IndexOf("callbackUrl=", StringComparison.OrdinalIgnoreCase) >= 0)
                return webFormUrl;

            var separator = webFormUrl.Contains('?') ? "&" : "?";
            return webFormUrl + separator + "callbackUrl=" + Uri.EscapeDataString(callbackUrl);
        }

        // ── Kontoprodukte + Umsätze ───────────────────────────────────

        /// <summary>
        /// Ruft die Zahlungskonten (Produkte) des eingerichteten Bankzugangs ab
        /// (z. B. zur Auswahl des abzugleichenden Kontos).
        /// </summary>
        public async Task<List<BankProduct>> GetBankProductsAsync()
        {
            var accesses = await LoadBankAccessesAsync();
            var result = new List<BankProduct>();

            foreach (var (accessId, access) in accesses)
            {
                foreach (var p in access.bankprodukte ?? new List<BanksApiProduct>())
                {
                    var productId = p.produktId ?? p.id;
                    if (string.IsNullOrWhiteSpace(productId)) continue;

                    result.Add(new BankProduct
                    {
                        AccessId = accessId,
                        ProductId = productId!,
                        Display = BuildProductDisplay(p)
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Ruft die Kontoumsätze des eingerichteten Bankzugangs im angegebenen Zeitraum ab
        /// und liefert nur Zahlungseingänge (Gutschriften) als <see cref="BankTransaction"/>.
        /// Ist keine feste Produkt-ID konfiguriert, werden alle Produkte des Zugangs abgefragt.
        /// </summary>
        public async Task<BankTransactionResult> GetIncomingTransactionsAsync(DateTime from, DateTime to)
        {
            var result = new BankTransactionResult();
            try
            {
                if (!config.HasBankAccess)
                {
                    result.Message = "Es wurde noch kein Bankzugang eingerichtet. " +
                        "Bitte zuerst 'Bankverbindung einrichten' ausführen.";
                    return result;
                }

                var token = await GetUserTokenAsync();

                // Zu prüfende Produkte bestimmen.
                var products = new List<BankProduct>();
                if (!string.IsNullOrWhiteSpace(config.ProductId))
                {
                    products.Add(new BankProduct
                    {
                        AccessId = config.AccessId,
                        ProductId = config.ProductId
                    });
                }
                else
                {
                    products = await GetBankProductsAsync();
                    if (products.Count == 0)
                    {
                        result.Message = "Der Bankzugang enthält keine abrufbaren Konten. " +
                            "Wurde die Einrichtung in der BANKSapi-WebForm abgeschlossen?";
                        return result;
                    }
                }

                var fromArg = Uri.EscapeDataString(from.ToString("yyyy-MM-ddTHH:mm:ss"));
                var toArg = Uri.EscapeDataString(to.ToString("yyyy-MM-ddTHH:mm:ss"));

                foreach (var product in products)
                {
                    var url = $"customer/v2/bankzugaenge/{product.AccessId}/{product.ProductId}/" +
                              $"kontoumsaetze?from={fromArg}&to={toArg}";

                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    using var response = await httpClient.SendAsync(request);
                    var body = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        result.Message = DescribeHttpError("Kontoumsätze abrufen", response.StatusCode, body);
                        return result;
                    }

                    var transactions = JsonSerializer.Deserialize<List<BanksApiTransaction>>(body, jsonOptions)
                                       ?? new List<BanksApiTransaction>();

                    foreach (var tx in transactions)
                    {
                        if (tx.betrag <= 0) continue; // nur Zahlungseingänge (Gutschriften)

                        result.Transactions.Add(new BankTransaction
                        {
                            ValueDate = ParseDate(tx.wertstellungsdatum ?? tx.buchungsdatum),
                            Amount = tx.betrag,
                            Purpose = tx.verwendungszweck?.Trim() ?? "",
                            PartnerName = tx.gegenkontoInhaber?.Trim() ?? "",
                            PartnerIban = tx.gegenkontoIban?.Trim() ?? "",
                            EndToEndId = ""
                        });
                    }
                }

                result.Success = true;
                return result;
            }
            catch (BanksApiException ex)
            {
                result.Message = ex.Message;
                return result;
            }
            catch (Exception ex)
            {
                result.Message = "Fehler beim Kontoabruf: " + DescribeException(ex);
                return result;
            }
        }

        private async Task<Dictionary<string, BanksApiBankAccess>> LoadBankAccessesAsync()
        {
            var token = await GetUserTokenAsync();

            using var request = new HttpRequestMessage(HttpMethod.Get, "customer/v2/bankzugaenge");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new BanksApiException(DescribeHttpError("Bankzugänge abrufen", response.StatusCode, body));

            return JsonSerializer.Deserialize<Dictionary<string, BanksApiBankAccess>>(body, jsonOptions)
                   ?? new Dictionary<string, BanksApiBankAccess>();
        }

        // ── Hilfsfunktionen ────────────────────────────────────────────

        private static string BuildProductDisplay(BanksApiProduct p)
        {
            var name = !string.IsNullOrWhiteSpace(p.produktbezeichnung) ? p.produktbezeichnung : p.kategorie;
            var iban = !string.IsNullOrWhiteSpace(p.iban) ? p.iban : p.kontonummer;
            return string.Join(" · ", new[] { name, iban, p.inhaber }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        private static DateTime ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DateTime.Today;

            if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
                return exact;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed;

            return DateTime.Today;
        }

        private static string DescribeHttpError(string operation, HttpStatusCode status, string body)
        {
            var detail = ExtractApiMessage(body);
            var reason = (int)status switch
            {
                401 => "Zugangsdaten (Tenant/Client/Secret) ungültig.",
                402 => "Der BANKSapi-Mandant ist abgelaufen.",
                423 => "Der BANKSapi-Benutzer ist gesperrt.",
                _ => ""
            };

            var sb = new StringBuilder($"{operation} fehlgeschlagen (HTTP {(int)status}).");
            if (!string.IsNullOrWhiteSpace(reason)) sb.Append(' ').Append(reason);
            if (!string.IsNullOrWhiteSpace(detail)) sb.Append(' ').Append(detail);
            return sb.ToString();
        }

        private static string ExtractApiMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return "";
            try
            {
                using var doc = JsonDocument.Parse(body);
                foreach (var key in new[] { "message", "error_description", "error", "details" })
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty(key, out var el) &&
                        el.ValueKind == JsonValueKind.String)
                    {
                        var val = el.GetString();
                        if (!string.IsNullOrWhiteSpace(val)) return val!;
                    }
                }
            }
            catch
            {
                // Kein JSON -> Rohtext (gekürzt) zurückgeben.
                return Truncate(body, 300);
            }
            return "";
        }

        private static string DescribeException(Exception ex)
        {
            var messages = new List<string>();
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (!string.IsNullOrWhiteSpace(current.Message))
                    messages.Add(current.Message.Trim());
            }
            var text = string.Join(" → ", messages.Distinct());
            return string.IsNullOrWhiteSpace(text) ? ex.GetType().Name : text;
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value.Substring(0, max) + "…";

        // ── DTOs ───────────────────────────────────────────────────────

        private class TokenResponse
        {
            public string? access_token { get; set; }
            public string? scope { get; set; }
            public int expires_in { get; set; }
        }

        private class BanksApiBankAccess
        {
            public string? id { get; set; }
            public List<BanksApiProduct>? bankprodukte { get; set; }
        }

        private class BanksApiProduct
        {
            public string? produktId { get; set; }
            public string? id { get; set; }
            public string? iban { get; set; }
            public string? kontonummer { get; set; }
            public string? produktbezeichnung { get; set; }
            public string? kategorie { get; set; }
            public string? inhaber { get; set; }
        }

        private class BanksApiTransaction
        {
            public decimal betrag { get; set; }
            public string? verwendungszweck { get; set; }
            public string? buchungsdatum { get; set; }
            public string? wertstellungsdatum { get; set; }
            public string? gegenkontoInhaber { get; set; }
            public string? gegenkontoIban { get; set; }
        }
    }

    /// <summary>Ein abrufbares Konto (Produkt) eines BANKSapi-Bankzugangs.</summary>
    public class BankProduct
    {
        public string AccessId { get; set; } = "";
        public string ProductId { get; set; } = "";
        public string Display { get; set; } = "";
    }

    /// <summary>Ergebnis eines Kontoumsatz-Abrufs.</summary>
    public class BankTransactionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<BankTransaction> Transactions { get; set; } = new();
    }

    /// <summary>Fehler bei der Kommunikation mit der BANKSapi.</summary>
    public class BanksApiException : Exception
    {
        public BanksApiException(string message) : base(message) { }
    }

    /// <summary>
    /// Spezialfall von <see cref="BanksApiException"/>: Die API hat das Anlegen eines
    /// Bankzugangs mit HTTP 400 ("credentials") abgelehnt, weil der technische User in einem
    /// inkonsistenten Zustand ist. Signalisiert dem Aufrufer, dass mit einem frischen
    /// technischen User erneut versucht werden soll.
    /// </summary>
    public class BanksApiCredentialsConflictException : BanksApiException
    {
        public BanksApiCredentialsConflictException(string message) : base(message) { }
    }
}
