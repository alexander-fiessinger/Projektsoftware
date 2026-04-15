# ═══════════════════════════════════════════════════════════════
# Projektsoftware – Publish & Deploy auf Dogado VPS (81.88.26.77)
# Dieses Script auf dem Windows-PC ausführen
# ═══════════════════════════════════════════════════════════════

param(
    [string]$ServerIp = "81.88.26.77",
    [string]$User = "administrator"
)

Write-Host "══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Projektsoftware – Deploy auf $ServerIp" -ForegroundColor Cyan
Write-Host "══════════════════════════════════════════════════" -ForegroundColor Cyan

# 1. Publish
Write-Host "`n→ App wird veröffentlicht..." -ForegroundColor Yellow
dotnet publish Projektsoftware.Api/Projektsoftware.Api.csproj -c Release -o ./publish
if ($LASTEXITCODE -ne 0) { Write-Host "Publish fehlgeschlagen!" -ForegroundColor Red; exit 1 }

# 2. Auf Server kopieren
Write-Host "`n→ Dateien werden auf Server kopiert..." -ForegroundColor Yellow
scp -r ./publish/* "${User}@${ServerIp}:/var/www/projektsoftware/"

# 3. App neu starten
Write-Host "`n→ App wird auf Server neu gestartet..." -ForegroundColor Yellow
ssh "${User}@${ServerIp}" "sudo systemctl restart projektsoftware"

Write-Host "`n══════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅ Deploy abgeschlossen!" -ForegroundColor Green
Write-Host "  Öffne: http://${ServerIp}" -ForegroundColor Green
Write-Host "══════════════════════════════════════════════════" -ForegroundColor Green
