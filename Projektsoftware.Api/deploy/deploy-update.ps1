# Schnelles Update-Deploy für Projektsoftware.Api
# Nur publishen + uploaden + neu starten (ohne Server-Setup)

$ServerIp = "81.88.26.77"
$User = "administrator"
$Remote = "$User@$ServerIp"

Write-Host ""
Write-Host "=== Projektsoftware - Quick Update Deploy ===" -ForegroundColor Cyan
Write-Host ""

# 1. App publishen
Write-Host "=== SCHRITT 1/3: App publishen ===" -ForegroundColor Cyan
dotnet publish "E:\Projektsoftware\Projektsoftware.Api\Projektsoftware.Api.csproj" -c Release -o "E:\Projektsoftware\publish\api-update"
if ($LASTEXITCODE -ne 0) { 
    Write-Host "Publish fehlgeschlagen!" -ForegroundColor Red
    exit 1 
}
Write-Host "✓ Publish erfolgreich" -ForegroundColor Green

# 2. Auf Server kopieren
Write-Host ""
Write-Host "=== SCHRITT 2/3: Auf Server hochladen ===" -ForegroundColor Cyan

# Sichere alte appsettings.Production.json auf Server
ssh $Remote "cp /var/www/projektsoftware/appsettings.Production.json /tmp/appsettings.Production.backup.json 2>/dev/null || true"

# Upload
scp -r "E:\Projektsoftware\publish\api-update\*" "${Remote}:/var/www/projektsoftware/"
if ($LASTEXITCODE -ne 0) { 
    Write-Host "Upload fehlgeschlagen!" -ForegroundColor Red
    exit 1 
}

# Stelle alte appsettings.Production.json wieder her (mit echten Passwörtern)
ssh $Remote "cp /tmp/appsettings.Production.backup.json /var/www/projektsoftware/appsettings.Production.json 2>/dev/null || true"

Write-Host "✓ Upload erfolgreich" -ForegroundColor Green

# 3. App neu starten
Write-Host ""
Write-Host "=== SCHRITT 3/3: App neu starten ===" -ForegroundColor Cyan
ssh $Remote "sudo systemctl restart projektsoftware && sleep 3 && sudo systemctl status projektsoftware --no-pager"
Write-Host "✓ App neu gestartet" -ForegroundColor Green

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host "  UPDATE ABGESCHLOSSEN!" -ForegroundColor Green
Write-Host "  Web-App: http://app.af-software-engineering.de" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
