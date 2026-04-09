using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Projektsoftware.Services
{
    public class EwsService
    {
        private readonly EwsConfig config;

        public EwsService()
        {
            config = EwsConfig.Load();
        }

        public EwsService(EwsConfig config)
        {
            this.config = config;
        }

        public bool IsConfigured => config.IsConfigured;

        public async Task<List<InboxEmail>> FetchInboxEmailsAsync(List<Customer> customers, int maxMessages = 50)
        {
            // ── Kundenlookups aufbauen ────────────────────────────────────────
            // 1) Exakter E-Mail-Match (Trim + Lowercase)
            var exactLookup = customers
                .Where(c => !string.IsNullOrWhiteSpace(c.Email))
                .GroupBy(c => c.Email!.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            // 2) Domain-Fallback (ignoriert allgemeine Provider)
            var genericDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "gmail.com", "googlemail.com", "outlook.com", "hotmail.com", "hotmail.de",
                "yahoo.com", "yahoo.de", "web.de", "gmx.de", "gmx.net", "gmx.com",
                "t-online.de", "freenet.de", "icloud.com", "me.com", "live.com", "live.de"
            };

            var domainLookup = customers
                .Where(c => !string.IsNullOrWhiteSpace(c.Email) && c.Email.Contains('@'))
                .GroupBy(c => c.Email!.Trim().ToLowerInvariant().Split('@')[1])
                .Where(g => !genericDomains.Contains(g.Key))
                .ToDictionary(g => g.Key, g => g.First());

            using var client = CreateEwsHttpClient();

            var findXml = await PostEwsAsync(client, BuildFindItemSoap(maxMessages));
            var items = ParseFindItems(findXml);

            if (items.Count == 0) return new List<InboxEmail>();

            var getXml = await PostEwsAsync(client, BuildGetItemSoap(items.Select(i => (i.ItemId, i.ChangeKey)).ToList()));
            var bodies = ParseGetItemBodies(getXml);

            var emails = new List<InboxEmail>();
            foreach (var item in items)
            {
                var email = new InboxEmail
                {
                    EwsItemId = item.ItemId,
                    Subject = item.Subject,
                    FromName = item.FromName,
                    FromEmail = item.FromEmail,
                    Date = item.Date,
                    IsRead = item.IsRead,
                    Body = bodies.TryGetValue(item.ItemId, out var body) ? body : string.Empty
                };

                // Absender normalisieren: "SMTP:user@domain.de" → "user@domain.de"
                var fromNormalized = email.FromEmail.Trim();
                if (fromNormalized.StartsWith("SMTP:", StringComparison.OrdinalIgnoreCase))
                    fromNormalized = fromNormalized[5..].Trim();
                var fromLower = fromNormalized.ToLowerInvariant();

                // 1) Exakter Match
                if (exactLookup.TryGetValue(fromLower, out var exactCustomer))
                {
                    email.MatchedCustomer = exactCustomer;
                }
                // 2) Domain-Fallback (nur für Unternehmensdomains)
                else if (fromLower.Contains('@'))
                {
                    var domain = fromLower.Split('@')[1];
                    if (domainLookup.TryGetValue(domain, out var domainCustomer))
                        email.MatchedCustomer = domainCustomer;
                }

                emails.Add(email);
            }

            return emails;
        }

        public async Task<(bool Success, string Error)> TestConnectionAsync()
        {
            try
            {
                using var client = CreateEwsHttpClient();

                const string soap = """
                    <?xml version="1.0" encoding="utf-8"?>
                    <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                                   xmlns:t="http://schemas.microsoft.com/exchange/services/2006/types"
                                   xmlns:m="http://schemas.microsoft.com/exchange/services/2006/messages">
                      <soap:Body>
                        <m:GetFolder>
                          <m:FolderShape><t:BaseShape>Default</t:BaseShape></m:FolderShape>
                          <m:FolderIds><t:DistinguishedFolderId Id="inbox"/></m:FolderIds>
                        </m:GetFolder>
                      </soap:Body>
                    </soap:Envelope>
                    """;

                await PostEwsAsync(client, soap);
                var rawUser = string.IsNullOrWhiteSpace(config.EwsUsername) ? config.EwsEmail : config.EwsUsername;
                var dom = config.EwsDomain?.Trim() ?? string.Empty;
                string authLabel;
                if (config.UseWindowsAuth)
                    authLabel = "Windows-Authentifizierung";
                else if (config.UseBasicAuth)
                    authLabel = $"Basic Auth • {rawUser}";
                else if (!string.IsNullOrWhiteSpace(config.EwsUsername) && !string.IsNullOrWhiteSpace(dom))
                    authLabel = $"NTLM {dom}\\{config.EwsUsername}";
                else
                    authLabel = $"NTLM/UPN • {rawUser}";
                return (true, $"Verbindung erfolgreich • {authLabel}");
            }
            catch (Exception ex)
            {
                var rawUser = string.IsNullOrWhiteSpace(config.EwsUsername) ? config.EwsEmail : config.EwsUsername;
                var dom = config.EwsDomain?.Trim() ?? string.Empty;
                string authLabel;
                if (config.UseWindowsAuth)
                    authLabel = string.Empty;
                else if (config.UseBasicAuth)
                    authLabel = $"\nVerwendeter Benutzer: {rawUser} (Basic Auth)";
                else if (!string.IsNullOrWhiteSpace(config.EwsUsername) && !string.IsNullOrWhiteSpace(dom))
                    authLabel = $"\nVerwendeter Benutzer: {dom}\\{config.EwsUsername} (NTLM)";
                else
                    authLabel = $"\nVerwendeter Benutzer: {rawUser} (NTLM/UPN)";
                var msg = ex.InnerException is not null ? $"{ex.Message} → {ex.InnerException.Message}" : ex.Message;
                return (false, msg + authLabel);
            }
        }

        // ── HTTP / Auth ──────────────────────────────────────────────────────

        private HttpClient CreateEwsHttpClient()
        {
            var rawUsername = string.IsNullOrWhiteSpace(config.EwsUsername) ? config.EwsEmail : config.EwsUsername;

            if (config.UseBasicAuth)
            {
                // Force Basic Auth: preemptive Authorization header
                var handler = new HttpClientHandler();
                if (config.AcceptInvalidCertificates)
                    handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                handler.UseDefaultCredentials = false;
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{rawUsername}:{config.EwsPassword}"));
                var basicClient = new HttpClient(handler);
                basicClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", token);
                return basicClient;
            }

            var wh = new HttpClientHandler();
            if (config.AcceptInvalidCertificates)
                wh.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

            if (config.UseWindowsAuth)
            {
                wh.UseDefaultCredentials = true;
                return new HttpClient(wh);
            }

            var domain = config.EwsDomain?.Trim() ?? string.Empty;
            var baseUri = new Uri(new Uri(config.EwsUrl).GetLeftPart(UriPartial.Authority));
            var credCache = new CredentialCache();

            // NTLM credential selection:
            // – custom EwsUsername set → SAM format (domain\username)
            // – email only (no custom username) → UPN format (full email, no domain)
            NetworkCredential ntlmCred = !string.IsNullOrWhiteSpace(config.EwsUsername) && !string.IsNullOrWhiteSpace(domain)
                ? new NetworkCredential(config.EwsUsername, config.EwsPassword, domain)
                : new NetworkCredential(rawUsername, config.EwsPassword);

            credCache.Add(baseUri, "NTLM",  ntlmCred);
            credCache.Add(baseUri, "Basic", new NetworkCredential(rawUsername, config.EwsPassword));

            wh.Credentials = credCache;
            return new HttpClient(wh);
        }

        private async Task<string> PostEwsAsync(HttpClient client, string soap)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, config.EwsUrl);
            request.Content = new StringContent(soap, Encoding.UTF8, "text/xml");
            request.Headers.TryAddWithoutValidation("SOAPAction", "\"\"");

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var wwwAuth = response.Headers.WwwAuthenticate.ToString();
                var body = await response.Content.ReadAsStringAsync();
                var bodySnippet = body.Length > 400
                    ? body[..400].Replace("\n", " ").Replace("\r", "") + "…"
                    : body.Replace("\n", " ").Replace("\r", "");
                var hint401 = (int)response.StatusCode == 401 && !string.IsNullOrEmpty(wwwAuth)
                    ? $"\nServer unterstützt: {wwwAuth}"
                    : string.Empty;
                var bodyHint = !string.IsNullOrWhiteSpace(bodySnippet)
                    ? $"\nServer-Antwort: {bodySnippet}"
                    : string.Empty;
                throw new HttpRequestException(
                    $"EWS HTTP {(int)response.StatusCode}: {response.ReasonPhrase}{hint401}{bodyHint}");
            }
            return await response.Content.ReadAsStringAsync();
        }

        // ── SOAP-Builder ─────────────────────────────────────────────────────

        private string BuildFindItemSoap(int maxItems) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                           xmlns:t="http://schemas.microsoft.com/exchange/services/2006/types"
                           xmlns:m="http://schemas.microsoft.com/exchange/services/2006/messages">
              <soap:Body>
                <m:FindItem Traversal="Shallow">
                  <m:ItemShape>
                    <t:BaseShape>IdOnly</t:BaseShape>
                    <t:AdditionalProperties>
                      <t:FieldURI FieldURI="item:Subject"/>
                      <t:FieldURI FieldURI="item:DateTimeReceived"/>
                      <t:FieldURI FieldURI="message:From"/>
                      <t:FieldURI FieldURI="message:IsRead"/>
                    </t:AdditionalProperties>
                  </m:ItemShape>
                  <m:IndexedPageItemView MaxEntriesReturned="{maxItems}" Offset="0" BasePoint="Beginning"/>
                  <m:SortOrder>
                    <t:FieldOrder Order="Descending">
                      <t:FieldURI FieldURI="item:DateTimeReceived"/>
                    </t:FieldOrder>
                  </m:SortOrder>
                  <m:ParentFolderIds>
                    <t:DistinguishedFolderId Id="inbox"/>
                  </m:ParentFolderIds>
                </m:FindItem>
              </soap:Body>
            </soap:Envelope>
            """;

        private string BuildGetItemSoap(List<(string ItemId, string ChangeKey)> items)
        {
            var ids = string.Join("\n    ", items.Select(i =>
                $"""<t:ItemId Id="{i.ItemId}" ChangeKey="{i.ChangeKey}"/>"""));
            return $"""
                <?xml version="1.0" encoding="utf-8"?>
                <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                               xmlns:t="http://schemas.microsoft.com/exchange/services/2006/types"
                               xmlns:m="http://schemas.microsoft.com/exchange/services/2006/messages">
                  <soap:Body>
                    <m:GetItem>
                      <m:ItemShape>
                        <t:BaseShape>Default</t:BaseShape>
                        <t:BodyType>Text</t:BodyType>
                      </m:ItemShape>
                      <m:ItemIds>
                        {ids}
                      </m:ItemIds>
                    </m:GetItem>
                  </soap:Body>
                </soap:Envelope>
                """;
        }

        // ── XML-Parser ───────────────────────────────────────────────────────

        private static readonly XNamespace _t = "http://schemas.microsoft.com/exchange/services/2006/types";
        private static readonly XNamespace _m = "http://schemas.microsoft.com/exchange/services/2006/messages";

        private record EwsItem(string ItemId, string ChangeKey, string Subject,
                               string FromName, string FromEmail, DateTime Date, bool IsRead);

        private List<EwsItem> ParseFindItems(string xml)
        {
            var results = new List<EwsItem>();
            var doc = XDocument.Parse(xml);

            foreach (var msg in doc.Descendants(_t + "Message"))
            {
                var itemIdEl = msg.Element(_t + "ItemId");
                if (itemIdEl == null) continue;

                var from = msg.Element(_t + "From")?.Element(_t + "Mailbox");
                var dateStr = msg.Element(_t + "DateTimeReceived")?.Value;

                results.Add(new EwsItem(
                    ItemId: itemIdEl.Attribute("Id")?.Value ?? string.Empty,
                    ChangeKey: itemIdEl.Attribute("ChangeKey")?.Value ?? string.Empty,
                    Subject: msg.Element(_t + "Subject")?.Value ?? "(Kein Betreff)",
                    FromName: from?.Element(_t + "Name")?.Value ?? string.Empty,
                    FromEmail: from?.Element(_t + "EmailAddress")?.Value ?? string.Empty,
                    Date: dateStr != null
                        ? DateTime.Parse(dateStr, null, System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime()
                        : DateTime.Now,
                    IsRead: string.Equals(msg.Element(_t + "IsRead")?.Value, "true",
                                StringComparison.OrdinalIgnoreCase)
                ));
            }
            return results;
        }

        private Dictionary<string, string> ParseGetItemBodies(string xml)
        {
            var result = new Dictionary<string, string>();
            var doc = XDocument.Parse(xml);

            foreach (var msg in doc.Descendants(_t + "Message"))
            {
                var id = msg.Element(_t + "ItemId")?.Attribute("Id")?.Value;
                var body = msg.Element(_t + "Body")?.Value ?? string.Empty;
                if (id != null)
                    result[id] = body.Trim();
            }
            return result;
        }
    }
}
