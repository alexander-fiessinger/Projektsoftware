#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# Projektsoftware – Dogado VPS Setup (81.88.26.77)
# Als administrator ausführen: sudo bash setup-server.sh
# ═══════════════════════════════════════════════════════════════

set -e

# Prüfen ob root/sudo
if [ "$EUID" -ne 0 ]; then
    echo "Bitte mit sudo ausführen: sudo bash setup-server.sh"
    exit 1
fi

echo "══════════════════════════════════════════════════"
echo "  Projektsoftware – Server-Setup"
echo "══════════════════════════════════════════════════"

# ── 1. System aktualisieren ──────────────────────────────────
echo "→ System wird aktualisiert..."
apt update && apt upgrade -y

# ── 2. .NET 10 Runtime installieren ──────────────────────────
echo "→ .NET 10 wird installiert..."
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet
ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
echo 'export DOTNET_ROOT=/usr/share/dotnet' >> /etc/environment
dotnet --info

# ── 3. MySQL installieren ───────────────────────────────────
echo "→ MySQL wird installiert..."
apt install -y mysql-server
systemctl enable mysql
systemctl start mysql

echo "→ Datenbank + Benutzer anlegen..."
mysql -e "CREATE DATABASE IF NOT EXISTS projektsoftware CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
mysql -e "CREATE USER IF NOT EXISTS 'projektsoftware_app'@'localhost' IDENTIFIED BY 'HIER_SICHERES_PASSWORT_EINTRAGEN';"
mysql -e "GRANT ALL PRIVILEGES ON projektsoftware.* TO 'projektsoftware_app'@'localhost';"
mysql -e "FLUSH PRIVILEGES;"

# ── 4. Nginx als Reverse Proxy ──────────────────────────────
echo "→ Nginx wird installiert..."
apt install -y nginx

cat > /etc/nginx/sites-available/projektsoftware << 'NGINX'
server {
    listen 80;
    server_name _;  # Wird später durch Domain ersetzt

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;

        # Blazor SignalR braucht WebSockets
        proxy_read_timeout 86400;
    }
}
NGINX

ln -sf /etc/nginx/sites-available/projektsoftware /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl restart nginx

# ── 5. App-Verzeichnis erstellen ─────────────────────────────
echo "→ App-Verzeichnis wird erstellt..."
mkdir -p /var/www/projektsoftware
chown www-data:www-data /var/www/projektsoftware

# ── 6. Systemd-Service erstellen ─────────────────────────────
echo "→ Systemd-Service wird erstellt..."
cat > /etc/systemd/system/projektsoftware.service << 'SERVICE'
[Unit]
Description=Projektsoftware API + Web
After=network.target mysql.service

[Service]
WorkingDirectory=/var/www/projektsoftware
ExecStart=/usr/bin/dotnet /var/www/projektsoftware/Projektsoftware.Api.dll
Restart=always
RestartSec=5
SyslogIdentifier=projektsoftware
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=DOTNET_ROOT=/usr/share/dotnet

[Install]
WantedBy=multi-user.target
SERVICE

systemctl daemon-reload
systemctl enable projektsoftware

echo ""
echo "══════════════════════════════════════════════════"
echo "  ✅ Server-Setup abgeschlossen!"
echo ""
echo "  Nächste Schritte (auf deinem Windows-PC):"
echo ""
echo "  1. Deploy-Script ausführen:"
echo "     .\\Projektsoftware.Api\\deploy\\deploy.ps1"
echo ""
echo "  2. Dann im Browser öffnen:"
echo "     http://81.88.26.77"
echo "══════════════════════════════════════════════════"
