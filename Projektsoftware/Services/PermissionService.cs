using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Verwaltet Modul-Berechtigungen pro Benutzer.
    /// Admins haben automatisch Zugriff auf alle Module.
    /// Berechtigungen werden beim Login aus der DB geladen und im Speicher gecacht.
    /// </summary>
    public static class PermissionService
    {
        private static readonly HashSet<string> _allowedModules = new(StringComparer.OrdinalIgnoreCase);
        private static bool _isAdmin;

        /// <summary>
        /// Alle definierten Module mit Anzeigenamen
        /// </summary>
        public static readonly (string Key, string DisplayName)[] AllModules =
        [
            ("kunden",             "👤 Kunden"),
            ("projekte",           "📁 Projekte"),
            ("zeiterfassung",      "⏱ Zeiterfassung"),
            ("kalender",           "📅 Kalender"),
            ("protokolle",         "📋 Besprechungsprotokolle"),
            ("aufgaben",           "✓ Aufgaben"),
            ("tickets",            "🎫 Tickets"),
            ("crm",                "🤝 CRM"),
            ("einkauf",            "🛒 Einkauf"),
            ("sales",              "💼 Sales"),
            ("mitarbeiter",        "👥 Mitarbeiter"),
            ("einstellungen",      "⚙ Einstellungen"),
            // Erweiterte Module
            ("global_search",      "🔍 Globale Suche"),
            ("kpi_dashboard",      "📈 KPI-Dashboard"),
            ("activity_feed",      "📰 Aktivitäts-Feed"),
            ("sla_monitoring",     "⏰ SLA-Überwachung"),
            ("lead_kanban",        "📋 Lead-Pipeline"),
            ("lead_statistics",    "📊 Lead-Statistik"),
            ("follow_ups",         "🔔 Wiedervorlage"),
            ("gantt",              "📅 Gantt-Diagramm"),
            ("budget_tracking",    "💰 Budget-Tracking"),
            ("project_templates",  "📑 Projektvorlagen"),
            ("email_history",      "✉ E-Mail-Verlauf"),
            ("offer_history",      "📋 Angebots-Historie"),
            ("csv_import",         "📥 Kontakt-Import (CSV)"),
            ("supplier_rating",    "⭐ Lieferanten-Bewertung"),
            ("expenses_analytics", "📉 Ausgaben-Auswertung"),
            ("documents",          "📄 Dokumente"),
            ("easybill_products",  "📦 Produkte"),
            ("audit_log",          "📋 Audit-Log"),
            ("export",             "📤 Export"),
        ];

        /// <summary>
        /// Lädt die Berechtigungen des Benutzers aus der DB.
        /// Wird einmalig nach dem Login aufgerufen.
        /// </summary>
        public static async Task LoadAsync(int userId, bool isAdmin, DatabaseService db)
        {
            _allowedModules.Clear();
            _isAdmin = isAdmin;

            if (!isAdmin)
            {
                var permissions = await db.GetUserPermissionsAsync(userId);
                foreach (var module in permissions)
                {
                    _allowedModules.Add(module);
                }
            }
        }

        /// <summary>
        /// Prüft ob der aktuelle Benutzer Zugriff auf ein Modul hat.
        /// Admins haben immer Zugriff; Dashboard ist immer sichtbar.
        /// </summary>
        public static bool HasAccess(string moduleKey)
        {
            if (_isAdmin) return true;
            if (string.Equals(moduleKey, "dashboard", StringComparison.OrdinalIgnoreCase)) return true;
            return _allowedModules.Contains(moduleKey);
        }

        /// <summary>
        /// Gibt die aktuell zugewiesenen Modul-Schlüssel zurück (für Anzeige im Dialog).
        /// </summary>
        public static HashSet<string> GetAllowedModules() => new(_allowedModules, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Leert den Cache (beim Logout aufrufen).
        /// </summary>
        public static void Clear()
        {
            _allowedModules.Clear();
            _isAdmin = false;
        }
    }
}
