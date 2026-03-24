<?php
/**
 * Ticket API Endpoint für Webspace
 * Speichert Support-Tickets in MySQL-Datenbank
 * 
 * INSTALLATION:
 * 1. Diese Datei auf Webspace hochladen (z.B. /api/create-ticket.php)
 * 2. config.php erstellen mit DB-Zugangsdaten
 * 3. In HTML-Formular API-URL anpassen
 */

// Fehlerberichterstattung (für Produktion)
error_reporting(E_ALL);
ini_set('display_errors', 0); // DEBUG DEAKTIVIERT - keine Details für Besucher sichtbar

// CORS-Header setzen (erlaubt Zugriff von Ihrer Website)
header('Access-Control-Allow-Origin: *'); // ANPASSEN: https://af-software-engineering.de
header('Access-Control-Allow-Methods: POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');
header('Content-Type: application/json; charset=utf-8');

// Preflight-Request (OPTIONS) behandeln
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

// Nur POST-Requests erlauben
if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode([
        'success' => false,
        'message' => 'Nur POST-Requests sind erlaubt'
    ]);
    exit;
}

// Datenbank-Konfiguration laden
require_once __DIR__ . '/config.php';

// Datenbankverbindung herstellen
try {
    $dsn = "mysql:host=" . DB_HOST . ";port=" . DB_PORT . ";dbname=" . DB_NAME . ";charset=utf8mb4";
    $db = new PDO(
        $dsn,
        DB_USER,
        DB_PASS,
        [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false
        ]
    );
} catch (PDOException $e) {
    http_response_code(500);
    echo json_encode([
        'success' => false,
        'message' => 'Datenbankverbindung fehlgeschlagen'
    ]);
    error_log("DB Error: " . $e->getMessage());
    exit;
}

// JSON-Daten empfangen
$input = file_get_contents('php://input');
$data = json_decode($input, true);

if (!$data) {
    http_response_code(400);
    echo json_encode([
        'success' => false,
        'message' => 'Ungültige JSON-Daten'
    ]);
    exit;
}

// Validierung
$errors = [];

// Name
if (empty($data['customerName']) || strlen(trim($data['customerName'])) < 2) {
    $errors[] = 'Name muss mindestens 2 Zeichen lang sein';
}

// E-Mail
if (empty($data['customerEmail']) || !filter_var($data['customerEmail'], FILTER_VALIDATE_EMAIL)) {
    $errors[] = 'Ungültige E-Mail-Adresse';
}

// Betreff
if (empty($data['subject']) || strlen(trim($data['subject'])) < 5) {
    $errors[] = 'Betreff muss mindestens 5 Zeichen lang sein';
}

// Beschreibung
if (empty($data['description']) || strlen(trim($data['description'])) < 20) {
    $errors[] = 'Beschreibung muss mindestens 20 Zeichen lang sein';
}

// Kategorie
$category = isset($data['category']) ? intval($data['category']) : 0;
if ($category < 0 || $category > 4) {
    $errors[] = 'Ungültige Kategorie';
}

// Priorität
$priority = isset($data['priority']) ? intval($data['priority']) : 1;
if ($priority < 0 || $priority > 3) {
    $errors[] = 'Ungültige Priorität';
}

// Bei Validierungsfehlern
if (!empty($errors)) {
    http_response_code(400);
    echo json_encode([
        'success' => false,
        'message' => 'Validierungsfehler',
        'errors' => $errors
    ]);
    exit;
}

// Daten bereinigen
$customerName = trim($data['customerName']);
$customerEmail = trim($data['customerEmail']);
$customerPhone = isset($data['customerPhone']) ? trim($data['customerPhone']) : '';
$subject = trim($data['subject']);
$description = trim($data['description']);

// IP-Adresse und User-Agent erfassen
$ipAddress = $_SERVER['REMOTE_ADDR'] ?? 'unknown';
$userAgent = $_SERVER['HTTP_USER_AGENT'] ?? 'unknown';

// Status: 0 = Neu
$status = 0;

// Aktuelles Datum
$createdAt = date('Y-m-d H:i:s');

try {
    // Ticket in Datenbank einfügen
    $sql = "INSERT INTO tickets (
        customer_name, 
        customer_email, 
        customer_phone, 
        subject, 
        description, 
        priority, 
        status, 
        category, 
        ip_address, 
        user_agent, 
        created_at
    ) VALUES (
        :customer_name,
        :customer_email,
        :customer_phone,
        :subject,
        :description,
        :priority,
        :status,
        :category,
        :ip_address,
        :user_agent,
        :created_at
    )";

    $stmt = $db->prepare($sql);
    
    $stmt->execute([
        ':customer_name' => $customerName,
        ':customer_email' => $customerEmail,
        ':customer_phone' => $customerPhone,
        ':subject' => $subject,
        ':description' => $description,
        ':priority' => $priority,
        ':status' => $status,
        ':category' => $category,
        ':ip_address' => $ipAddress,
        ':user_agent' => $userAgent,
        ':created_at' => $createdAt
    ]);

    // ID des eingefügten Tickets
    $ticketId = $db->lastInsertId();
    
    // Ticketnummer formatieren
    $ticketNumber = '#' . str_pad($ticketId, 6, '0', STR_PAD_LEFT);

    // Kategorie-Text
    $categoryTexts = [
        0 => 'Allgemein',
        1 => 'Technisch',
        2 => 'Abrechnung',
        3 => 'Feature-Anfrage',
        4 => 'Fehler'
    ];

    // Priorität-Text
    $priorityTexts = [
        0 => 'Niedrig',
        1 => 'Mittel',
        2 => 'Hoch',
        3 => 'Dringend'
    ];

    // Optional: E-Mail-Benachrichtigung senden
    if (defined('ENABLE_EMAIL_NOTIFICATIONS') && ENABLE_EMAIL_NOTIFICATIONS) {
        sendTicketConfirmation($customerEmail, $customerName, $ticketNumber, $subject);
    }

    // Erfolgsantwort
    http_response_code(201);
    echo json_encode([
        'success' => true,
        'data' => [
            'id' => intval($ticketId),
            'ticketNumber' => $ticketNumber,
            'customerName' => $customerName,
            'customerEmail' => $customerEmail,
            'subject' => $subject,
            'status' => 'Neu',
            'priority' => $priorityTexts[$priority],
            'category' => $categoryTexts[$category],
            'createdAt' => $createdAt,
            'message' => 'Ihr Support-Ticket wurde erfolgreich erstellt. Wir werden uns schnellstmöglich bei Ihnen melden.'
        ]
    ]);

    // Log (optional)
    error_log("Neues Ticket erstellt: $ticketNumber von $customerEmail");

} catch (PDOException $e) {
    http_response_code(500);
    echo json_encode([
        'success' => false,
        'message' => 'Fehler beim Speichern des Tickets'
    ]);
    error_log("SQL Error: " . $e->getMessage());
}

/**
 * E-Mail-Bestätigung senden (optional)
 */
function sendTicketConfirmation($to, $name, $ticketNumber, $subject) {
    if (!defined('EMAIL_FROM')) {
        return false;
    }
    
    $emailSubject = "Ticket $ticketNumber - Bestätigung";
    
    $message = "
    <html>
    <head>
        <style>
            body { font-family: Arial, sans-serif; }
            .container { max-width: 600px; margin: 0 auto; padding: 20px; }
            .header { background: #2196F3; color: white; padding: 20px; text-align: center; }
            .content { padding: 20px; background: #f9f9f9; }
            .ticket-info { background: white; padding: 15px; margin: 15px 0; border-left: 4px solid #2196F3; }
            .footer { text-align: center; color: #666; font-size: 12px; margin-top: 20px; }
        </style>
    </head>
    <body>
        <div class='container'>
            <div class='header'>
                <h2>Vielen Dank für Ihre Anfrage!</h2>
            </div>
            <div class='content'>
                <p>Hallo " . htmlspecialchars($name) . ",</p>
                <p>Ihr Support-Ticket wurde erfolgreich erstellt.</p>
                
                <div class='ticket-info'>
                    <strong>Ticket-Nummer:</strong> $ticketNumber<br>
                    <strong>Betreff:</strong> " . htmlspecialchars($subject) . "<br>
                    <strong>Status:</strong> Neu
                </div>
                
                <p>Wir werden uns schnellstmöglich bei Ihnen melden.</p>
                <p>Bei dringenden Fragen können Sie uns auch telefonisch erreichen.</p>
            </div>
            <div class='footer'>
                <p>Dies ist eine automatische Nachricht. Bitte antworten Sie nicht auf diese E-Mail.</p>
            </div>
        </div>
    </body>
    </html>
    ";

    $headers = "MIME-Version: 1.0" . "\r\n";
    $headers .= "Content-type:text/html;charset=UTF-8" . "\r\n";
    $headers .= "From: " . EMAIL_FROM . "\r\n";

    return mail($to, $emailSubject, $message, $headers);
}
?>
