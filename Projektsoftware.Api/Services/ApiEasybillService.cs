using Projektsoftware.Api.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Projektsoftware.Api.Services;

/// <summary>
/// Server-seitiger Easybill REST API Client.
/// Wird pro Benutzer mit dessen Zugangsdaten initialisiert.
/// </summary>
public class ApiEasybillService : IDisposable
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    public ApiEasybillService(string email, string apiKey)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri("https://api.easybill.de/rest/v1/")
        };

        var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{email}:{apiKey}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", creds);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Projektsoftware-Web/1.0");

        _json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    // ── Connection Test ─────────────────────────────────────────────

    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        try
        {
            var resp = await _http.GetAsync("customers?limit=1");
            return resp.IsSuccessStatusCode
                ? (true, "Verbindung erfolgreich!")
                : (false, $"HTTP {(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync()}");
        }
        catch (Exception ex)
        {
            return (false, $"Fehler: {ex.Message}");
        }
    }

    // ── Documents ───────────────────────────────────────────────────

    public async Task<List<EbDocument>> GetDocumentsAsync(string? type = null)
    {
        var all = new List<EbDocument>();
        int page = 1, totalPages = 1;

        while (page <= totalPages)
        {
            var url = $"documents?page={page}&limit=100";
            if (!string.IsNullOrEmpty(type)) url += $"&type={type}";

            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EbDocumentList>(json, _json);

            if (result?.Items != null)
            {
                all.AddRange(result.Items);
                totalPages = result.Pages;
            }
            page++;
        }
        return all;
    }

    public async Task<EbDocument> GetDocumentAsync(long documentId)
    {
        var resp = await _http.GetAsync($"documents/{documentId}");
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EbDocument>(json, _json)!;
    }

    public async Task<EbDocument> CreateDocumentAsync(EbDocument document)
    {
        var content = new StringContent(JsonSerializer.Serialize(document, _json), Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync("documents", content);

        if (!resp.IsSuccessStatusCode)
        {
            var error = await resp.Content.ReadAsStringAsync();
            throw new Exception($"Easybill API-Fehler: {resp.StatusCode} – {error}");
        }

        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EbDocument>(json, _json)!;
    }

    public async Task<EbDocument> FinalizeDocumentAsync(long documentId)
    {
        var resp = await _http.PutAsync($"documents/{documentId}/done", null);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EbDocument>(json, _json)!;
    }

    public async Task DeleteDocumentAsync(long documentId)
    {
        var resp = await _http.DeleteAsync($"documents/{documentId}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<byte[]> DownloadPdfAsync(long documentId)
    {
        var resp = await _http.GetAsync($"documents/{documentId}/pdf");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync();
    }

    public async Task SendDocumentEmailAsync(long documentId, string to, string subject, string message)
    {
        var data = new { to, subject, message };
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync($"documents/{documentId}/send/email", content);

        if (!resp.IsSuccessStatusCode)
        {
            var error = await resp.Content.ReadAsStringAsync();
            throw new Exception($"Fehler beim Versenden: {resp.StatusCode} – {error}");
        }
    }

    // ── Customers ───────────────────────────────────────────────────

    public async Task<List<EbCustomer>> GetCustomersAsync()
    {
        var all = new List<EbCustomer>();
        int page = 1, totalPages = 1;

        while (page <= totalPages)
        {
            var resp = await _http.GetAsync($"customers?page={page}&limit=100");
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EbCustomerList>(json, _json);

            if (result?.Items != null)
            {
                all.AddRange(result.Items);
                totalPages = result.Pages;
            }
            page++;
        }
        return all;
    }

    public void Dispose() => _http.Dispose();
}
