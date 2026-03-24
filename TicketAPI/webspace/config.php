<?php
/**
 * Datenbank-Konfiguration für Webspace
 * 
 * WICHTIG: 
 * - Passen Sie die Werte an Ihre Webspace-Datenbank an
 * - Diese Datei NICHT öffentlich zugänglich machen (.htaccess)
 * - Sichere Passwörter verwenden
 */

// Datenbank-Verbindung - DOGADO KONFIGURATION
// DIESE WERTE ANPASSEN!
define('DB_HOST', '127.0.0.1');              // Dogado: 127.0.0.1
define('DB_PORT', '3307');                   // Dogado: Port 3307 (nicht 3306!)
define('DB_NAME', 'h761307_projektsoftware');              // Ihr Datenbankname
define('DB_USER', 'h761307_fiessinger');                // Ihr Datenbank-Benutzername
define('DB_PASS', 'wmf15rBG....');      // Ihr Datenbank-Passwort

// E-Mail-Benachrichtigungen
define('ENABLE_EMAIL_NOTIFICATIONS', false);  // true = E-Mails aktivieren
define('EMAIL_FROM', 'support@ihre-domain.de'); // Absender-Adresse

/**
 * BEISPIEL-KONFIGURATIONEN für verschiedene Hosting-Anbieter:
 * 
 * === ALL-INKL.COM ===
 * DB_HOST: 'localhost' oder 'db12345.kasserver.com'
 * DB_NAME: 'd01234567'
 * DB_USER: 'd01234567'
 * 
 * === STRATO ===
 * DB_HOST: 'rdbms.strato.de'
 * DB_NAME: 'DB123456'
 * DB_USER: 'U123456'
 * 
 * === 1&1 / IONOS ===
 * DB_HOST: 'dbXXXXX.db.1and1.com'
 * DB_NAME: 'db123456789'
 * DB_USER: 'dboXXXXXXX'
 * 
 * === HOSTEUROPE ===
 * DB_HOST: 'mysql5.domainname.de'
 * DB_NAME: 'usr_p123456_1'
 * DB_USER: 'usr_p123456_1'
 * 
 * === WEBGO ===
 * DB_HOST: 'dbXXXX.webhosting.eu'
 * DB_NAME: 'db123456'
 * DB_USER: 'db123456'
 */

/**
 * HINWEIS ZUR VERBINDUNG:
 * 
 * Wenn Sie eine REMOTE-Verbindung benötigen (von extern zugreifen):
 * 1. Im Webspace-Panel "Remote MySQL" aktivieren
 * 2. Ihre IP-Adresse freigeben
 * 3. Eventuell Port ändern (z.B. 3306)
 * 
 * Für die Desktop-App remote:
 * $connectionString = "Server=db.ihre-domain.de;Port=3306;Database=projektdb;Uid=db_user;Pwd=passwort;"
 */

/**
 * SICHERHEIT:
 * 
 * Erstellen Sie eine .htaccess im gleichen Ordner:
 * 
 * <Files "config.php">
 *     Order Allow,Deny
 *     Deny from all
 * </Files>
 */
?>
