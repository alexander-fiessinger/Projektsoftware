using Projektsoftware.Models;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Service für Authentifizierung und Passwortverwaltung
    /// </summary>
    public class AuthenticationService
    {
        private static User _currentUser;

        /// <summary>
        /// Aktuell angemeldeter Benutzer
        /// </summary>
        public static User CurrentUser
        {
            get => _currentUser;
            set => _currentUser = value;
        }

        public static bool IsAuthenticated => _currentUser != null;
        public static bool IsAdmin => _currentUser?.Role == "Admin";

        /// <summary>
        /// Erstellt einen SHA256-Hash aus einem Passwort
        /// </summary>
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Überprüft, ob ein Passwort mit dem Hash übereinstimmt
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            string passwordHash = HashPassword(password);
            return passwordHash.Equals(hash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Meldet den aktuellen Benutzer ab
        /// </summary>
        public static void Logout()
        {
            _currentUser = null;
        }
    }
}
