using MySql.Data.MySqlClient;
using Projektsoftware.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Projektsoftware.Services
{
    public partial class DatabaseService
    {
        private readonly string connectionString;
        private readonly DatabaseConfig config;

        public DatabaseService()
        {
            try
            {
                // Lade Konfiguration aus persistenter Datei (nicht App.config!)
                config = DatabaseConfig.Load();

                if (!config.IsConfigured())
                {
                    throw new Exception("Datenbankverbindung nicht konfiguriert!\n\nBitte konfigurieren Sie die Verbindung über:\nMenü → Einstellungen → Datenbankverbindung");
                }

                connectionString = config.GetConnectionString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Laden der Datenbankverbindung: {ex.Message}", ex);
            }
        }

        public DatabaseService(string server, string database, string user, string password)
        {
            connectionString = $"Server={server};Database={database};Uid={user};Pwd={password};";
        }

        /// <summary>
        /// Konvertiert MySQL TIME-Werte sicher in TimeSpan
        /// </summary>
        private TimeSpan ParseMySqlTime(object value)
        {
            if (value == null || value == DBNull.Value)
                return TimeSpan.Zero;

            // MySQL kann TIME als TimeSpan oder als String zurückgeben
            if (value is TimeSpan ts)
                return ts;

            if (value is string str)
                return TimeSpan.Parse(str);

            return TimeSpan.Zero;
        }

        public async Task InitializeDatabaseAsync()
        {
            System.Diagnostics.Debug.WriteLine("🔧 Starte Datenbankinitialisierung...");

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            System.Diagnostics.Debug.WriteLine($"✅ Datenbankverbindung hergestellt: {connection.Database}");

            // Mitarbeiter Tabelle
            string createEmployeesTable = @"
                CREATE TABLE IF NOT EXISTS employees (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    first_name VARCHAR(100) NOT NULL,
                    last_name VARCHAR(100) NOT NULL,
                    email VARCHAR(255),
                    phone VARCHAR(50),
                    position VARCHAR(100),
                    department VARCHAR(100),
                    hourly_rate DECIMAL(10,2),
                    is_active BOOLEAN DEFAULT TRUE,
                    hire_date DATE,
                    created_at DATETIME
                )";

            // Projekte Tabelle
            string createProjectsTable = @"
                CREATE TABLE IF NOT EXISTS projects (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    description TEXT,
                    start_date DATE,
                    end_date DATE,
                    status VARCHAR(50),
                    client_name VARCHAR(255),
                    easybill_customer_id BIGINT,
                    budget DECIMAL(10,2),
                    created_at DATETIME
                )";

            // Meilensteine Tabelle
            string createMilestonesTable = @"
                CREATE TABLE IF NOT EXISTS milestones (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    project_id INT,
                    title VARCHAR(255) NOT NULL,
                    description TEXT,
                    due_date DATE,
                    completed_date DATE,
                    status VARCHAR(50),
                    completion_percentage INT DEFAULT 0,
                    created_at DATETIME,
                    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
                )";

            // Aufgaben Tabelle
            string createTasksTable = @"
                CREATE TABLE IF NOT EXISTS tasks (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    project_id INT,
                    title VARCHAR(255) NOT NULL,
                    description TEXT,
                    assigned_to VARCHAR(255),
                    client_name VARCHAR(255),
                    easybill_customer_id BIGINT,
                    status VARCHAR(50),
                    priority VARCHAR(50),
                    due_date DATE,
                    completed_date DATE,
                    estimated_hours INT DEFAULT 0,
                    actual_hours INT DEFAULT 0,
                    created_at DATETIME,
                    updated_at DATETIME,
                    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
                )";

            // Zeiteinträge Tabelle
            string createTimeEntriesTable = @"
                CREATE TABLE IF NOT EXISTS time_entries (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    project_id INT,
                    employee_name VARCHAR(255),
                    client_name VARCHAR(255),
                    easybill_customer_id BIGINT,
                    date DATE,
                    duration TIME,
                    description TEXT,
                    activity VARCHAR(255),
                    created_at DATETIME,
                    is_exported BOOLEAN DEFAULT FALSE,
                    exported_to_easybill_at DATETIME,
                    easybill_position_id BIGINT,
                    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
                )";

            // Besprechungsprotokolle Tabelle
            string createMeetingProtocolsTable = @"
                CREATE TABLE IF NOT EXISTS meeting_protocols (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    project_id INT,
                    title VARCHAR(255),
                    meeting_date DATETIME,
                    location VARCHAR(255),
                    participants TEXT,
                    agenda TEXT,
                    discussion TEXT,
                    decisions TEXT,
                    action_items TEXT,
                    next_meeting_date VARCHAR(100),
                    created_at DATETIME,
                    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
                )";

            // Benutzer Tabelle für Authentifizierung
            string createUsersTable = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    employee_id INT,
                    username VARCHAR(100) UNIQUE NOT NULL,
                    password_hash VARCHAR(255) NOT NULL,
                    role VARCHAR(50) DEFAULT 'User',
                    is_active BOOLEAN DEFAULT TRUE,
                    created_at DATETIME,
                    last_login DATETIME,
                    FOREIGN KEY (employee_id) REFERENCES employees(id) ON DELETE SET NULL
                )";

            // Kunden Tabelle
            string createCustomersTable = @"
                CREATE TABLE IF NOT EXISTS customers (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    company_name VARCHAR(255),
                    first_name VARCHAR(100),
                    last_name VARCHAR(100),
                    email VARCHAR(255),
                    phone VARCHAR(50),
                    street VARCHAR(255),
                    zip_code VARCHAR(20),
                    city VARCHAR(100),
                    country VARCHAR(100) DEFAULT 'Deutschland',
                    vat_id VARCHAR(50),
                    note TEXT,
                    easybill_customer_id BIGINT,
                    last_synced_at DATETIME,
                    created_at DATETIME,
                    updated_at DATETIME,
                    is_active BOOLEAN DEFAULT TRUE,
                    INDEX idx_easybill_customer_id (easybill_customer_id)
                )";

            using var cmd0 = new MySqlCommand(createEmployeesTable, connection);
            await cmd0.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'employees' erstellt/geprüft");

            using var cmd1 = new MySqlCommand(createProjectsTable, connection);
            await cmd1.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'projects' erstellt/geprüft");

            using var cmd1b = new MySqlCommand(createMilestonesTable, connection);
            await cmd1b.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'milestones' erstellt/geprüft");

            using var cmd1c = new MySqlCommand(createTasksTable, connection);
            await cmd1c.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'tasks' erstellt/geprüft");

            using var cmd2 = new MySqlCommand(createTimeEntriesTable, connection);
            await cmd2.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'time_entries' erstellt/geprüft");

            using var cmd3 = new MySqlCommand(createMeetingProtocolsTable, connection);
            await cmd3.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'meeting_protocols' erstellt/geprüft");

            using var cmd4 = new MySqlCommand(createUsersTable, connection);
            await cmd4.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'users' CREATE-Befehl ausgeführt");

            using var cmd5 = new MySqlCommand(createCustomersTable, connection);
            await cmd5.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'customers' erstellt/geprüft");

            // Tickets Tabelle
            string createTicketsTable = @"
                CREATE TABLE IF NOT EXISTS tickets (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    customer_name VARCHAR(255) NOT NULL,
                    customer_email VARCHAR(255) NOT NULL,
                    customer_phone VARCHAR(50),
                    customer_id INT,
                    subject VARCHAR(255) NOT NULL,
                    description TEXT NOT NULL,
                    priority INT DEFAULT 1,
                    status INT DEFAULT 0,
                    category INT DEFAULT 0,
                    ip_address VARCHAR(45),
                    user_agent VARCHAR(500),
                    assigned_to_employee_id INT,
                    resolution TEXT,
                    resolved_at DATETIME,
                    created_at DATETIME,
                    updated_at DATETIME,
                    FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE SET NULL,
                    FOREIGN KEY (assigned_to_employee_id) REFERENCES employees(id) ON DELETE SET NULL,
                    INDEX idx_status (status),
                    INDEX idx_priority (priority),
                    INDEX idx_created_at (created_at)
                )";

            using var cmd6 = new MySqlCommand(createTicketsTable, connection);
            await cmd6.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'tickets' erstellt/geprüft");

            // Ticket-Kommentare Tabelle
            string createTicketCommentsTable = @"
                CREATE TABLE IF NOT EXISTS ticket_comments (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    ticket_id INT NOT NULL,
                    employee_id INT NOT NULL,
                    comment TEXT NOT NULL,
                    is_internal BOOLEAN DEFAULT TRUE,
                    created_at DATETIME NOT NULL,
                    FOREIGN KEY (ticket_id) REFERENCES tickets(id) ON DELETE CASCADE,
                    FOREIGN KEY (employee_id) REFERENCES employees(id) ON DELETE CASCADE,
                    INDEX idx_ticket_id (ticket_id),
                    INDEX idx_created_at (created_at)
                )";

            using var cmd7 = new MySqlCommand(createTicketCommentsTable, connection);
            await cmd7.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'ticket_comments' erstellt/geprüft");

            // Ticket-Zeiterfassung Tabelle
            string createTicketTimeLogsTable = @"
                CREATE TABLE IF NOT EXISTS ticket_time_logs (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    ticket_id INT NOT NULL,
                    employee_id INT NOT NULL,
                    description VARCHAR(500),
                    minutes_spent INT NOT NULL,
                    logged_at DATETIME NOT NULL,
                    FOREIGN KEY (ticket_id) REFERENCES tickets(id) ON DELETE CASCADE,
                    FOREIGN KEY (employee_id) REFERENCES employees(id) ON DELETE CASCADE,
                    INDEX idx_ticket_id (ticket_id),
                    INDEX idx_logged_at (logged_at)
                )";

            using var cmd8 = new MySqlCommand(createTicketTimeLogsTable, connection);
            await cmd8.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'ticket_time_logs' erstellt/geprüft");

            // Meetings / Kalender Tabelle
            string createMeetingsTable = @"
                CREATE TABLE IF NOT EXISTS meetings (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    title VARCHAR(255) NOT NULL,
                    description TEXT,
                    start_time DATETIME NOT NULL,
                    end_time DATETIME NOT NULL,
                    location VARCHAR(255),
                    participants TEXT,
                    project_id INT,
                    is_webex_meeting BOOLEAN DEFAULT FALSE,
                    webex_meeting_id VARCHAR(255),
                    webex_join_link VARCHAR(1000),
                    webex_host_key VARCHAR(50),
                    webex_password VARCHAR(100),
                    webex_sip_address VARCHAR(255),
                    created_at DATETIME NOT NULL,
                    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE SET NULL,
                    INDEX idx_start_time (start_time),
                    INDEX idx_project_id (project_id)
                )";

            using var cmd9 = new MySqlCommand(createMeetingsTable, connection);
            await cmd9.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'meetings' erstellt/geprüft");

            // Kundendokumente Tabelle
            string createCustomerDocumentsTable = @"
                CREATE TABLE IF NOT EXISTS customer_documents (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    customer_id INT NOT NULL,
                    file_name VARCHAR(500) NOT NULL,
                    file_path VARCHAR(1000) NOT NULL,
                    file_type VARCHAR(100),
                    file_size BIGINT DEFAULT 0,
                    description TEXT,
                    uploaded_by VARCHAR(255),
                    uploaded_at DATETIME NOT NULL,
                    FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE,
                    INDEX idx_customer_id (customer_id)
                )";

            using var cmd10 = new MySqlCommand(createCustomerDocumentsTable, connection);
            await cmd10.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'customer_documents' erstellt/geprüft");

            // Projektdokumente Tabelle
            string createProjectDocumentsTable = @"
                CREATE TABLE IF NOT EXISTS project_documents (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    project_id INT NOT NULL,
                    file_name VARCHAR(500) NOT NULL,
                    file_path VARCHAR(1000) NOT NULL,
                    file_type VARCHAR(100),
                    file_size BIGINT DEFAULT 0,
                    description TEXT,
                    uploaded_by VARCHAR(255),
                    uploaded_at DATETIME NOT NULL,
                    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
                    INDEX idx_project_id (project_id)
                )";

            using var cmd11 = new MySqlCommand(createProjectDocumentsTable, connection);
            await cmd11.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'project_documents' erstellt/geprüft");

            // Lieferanten Tabelle
            string createSuppliersTable = @"
                CREATE TABLE IF NOT EXISTS suppliers (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    contact_person VARCHAR(255),
                    email VARCHAR(255),
                    phone VARCHAR(50),
                    address VARCHAR(255),
                    zip_code VARCHAR(20),
                    city VARCHAR(100),
                    country VARCHAR(100) DEFAULT 'Deutschland',
                    tax_number VARCHAR(50),
                    bank_iban VARCHAR(50),
                    bank_bic VARCHAR(20),
                    notes TEXT,
                    created_at DATETIME NOT NULL,
                    updated_at DATETIME,
                    INDEX idx_name (name)
                )";

            using var cmdS1 = new MySqlCommand(createSuppliersTable, connection);
            await cmdS1.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'suppliers' erstellt/geprüft");

            // Bestellungen Tabelle
            string createPurchaseOrdersTable = @"
                CREATE TABLE IF NOT EXISTS purchase_orders (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    supplier_id INT,
                    order_number VARCHAR(100),
                    order_date DATE NOT NULL,
                    delivery_date_expected DATE,
                    delivery_date_actual DATE,
                    status VARCHAR(50) DEFAULT 'Offen',
                    total_net DECIMAL(12,2) DEFAULT 0,
                    total_gross DECIMAL(12,2) DEFAULT 0,
                    notes TEXT,
                    created_at DATETIME NOT NULL,
                    FOREIGN KEY (supplier_id) REFERENCES suppliers(id) ON DELETE SET NULL,
                    INDEX idx_status (status),
                    INDEX idx_supplier_id (supplier_id)
                )";

            using var cmdS2 = new MySqlCommand(createPurchaseOrdersTable, connection);
            await cmdS2.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'purchase_orders' erstellt/geprüft");

            // Bestellpositionen Tabelle
            string createPurchaseOrderItemsTable = @"
                CREATE TABLE IF NOT EXISTS purchase_order_items (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    purchase_order_id INT NOT NULL,
                    description VARCHAR(500) NOT NULL,
                    quantity DECIMAL(10,2) DEFAULT 1,
                    unit VARCHAR(50) DEFAULT 'Stk.',
                    unit_price_net DECIMAL(12,4) DEFAULT 0,
                    total_net DECIMAL(12,2) DEFAULT 0,
                    vat_percent DECIMAL(5,2) DEFAULT 19,
                    FOREIGN KEY (purchase_order_id) REFERENCES purchase_orders(id) ON DELETE CASCADE
                )";

            using var cmdS3 = new MySqlCommand(createPurchaseOrderItemsTable, connection);
            await cmdS3.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'purchase_order_items' erstellt/geprüft");

            // Eingangsrechnungen Tabelle
            string createPurchaseInvoicesTable = @"
                CREATE TABLE IF NOT EXISTS purchase_invoices (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    supplier_id INT,
                    invoice_number VARCHAR(100) NOT NULL,
                    invoice_date DATE NOT NULL,
                    due_date DATE,
                    total_net DECIMAL(12,2) DEFAULT 0,
                    total_gross DECIMAL(12,2) DEFAULT 0,
                    status VARCHAR(50) DEFAULT 'Offen',
                    payment_date DATE,
                    purchase_order_id INT,
                    notes TEXT,
                    created_at DATETIME NOT NULL,
                    FOREIGN KEY (supplier_id) REFERENCES suppliers(id) ON DELETE SET NULL,
                    FOREIGN KEY (purchase_order_id) REFERENCES purchase_orders(id) ON DELETE SET NULL,
                    INDEX idx_status (status),
                    INDEX idx_due_date (due_date)
                )";

            using var cmdS4 = new MySqlCommand(createPurchaseInvoicesTable, connection);
            await cmdS4.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'purchase_invoices' erstellt/geprüft");

            // Belegablage Tabelle
            string createPurchaseDocumentsTable = @"
                CREATE TABLE IF NOT EXISTS purchase_documents (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    supplier_id INT NULL,
                    document_name VARCHAR(255) NOT NULL DEFAULT '',
                    document_type VARCHAR(50) NOT NULL DEFAULT 'Rechnung',
                    document_date DATE NOT NULL,
                    original_file_name VARCHAR(255) NULL,
                    local_file_path VARCHAR(1000) NULL,
                    notes TEXT NULL,
                    created_at DATETIME NOT NULL,
                    easybill_attachment_id BIGINT NULL,
                    easybill_synced_at DATETIME NULL,
                    FOREIGN KEY (supplier_id) REFERENCES suppliers(id) ON DELETE SET NULL,
                    INDEX idx_document_date (document_date),
                    INDEX idx_supplier_id (supplier_id)
                )";

            using var cmdS5 = new MySqlCommand(createPurchaseDocumentsTable, connection);
            await cmdS5.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'purchase_documents' erstellt/geprüft");

            // Audit-Log Tabelle
            string createAuditLogTable = @"
                CREATE TABLE IF NOT EXISTS audit_log (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    timestamp DATETIME NOT NULL,
                    user_name VARCHAR(100) NOT NULL,
                    entity_type VARCHAR(50) NOT NULL,
                    entity_id VARCHAR(50),
                    action VARCHAR(50) NOT NULL,
                    details TEXT,
                    INDEX idx_timestamp (timestamp),
                    INDEX idx_entity_type (entity_type),
                    INDEX idx_user_name (user_name)
                )";

            using var cmdAudit = new MySqlCommand(createAuditLogTable, connection);
            await cmdAudit.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("✅ Tabelle 'audit_log' erstellt/geprüft");

// CRM Kontakte Tabelle
string createCrmContactsTable = @"
    CREATE TABLE IF NOT EXISTS crm_contacts (
        id INT AUTO_INCREMENT PRIMARY KEY,
        customer_id INT NULL,
        first_name VARCHAR(100) NOT NULL DEFAULT '',
        last_name VARCHAR(100) NOT NULL DEFAULT '',
        position VARCHAR(150),
        email VARCHAR(255),
        phone VARCHAR(50),
        mobile VARCHAR(50),
        notes TEXT,
        is_active BOOLEAN DEFAULT TRUE,
        created_at DATETIME NOT NULL,
        updated_at DATETIME,
        FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE SET NULL,
        INDEX idx_customer_id (customer_id),
        INDEX idx_is_active (is_active)
    )";

using var cmdCrm1 = new MySqlCommand(createCrmContactsTable, connection);
await cmdCrm1.ExecuteNonQueryAsync();
System.Diagnostics.Debug.WriteLine("✅ Tabelle 'crm_contacts' erstellt/geprüft");

// CRM Aktivitäten Tabelle
string createCrmActivitiesTable = @"
    CREATE TABLE IF NOT EXISTS crm_activities (
        id INT AUTO_INCREMENT PRIMARY KEY,
        contact_id INT NULL,
        customer_id INT NULL,
        type INT NOT NULL DEFAULT 0,
        subject VARCHAR(255) NOT NULL DEFAULT '',
        notes TEXT,
        due_date DATETIME,
        completed_at DATETIME,
        is_completed BOOLEAN DEFAULT FALSE,
        created_by VARCHAR(100),
        created_at DATETIME NOT NULL,
        FOREIGN KEY (contact_id) REFERENCES crm_contacts(id) ON DELETE SET NULL,
        FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE SET NULL,
        INDEX idx_contact_id (contact_id),
        INDEX idx_is_completed (is_completed),
        INDEX idx_due_date (due_date)
    )";

using var cmdCrm2 = new MySqlCommand(createCrmActivitiesTable, connection);
await cmdCrm2.ExecuteNonQueryAsync();
System.Diagnostics.Debug.WriteLine("✅ Tabelle 'crm_activities' erstellt/geprüft");

// CRM Deals Tabelle
string createCrmDealsTable = @"
    CREATE TABLE IF NOT EXISTS crm_deals (
        id INT AUTO_INCREMENT PRIMARY KEY,
        customer_id INT NULL,
        contact_id INT NULL,
        title VARCHAR(255) NOT NULL DEFAULT '',
        value DECIMAL(12,2) DEFAULT 0,
        stage INT NOT NULL DEFAULT 0,
        probability INT DEFAULT 50,
        expected_close_date DATE,
        notes TEXT,
        assigned_to VARCHAR(100),
        created_at DATETIME NOT NULL,
        updated_at DATETIME,
        won_at DATETIME,
        lost_at DATETIME,
        lost_reason VARCHAR(500),
        FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE SET NULL,
        FOREIGN KEY (contact_id) REFERENCES crm_contacts(id) ON DELETE SET NULL,
        INDEX idx_stage (stage),
        INDEX idx_customer_id (customer_id)
    )";

using var cmdCrm3 = new MySqlCommand(createCrmDealsTable, connection);
await cmdCrm3.ExecuteNonQueryAsync();
System.Diagnostics.Debug.WriteLine("✅ Tabelle 'crm_deals' erstellt/geprüft");
            // Überprüfe ob users Tabelle tatsächlich erstellt wurde
            string checkUsersTable = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'users'";

            using var checkCmd = new MySqlCommand(checkUsersTable, connection);
            var usersTableExists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            System.Diagnostics.Debug.WriteLine($"✅ Tabelle 'users' existiert: {usersTableExists}");

            if (!usersTableExists)
            {
                throw new Exception("Fehler: users Tabelle konnte nicht erstellt werden!");
            }

            // Migrationen für neue Spalten
            System.Diagnostics.Debug.WriteLine("🔧 Starte Migrationen...");
            await MigrateTablesAsync(connection);
            System.Diagnostics.Debug.WriteLine("✅ Datenbankinitialisierung abgeschlossen");
        }

        private async Task MigrateTablesAsync(MySqlConnection connection)
        {
            try
            {
                // Migration für projects Tabelle
                string checkProjectsEasybillColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'projects' 
                    AND COLUMN_NAME = 'easybill_customer_id'";

                using var checkProjCmd = new MySqlCommand(checkProjectsEasybillColumn, connection);
                var projectsEasybillExists = Convert.ToInt32(await checkProjCmd.ExecuteScalarAsync()) > 0;

                if (!projectsEasybillExists)
                {
                    string addProjectsEasybillColumn = "ALTER TABLE projects ADD COLUMN easybill_customer_id BIGINT";
                    using var alterProjCmd = new MySqlCommand(addProjectsEasybillColumn, connection);
                    await alterProjCmd.ExecuteNonQueryAsync();
                }

                // Migration für tasks Tabelle - client_name
                string checkClientNameColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'tasks' 
                    AND COLUMN_NAME = 'client_name'";

                using var checkCmd1 = new MySqlCommand(checkClientNameColumn, connection);
                var clientNameExists = Convert.ToInt32(await checkCmd1.ExecuteScalarAsync()) > 0;

                if (!clientNameExists)
                {
                    string addClientNameColumn = "ALTER TABLE tasks ADD COLUMN client_name VARCHAR(255)";
                    using var alterCmd1 = new MySqlCommand(addClientNameColumn, connection);
                    await alterCmd1.ExecuteNonQueryAsync();
                }

                // Prüfe ob easybill_customer_id Spalte existiert
                string checkEasybillColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'tasks' 
                    AND COLUMN_NAME = 'easybill_customer_id'";

                using var checkCmd2 = new MySqlCommand(checkEasybillColumn, connection);
                var easybillIdExists = Convert.ToInt32(await checkCmd2.ExecuteScalarAsync()) > 0;

                if (!easybillIdExists)
                {
                    string addEasybillIdColumn = "ALTER TABLE tasks ADD COLUMN easybill_customer_id BIGINT";
                    using var alterCmd2 = new MySqlCommand(addEasybillIdColumn, connection);
                    await alterCmd2.ExecuteNonQueryAsync();
                }

                // Migration für time_entries Tabelle - client_name
                string checkTimeEntriesClientColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'time_entries' 
                    AND COLUMN_NAME = 'client_name'";

                using var checkCmd3 = new MySqlCommand(checkTimeEntriesClientColumn, connection);
                var timeEntriesClientExists = Convert.ToInt32(await checkCmd3.ExecuteScalarAsync()) > 0;

                if (!timeEntriesClientExists)
                {
                    string addTimeEntriesClientColumn = "ALTER TABLE time_entries ADD COLUMN client_name VARCHAR(255)";
                    using var alterCmd3 = new MySqlCommand(addTimeEntriesClientColumn, connection);
                    await alterCmd3.ExecuteNonQueryAsync();
                }

                // Migration für time_entries Tabelle - easybill_customer_id
                string checkTimeEntriesEasybillColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'time_entries' 
                    AND COLUMN_NAME = 'easybill_customer_id'";

                using var checkCmd4 = new MySqlCommand(checkTimeEntriesEasybillColumn, connection);
                var timeEntriesEasybillExists = Convert.ToInt32(await checkCmd4.ExecuteScalarAsync()) > 0;

                if (!timeEntriesEasybillExists)
                {
                    string addTimeEntriesEasybillColumn = "ALTER TABLE time_entries ADD COLUMN easybill_customer_id BIGINT";
                    using var alterCmd4 = new MySqlCommand(addTimeEntriesEasybillColumn, connection);
                    await alterCmd4.ExecuteNonQueryAsync();
                }

                // Migration für time_entries - is_exported
                string checkIsExportedColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'time_entries' 
                    AND COLUMN_NAME = 'is_exported'";

                using var checkCmd5 = new MySqlCommand(checkIsExportedColumn, connection);
                var isExportedExists = Convert.ToInt32(await checkCmd5.ExecuteScalarAsync()) > 0;

                if (!isExportedExists)
                {
                    string addIsExportedColumn = "ALTER TABLE time_entries ADD COLUMN is_exported BOOLEAN DEFAULT FALSE";
                    using var alterCmd5 = new MySqlCommand(addIsExportedColumn, connection);
                    await alterCmd5.ExecuteNonQueryAsync();
                }

                // Migration für time_entries - exported_to_easybill_at
                string checkExportedAtColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'time_entries' 
                    AND COLUMN_NAME = 'exported_to_easybill_at'";

                using var checkCmd6 = new MySqlCommand(checkExportedAtColumn, connection);
                var exportedAtExists = Convert.ToInt32(await checkCmd6.ExecuteScalarAsync()) > 0;

                if (!exportedAtExists)
                {
                    string addExportedAtColumn = "ALTER TABLE time_entries ADD COLUMN exported_to_easybill_at DATETIME";
                    using var alterCmd6 = new MySqlCommand(addExportedAtColumn, connection);
                    await alterCmd6.ExecuteNonQueryAsync();
                }

                // Migration für time_entries - easybill_position_id
                string checkPositionIdColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'time_entries' 
                    AND COLUMN_NAME = 'easybill_position_id'";

                using var checkCmd7 = new MySqlCommand(checkPositionIdColumn, connection);
                var positionIdExists = Convert.ToInt32(await checkCmd7.ExecuteScalarAsync()) > 0;

                if (!positionIdExists)
                {
                    string addPositionIdColumn = "ALTER TABLE time_entries ADD COLUMN easybill_position_id BIGINT";
                    using var alterCmd7 = new MySqlCommand(addPositionIdColumn, connection);
                    await alterCmd7.ExecuteNonQueryAsync();
                }

                // Migration für projects Tabelle - easybill_project_id
                string checkProjectIdColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'projects' 
                    AND COLUMN_NAME = 'easybill_project_id'";

                using var checkCmd8 = new MySqlCommand(checkProjectIdColumn, connection);
                var projectIdExists = Convert.ToInt32(await checkCmd8.ExecuteScalarAsync()) > 0;

                if (!projectIdExists)
                {
                    string addProjectIdColumn = "ALTER TABLE projects ADD COLUMN easybill_project_id BIGINT";
                    using var alterCmd8 = new MySqlCommand(addProjectIdColumn, connection);
                    await alterCmd8.ExecuteNonQueryAsync();
                }

                // Migration für tickets Tabelle - project_id
                string checkTicketProjectIdColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'tickets' 
                    AND COLUMN_NAME = 'project_id'";

                using var checkCmd9 = new MySqlCommand(checkTicketProjectIdColumn, connection);
                var ticketProjectIdExists = Convert.ToInt32(await checkCmd9.ExecuteScalarAsync()) > 0;

                if (!ticketProjectIdExists)
                {
                    string addTicketProjectId = "ALTER TABLE tickets ADD COLUMN project_id INT NULL, ADD INDEX idx_ticket_project_id (project_id)";
                    using var alterCmd9 = new MySqlCommand(addTicketProjectId, connection);
                    await alterCmd9.ExecuteNonQueryAsync();
                }

                // Migration für suppliers Tabelle - easybill_customer_id
                string checkSuppliersEasybillColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'suppliers' 
                    AND COLUMN_NAME = 'easybill_customer_id'";

                using var checkSuppEb = new MySqlCommand(checkSuppliersEasybillColumn, connection);
                if (Convert.ToInt32(await checkSuppEb.ExecuteScalarAsync()) == 0)
                {
                    using var alterSuppEb = new MySqlCommand(
                        "ALTER TABLE suppliers ADD COLUMN easybill_customer_id BIGINT NULL, ADD COLUMN easybill_synced_at DATETIME NULL",
                        connection);
                    await alterSuppEb.ExecuteNonQueryAsync();
                }

                // Migration für purchase_invoices Tabelle - easybill_document_id
                string checkInvEasybillColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'purchase_invoices' 
                    AND COLUMN_NAME = 'easybill_document_id'";

                using var checkInvEb = new MySqlCommand(checkInvEasybillColumn, connection);
                if (Convert.ToInt32(await checkInvEb.ExecuteScalarAsync()) == 0)
                {
                    using var alterInvEb = new MySqlCommand(
                        "ALTER TABLE purchase_invoices ADD COLUMN easybill_document_id BIGINT NULL, ADD COLUMN easybill_synced_at DATETIME NULL",
                        connection);
                    await alterInvEb.ExecuteNonQueryAsync();
                }

                // Migration für purchase_invoices - easybill_attachment_id
                string checkInvAttColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'purchase_invoices' 
                    AND COLUMN_NAME = 'easybill_attachment_id'";

                using var checkInvAtt = new MySqlCommand(checkInvAttColumn, connection);
                if (Convert.ToInt32(await checkInvAtt.ExecuteScalarAsync()) == 0)
                {
                    using var alterInvAtt = new MySqlCommand(
                        "ALTER TABLE purchase_invoices ADD COLUMN easybill_attachment_id BIGINT NULL",
                        connection);
                    await alterInvAtt.ExecuteNonQueryAsync();
                }

                // Migration für purchase_orders Tabelle - easybill_document_id
                string checkOrderEasybillColumn = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'purchase_orders' 
                    AND COLUMN_NAME = 'easybill_document_id'";

                using var checkOrderEb = new MySqlCommand(checkOrderEasybillColumn, connection);
                if (Convert.ToInt32(await checkOrderEb.ExecuteScalarAsync()) == 0)
                {
                    using var alterOrderEb = new MySqlCommand(
                        "ALTER TABLE purchase_orders ADD COLUMN easybill_document_id BIGINT NULL, ADD COLUMN easybill_synced_at DATETIME NULL",
                        connection);
                    await alterOrderEb.ExecuteNonQueryAsync();
                }

                // Migration: file_data LONGBLOB in customer_documents
                string checkCustDocFileData = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'customer_documents' 
                    AND COLUMN_NAME = 'file_data'";

                using var checkCustFileData = new MySqlCommand(checkCustDocFileData, connection);
                if (Convert.ToInt32(await checkCustFileData.ExecuteScalarAsync()) == 0)
                {
                    using var alterCustDoc = new MySqlCommand(
                        "ALTER TABLE customer_documents ADD COLUMN file_data LONGBLOB NULL",
                        connection);
                    await alterCustDoc.ExecuteNonQueryAsync();
                }

                // Migration: file_data LONGBLOB in project_documents
                string checkProjDocFileData = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'project_documents' 
                    AND COLUMN_NAME = 'file_data'";

                using var checkProjFileData = new MySqlCommand(checkProjDocFileData, connection);
                if (Convert.ToInt32(await checkProjFileData.ExecuteScalarAsync()) == 0)
                {
                    using var alterProjDoc = new MySqlCommand(
                        "ALTER TABLE project_documents ADD COLUMN file_data LONGBLOB NULL",
                        connection);
                    await alterProjDoc.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Migration error: {ex.Message}");
            }
        }

        #region Project Methods

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            var projects = new List<Project>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "SELECT * FROM projects ORDER BY created_at DESC";
            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                projects.Add(new Project
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                    StartDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
                    EndDate = reader.IsDBNull(reader.GetOrdinal("end_date")) ? null : reader.GetDateTime(reader.GetOrdinal("end_date")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    ClientName = reader.IsDBNull(reader.GetOrdinal("client_name")) ? "" : reader.GetString(reader.GetOrdinal("client_name")),
                    EasybillCustomerId = reader.IsDBNull(reader.GetOrdinal("easybill_customer_id")) ? null : reader.GetInt64(reader.GetOrdinal("easybill_customer_id")),
                    EasybillProjectId = reader.IsDBNull(reader.GetOrdinal("easybill_project_id")) ? null : reader.GetInt64(reader.GetOrdinal("easybill_project_id")),
                    Budget = reader.GetDecimal(reader.GetOrdinal("budget")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }

            return projects;
        }

        public async Task<int> AddProjectAsync(Project project)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO projects (name, description, start_date, end_date, status, client_name, easybill_customer_id, easybill_project_id, budget, created_at)
                           VALUES (@name, @description, @startDate, @endDate, @status, @clientName, @easybillCustomerId, @easybillProjectId, @budget, @createdAt);
                           SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@name", project.Name);
            cmd.Parameters.AddWithValue("@description", project.Description ?? "");
            cmd.Parameters.AddWithValue("@startDate", project.StartDate);
            cmd.Parameters.AddWithValue("@endDate", project.EndDate.HasValue ? (object)project.EndDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@status", project.Status);
            cmd.Parameters.AddWithValue("@clientName", project.ClientName ?? "");
            cmd.Parameters.AddWithValue("@easybillCustomerId", project.EasybillCustomerId.HasValue ? (object)project.EasybillCustomerId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@easybillProjectId", project.EasybillProjectId.HasValue ? (object)project.EasybillProjectId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@budget", project.Budget);
            cmd.Parameters.AddWithValue("@createdAt", project.CreatedAt);

            var result = await cmd.ExecuteScalarAsync();
            var newId = Convert.ToInt32(result);
            _ = new AuditLogService(connectionString).LogAsync("Projekt", newId.ToString(), "Erstellt", project.Name);
            return newId;
        }

        public async Task UpdateProjectAsync(Project project)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"UPDATE projects SET name=@name, description=@description, start_date=@startDate,
                           end_date=@endDate, status=@status, client_name=@clientName, easybill_customer_id=@easybillCustomerId, 
                           easybill_project_id=@easybillProjectId, budget=@budget
                           WHERE id=@id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", project.Id);
            cmd.Parameters.AddWithValue("@name", project.Name);
            cmd.Parameters.AddWithValue("@description", project.Description ?? "");
            cmd.Parameters.AddWithValue("@startDate", project.StartDate);
            cmd.Parameters.AddWithValue("@endDate", project.EndDate.HasValue ? (object)project.EndDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@status", project.Status);
            cmd.Parameters.AddWithValue("@clientName", project.ClientName ?? "");
            cmd.Parameters.AddWithValue("@easybillCustomerId", project.EasybillCustomerId.HasValue ? (object)project.EasybillCustomerId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@easybillProjectId", project.EasybillProjectId.HasValue ? (object)project.EasybillProjectId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@budget", project.Budget);

            await cmd.ExecuteNonQueryAsync();
            _ = new AuditLogService(connectionString).LogAsync("Projekt", project.Id.ToString(), "Aktualisiert", project.Name);
        }

        public async Task DeleteProjectAsync(int projectId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "DELETE FROM projects WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", projectId);

            await cmd.ExecuteNonQueryAsync();
            _ = new AuditLogService(connectionString).LogAsync("Projekt", projectId.ToString(), "Gelöscht", string.Empty);
        }

        #endregion

        #region TimeEntry Methods

        public async Task<List<TimeEntry>> GetAllTimeEntriesAsync()
        {
            var entries = new List<TimeEntry>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"SELECT te.*, p.name as project_name 
                           FROM time_entries te
                           LEFT JOIN projects p ON te.project_id = p.id
                           ORDER BY te.date DESC";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                entries.Add(new TimeEntry
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    ProjectId = reader.GetInt32(reader.GetOrdinal("project_id")),
                    ProjectName = reader.IsDBNull(reader.GetOrdinal("project_name")) ? "" : reader.GetString(reader.GetOrdinal("project_name")),
                    EmployeeName = reader.GetString(reader.GetOrdinal("employee_name")),
                    ClientName = reader.IsDBNull(reader.GetOrdinal("client_name")) ? "" : reader.GetString(reader.GetOrdinal("client_name")),
                    EasybillCustomerId = reader.IsDBNull(reader.GetOrdinal("easybill_customer_id")) ? null : reader.GetInt64(reader.GetOrdinal("easybill_customer_id")),
                    Date = reader.GetDateTime(reader.GetOrdinal("date")),
                    Duration = ParseMySqlTime(reader.GetValue(reader.GetOrdinal("duration"))),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                    Activity = reader.IsDBNull(reader.GetOrdinal("activity")) ? "" : reader.GetString(reader.GetOrdinal("activity")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    IsExported = reader.IsDBNull(reader.GetOrdinal("is_exported")) ? false : reader.GetBoolean(reader.GetOrdinal("is_exported")),
                    ExportedToEasybillAt = reader.IsDBNull(reader.GetOrdinal("exported_to_easybill_at")) ? null : reader.GetDateTime(reader.GetOrdinal("exported_to_easybill_at")),
                    EasybillPositionId = reader.IsDBNull(reader.GetOrdinal("easybill_position_id")) ? null : reader.GetInt64(reader.GetOrdinal("easybill_position_id"))
                });
            }

            return entries;
        }

        public async Task<int> AddTimeEntryAsync(TimeEntry entry)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO time_entries (project_id, employee_name, client_name, easybill_customer_id, date, duration, description, activity, created_at)
                           VALUES (@projectId, @employeeName, @clientName, @easybillCustomerId, @date, @duration, @description, @activity, @createdAt);
                           SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@projectId", entry.ProjectId);
            cmd.Parameters.AddWithValue("@employeeName", entry.EmployeeName);
            cmd.Parameters.AddWithValue("@clientName", entry.ClientName ?? "");
            cmd.Parameters.AddWithValue("@easybillCustomerId", entry.EasybillCustomerId.HasValue ? (object)entry.EasybillCustomerId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@date", entry.Date);
            cmd.Parameters.AddWithValue("@duration", entry.Duration);
            cmd.Parameters.AddWithValue("@description", entry.Description ?? "");
            cmd.Parameters.AddWithValue("@activity", entry.Activity ?? "");
            cmd.Parameters.AddWithValue("@createdAt", entry.CreatedAt);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task DeleteTimeEntryAsync(int entryId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "DELETE FROM time_entries WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", entryId);

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Markiert Zeiteinträge als nach Easybill exportiert
        /// </summary>
        public async Task MarkTimeEntriesAsExportedAsync(List<int> entryIds, List<long> positionIds)
        {
            if (entryIds.Count != positionIds.Count)
            {
                throw new ArgumentException("entryIds und positionIds müssen gleich viele Elemente haben!");
            }

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            for (int i = 0; i < entryIds.Count; i++)
            {
                string query = @"UPDATE time_entries 
                               SET is_exported = TRUE, 
                                   exported_to_easybill_at = @exportedAt,
                                   easybill_position_id = @positionId
                               WHERE id = @id";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", entryIds[i]);
                cmd.Parameters.AddWithValue("@exportedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@positionId", positionIds[i]);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Holt alle noch nicht exportierten Zeiteinträge für einen Kunden
        /// </summary>
        public async Task<List<TimeEntry>> GetUnexportedTimeEntriesForCustomerAsync(long easybillCustomerId)
        {
            var entries = new List<TimeEntry>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"SELECT te.*, p.name as project_name 
                           FROM time_entries te
                           LEFT JOIN projects p ON te.project_id = p.id
                           WHERE te.easybill_customer_id = @customerId 
                           AND (te.is_exported IS NULL OR te.is_exported = FALSE)
                           ORDER BY te.date ASC";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@customerId", easybillCustomerId);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                entries.Add(new TimeEntry
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    ProjectId = reader.GetInt32(reader.GetOrdinal("project_id")),
                    ProjectName = reader.IsDBNull(reader.GetOrdinal("project_name")) ? "" : reader.GetString(reader.GetOrdinal("project_name")),
                    EmployeeName = reader.GetString(reader.GetOrdinal("employee_name")),
                    ClientName = reader.IsDBNull(reader.GetOrdinal("client_name")) ? "" : reader.GetString(reader.GetOrdinal("client_name")),
                    EasybillCustomerId = reader.IsDBNull(reader.GetOrdinal("easybill_customer_id")) ? null : reader.GetInt64(reader.GetOrdinal("easybill_customer_id")),
                    Date = reader.GetDateTime(reader.GetOrdinal("date")),
                    Duration = ParseMySqlTime(reader.GetValue(reader.GetOrdinal("duration"))),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                    Activity = reader.IsDBNull(reader.GetOrdinal("activity")) ? "" : reader.GetString(reader.GetOrdinal("activity")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    IsExported = reader.IsDBNull(reader.GetOrdinal("is_exported")) ? false : reader.GetBoolean(reader.GetOrdinal("is_exported")),
                    ExportedToEasybillAt = reader.IsDBNull(reader.GetOrdinal("exported_to_easybill_at")) ? null : reader.GetDateTime(reader.GetOrdinal("exported_to_easybill_at")),
                    EasybillPositionId = reader.IsDBNull(reader.GetOrdinal("easybill_position_id")) ? null : reader.GetInt64(reader.GetOrdinal("easybill_position_id"))
                });
            }

            return entries;
        }

        #endregion

        #region MeetingProtocol Methods

        public async Task<List<MeetingProtocol>> GetAllMeetingProtocolsAsync()
        {
            var protocols = new List<MeetingProtocol>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"SELECT mp.*, p.name as project_name 
                           FROM meeting_protocols mp
                           LEFT JOIN projects p ON mp.project_id = p.id
                           ORDER BY mp.meeting_date DESC";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                protocols.Add(new MeetingProtocol
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    ProjectId = reader.GetInt32(reader.GetOrdinal("project_id")),
                    ProjectName = reader.IsDBNull(reader.GetOrdinal("project_name")) ? "" : reader.GetString(reader.GetOrdinal("project_name")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    MeetingDate = reader.GetDateTime(reader.GetOrdinal("meeting_date")),
                    Location = reader.IsDBNull(reader.GetOrdinal("location")) ? "" : reader.GetString(reader.GetOrdinal("location")),
                    Participants = reader.IsDBNull(reader.GetOrdinal("participants")) ? "" : reader.GetString(reader.GetOrdinal("participants")),
                    Agenda = reader.IsDBNull(reader.GetOrdinal("agenda")) ? "" : reader.GetString(reader.GetOrdinal("agenda")),
                    Discussion = reader.IsDBNull(reader.GetOrdinal("discussion")) ? "" : reader.GetString(reader.GetOrdinal("discussion")),
                    Decisions = reader.IsDBNull(reader.GetOrdinal("decisions")) ? "" : reader.GetString(reader.GetOrdinal("decisions")),
                    ActionItems = reader.IsDBNull(reader.GetOrdinal("action_items")) ? "" : reader.GetString(reader.GetOrdinal("action_items")),
                    NextMeetingDate = reader.IsDBNull(reader.GetOrdinal("next_meeting_date")) ? "" : reader.GetString(reader.GetOrdinal("next_meeting_date")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }

            return protocols;
        }

        public async Task<int> AddMeetingProtocolAsync(MeetingProtocol protocol)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO meeting_protocols (project_id, title, meeting_date, location, participants, 
                           agenda, discussion, decisions, action_items, next_meeting_date, created_at)
                           VALUES (@projectId, @title, @meetingDate, @location, @participants, @agenda, 
                           @discussion, @decisions, @actionItems, @nextMeetingDate, @createdAt);
                           SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@projectId", protocol.ProjectId);
            cmd.Parameters.AddWithValue("@title", protocol.Title);
            cmd.Parameters.AddWithValue("@meetingDate", protocol.MeetingDate);
            cmd.Parameters.AddWithValue("@location", protocol.Location ?? "");
            cmd.Parameters.AddWithValue("@participants", protocol.Participants ?? "");
            cmd.Parameters.AddWithValue("@agenda", protocol.Agenda ?? "");
            cmd.Parameters.AddWithValue("@discussion", protocol.Discussion ?? "");
            cmd.Parameters.AddWithValue("@decisions", protocol.Decisions ?? "");
            cmd.Parameters.AddWithValue("@actionItems", protocol.ActionItems ?? "");
            cmd.Parameters.AddWithValue("@nextMeetingDate", protocol.NextMeetingDate ?? "");
            cmd.Parameters.AddWithValue("@createdAt", protocol.CreatedAt);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task DeleteMeetingProtocolAsync(int protocolId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "DELETE FROM meeting_protocols WHERE id=@id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", protocolId);

            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Database Reset

        /// <summary>
        /// Setzt alle AUTO_INCREMENT-Werte zurück und löscht optional alle Daten
        /// </summary>
        /// <param name="deleteAllData">Wenn true, werden alle Daten gelöscht; wenn false, nur AUTO_INCREMENT zurückgesetzt</param>
        public async Task ResetDatabaseAsync(bool deleteAllData = true)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // Korrekte Tabellenliste (in Reihenfolge für Foreign Keys)
            var tablesToReset = new List<string>
            {
                "meeting_protocols",    // Reihenfolge wichtig wegen Foreign Keys
                "time_entries",
                "milestones",
                "tasks",
                "projects",
                "employees"
            };

            // Deaktiviere Foreign Key Checks temporär
            using (var cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            try
            {
                foreach (var table in tablesToReset)
                {
                    if (deleteAllData)
                    {
                        // TRUNCATE löscht alle Daten UND setzt AUTO_INCREMENT zurück
                        string truncateQuery = $"TRUNCATE TABLE {table}";
                        using var truncateCmd = new MySqlCommand(truncateQuery, connection);
                        await truncateCmd.ExecuteNonQueryAsync();
                        System.Diagnostics.Debug.WriteLine($"✅ Tabelle '{table}' geleert und AUTO_INCREMENT zurückgesetzt");
                    }
                    else
                    {
                        // Nur AUTO_INCREMENT zurücksetzen (funktioniert nur bei leeren Tabellen)
                        string resetQuery = $"ALTER TABLE {table} AUTO_INCREMENT = 1";
                        using var resetCmd = new MySqlCommand(resetQuery, connection);
                        await resetCmd.ExecuteNonQueryAsync();
                        System.Diagnostics.Debug.WriteLine($"✅ AUTO_INCREMENT für '{table}' zurückgesetzt");
                    }
                }
            }
            finally
            {
                // Foreign Key Checks wieder aktivieren
                using var cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1", connection);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        #endregion

        #region User Management

        /// <summary>
        /// Holt alle Benutzer
        /// </summary>
        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT u.*, CONCAT(e.first_name, ' ', e.last_name) AS employee_name
                FROM users u
                LEFT JOIN employees e ON u.employee_id = e.id
                ORDER BY u.username";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    EmployeeId = reader.IsDBNull(reader.GetOrdinal("employee_id")) ? null : reader.GetInt32(reader.GetOrdinal("employee_id")),
                    Username = reader.GetString(reader.GetOrdinal("username")),
                    PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                    Role = reader.GetString(reader.GetOrdinal("role")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    LastLogin = reader.IsDBNull(reader.GetOrdinal("last_login")) ? null : reader.GetDateTime(reader.GetOrdinal("last_login")),
                    EmployeeName = reader.IsDBNull(reader.GetOrdinal("employee_name")) ? "" : reader.GetString(reader.GetOrdinal("employee_name"))
                });
            }

            return users;
        }

        /// <summary>
        /// Authentifiziert einen Benutzer
        /// </summary>
        public async Task<User> AuthenticateUserAsync(string username, string passwordHash)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT u.*, CONCAT(e.first_name, ' ', e.last_name) AS employee_name
                FROM users u
                LEFT JOIN employees e ON u.employee_id = e.id
                WHERE u.username = @username AND u.password_hash = @passwordHash AND u.is_active = TRUE";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@passwordHash", passwordHash);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var user = new User
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    EmployeeId = reader.IsDBNull(reader.GetOrdinal("employee_id")) ? null : reader.GetInt32(reader.GetOrdinal("employee_id")),
                    Username = reader.GetString(reader.GetOrdinal("username")),
                    PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                    Role = reader.GetString(reader.GetOrdinal("role")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    LastLogin = reader.IsDBNull(reader.GetOrdinal("last_login")) ? null : reader.GetDateTime(reader.GetOrdinal("last_login")),
                    EmployeeName = reader.IsDBNull(reader.GetOrdinal("employee_name")) ? "" : reader.GetString(reader.GetOrdinal("employee_name"))
                };

                reader.Close();

                // Update last login
                await UpdateLastLoginAsync(user.Id);
                _ = new AuditLogService(connectionString).LogAsync("Benutzer", user.Id.ToString(), "Angemeldet", user.Username);

                return user;
            }

            return null;
        }

        /// <summary>
        /// Aktualisiert den letzten Login-Zeitpunkt
        /// </summary>
        private async Task UpdateLastLoginAsync(int userId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "UPDATE users SET last_login = @lastLogin WHERE id = @id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@lastLogin", DateTime.Now);
            cmd.Parameters.AddWithValue("@id", userId);

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Erstellt einen neuen Benutzer
        /// </summary>
        public async Task<int> AddUserAsync(User user)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO users (employee_id, username, password_hash, role, is_active, created_at)
                VALUES (@employeeId, @username, @passwordHash, @role, @isActive, @createdAt);
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@employeeId", (object)user.EmployeeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@username", user.Username);
            cmd.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@role", user.Role);
            cmd.Parameters.AddWithValue("@isActive", user.IsActive);
            cmd.Parameters.AddWithValue("@createdAt", user.CreatedAt);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Aktualisiert einen Benutzer
        /// </summary>
        public async Task UpdateUserAsync(User user)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                UPDATE users 
                SET employee_id = @employeeId,
                    username = @username,
                    role = @role,
                    is_active = @isActive
                WHERE id = @id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", user.Id);
            cmd.Parameters.AddWithValue("@employeeId", (object)user.EmployeeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@username", user.Username);
            cmd.Parameters.AddWithValue("@role", user.Role);
            cmd.Parameters.AddWithValue("@isActive", user.IsActive);

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Ändert das Passwort eines Benutzers
        /// </summary>
        public async Task ChangePasswordAsync(int userId, string newPasswordHash)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "UPDATE users SET password_hash = @passwordHash WHERE id = @id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@passwordHash", newPasswordHash);
            cmd.Parameters.AddWithValue("@id", userId);

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Löscht einen Benutzer
        /// </summary>
        public async Task DeleteUserAsync(int userId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "DELETE FROM users WHERE id = @id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", userId);

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Prüft, ob mindestens ein Admin-Benutzer existiert
        /// </summary>
        public async Task<bool> HasAdminUserAsync()
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "SELECT COUNT(*) FROM users WHERE role = 'Admin' AND is_active = TRUE";

            using var cmd = new MySqlCommand(query, connection);
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return count > 0;
        }

        #endregion

        #region Customer Methods

        /// <summary>
        /// Holt alle Kunden aus der Datenbank
        /// </summary>
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT id, company_name, first_name, last_name, email, phone,
                       street, zip_code, city, country, vat_id, note,
                       easybill_customer_id, last_synced_at, created_at, updated_at, is_active
                FROM customers
                WHERE is_active = TRUE
                ORDER BY company_name, last_name, first_name";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                customers.Add(new Customer
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    CompanyName = reader.IsDBNull(reader.GetOrdinal("company_name")) ? null : reader.GetString(reader.GetOrdinal("company_name")),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetString(reader.GetOrdinal("last_name")),
                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                    Street = reader.IsDBNull(reader.GetOrdinal("street")) ? null : reader.GetString(reader.GetOrdinal("street")),
                    ZipCode = reader.IsDBNull(reader.GetOrdinal("zip_code")) ? null : reader.GetString(reader.GetOrdinal("zip_code")),
                    City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString(reader.GetOrdinal("city")),
                    Country = reader.IsDBNull(reader.GetOrdinal("country")) ? null : reader.GetString(reader.GetOrdinal("country")),
                    VatId = reader.IsDBNull(reader.GetOrdinal("vat_id")) ? null : reader.GetString(reader.GetOrdinal("vat_id")),
                    Note = reader.IsDBNull(reader.GetOrdinal("note")) ? null : reader.GetString(reader.GetOrdinal("note")),
                    EasybillCustomerId = reader.IsDBNull(reader.GetOrdinal("easybill_customer_id")) ? null : reader.GetInt64(reader.GetOrdinal("easybill_customer_id")),
                    LastSyncedAt = reader.IsDBNull(reader.GetOrdinal("last_synced_at")) ? null : reader.GetDateTime(reader.GetOrdinal("last_synced_at")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active"))
                });
            }

            return customers;
        }

        /// <summary>
        /// Fügt einen neuen Kunden hinzu
        /// </summary>
        public async Task<Customer> AddCustomerAsync(Customer customer)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO customers 
                (company_name, first_name, last_name, email, phone, street, zip_code, city, country, 
                 vat_id, note, easybill_customer_id, last_synced_at, created_at, is_active)
                VALUES 
                (@company_name, @first_name, @last_name, @email, @phone, @street, @zip_code, @city, @country,
                 @vat_id, @note, @easybill_customer_id, @last_synced_at, @created_at, @is_active);
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@company_name", customer.CompanyName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@first_name", customer.FirstName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@last_name", customer.LastName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@email", customer.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", customer.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@street", customer.Street ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@zip_code", customer.ZipCode ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@city", customer.City ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@country", customer.Country ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@vat_id", customer.VatId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@note", customer.Note ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@easybill_customer_id", customer.EasybillCustomerId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@last_synced_at", customer.LastSyncedAt ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@created_at", customer.CreatedAt);
            cmd.Parameters.AddWithValue("@is_active", customer.IsActive);

            customer.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            _ = new AuditLogService(connectionString).LogAsync("Kunde", customer.Id.ToString(), "Erstellt", customer.CompanyName ?? customer.LastName ?? "");
            return customer;
        }

        /// <summary>
        /// Aktualisiert einen Kunden
        /// </summary>
        public async Task UpdateCustomerAsync(Customer customer)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                UPDATE customers SET
                    company_name = @company_name,
                    first_name = @first_name,
                    last_name = @last_name,
                    email = @email,
                    phone = @phone,
                    street = @street,
                    zip_code = @zip_code,
                    city = @city,
                    country = @country,
                    vat_id = @vat_id,
                    note = @note,
                    easybill_customer_id = @easybill_customer_id,
                    last_synced_at = @last_synced_at,
                    updated_at = @updated_at,
                    is_active = @is_active
                WHERE id = @id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", customer.Id);
            cmd.Parameters.AddWithValue("@company_name", customer.CompanyName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@first_name", customer.FirstName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@last_name", customer.LastName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@email", customer.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", customer.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@street", customer.Street ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@zip_code", customer.ZipCode ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@city", customer.City ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@country", customer.Country ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@vat_id", customer.VatId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@note", customer.Note ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@easybill_customer_id", customer.EasybillCustomerId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@last_synced_at", customer.LastSyncedAt ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@updated_at", DateTime.Now);
            cmd.Parameters.AddWithValue("@is_active", customer.IsActive);

            await cmd.ExecuteNonQueryAsync();
            _ = new AuditLogService(connectionString).LogAsync("Kunde", customer.Id.ToString(), "Aktualisiert", customer.CompanyName ?? customer.LastName ?? "");
        }

        /// <summary>
        /// Löscht einen Kunden (soft delete)
        /// </summary>
        public async Task DeleteCustomerAsync(Customer customer)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "UPDATE customers SET is_active = FALSE WHERE id = @id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", customer.Id);

            await cmd.ExecuteNonQueryAsync();
            _ = new AuditLogService(connectionString).LogAsync("Kunde", customer.Id.ToString(), "Gelöscht", customer.CompanyName ?? customer.LastName ?? "");
        }

        /// <summary>
        /// Sucht einen Kunden anhand der Easybill-Kunden-ID
        /// </summary>
        public async Task<Customer> GetCustomerByEasybillIdAsync(long easybillCustomerId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT id, company_name, first_name, last_name, email, phone,
                       street, zip_code, city, country, vat_id, note,
                       easybill_customer_id, last_synced_at, created_at, updated_at, is_active
                FROM customers
                WHERE easybill_customer_id = @easybill_customer_id AND is_active = TRUE
                LIMIT 1";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@easybill_customer_id", easybillCustomerId);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Customer
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    CompanyName = reader.IsDBNull(reader.GetOrdinal("company_name")) ? null : reader.GetString(reader.GetOrdinal("company_name")),
                    FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetString(reader.GetOrdinal("last_name")),
                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                    Street = reader.IsDBNull(reader.GetOrdinal("street")) ? null : reader.GetString(reader.GetOrdinal("street")),
                    ZipCode = reader.IsDBNull(reader.GetOrdinal("zip_code")) ? null : reader.GetString(reader.GetOrdinal("zip_code")),
                    City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString(reader.GetOrdinal("city")),
                    Country = reader.IsDBNull(reader.GetOrdinal("country")) ? null : reader.GetString(reader.GetOrdinal("country")),
                    VatId = reader.IsDBNull(reader.GetOrdinal("vat_id")) ? null : reader.GetString(reader.GetOrdinal("vat_id")),
                    Note = reader.IsDBNull(reader.GetOrdinal("note")) ? null : reader.GetString(reader.GetOrdinal("note")),
                    EasybillCustomerId = reader.IsDBNull(reader.GetOrdinal("easybill_customer_id")) ? null : reader.GetInt64(reader.GetOrdinal("easybill_customer_id")),
                    LastSyncedAt = reader.IsDBNull(reader.GetOrdinal("last_synced_at")) ? null : reader.GetDateTime(reader.GetOrdinal("last_synced_at")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active"))
                };
            }

            return null;
        }

        #endregion

        #region Customer Document Methods

        public async Task<List<CustomerDocument>> GetCustomerDocumentsAsync(int customerId)
        {
            var documents = new List<CustomerDocument>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "SELECT * FROM customer_documents WHERE customer_id = @customerId ORDER BY uploaded_at DESC";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@customerId", customerId);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                documents.Add(new CustomerDocument
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    CustomerId = reader.GetInt32(reader.GetOrdinal("customer_id")),
                    FileName = reader.GetString(reader.GetOrdinal("file_name")),
                    FilePath = reader.GetString(reader.GetOrdinal("file_path")),
                    FileType = reader.IsDBNull(reader.GetOrdinal("file_type")) ? "" : reader.GetString(reader.GetOrdinal("file_type")),
                    FileSize = reader.IsDBNull(reader.GetOrdinal("file_size")) ? 0 : reader.GetInt64(reader.GetOrdinal("file_size")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                    UploadedBy = reader.IsDBNull(reader.GetOrdinal("uploaded_by")) ? "" : reader.GetString(reader.GetOrdinal("uploaded_by")),
                    UploadedAt = reader.GetDateTime(reader.GetOrdinal("uploaded_at"))
                });
            }

            return documents;
        }

        public async Task<int> AddCustomerDocumentAsync(CustomerDocument document)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO customer_documents (customer_id, file_name, file_path, file_type, file_size, description, uploaded_by, uploaded_at, file_data)
                           VALUES (@customerId, @fileName, @filePath, @fileType, @fileSize, @description, @uploadedBy, @uploadedAt, @fileData);
                           SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@customerId", document.CustomerId);
            cmd.Parameters.AddWithValue("@fileName", document.FileName);
            cmd.Parameters.AddWithValue("@filePath", document.FilePath ?? "");
            cmd.Parameters.AddWithValue("@fileType", document.FileType ?? "");
            cmd.Parameters.AddWithValue("@fileSize", document.FileSize);
            cmd.Parameters.AddWithValue("@description", document.Description ?? "");
            cmd.Parameters.AddWithValue("@uploadedBy", document.UploadedBy ?? "");
            cmd.Parameters.AddWithValue("@uploadedAt", document.UploadedAt);
            cmd.Parameters.AddWithValue("@fileData", document.FileData != null && document.FileData.Length > 0 ? (object)document.FileData : DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task DeleteCustomerDocumentAsync(int documentId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "DELETE FROM customer_documents WHERE id = @id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", documentId);
            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Project Document Methods

        public async Task<List<ProjectDocument>> GetProjectDocumentsAsync(int projectId)
        {
            var documents = new List<ProjectDocument>();
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "SELECT * FROM project_documents WHERE project_id = @projectId ORDER BY uploaded_at DESC";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@projectId", projectId);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                documents.Add(new ProjectDocument
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    ProjectId = reader.GetInt32(reader.GetOrdinal("project_id")),
                    FileName = reader.GetString(reader.GetOrdinal("file_name")),
                    FilePath = reader.GetString(reader.GetOrdinal("file_path")),
                    FileType = reader.IsDBNull(reader.GetOrdinal("file_type")) ? "" : reader.GetString(reader.GetOrdinal("file_type")),
                    FileSize = reader.IsDBNull(reader.GetOrdinal("file_size")) ? 0 : reader.GetInt64(reader.GetOrdinal("file_size")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                    UploadedBy = reader.IsDBNull(reader.GetOrdinal("uploaded_by")) ? "" : reader.GetString(reader.GetOrdinal("uploaded_by")),
                    UploadedAt = reader.GetDateTime(reader.GetOrdinal("uploaded_at"))
                });
            }

            return documents;
        }

        public async Task<int> AddProjectDocumentAsync(ProjectDocument document)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"INSERT INTO project_documents (project_id, file_name, file_path, file_type, file_size, description, uploaded_by, uploaded_at, file_data)
                           VALUES (@projectId, @fileName, @filePath, @fileType, @fileSize, @description, @uploadedBy, @uploadedAt, @fileData);
                           SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@projectId", document.ProjectId);
            cmd.Parameters.AddWithValue("@fileName", document.FileName);
            cmd.Parameters.AddWithValue("@filePath", document.FilePath ?? "");
            cmd.Parameters.AddWithValue("@fileType", document.FileType ?? "");
            cmd.Parameters.AddWithValue("@fileSize", document.FileSize);
            cmd.Parameters.AddWithValue("@description", document.Description ?? "");
            cmd.Parameters.AddWithValue("@uploadedBy", document.UploadedBy ?? "");
            cmd.Parameters.AddWithValue("@uploadedAt", document.UploadedAt);
            cmd.Parameters.AddWithValue("@fileData", document.FileData != null && document.FileData.Length > 0 ? (object)document.FileData : DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task DeleteProjectDocumentAsync(int documentId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "DELETE FROM project_documents WHERE id = @id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", documentId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<byte[]> GetCustomerDocumentFileDataAsync(int documentId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "SELECT file_data FROM customer_documents WHERE id = @id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", documentId);

            var result = await cmd.ExecuteScalarAsync();
            return result is byte[] data ? data : null;
        }

        public async Task<byte[]> GetProjectDocumentFileDataAsync(int documentId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "SELECT file_data FROM project_documents WHERE id = @id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@id", documentId);

            var result = await cmd.ExecuteScalarAsync();
            return result is byte[] data ? data : null;
        }

        #endregion
    }
}
