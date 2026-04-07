using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Webex REST API Integration für Meeting-Erstellung und -Verwaltung
    /// Dokumentation: https://developer.webex.com/docs/meetings
    /// </summary>
    public class WebexService
    {
        private readonly HttpClient httpClient;
        private readonly WebexConfig config;
        private const string BaseUrl = "https://webexapis.com/v1/";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public WebexService()
        {
            config = WebexConfig.Load();
            httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", config.AccessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public bool IsConfigured => config.IsConfigured;

        private async Task EnsureValidTokenAsync()
        {
            if (!config.IsTokenExpired || !config.CanRefresh) return;
            try
            {
                await RefreshAccessTokenAsync();
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException(
                    "Webex Token konnte nicht automatisch erneuert werden.\n" +
                    "Bitte melden Sie sich erneut an unter Einstellungen \u2192 Webex.\n\n" +
                    $"Fehler: {ex.Message}");
            }
        }

        public async Task RefreshAccessTokenAsync()
        {
            using var refreshClient = new HttpClient();
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = config.ClientId,
                ["client_secret"] = config.ClientSecret,
                ["refresh_token"] = config.RefreshToken
            });
            var response = await refreshClient.PostAsync("https://webexapis.com/v1/access_token", form);
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"HTTP {(int)response.StatusCode}: {json}");
            var tokenData = JsonSerializer.Deserialize<WebexTokenResponse>(json)
                ?? throw new Exception("Ung\u00fcltige Token-Antwort von Webex");
            config.AccessToken = tokenData.AccessToken ?? "";
            if (!string.IsNullOrWhiteSpace(tokenData.RefreshToken))
                config.RefreshToken = tokenData.RefreshToken;
            config.TokenExpiry = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);
            config.Save();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", config.AccessToken);
        }

        #region Meeting CRUD

        /// <summary>
        /// Erstellt ein neues Webex-Meeting
        /// </summary>
        public async Task<WebexMeetingResponse> CreateMeetingAsync(Meeting meeting)
        {
            await EnsureValidTokenAsync();
            var invitees = BuildInvitees(meeting.Participants);

            var request = new
            {
                title = meeting.Title,
                start = meeting.StartTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                end = meeting.EndTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                agenda = meeting.Description,
                password = GenerateMeetingPassword(),
                enabledAutoRecordMeeting = false,
                allowAnyUserToBeCoHost = false,
                enabledJoinBeforeHost = true,
                joinBeforeHostMinutes = 5,
                sendEmail = config.SendInviteEmails,
                invitees = invitees.Count > 0 ? invitees : null
            };

            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("meetings", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException(
                    "Webex Access Token ist ungültig oder abgelaufen.\n" +
                    "Bitte aktualisieren Sie den Token unter Einstellungen → Webex.");
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException(
                    "Webex: Fehlende Berechtigungen oder keine Meetings-Lizenz.\n\n" +
                    "Mögliche Ursachen:\n" +
                    "• Das Token hat nicht den Scope 'meeting:schedules_write'\n" +
                    "• Der Webex-Account hat keine Meetings-Lizenz\n" +
                    "• Bot-Token werden für die Meetings-API nicht unterstützt\n\n" +
                    "Lösung: Melden Sie sich erneut über die OAuth-Anmeldung in den\n" +
                    "Einstellungen → Webex an (Schritt 1–3). Bot-Token können keine\n" +
                    "Meetings erstellen – es wird ein persönliches Benutzerkonto benötigt.");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Webex API Fehler ({response.StatusCode}): {responseBody}");

            return JsonSerializer.Deserialize<WebexMeetingResponse>(responseBody, JsonOptions)
                ?? throw new Exception("Ungültige Antwort von Webex API");
        }

        /// <summary>
        /// Aktualisiert ein bestehendes Webex-Meeting
        /// </summary>
        public async Task<WebexMeetingResponse> UpdateMeetingAsync(string webexMeetingId, Meeting meeting)
        {
            await EnsureValidTokenAsync();
            var request = new
            {
                title = meeting.Title,
                start = meeting.StartTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                end = meeting.EndTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                agenda = meeting.Description
            };

            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PutAsync($"meetings/{webexMeetingId}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException(
                    "Webex Access Token ist ungültig oder abgelaufen.\n" +
                    "Bitte aktualisieren Sie den Token unter Einstellungen → Webex.");
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException(
                    "Webex: Fehlende Berechtigungen oder keine Meetings-Lizenz.\n\n" +
                    "Mögliche Ursachen:\n" +
                    "• Das Token hat nicht den Scope 'meeting:schedules_write'\n" +
                    "• Der Webex-Account hat keine Meetings-Lizenz\n" +
                    "• Bot-Token werden für die Meetings-API nicht unterstützt\n\n" +
                    "Lösung: Melden Sie sich erneut über die OAuth-Anmeldung in den\n" +
                    "Einstellungen → Webex an (Schritt 1–3). Bot-Token können keine\n" +
                    "Meetings erstellen – es wird ein persönliches Benutzerkonto benötigt.");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Webex API Fehler ({response.StatusCode}): {responseBody}");

            return JsonSerializer.Deserialize<WebexMeetingResponse>(responseBody, JsonOptions)
                ?? throw new Exception("Ungültige Antwort von Webex API");
        }

        /// <summary>
        /// Löscht ein Webex-Meeting
        /// </summary>
        public async Task DeleteMeetingAsync(string webexMeetingId)
        {
            var response = await httpClient.DeleteAsync($"meetings/{webexMeetingId}");

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Webex API Fehler ({response.StatusCode}): {error}");
            }
        }

        /// <summary>
        /// Ruft Details zu einem Meeting ab
        /// </summary>
        public async Task<WebexMeetingResponse?> GetMeetingAsync(string webexMeetingId)
        {
            var response = await httpClient.GetAsync($"meetings/{webexMeetingId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Webex API Fehler ({response.StatusCode}): {responseBody}");

            return JsonSerializer.Deserialize<WebexMeetingResponse>(responseBody, JsonOptions);
        }

        /// <summary>
        /// Prüft ob API-Verbindung funktioniert (lädt eigene Benutzerinfos)
        /// </summary>
        public async Task<WebexPersonInfo> TestConnectionAsync()
        {
            await EnsureValidTokenAsync();
            var response = await httpClient.GetAsync("people/me");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Webex-Verbindung fehlgeschlagen ({response.StatusCode}): {body}");

            return JsonSerializer.Deserialize<WebexPersonInfo>(body, JsonOptions)
                ?? throw new Exception("Ungültige Antwort von Webex API");
        }

        /// <summary>
        /// Prüft ob der Token den Scope 'meeting:schedules_write' besitzt.
        /// Gibt true zurück wenn ja, false bei 403 (Scope fehlt), null bei anderem Fehler.
        /// </summary>
        public async Task<bool?> TestMeetingsScopeAsync()
        {
            try
            {
                var response = await httpClient.GetAsync("meetings?max=1");
                if (response.IsSuccessStatusCode) return true;
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden) return false;
                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Helpers

        private static List<object> BuildInvitees(string? participants)
        {
            var invitees = new List<object>();
            if (string.IsNullOrWhiteSpace(participants))
                return invitees;

            foreach (var part in participants.Split(',', ';', '\n'))
            {
                var email = part.Trim();
                if (email.Contains('@'))
                    invitees.Add(new { email, coHost = false });
            }
            return invitees;
        }

        private static string GenerateMeetingPassword()
        {
            var random = new Random();
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #endregion
    }

    // ── Webex API Response Models ─────────────────────────────────────────────

    public class WebexMeetingResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("start")]
        public string? Start { get; set; }

        [JsonPropertyName("end")]
        public string? End { get; set; }

        [JsonPropertyName("webLink")]
        public string? WebLink { get; set; }

        [JsonPropertyName("joinLink")]
        public string? JoinLink { get; set; }

        [JsonPropertyName("sipAddress")]
        public string? SipAddress { get; set; }

        [JsonPropertyName("dialInIpAddress")]
        public string? DialInIpAddress { get; set; }

        [JsonPropertyName("hostKey")]
        public string? HostKey { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    public class WebexPersonInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("emails")]
        public string[]? Emails { get; set; }

        public string Email => Emails?.FirstOrDefault() ?? "";
    }

    public class WebexTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
