using MimeKit;
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
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var wwwAuth = response.Headers.WwwAuthenticate.ToString();
                var bodySnippet = responseBody.Length > 400
                    ? responseBody[..400].Replace("\n", " ").Replace("\r", "") + "…"
                    : responseBody.Replace("\n", " ").Replace("\r", "");
                var hint401 = (int)response.StatusCode == 401 && !string.IsNullOrEmpty(wwwAuth)
                    ? $"\nServer unterstützt: {wwwAuth}"
                    : string.Empty;
                var bodyHint = !string.IsNullOrWhiteSpace(bodySnippet)
                    ? $"\nServer-Antwort: {bodySnippet}"
                    : string.Empty;
                throw new HttpRequestException(
                    $"EWS HTTP {(int)response.StatusCode}: {response.ReasonPhrase}{hint401}{bodyHint}");
            }

            // SOAP-Fault prüfen (Exchange liefert HTTP 200 auch bei Fehlern)
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    var doc = XDocument.Parse(responseBody);
                    var ns = XNamespace.Get("http://schemas.xmlsoap.org/soap/envelope/");

                    // SOAP Fault
                    var fault = doc.Descendants(ns + "Fault").FirstOrDefault();
                    if (fault != null)
                    {
                        var faultString = fault.Element("faultstring")?.Value ?? fault.ToString();
                        throw new InvalidOperationException($"EWS SOAP Fault: {faultString}");
                    }

                    // EWS ResponseCode != NoError
                    var responseCodes = doc.Descendants(_m + "ResponseCode")
                        .Concat(doc.Descendants(_t + "ResponseCode"))
                        .Where(el => el.Value != "NoError")
                        .ToList();
                    if (responseCodes.Any())
                    {
                        var codes = string.Join("; ", responseCodes.Select(el =>
                        {
                            var msgEl = el.Parent?.Element(_m + "MessageText")
                                     ?? el.Parent?.Element(_t + "MessageText");
                            return msgEl != null ? $"{el.Value}: {msgEl.Value}" : el.Value;
                        }));
                        throw new InvalidOperationException($"EWS Fehler: {codes}");
                    }
                }
                catch (InvalidOperationException) { throw; }
                catch { /* XML-Parse-Fehler ignorieren – Antwort trotzdem zurückgeben */ }
            }

            return responseBody;
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

        /// <summary>
        /// Legt einen Kalendertermin als echten EWS CalendarItem an und sendet Meeting-Requests
        /// an alle Teilnehmer (SendToAllAndSaveCopy). Gibt die von Exchange vergebene UID zurück.
        /// </summary>
        public async Task<string> CreateCalendarItemAsync(Models.SalesAppointment appt)
        {
            var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE");

            // UID vorab generieren – Exchange übernimmt sie, sodass RSVP-Antworten sicher gematchet werden können
            var uid = Guid.NewGuid().ToString("D").ToUpperInvariant();

            var bodyHtml = new System.Text.StringBuilder();
            bodyHtml.Append($"<p>Sehr geehrte/r {System.Security.SecurityElement.Escape(appt.ContactName ?? appt.ContactEmail)},</p>");
            bodyHtml.Append("<p>wir laden Sie herzlich zu folgendem Termin ein:</p>");
            bodyHtml.Append("<table style=\"border-collapse:collapse;font-family:sans-serif;font-size:14px\">");
            bodyHtml.Append($"<tr><td style=\"padding:4px 12px;font-weight:bold\">Betreff</td><td>{System.Security.SecurityElement.Escape(appt.Title)}</td></tr>");
            bodyHtml.Append($"<tr><td style=\"padding:4px 12px;font-weight:bold\">Datum</td><td>{appt.AppointmentDate.ToString("dddd, dd. MMMM yyyy", de)}</td></tr>");
            bodyHtml.Append($"<tr><td style=\"padding:4px 12px;font-weight:bold\">Uhrzeit</td><td>{appt.AppointmentDate.ToString("HH:mm", de)} – {appt.AppointmentEnd.ToString("HH:mm", de)} Uhr</td></tr>");
            if (!string.IsNullOrWhiteSpace(appt.Location))
                bodyHtml.Append($"<tr><td style=\"padding:4px 12px;font-weight:bold\">Ort</td><td>{System.Security.SecurityElement.Escape(appt.Location)}</td></tr>");
            if (!string.IsNullOrWhiteSpace(appt.WebexJoinLink))
                bodyHtml.Append($"<tr><td style=\"padding:4px 12px;font-weight:bold\">Webex</td><td><a href=\"{appt.WebexJoinLink}\">Meeting beitreten</a><br/><small>{appt.WebexJoinLink}</small></td></tr>");
            bodyHtml.Append("</table>");
            if (!string.IsNullOrWhiteSpace(appt.Notes))
                bodyHtml.Append($"<p>{System.Security.SecurityElement.Escape(appt.Notes)}</p>");
            bodyHtml.Append("<p>Bitte nehmen Sie die Einladung in Ihrem Kalender an oder ab.</p>");
            bodyHtml.Append("<p>Mit freundlichen Grüßen<br/>AF Software Engineering – Sales</p>");

            var location = !string.IsNullOrWhiteSpace(appt.Location)
                ? appt.Location
                : (appt.WebexJoinLink ?? string.Empty);

            using var client = CreateEwsHttpClient();

            // CalendarItem mit vorab generierter UID und direktem Versand an alle Teilnehmer
            var createSoap = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                               xmlns:t="http://schemas.microsoft.com/exchange/services/2006/types"
                               xmlns:m="http://schemas.microsoft.com/exchange/services/2006/messages">
                  <soap:Body>
                    <m:CreateItem SendMeetingInvitations="SendToAllAndSaveCopy">
                      <m:SavedItemFolderId>
                        <t:DistinguishedFolderId Id="calendar"/>
                      </m:SavedItemFolderId>
                      <m:Items>
                        <t:CalendarItem>
                          <t:Subject>{System.Security.SecurityElement.Escape(appt.Title)}</t:Subject>
                          <t:Body BodyType="HTML">{bodyHtml}</t:Body>
                          <t:Start>{appt.AppointmentDate.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}</t:Start>
                          <t:End>{appt.AppointmentEnd.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}</t:End>
                          {(string.IsNullOrWhiteSpace(location) ? "" : $"<t:Location>{System.Security.SecurityElement.Escape(location)}</t:Location>")}
                          <t:UID>{uid}</t:UID>
                          <t:RequiredAttendees>
                            {BuildEwsAttendees(appt.ContactName, appt.ContactEmail)}
                          </t:RequiredAttendees>
                          <t:IsResponseRequested>true</t:IsResponseRequested>
                          <t:AllowNewTimeProposal>false</t:AllowNewTimeProposal>
                        </t:CalendarItem>
                      </m:Items>
                    </m:CreateItem>
                  </soap:Body>
                </soap:Envelope>
                """;

            var createXml = await PostEwsAsync(client, createSoap);
            var createDoc = XDocument.Parse(createXml);
            // Prüfen ob EWS einen Fehler zurückgegeben hat
            var responseCode = createDoc.Descendants(_m + "ResponseCode").FirstOrDefault()?.Value;
            if (responseCode != null && responseCode != "NoError")
            {
                var msg = createDoc.Descendants(_m + "MessageText").FirstOrDefault()?.Value ?? responseCode;
                throw new InvalidOperationException($"EWS CreateItem Fehler: {msg}");
            }

            return uid;
        }

        /// <summary>
        /// Sucht das CalendarItem anhand der UID und sendet eine Absage an alle Teilnehmer.
        /// </summary>
        public async Task CancelCalendarItemAsync(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return;

            using var client = CreateEwsHttpClient();

            // CalendarItem per UID finden
            var findSoap = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                               xmlns:t="http://schemas.microsoft.com/exchange/services/2006/types"
                               xmlns:m="http://schemas.microsoft.com/exchange/services/2006/messages">
                  <soap:Body>
                    <m:FindItem Traversal="Shallow">
                      <m:ItemShape>
                        <t:BaseShape>IdOnly</t:BaseShape>
                        <t:AdditionalProperties>
                          <t:FieldURI FieldURI="calendar:UID"/>
                        </t:AdditionalProperties>
                      </m:ItemShape>
                      <m:IndexedPageItemView MaxEntriesReturned="10" Offset="0" BasePoint="Beginning"/>
                      <m:Restriction>
                        <t:IsEqualTo>
                          <t:FieldURI FieldURI="calendar:UID"/>
                          <t:FieldURIOrConstant>
                            <t:Constant Value="{System.Security.SecurityElement.Escape(uid)}"/>
                          </t:FieldURIOrConstant>
                        </t:IsEqualTo>
                      </m:Restriction>
                      <m:ParentFolderIds>
                        <t:DistinguishedFolderId Id="calendar"/>
                      </m:ParentFolderIds>
                    </m:FindItem>
                  </soap:Body>
                </soap:Envelope>
                """;

            var findXml = await PostEwsAsync(client, findSoap);
            var findDoc = XDocument.Parse(findXml);
            var itemIdEl = findDoc.Descendants(_t + "ItemId").FirstOrDefault();
            if (itemIdEl == null) return; // nicht gefunden – kein Fehler

            var itemId    = itemIdEl.Attribute("Id")?.Value    ?? string.Empty;
            var changeKey = itemIdEl.Attribute("ChangeKey")?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(itemId)) return;

            // CalendarItem löschen und Absage senden
            var deleteSoap = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                               xmlns:t="http://schemas.microsoft.com/exchange/services/2006/types"
                               xmlns:m="http://schemas.microsoft.com/exchange/services/2006/messages">
                  <soap:Body>
                    <m:DeleteItem DeleteType="MoveToDeletedItems" SendMeetingCancellations="SendToAllAndSaveCopy">
                      <m:ItemIds>
                        <t:ItemId Id="{itemId}" ChangeKey="{changeKey}"/>
                      </m:ItemIds>
                    </m:DeleteItem>
                  </soap:Body>
                </soap:Envelope>
                """;

            await PostEwsAsync(client, deleteSoap);
        }

        /// <summary>
        /// Sendet eine E-Mail mit optionalem iCal-Kalendereinladung über EWS.
        /// Das iCal wird als text/calendar MIME-Part eingebettet (nicht als Dateianhang),
        /// damit Outlook die Annehmen/Ablehnen-Schaltflächen anzeigt.
        /// </summary>
        public async Task SendMailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            string textBody,
            byte[]? icalAttachment = null,
            string icalMethod = "REQUEST")
        {
            // Vollständige MIME-Nachricht mit MimeKit aufbauen
            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress(config.EwsEmail, config.EwsEmail));
            message.To.Add(MimeKit.MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            MimeKit.MimeEntity body;

            if (icalAttachment != null)
            {
                // multipart/mixed: HTML-Text + text/calendar (kein Dateianhang!)
                var multipart = new MimeKit.Multipart("mixed");

                // HTML-Part
                var htmlPart = new MimeKit.MimePart("text", "html")
                {
                    Content = new MimeKit.MimeContent(
                        new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlBody))),
                    ContentTransferEncoding = MimeKit.ContentEncoding.QuotedPrintable
                };
                multipart.Add(htmlPart);

                // text/calendar Part – dieser sorgt für die RSVP-Buttons in Outlook
                var calPart = new MimeKit.MimePart("text", "calendar")
                {
                    ContentType = { Parameters = { { "method", icalMethod }, { "charset", "utf-8" } } },
                    ContentDisposition = new MimeKit.ContentDisposition(MimeKit.ContentDisposition.Inline),
                    ContentTransferEncoding = MimeKit.ContentEncoding.Base64,
                    Content = new MimeKit.MimeContent(
                        new System.IO.MemoryStream(icalAttachment))
                };
                multipart.Add(calPart);

                body = multipart;
            }
            else
            {
                body = new MimeKit.TextPart("html")
                {
                    Text = htmlBody
                };
            }

            message.Body = body;

            // MIME-Nachricht in einen Stream serialisieren und Base64 kodieren
            using var mimeStream = new System.IO.MemoryStream();
            await message.WriteToAsync(mimeStream);
            var mimeBase64 = Convert.ToBase64String(mimeStream.ToArray());

            // Per EWS CreateItem mit MimeContent senden – behält die exakte MIME-Struktur
            var soap = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                               xmlns:t="http://schemas.microsoft.com/exchange/services/2006/types"
                               xmlns:m="http://schemas.microsoft.com/exchange/services/2006/messages">
                  <soap:Body>
                    <m:CreateItem MessageDisposition="SendAndSaveCopy">
                      <m:SavedItemFolderId>
                        <t:DistinguishedFolderId Id="sentitems"/>
                      </m:SavedItemFolderId>
                      <m:Items>
                        <t:Message>
                          <t:MimeContent CharacterSet="UTF-8">{mimeBase64}</t:MimeContent>
                        </t:Message>
                      </m:Items>
                    </m:CreateItem>
                  </soap:Body>
                </soap:Envelope>
                """;

            using var client = CreateEwsHttpClient();
            await PostEwsAsync(client, soap);
        }

        public async Task<List<(string ICalUid, string ResponseType, string FromEmail)>> FetchMeetingResponsesAsync(int maxItems = 100)
            => (await FetchMeetingResponsesWithDiagnosticsAsync(maxItems)).Responses;

        public async Task<(List<(string ICalUid, string ResponseType, string FromEmail)> Responses, string Diagnostics)>
            FetchMeetingResponsesWithDiagnosticsAsync(int maxItems = 100)
        {
            var results = new List<(string, string, string)>();
            var diag    = new StringBuilder();
            using var client = CreateEwsHttpClient();

            // Mehrere Ordner durchsuchen: Posteingang, Gelöschte Elemente, Archiv
            var folderIds = new[] { "inbox", "deleteditems", "junkemail" };
            var itemsToFetch = new List<(string Id, string ChangeKey, string FromEmail, string ItemClass, string Subject)>();

            foreach (var folderId in folderIds)
            {
                try
                {
                    var findSoap = $"""
                        <?xml version="1.0" encoding="utf-8"?>
                        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                                       xmlns:t="http://schemas.microsoft.com/exchange/services/2006/types"
                                       xmlns:m="http://schemas.microsoft.com/exchange/services/2006/messages">
                          <soap:Body>
                            <m:FindItem Traversal="Shallow">
                              <m:ItemShape>
                                <t:BaseShape>IdOnly</t:BaseShape>
                                <t:AdditionalProperties>
                                  <t:FieldURI FieldURI="item:ItemClass"/>
                                  <t:FieldURI FieldURI="message:From"/>
                                  <t:FieldURI FieldURI="item:Subject"/>
                                  <t:FieldURI FieldURI="item:DateTimeReceived"/>
                                </t:AdditionalProperties>
                              </m:ItemShape>
                              <m:IndexedPageItemView MaxEntriesReturned="{maxItems}" Offset="0" BasePoint="Beginning"/>
                              <m:SortOrder>
                                <t:FieldOrder Order="Descending">
                                  <t:FieldURI FieldURI="item:DateTimeReceived"/>
                                </t:FieldOrder>
                              </m:SortOrder>
                              <m:ParentFolderIds>
                                <t:DistinguishedFolderId Id="{folderId}"/>
                              </m:ParentFolderIds>
                            </m:FindItem>
                          </soap:Body>
                        </soap:Envelope>
                        """;

                    var findXml = await PostEwsAsync(client, findSoap);
                    var findDoc = XDocument.Parse(findXml);

                    var allElements = findDoc.Descendants()
                        .Where(el => el.Name.Namespace == _t && el.Element(_t + "ItemId") != null)
                        .ToList();

                    diag.AppendLine($"Ordner '{folderId}': {allElements.Count} Item(s).");

                    foreach (var el in allElements)
                    {
                        var itemId    = el.Element(_t + "ItemId");
                        var itemClass = el.Element(_t + "ItemClass")?.Value ?? string.Empty;
                        var subject   = el.Element(_t + "Subject")?.Value ?? string.Empty;
                        var fromEmail = el.Element(_t + "From")?.Element(_t + "Mailbox")?.Element(_t + "EmailAddress")?.Value ?? string.Empty;
                        if (fromEmail.StartsWith("SMTP:", StringComparison.OrdinalIgnoreCase))
                            fromEmail = fromEmail[5..].Trim();

                        if (itemId == null) continue;

                        bool isMeetingResp = el.Name.LocalName == "MeetingResponse" ||
                                             itemClass.StartsWith("IPM.Schedule.Meeting.Resp", StringComparison.OrdinalIgnoreCase);
                        bool isNote = el.Name.LocalName == "Message" &&
                                      (string.IsNullOrEmpty(itemClass) || itemClass.Equals("IPM.Note", StringComparison.OrdinalIgnoreCase));

                        if (!isMeetingResp && !isNote) continue;

                        var id = itemId.Attribute("Id")?.Value ?? string.Empty;
                        if (string.IsNullOrEmpty(id) || itemsToFetch.Any(x => x.Id == id)) continue;

                        itemsToFetch.Add((id, itemId.Attribute("ChangeKey")?.Value ?? string.Empty,
                            fromEmail, itemClass, subject));
                    }
                }
                catch (Exception ex)
                {
                    diag.AppendLine($"Ordner '{folderId}' Fehler: {ex.Message}");
                }
            }

            diag.AppendLine($"→ {itemsToFetch.Count} Items zur Analyse gesamt.");

            foreach (var item in itemsToFetch)
            {
                try
                {
                    // Für native MeetingResponse-Items EWS-Properties direkt auslesen
                    bool isMeetingRespItem = item.ItemClass.StartsWith(
                        "IPM.Schedule.Meeting.Resp", StringComparison.OrdinalIgnoreCase);

                    if (isMeetingRespItem)
                    {
                        diag.AppendLine($"  GetItem (MeetingResponse): \"{item.Subject}\"");
                        var getItemSoap = $"""
                            <?xml version="1.0" encoding="utf-8"?>
                            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                                           xmlns:t="http://schemas.microsoft.com/exchange/services/2006/types"
                                           xmlns:m="http://schemas.microsoft.com/exchange/services/2006/messages">
                              <soap:Body>
                                <m:GetItem>
                                  <m:ItemShape>
                                    <t:BaseShape>AllProperties</t:BaseShape>
                                  </m:ItemShape>
                                  <m:ItemIds>
                                    <t:ItemId Id="{item.Id}"/>
                                  </m:ItemIds>
                                </m:GetItem>
                              </soap:Body>
                            </soap:Envelope>
                            """;
                        var getXml  = await PostEwsAsync(client, getItemSoap);
                        var getDoc  = XDocument.Parse(getXml);

                        var nsT     = (XNamespace)"http://schemas.microsoft.com/exchange/services/2006/types";
                        var mrElem  = getDoc.Descendants(nsT + "MeetingResponse").FirstOrDefault()
                                   ?? getDoc.Descendants(nsT + "Message").FirstOrDefault();

                        // Alle direkten Child-Elementnamen loggen für Diagnose
                        if (mrElem != null)
                        {
                            var childNames = string.Join(", ", mrElem.Elements()
                                .Select(x => x.Name.LocalName).Distinct());
                            diag.AppendLine($"    XML-Felder: {childNames}");
                        }
                        else
                        {
                            diag.AppendLine($"    → kein MeetingResponse/Message-Element in Antwort");
                        }

                        var uidRaw  = mrElem?.Element(nsT + "UID")?.Value
                                   ?? mrElem?.Element(nsT + "CalendarItemUID")?.Value;

                        // ResponseType: Accepted/Declined/Tentative
                        var responseTypeRaw = mrElem?.Element(nsT + "ResponseType")?.Value ?? string.Empty;

                        // Absender
                        var fromEmail = item.FromEmail;
                        if (string.IsNullOrEmpty(fromEmail))
                            fromEmail = mrElem?.Element(nsT + "From")?.Element(nsT + "Mailbox")?.Element(nsT + "EmailAddress")?.Value ?? string.Empty;

                        diag.AppendLine($"    UID={uidRaw} ResponseType={responseTypeRaw} From={fromEmail}");

                        // Wenn direkte UID fehlt, MIME als Fallback
                        if (string.IsNullOrEmpty(uidRaw))
                        {
                            var mimeB64 = await FetchMimeContentAsync(item.Id);
                            if (!string.IsNullOrEmpty(mimeB64))
                            {
                                var mimeMsg2 = MimeMessage.Load(new System.IO.MemoryStream(Convert.FromBase64String(mimeB64)));
                                foreach (var part in mimeMsg2.BodyParts.OfType<MimePart>())
                                {
                                    if (string.Equals(part.ContentType.MediaType, "text", StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(part.ContentType.MediaSubtype, "calendar", StringComparison.OrdinalIgnoreCase))
                                    {
                                        using var ms2 = new System.IO.MemoryStream();
                                        await part.Content.DecodeToAsync(ms2);
                                        var ical2 = System.Text.Encoding.UTF8.GetString(ms2.ToArray());
                                        uidRaw = ExtractICalLine(ical2, "UID");
                                        if (string.IsNullOrEmpty(responseTypeRaw))
                                        {
                                            var ps = ExtractICalAttendeePartstat(ical2);
                                            responseTypeRaw = ps;
                                        }
                                        diag.AppendLine($"    MIME fallback UID={uidRaw} PARTSTAT={responseTypeRaw}");
                                        break;
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(uidRaw) || string.IsNullOrEmpty(responseTypeRaw)) continue;
                        results.Add((uidRaw, responseTypeRaw, fromEmail));
                        continue;
                    }

                    // Für normale Nachrichten (IPM.Note) MIME/iCal analysieren
                    diag.AppendLine($"  MIME lesen: \"{item.Subject}\"");
                    var mimeBase64 = await FetchMimeContentAsync(item.Id);
                    if (string.IsNullOrEmpty(mimeBase64)) { diag.AppendLine("    → MIME leer"); continue; }

                    var mimeMsg = MimeMessage.Load(new System.IO.MemoryStream(Convert.FromBase64String(mimeBase64)));

                    string? icalText = null;
                    foreach (var part in mimeMsg.BodyParts.OfType<MimePart>())
                    {
                        if (string.Equals(part.ContentType.MediaType, "text", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(part.ContentType.MediaSubtype, "calendar", StringComparison.OrdinalIgnoreCase))
                        {
                            using var ms = new System.IO.MemoryStream();
                            await part.Content.DecodeToAsync(ms);
                            icalText = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                            break;
                        }
                    }

                    if (icalText == null) { diag.AppendLine("    → kein text/calendar Part"); continue; }

                    var method   = ExtractICalLine(icalText, "METHOD");
                    var uid      = ExtractICalLine(icalText, "UID");
                    var partstat = ExtractICalAttendeePartstat(icalText);
                    diag.AppendLine($"    METHOD={method} UID={uid} PARTSTAT={partstat}");

                    if (!method.Equals("REPLY", StringComparison.OrdinalIgnoreCase)) continue;

                    var fromEmailNote = item.FromEmail;
                    if (string.IsNullOrEmpty(fromEmailNote))
                        fromEmailNote = mimeMsg.From.OfType<MailboxAddress>().FirstOrDefault()?.Address ?? string.Empty;

                    if (!string.IsNullOrEmpty(uid) && !string.IsNullOrEmpty(partstat))
                        results.Add((uid, partstat, fromEmailNote));
                }
                catch (Exception ex)
                {
                    diag.AppendLine($"    → Fehler: {ex.Message}");
                }
            }

            return (results, diag.ToString());
        }

        /// <summary>
        /// Ruft den vollständigen MIME-Inhalt einer Nachricht per EWS GetItem ab
        /// und gibt ihn Base64-kodiert zurück (für RSVP-iCal-Auswertung).
        /// </summary>
        public async Task<string?> FetchMimeContentAsync(string itemId)
        {
            try
            {
                using var client = CreateEwsHttpClient();
                var soap = $"""
                    <?xml version="1.0" encoding="utf-8"?>
                    <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                                   xmlns:t="http://schemas.microsoft.com/exchange/services/2006/types"
                                   xmlns:m="http://schemas.microsoft.com/exchange/services/2006/messages">
                      <soap:Body>
                        <m:GetItem>
                          <m:ItemShape>
                            <t:BaseShape>IdOnly</t:BaseShape>
                            <t:IncludeMimeContent>true</t:IncludeMimeContent>
                          </m:ItemShape>
                          <m:ItemIds>
                            <t:ItemId Id="{itemId}"/>
                          </m:ItemIds>
                        </m:GetItem>
                      </soap:Body>
                    </soap:Envelope>
                    """;
                var xml = await PostEwsAsync(client, soap);
                var doc = XDocument.Parse(xml);
                return doc.Descendants(_t + "MimeContent").FirstOrDefault()?.Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FetchMimeContentAsync Fehler: {ex.Message}");
                return null;
            }
        }

        private static string ExtractICalLine(string ical, string field)
        {
            foreach (var line in UnfoldIcal(ical))
            {
                if (line.StartsWith(field + ":", StringComparison.OrdinalIgnoreCase))
                    return line[(field.Length + 1)..].Trim();
            }
            return string.Empty;
        }

        private static string BuildEwsAttendees(string? name, string emailField)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var addr in (emailField ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                sb.Append("<t:Attendee><t:Mailbox>");
                sb.Append($"<t:Name>{System.Security.SecurityElement.Escape(name ?? addr)}</t:Name>");
                sb.Append($"<t:EmailAddress>{System.Security.SecurityElement.Escape(addr)}</t:EmailAddress>");
                sb.Append("</t:Mailbox></t:Attendee>");
            }
            return sb.ToString();
        }

        private static string ExtractICalAttendeePartstat(string ical)
        {
            foreach (var line in UnfoldIcal(ical))
            {
                if (!line.StartsWith("ATTENDEE", StringComparison.OrdinalIgnoreCase)) continue;
                var idx = line.IndexOf("PARTSTAT=", StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;
                var val = line[(idx + 9)..];
                var end = val.IndexOfAny(new[] { ';', ':' });
                return end >= 0 ? val[..end] : val;
            }
            return string.Empty;
        }

        /// <summary>
        /// Entfaltet iCal-Zeilen gemäß RFC 5545 (§3.1): Zeilen, die mit einem
        /// Leerzeichen oder Tab beginnen, sind Fortsetzungen der vorherigen Zeile.
        /// Ohne dieses Unfolding werden lange UIDs oder ATTENDEE-Parameter
        /// abgeschnitten, was das RSVP-Matching unzuverlässig macht.
        /// </summary>
        private static List<string> UnfoldIcal(string ical)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(ical)) return result;

            // CRLF und LF normalisieren, dann zeilenweise verarbeiten
            foreach (var rawLine in ical.Replace("\r\n", "\n").Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                if (line.Length > 0 && (line[0] == ' ' || line[0] == '\t') && result.Count > 0)
                {
                    // Fortsetzungszeile: an vorherige anhängen (führendes Whitespace entfernen)
                    result[^1] += line[1..];
                }
                else
                {
                    result.Add(line);
                }
            }
            return result;
        }
    }
}
