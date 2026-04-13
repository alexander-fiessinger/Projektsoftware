using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Verwaltet benutzerspezifische Zugangsdaten (Easybill, Exchange, Webex).
    /// Credentials werden beim Login aus der DB geladen und im Speicher gecacht,
    /// damit synchrone Config.Load()-Aufrufe die Werte sofort nutzen können.
    /// </summary>
    public static class UserCredentialService
    {
        private static readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
        private static int _userId;

        // Schlüssel-Konstanten
        public const string EasybillEmail = "easybill_email";
        public const string EasybillApiKey = "easybill_api_key";
        public const string ExchangeEmail = "exchange_email";
        public const string ExchangePassword = "exchange_password";
        public const string ExchangeSenderName = "exchange_sender_name";
        public const string WebexBotName = "webex_bot_name";

        /// <summary>
        /// Lädt alle Zugangsdaten des Benutzers aus der DB in den Cache.
        /// Wird einmalig nach dem Login aufgerufen.
        /// </summary>
        public static async Task LoadAsync(int userId, DatabaseService db)
        {
            _cache.Clear();
            _userId = userId;

            var credentials = await db.GetUserCredentialsAsync(userId);
            foreach (var kvp in credentials)
            {
                _cache[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Gibt den gecachten Wert für einen Schlüssel zurück, oder null.
        /// </summary>
        public static string? Get(string key)
        {
            return _cache.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Speichert einen Wert im Cache und in der DB.
        /// </summary>
        public static async Task SaveAsync(string key, string value, DatabaseService db)
        {
            if (_userId <= 0) return;
            _cache[key] = value;
            await db.SaveUserCredentialAsync(_userId, key, value);
        }

        /// <summary>
        /// Speichert mehrere Werte auf einmal im Cache und in der DB.
        /// </summary>
        public static async Task SaveManyAsync(Dictionary<string, string> credentials, DatabaseService db)
        {
            if (_userId <= 0) return;
            foreach (var kvp in credentials)
            {
                _cache[kvp.Key] = kvp.Value;
                await db.SaveUserCredentialAsync(_userId, kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Leert den Cache (beim Logout aufrufen).
        /// </summary>
        public static void Clear()
        {
            _cache.Clear();
            _userId = 0;
        }
    }
}
