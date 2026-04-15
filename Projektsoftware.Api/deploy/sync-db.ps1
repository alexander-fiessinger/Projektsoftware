# ---------------------------------------------------------------
# DB-Sync: Lokale MySQL → Server MySQL
# Synchronisiert die Desktop-DB mit der Web-App-DB
#
# Ausführen: .\Projektsoftware.Api\deploy\sync-db.ps1
# Automatisch alle 5 Min: Task Scheduler oder manuell
# ---------------------------------------------------------------

param(
    [switch]$Reverse  # Server → Lokal (falls nötig)
)

$ServerIp = "81.88.26.77"
$Remote = "administrator@$ServerIp"
$DbName = "projektsoftware"
$DumpFile = "$env:TEMP\projektsoftware_sync.sql"

# Lokale MySQL-Einstellungen (XAMPP)
$LocalMysqlDump = "C:\xampp\mysql\bin\mysqldump.exe"
$LocalMysql = "C:\xampp\mysql\bin\mysql.exe"
$LocalUser = "root"
$LocalPassword = ""

# Falls nicht XAMPP, versuche Standard-MySQL
if (!(Test-Path $LocalMysqlDump)) {
    $LocalMysqlDump = "mysqldump"
    $LocalMysql = "mysql"
}

Write-Host ""
Write-Host "=== Projektsoftware DB-Sync ===" -ForegroundColor Cyan
Write-Host "  Zeitpunkt: $(Get-Date -Format 'dd.MM.yyyy HH:mm:ss')" -ForegroundColor Gray

if ($Reverse) {
    # ── Server → Lokal ──────────────────────────────────────────
    Write-Host "  Richtung: Server -> Lokal" -ForegroundColor Yellow
    Write-Host ""

    Write-Host "[1/3] Exportiere Server-DB..." -ForegroundColor Gray
    ssh $Remote "sudo mysqldump --single-transaction --quick $DbName" > $DumpFile 2>$null
    if ($LASTEXITCODE -ne 0) { Write-Host "Export fehlgeschlagen!" -ForegroundColor Red; exit 1 }

    $size = [math]::Round((Get-Item $DumpFile).Length / 1KB, 1)
    Write-Host "      Dump: $size KB" -ForegroundColor Gray

    Write-Host "[2/3] Importiere in lokale DB..." -ForegroundColor Gray
    if ($LocalPassword) {
        & $LocalMysql -u $LocalUser -p"$LocalPassword" $DbName -e "source $DumpFile" 2>$null
    } else {
        & $LocalMysql -u $LocalUser $DbName -e "source $DumpFile" 2>$null
    }
    if ($LASTEXITCODE -ne 0) { Write-Host "Import fehlgeschlagen!" -ForegroundColor Red; exit 1 }

    Write-Host "[3/3] Aufräumen..." -ForegroundColor Gray
    Remove-Item $DumpFile -ErrorAction SilentlyContinue

} else {
    # ── Lokal → Server ──────────────────────────────────────────
    Write-Host "  Richtung: Lokal -> Server" -ForegroundColor Yellow
    Write-Host ""

    Write-Host "[1/3] Exportiere lokale DB..." -ForegroundColor Gray
    if ($LocalPassword) {
        & $LocalMysqlDump -u $LocalUser -p"$LocalPassword" --single-transaction --quick $DbName > $DumpFile 2>$null
    } else {
        & $LocalMysqlDump -u $LocalUser --single-transaction --quick $DbName > $DumpFile 2>$null
    }
    if ($LASTEXITCODE -ne 0) { Write-Host "Export fehlgeschlagen!" -ForegroundColor Red; exit 1 }

    $size = [math]::Round((Get-Item $DumpFile).Length / 1KB, 1)
    Write-Host "      Dump: $size KB" -ForegroundColor Gray

    Write-Host "[2/3] Kopiere auf Server und importiere..." -ForegroundColor Gray
    scp $DumpFile "${Remote}:/tmp/projektsoftware_sync.sql"
    if ($LASTEXITCODE -ne 0) { Write-Host "Upload fehlgeschlagen!" -ForegroundColor Red; exit 1 }

    ssh $Remote "sudo mysql $DbName < /tmp/projektsoftware_sync.sql && rm /tmp/projektsoftware_sync.sql"
    if ($LASTEXITCODE -ne 0) { Write-Host "Import fehlgeschlagen!" -ForegroundColor Red; exit 1 }

    Write-Host "[3/3] Aufräumen..." -ForegroundColor Gray
    Remove-Item $DumpFile -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "=== Sync abgeschlossen! ===" -ForegroundColor Green
Write-Host ""
