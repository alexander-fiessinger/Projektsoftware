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
                invitees = (object?)null
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

            var meetingResponse = JsonSerializer.Deserialize<WebexMeetingResponse>(responseBody, JsonOptions)
                ?? throw new Exception("Ungültige Antwort von Webex API");

            // Teilnehmer separat einladen damit sie RSVP-E-Mail (Annehmen/Ablehnen) erhalten
            if (invitees.Count > 0 && config.SendInviteEmails)
                meetingResponse.InviteeErrors = await SendInviteesAsync(meetingResponse.Id!, meeting.Participants);

            return meetingResponse;
        }

        /// <summary>
        /// Lädt Teilnehmer einzeln per POST /meetingInvitees ein (löst RSVP-E-Mail aus)
        /// Gibt eine Liste von Fehlermeldungen zurück (leer = alles OK)
        /// </summary>
        public async Task<List<string>> SendInviteesAsync(string meetingId, string? participants)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(participants)) return errors;

            foreach (var part in participants.Split(',', ';', '\n'))
            {
                var email = part.Trim();
                if (!email.Contains('@')) continue;

                try
                {
                    var body = JsonSerializer.Serialize(new
                    {
                        meetingId,
                        email,
                        sendEmail = true
                    }, JsonOptions);
                    var content = new StringContent(body, Encoding.UTF8, "application/json");
                    var resp = await httpClient.PostAsync("meetingInvitees", content);
                    if (!resp.IsSuccessStatusCode)
                    {
                        // 409 Conflict = Teilnehmer ist bereits eingeladen (z.B. Organisator) – kein Fehler
                        if (resp.StatusCode == System.Net.HttpStatusCode.Conflict)
                            continue;

                        var respBody = await resp.Content.ReadAsStringAsync();
                        errors.Add($"{email}: {resp.StatusCode} – {respBody}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{email}: {ex.Message}");
                }
            }
            return errors;
        }

        /// <summary>
        /// Erstellt ein Webex-Meeting für einen Sales-Termin und gibt (MeetingId, JoinLink) zurück.
        /// </summary>
        public async Task<(string MeetingId, string JoinLink)> CreateSalesMeetingAsync(Models.SalesAppointment appt)
        {
            await EnsureValidTokenAsync();

            // DateTime als Local behandeln (Kind=Unspecified → Local forcieren), dann als DateTimeOffset für korrekte UTC-Konvertierung
            var startLocal = appt.AppointmentDate.Kind == DateTimeKind.Utc
                ? appt.AppointmentDate.ToLocalTime()
                : DateTime.SpecifyKind(appt.AppointmentDate, DateTimeKind.Local);
            var endLocal   = appt.AppointmentEnd.Kind == DateTimeKind.Utc
                ? appt.AppointmentEnd.ToLocalTime()
                : DateTime.SpecifyKind(appt.AppointmentEnd, DateTimeKind.Local);

            var startOffset = new DateTimeOffset(startLocal);
            var endOffset   = new DateTimeOffset(endLocal);

            // Sicherheits-Check: Termin muss in der Zukunft liegen
            if (startOffset <= DateTimeOffset.UtcNow)
                throw new Exception($"Der Termin liegt in der Vergangenheit ({startLocal:dd.MM.yyyy HH:mm}). Webex-Meeting kann nicht erstellt werden.");

            // Windows-Timezone-ID → IANA-ID konvertieren (Webex erwartet IANA)
            var timezone = TimeZoneInfo.Local.HasIanaId
                ? TimeZoneInfo.Local.Id
                : TimeZoneInfo.TryConvertWindowsIdToIanaId(TimeZoneInfo.Local.Id, out var ianaId)
                    ? ianaId
                    : "Europe/Berlin"; // Fallback

            var request = new
            {
                title    = appt.Title,
                start    = startLocal.ToString("yyyy-MM-ddTHH:mm:ss"),
                end      = endLocal.ToString("yyyy-MM-ddTHH:mm:ss"),
                timezone,
                agenda   = string.IsNullOrWhiteSpace(appt.Notes) ? (string?)null : appt.Notes,
                password = GenerateMeetingPassword(),
                enabledAutoRecordMeeting = false,
                allowAnyUserToBeCoHost   = false,
                enabledJoinBeforeHost    = true,
                joinBeforeHostMinutes    = 5,
                sendEmail = false
            };

            var json    = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("meetings", content);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Webex API Fehler ({(int)response.StatusCode}): {body}");

            var result = JsonSerializer.Deserialize<WebexMeetingResponse>(body, JsonOptions)
                ?? throw new Exception("Ungültige Antwort von Webex API");

            return (result.Id ?? "", result.WebLink ?? result.JoinLink ?? "");
        }

        /// <summary>
        /// Löscht ein Webex-Meeting (z. B. bei Absage eines Sales-Termins).
        /// Fehler werden ignoriert wenn das Meeting bereits weg ist (404).
        /// </summary>
        public async Task DeleteSalesMeetingAsync(string meetingId)
        {
            if (string.IsNullOrEmpty(meetingId)) return;
            try
            {
                await EnsureValidTokenAsync();
                await httpClient.DeleteAsync($"meetings/{meetingId}");
            }
            catch { /* optional – Meeting evtl. schon gelöscht */ }
        }

        /// <summary>
        /// Aktualisiert ein bestehendes Webex-Meeting</summary>
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
        /// Ruft die Eingeladenen eines Meetings mit ihrem RSVP-Status ab
        /// </summary>
        public async Task<List<WebexInvitee>> GetMeetingInviteesAsync(string webexMeetingId)
        {
            var (items, _) = await GetMeetingInviteesWithDebugAsync(webexMeetingId);
            return items;
        }

        /// <summary>
        /// Wie GetMeetingInviteesAsync, gibt aber zusätzlich die rohe API-Antwort zurück
        /// </summary>
        public async Task<(List<WebexInvitee> Items, string RawResponse)> GetMeetingInviteesWithDebugAsync(string webexMeetingId)
        {
            await EnsureValidTokenAsync();

            // Versuche zuerst mit der übergebenen ID, dann ohne hostEmail-Filter
            var url = $"meetingInvitees?meetingId={Uri.EscapeDataString(webexMeetingId)}&max=100";
            var response = await httpClient.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Webex API Fehler ({response.StatusCode}): {body}");

            var result = JsonSerializer.Deserialize<WebexInviteesResponse>(body, JsonOptions);
            return (result?.Items ?? new List<WebexInvitee>(), body);
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
        /// <summary>Fehler beim Einladen von Teilnehmern (leer = alle erfolgreich)</summary>
        [JsonIgnore]
        public List<string> InviteeErrors { get; set; } = new();

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

    public class WebexInviteesResponse
    {
        [JsonPropertyName("items")]
        public List<WebexInvitee>? Items { get; set; }
    }

    public class WebexInvitee
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("rsvpStatus")]
        public string? RsvpStatus { get; set; }

        [JsonPropertyName("coHost")]
        public bool CoHost { get; set; }

        public string StatusIcon => RsvpStatus switch
        {
            "accept" => "✅",
            "decline" => "❌",
            "tentative" => "❓",
            _ => "⏳"
        };

        public string StatusText => RsvpStatus switch
        {
            "accept" => "Angenommen",
            "decline" => "Abgesagt",
            "tentative" => "Tentativ",
            _ => "Ausstehend"
        };

        public string DisplayLabel => $"{StatusIcon}  {Email ?? DisplayName ?? "–"}  ({StatusText})";
    }
}
