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

    /// <summary>
    /// Erfasst eine Zahlung für ein Dokument. Beträge werden – wie von Easybill erwartet – in Cent gesendet.
    /// </summary>
    /// <param name="amountCents">Zu buchender Betrag in Cent. Wenn null, wird der noch offene Betrag gebucht.</param>
    /// <param name="markFullyPaid">true = Rechnung vollständig als bezahlt markieren; false = nur (Teil-)Zahlung erfassen.</param>
    public async Task<EbDocument> MarkAsPaidAsync(long documentId, long? amountCents = null, bool markFullyPaid = true, string paymentType = "BANK_TRANSFER")
    {
        // Aktuellen Stand laden, um den offenen Betrag zu bestimmen.
        var document = await GetDocumentAsync(documentId);
        var gross = document.Amount ?? document.TotalGross ?? 0;
        var open = gross - (document.PaidAmount ?? 0);
        var payAmount = amountCents ?? (open > 0 ? open : gross);

        var payment = new
        {
            document_id = documentId,
            amount = payAmount, // Cent
            type = paymentType,
            payment_at = DateTime.Now.ToString("yyyy-MM-dd")
        };

        var paidFlag = markFullyPaid ? "true" : "false";
        var content = new StringContent(JsonSerializer.Serialize(payment), Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync($"document-payments?paid={paidFlag}", content);

        if (!resp.IsSuccessStatusCode)
        {
            var error = await resp.Content.ReadAsStringAsync();
            throw new Exception($"Fehler beim Buchen der Zahlung: {resp.StatusCode} – {error}");
        }

        return await GetDocumentAsync(documentId);
    }

    /// <summary>
    /// Konvertiert ein Dokument in einen anderen Typ über POST documents/{id}/{apiType}.
    /// Erlaubte Ziel-Typen (Easybill): INVOICE, CREDIT, DELIVERY (Lieferschein),
    /// CHARGE_CONFIRM (Auftragsbestätigung), ORDER, DUNNING, REMINDER, CHARGE.
    /// </summary>
    public async Task<EbDocument> ConvertDocumentAsync(long documentId, string targetType)
    {
        var apiType = targetType switch
        {
            "DELIVERY_NOTE" => "DELIVERY",
            "ORDER_CONFIRMATION" => "CHARGE_CONFIRM",
            _ => targetType
        };

        var resp = await _http.PostAsync($"documents/{documentId}/{apiType}", null);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Fehler beim Konvertieren: {resp.StatusCode} – {json}");

        return JsonSerializer.Deserialize<EbDocument>(json, _json)!;
    }

    /// <summary>
    /// Storniert ein Dokument über POST documents/{id}/cancel (erzeugt i. d. R. eine Storno-Rechnung).
    /// </summary>
    public async Task<EbDocument> CancelDocumentAsync(long documentId)
    {
        var resp = await _http.PostAsync($"documents/{documentId}/cancel", null);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Fehler beim Stornieren: {resp.StatusCode} – {json}");

        return JsonSerializer.Deserialize<EbDocument>(json, _json)!;
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

    // ── Products ────────────────────────────────────────────────────

    public async Task<List<EbProduct>> GetProductsAsync()
    {
        var all = new List<EbProduct>();
        int page = 1, totalPages = 1;

        while (page <= totalPages)
        {
            var resp = await _http.GetAsync($"products?page={page}&limit=100");
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EbProductList>(json, _json);

            if (result?.Items != null)
            {
                all.AddRange(result.Items);
                totalPages = result.Pages;
            }
            page++;
        }
        return all;
    }

    // ── Positions (Zeit-/Leistungs-Export) ──────────────────────────

    public async Task<List<EbPosition>> GetCustomerPositionsAsync(long customerId)
    {
        var all = new List<EbPosition>();
        int page = 1, totalPages = 1;

        while (page <= totalPages)
        {
            var resp = await _http.GetAsync($"positions?customer_id={customerId}&page={page}&limit=100");
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EbPositionList>(json, _json);

            if (result?.Items != null)
            {
                all.AddRange(result.Items);
                totalPages = result.Pages;
            }
            page++;
        }
        return all;
    }

    /// <summary>
    /// Erstellt eine Easybill-Position (Leistung) für einen Kunden, z. B. aus einem Zeiteintrag.
    /// </summary>
    public async Task<EbPosition> CreatePositionAsync(long customerId, string description, decimal quantity, decimal hourlyRate, decimal vatPercent = 19m)
    {
        var position = new EbPosition
        {
            CustomerId = customerId,
            Number = $"ZE-{DateTime.Now:yyyyMMddHHmmss}",
            Description = description,
            Quantity = quantity,
            SinglePriceNet = (long)Math.Round(hourlyRate * 100m),
            VatPercent = vatPercent
        };

        var content = new StringContent(JsonSerializer.Serialize(position, _json), Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync("positions", content);

        if (!resp.IsSuccessStatusCode)
        {
            var error = await resp.Content.ReadAsStringAsync();
            throw new Exception($"Fehler beim Erstellen der Position: {resp.StatusCode} – {error}");
        }

        var resultJson = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EbPosition>(resultJson, _json)!;
    }

    public void Dispose() => _http.Dispose();
}
