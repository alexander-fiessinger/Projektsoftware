using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Projektsoftware.Api.Models;

namespace Projektsoftware.Api.Services;

/// <summary>
/// Webex REST API Client mit automatischem OAuth Token-Refresh
/// </summary>
public class ApiWebexService : IDisposable
{
    private readonly HttpClient _http;
    private readonly WebexUserSettings _settings;
    private readonly Func<string, string?, DateTime?, Task>? _onTokenRefreshed;
    private const string BaseUrl = "https://webexapis.com/v1/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <param name="settings">Webex-Einstellungen mit Token + OAuth-Credentials</param>
    /// <param name="onTokenRefreshed">Callback zum Speichern des neuen Tokens (newAccessToken, newRefreshToken, newExpiry)</param>
    public ApiWebexService(WebexUserSettings settings, Func<string, string?, DateTime?, Task>? onTokenRefreshed = null)
    {
        _settings = settings;
        _onTokenRefreshed = onTokenRefreshed;
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task EnsureValidTokenAsync()
    {
        if (!_settings.IsTokenExpired || !_settings.CanRefresh) return;
        await RefreshAccessTokenAsync();
    }

    private async Task RefreshAccessTokenAsync()
    {
        using var refreshClient = new HttpClient();
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["refresh_token"] = _settings.RefreshToken
        });

        var response = await refreshClient.PostAsync("https://webexapis.com/v1/access_token", form);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Webex Token-Refresh fehlgeschlagen ({response.StatusCode}): {json}");

        var tokenData = JsonSerializer.Deserialize<WebexTokenResponse>(json, JsonOptions)
            ?? throw new Exception("Ungültige Token-Antwort von Webex");

        _settings.AccessToken = tokenData.AccessToken ?? "";
        if (!string.IsNullOrWhiteSpace(tokenData.RefreshToken))
            _settings.RefreshToken = tokenData.RefreshToken;
        _settings.TokenExpiry = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);

        if (_onTokenRefreshed != null)
            await _onTokenRefreshed(_settings.AccessToken, _settings.RefreshToken, _settings.TokenExpiry);
    }

    public async Task<(bool Ok, string Message)> TestConnectionAsync()
    {
        try
        {
            await EnsureValidTokenAsync();
            var response = await _http.GetAsync("people/me");
            var body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && _settings.CanRefresh)
            {
                await RefreshAccessTokenAsync();
                response = await _http.GetAsync("people/me");
                body = await response.Content.ReadAsStringAsync();
            }

            if (!response.IsSuccessStatusCode)
                return (false, $"Fehler ({response.StatusCode}): {body}");

            var person = JsonSerializer.Deserialize<WebexPerson>(body, JsonOptions);
            return (true, $"✅ Verbunden als {person?.DisplayName ?? "?"} ({person?.Email ?? ""})");
        }
        catch (Exception ex)
        {
            return (false, $"Verbindungsfehler: {ex.Message}");
        }
    }

    public async Task<WebexMeetingResult> CreateMeetingAsync(string title, DateTime start, DateTime end,
        string? description = null, string? participants = null)
    {
        await EnsureValidTokenAsync();
        var invitees = BuildInvitees(participants);

        var request = new
        {
            title,
            start = start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            end = end.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            agenda = description,
            password = GeneratePassword(),
            enabledAutoRecordMeeting = false,
            allowAnyUserToBeCoHost = false,
            enabledJoinBeforeHost = true,
            joinBeforeHostMinutes = 5,
            sendEmail = true,
            invitees = invitees.Count > 0 ? invitees : null
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("meetings", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && _settings.CanRefresh)
        {
            await RefreshAccessTokenAsync();
            content = new StringContent(json, Encoding.UTF8, "application/json");
            response = await _http.PostAsync("meetings", content);
            responseBody = await response.Content.ReadAsStringAsync();
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("Webex Access Token ist ungültig oder abgelaufen und konnte nicht erneuert werden.");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("Webex: Fehlende Berechtigungen oder keine Meetings-Lizenz.");
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Webex API Fehler ({response.StatusCode}): {responseBody}");

        var result = JsonSerializer.Deserialize<WebexMeetingResult>(responseBody, JsonOptions)
            ?? throw new Exception("Ungültige Antwort von Webex API");
        return result;
    }

    public async Task DeleteMeetingAsync(string webexMeetingId)
    {
        await EnsureValidTokenAsync();
        var response = await _http.DeleteAsync($"meetings/{webexMeetingId}");
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Webex Löschen fehlgeschlagen ({response.StatusCode}): {error}");
        }
    }

    private static List<object> BuildInvitees(string? participants)
    {
        var list = new List<object>();
        if (string.IsNullOrWhiteSpace(participants)) return list;
        foreach (var part in participants.Split(',', ';', '\n'))
        {
            var email = part.Trim();
            if (email.Contains('@'))
                list.Add(new { email, coHost = false });
        }
        return list;
    }

    private static string GeneratePassword()
    {
        var rng = new Random();
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[rng.Next(s.Length)]).ToArray());
    }

    public void Dispose() => _http.Dispose();
}

// ── Webex Response Models ──────────────────────────────────────────

public class WebexMeetingResult
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("webLink")]
    public string? WebLink { get; set; }

    [JsonPropertyName("joinLink")]
    public string? JoinLink { get; set; }

    [JsonPropertyName("sipAddress")]
    public string? SipAddress { get; set; }

    [JsonPropertyName("hostKey")]
    public string? HostKey { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class WebexPerson
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
