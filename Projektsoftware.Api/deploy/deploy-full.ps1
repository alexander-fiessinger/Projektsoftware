# Projektsoftware - Komplettes Deployment auf Dogado VPS
# Server: 81.88.26.77 (Ubuntu 24.04, User: administrator)
# Ausfuehren: .\Projektsoftware.Api\deploy\deploy-full.ps1

$ServerIp = "81.88.26.77"
$User = "administrator"
$DbPassword = "Pr0j3kt" + (Get-Random -Minimum 1000 -Maximum 9999) + "Sf"
$Remote = "$User@$ServerIp"

function Invoke-Ssh { param([string]$Cmd) ssh $Remote $Cmd; if ($LASTEXITCODE -ne 0) { Write-Host "FEHLER bei: $Cmd" -ForegroundColor Red } }

Write-Host ""
Write-Host "=== Projektsoftware - Full Deploy ===" -ForegroundColor Cyan
Write-Host "  Server:      $ServerIp" -ForegroundColor White
Write-Host "  DB-Passwort: $DbPassword" -ForegroundColor Yellow
Write-Host "  (Bitte notieren!)" -ForegroundColor Yellow
Write-Host ""

# === SCHRITT 1: Server einrichten ===
Write-Host "=== SCHRITT 1/4: Server einrichten ===" -ForegroundColor Cyan

Write-Host "  -> System aktualisieren..." -ForegroundColor Gray
Invoke-Ssh "sudo apt-get update -y && sudo DEBIAN_FRONTEND=noninteractive apt-get upgrade -y"

Write-Host "  -> .NET 10 installieren..." -ForegroundColor Gray
Invoke-Ssh "wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh && chmod +x /tmp/dotnet-install.sh && sudo /tmp/dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet && sudo ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet && dotnet --info"

Write-Host "  -> MySQL installieren..." -ForegroundColor Gray
Invoke-Ssh "sudo DEBIAN_FRONTEND=noninteractive apt-get install -y mysql-server && sudo systemctl enable mysql && sudo systemctl start mysql"

Write-Host "  -> Datenbank erstellen..." -ForegroundColor Gray
Invoke-Ssh "sudo mysql -e `"CREATE DATABASE IF NOT EXISTS projektsoftware CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;`""
Invoke-Ssh "sudo mysql -e `"CREATE USER IF NOT EXISTS 'projektsoftware_app'@'localhost' IDENTIFIED BY '$DbPassword';`""
Invoke-Ssh "sudo mysql -e `"GRANT ALL PRIVILEGES ON projektsoftware.* TO 'projektsoftware_app'@'localhost';`""
Invoke-Ssh "sudo mysql -e `"FLUSH PRIVILEGES;`""

Write-Host "  -> Nginx installieren..." -ForegroundColor Gray
Invoke-Ssh "sudo DEBIAN_FRONTEND=noninteractive apt-get install -y nginx && sudo systemctl enable nginx"

Write-Host "  -> Nginx konfigurieren..." -ForegroundColor Gray
Invoke-Ssh "printf 'server {\n    listen 80;\n    server_name _;\n    client_max_body_size 50M;\n    location / {\n        proxy_pass http://127.0.0.1:5000;\n        proxy_http_version 1.1;\n        proxy_set_header Upgrade \$http_upgrade;\n        proxy_set_header Connection upgrade;\n        proxy_set_header Host \$host;\n        proxy_set_header X-Real-IP \$remote_addr;\n        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;\n        proxy_set_header X-Forwarded-Proto \$scheme;\n        proxy_cache_bypass \$http_upgrade;\n        proxy_read_timeout 86400;\n        proxy_buffering off;\n    }\n}\n' | sudo tee /etc/nginx/sites-available/projektsoftware > /dev/null"
Invoke-Ssh "sudo ln -sf /etc/nginx/sites-available/projektsoftware /etc/nginx/sites-enabled/ && sudo rm -f /etc/nginx/sites-enabled/default && sudo nginx -t && sudo systemctl restart nginx"

Write-Host "  -> App-Verzeichnis erstellen..." -ForegroundColor Gray
Invoke-Ssh "sudo mkdir -p /var/www/projektsoftware && sudo chown administrator:administrator /var/www/projektsoftware"

Write-Host "  -> Systemd-Service erstellen..." -ForegroundColor Gray
Invoke-Ssh "printf '[Unit]\nDescription=Projektsoftware API + Web\nAfter=network.target mysql.service\n\n[Service]\nWorkingDirectory=/var/www/projektsoftware\nExecStart=/usr/bin/dotnet /var/www/projektsoftware/Projektsoftware.Api.dll\nRestart=always\nRestartSec=5\nSyslogIdentifier=projektsoftware\nUser=administrator\nEnvironment=ASPNETCORE_ENVIRONMENT=Production\nEnvironment=ASPNETCORE_URLS=http://127.0.0.1:5000\nEnvironment=DOTNET_ROOT=/usr/share/dotnet\n\n[Install]\nWantedBy=multi-user.target\n' | sudo tee /etc/systemd/system/projektsoftware.service > /dev/null"
Invoke-Ssh "sudo systemctl daemon-reload && sudo systemctl enable projektsoftware"

Write-Host "  Server-Setup abgeschlossen!" -ForegroundColor Green

# === SCHRITT 2: App publishen ===
Write-Host ""
Write-Host "=== SCHRITT 2/4: App publishen ===" -ForegroundColor Cyan

dotnet publish Projektsoftware.Api/Projektsoftware.Api.csproj -c Release -o ./publish
if ($LASTEXITCODE -ne 0) { Write-Host "Publish fehlgeschlagen!" -ForegroundColor Red; exit 1 }

# === SCHRITT 3: Config + Upload ===
Write-Host ""
Write-Host "=== SCHRITT 3/4: App auf Server kopieren ===" -ForegroundColor Cyan

$configPath = "./publish/appsettings.Production.json"
$configContent = Get-Content $configPath -Raw
$configContent = $configContent -replace "HIER_SICHERES_PASSWORT_EINTRAGEN", $DbPassword
Set-Content $configPath $configContent -Encoding utf8NoBOM

scp -r ./publish/* "${Remote}:/var/www/projektsoftware/"
if ($LASTEXITCODE -ne 0) { Write-Host "Upload fehlgeschlagen!" -ForegroundColor Red; exit 1 }

$configContent = Get-Content $configPath -Raw
$configContent = $configContent -replace [regex]::Escape($DbPassword), "HIER_SICHERES_PASSWORT_EINTRAGEN"
Set-Content $configPath $configContent -Encoding utf8NoBOM

# === SCHRITT 4: App starten ===
Write-Host ""
Write-Host "=== SCHRITT 4/4: App starten ===" -ForegroundColor Cyan

Invoke-Ssh "sudo systemctl restart projektsoftware && sleep 3 && sudo systemctl status projektsoftware --no-pager"

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host "  DEPLOYMENT ABGESCHLOSSEN!" -ForegroundColor Green
Write-Host ""
Write-Host "  Web-App:  http://81.88.26.77" -ForegroundColor Green
Write-Host "  iPhone:   Safari -> http://81.88.26.77" -ForegroundColor Green
Write-Host "            -> Teilen -> Zum Home-Bildschirm" -ForegroundColor Green
Write-Host ""
Write-Host "  DB-Passwort: $DbPassword" -ForegroundColor Yellow
Write-Host "  (Sicher aufbewahren!)" -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Green
