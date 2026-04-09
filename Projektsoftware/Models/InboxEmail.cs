using System;

namespace Projektsoftware.Models
{
    /// <summary>
    /// Repräsentiert eine eingegangene E-Mail aus dem Exchange-Posteingang (EWS).
    /// </summary>
    public class InboxEmail
    {
        public string EwsItemId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool IsRead { get; set; }

        /// <summary>
        /// Automatisch oder manuell zugeordneter Kunde (anhand Absender-E-Mail).
        /// </summary>
        public Customer? MatchedCustomer { get; set; }

        public string FromDisplay => string.IsNullOrWhiteSpace(FromName)
            ? FromEmail
            : $"{FromName} <{FromEmail}>";

        public string CustomerDisplay => MatchedCustomer?.DisplayName ?? "—";

        public string ReadStatus => IsRead ? string.Empty : "●";
    }
}
