<#
.SYNOPSIS
    Lokales Release-Skript fuer Projektsoftware
.EXAMPLE
    .\build\publish.ps1
    .\build\publish.ps1 -Minor
    .\build\publish.ps1 -Version "1.2.5" -Notes "- Feature X"

    Ausfuehren (falls Execution Policy blockiert):
    powershell -ExecutionPolicy Bypass -File .\build\publish.ps1
#>
param(
    [string]$Version  = "",
    [string]$Notes    = "",
    [switch]$Minor,
    [switch]$Major
)

$ErrorActionPreference = "Stop"

# PSScriptRoot-Fallback fuer verschiedene Ausfuehrungs-Kontexte
if (-not $PSScriptRoot) {
    $PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
}
$root    = Split-Path $PSScriptRoot -Parent
$cfgFile = "$PSScriptRoot\publish.config.json"

# ── Konfiguration laden ────────────────────────────────────────────────────────
if (-not (Test-Path $cfgFile)) {
    throw "Konfigurationsdatei fehlt: $cfgFile`nBitte publish.config.json anlegen."
}
$cfg = Get-Content $cfgFile -Raw | ConvertFrom-Json

if ($cfg.GitHubToken -eq "HIER_TOKEN_EINTRAGEN" -or -not $cfg.GitHubToken) {
    throw "GitHub-Token fehlt in publish.config.json.`nToken erstellen: https://github.com/settings/tokens/new (Scope: repo)"
}
$gitHubToken = $cfg.GitHubToken
$gitHubRepo  = $cfg.GitHubRepo

# ── Version automatisch ermitteln ─────────────────────────────────────────────
$csproj = "$root\Projektsoftware\Projektsoftware.csproj"
$currentVersion = (Select-String -Path $csproj -Pattern '<Version>(.*?)</Version>').Matches[0].Groups[1].Value

if (-not $Version) {
    $parts = $currentVersion -split '\.'
    $maj   = [int]$parts[0]
    $min   = [int]$parts[1]
    $pat   = [int]$parts[2]

    if     ($Major) { $maj++; $min = 0; $pat = 0 }
    elseif ($Minor) { $min++;           $pat = 0 }
    else            { $pat++ }

    $Version = "$maj.$min.$pat"
}

if (-not $Notes) { $Notes = "Version $Version" }

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Projektsoftware Release v$Version" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

try {

# ── 1. Version in .csproj patchen ─────────────────────────────────────────────
Write-Host "[1/6] Patche Version $Version in .csproj  (war: $currentVersion) ..." -ForegroundColor Yellow
(Get-Content $csproj) `
    -replace '<Version>.*?</Version>',                "<Version>$Version</Version>" `
    -replace '<AssemblyVersion>.*?</AssemblyVersion>', "<AssemblyVersion>$Version.0</AssemblyVersion>" `
    -replace '<FileVersion>.*?</FileVersion>',         "<FileVersion>$Version.0</FileVersion>" |
    Set-Content $csproj
Write-Host "    OK" -ForegroundColor Green

# ── 2. dotnet publish ──────────────────────────────────────────────────────────
Write-Host "[2/6] dotnet publish ..." -ForegroundColor Yellow
$publishDir = "$root\publish"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

dotnet publish "$root\Projektsoftware\Projektsoftware.csproj" `
    -c Release -r win-x64 --self-contained true -o $publishDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish fehlgeschlagen (Exit: $LASTEXITCODE)" }
Write-Host "    OK" -ForegroundColor Green

# ── 3. Inno Setup ─────────────────────────────────────────────────────────────
Write-Host "[3/6] Inno Setup ..." -ForegroundColor Yellow
$isccPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$iscc = $isccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    Write-Host "    Inno Setup nicht gefunden — installiere via winget ..." -ForegroundColor DarkYellow
    winget install JRSoftware.InnoSetup --silent --accept-package-agreements --accept-source-agreements
    Write-Host "    Warte auf Abschluss der Installation ..." -ForegroundColor DarkYellow
    $deadline = (Get-Date).AddSeconds(120)
    while (-not ($iscc = $isccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1) -and (Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 3
    }
    if (-not $iscc) { throw "Inno Setup konnte nicht installiert werden." }
    Write-Host "    Inno Setup installiert." -ForegroundColor Green
}
Write-Host "    ISCC: $iscc" -ForegroundColor DarkGray

& $iscc "/DMyAppVersion=$Version" "$PSScriptRoot\Setup.iss"
if ($LASTEXITCODE -ne 0) { throw "Inno Setup fehlgeschlagen (Exit: $LASTEXITCODE)" }

$setupExe = "$PSScriptRoot\Output\setup-$Version.exe"
if (-not (Test-Path $setupExe)) { throw "Setup-Datei nicht gefunden: $setupExe" }
Write-Host "    OK  ($setupExe)" -ForegroundColor Green

# ── 4. manifest.json erstellen ────────────────────────────────────────────────
Write-Host "[4/6] manifest.json erstellen ..." -ForegroundColor Yellow
$manifest = [ordered]@{
    version      = $Version
    downloadUrl  = "https://github.com/$gitHubRepo/releases/download/v$Version/setup-$Version.exe"
    releaseNotes = $Notes
} | ConvertTo-Json -Depth 3

$manifestFile = "$root\manifest.json"
Set-Content $manifestFile $manifest -Encoding UTF8
Write-Host $manifest
Write-Host "    OK" -ForegroundColor Green

# ── 5. manifest.json nach GitHub pushen ──────────────────────────────────────
Write-Host "[5/6] Git commit & push (manifest.json) ..." -ForegroundColor Yellow
git -C $root add manifest.json
$staged = git -C $root diff --cached --name-only
if ($staged -match 'manifest\.json') {
    git -C $root commit -m "Release v$Version [manifest]"
    if ($LASTEXITCODE -ne 0) { throw "git commit fehlgeschlagen (Exit: $LASTEXITCODE)" }
    git -C $root push
    if ($LASTEXITCODE -ne 0) { throw "git push fehlgeschlagen (Exit: $LASTEXITCODE)" }
    Write-Host "    OK  (gepusht)" -ForegroundColor Green
} else {
    Write-Host "    manifest.json unveraendert — kein Push noetig" -ForegroundColor DarkGray
}

# ── 6. GitHub Release erstellen & setup.exe hochladen ────────────────────────
Write-Host "[6/6] GitHub Release v$Version erstellen ..." -ForegroundColor Yellow
$ghHeaders = @{
    Authorization          = "Bearer $gitHubToken"
    Accept                 = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}
$releaseBody = @{
    tag_name   = "v$Version"
    name       = "v$Version"
    body       = $Notes
    draft      = $false
    prerelease = $false
} | ConvertTo-Json

$release = Invoke-RestMethod -Uri "https://api.github.com/repos/$gitHubRepo/releases" `
    -Method Post -Headers $ghHeaders -Body $releaseBody -ContentType "application/json"
Write-Host "    Release: $($release.html_url)" -ForegroundColor DarkGray

$uploadUrl = $release.upload_url -replace '\{.*\}', "?name=setup-$Version.exe"
Write-Host "    Uploading setup-$Version.exe ..."
Invoke-RestMethod -Uri $uploadUrl -Method Post -Headers $ghHeaders `
    -InFile $setupExe -ContentType "application/octet-stream" | Out-Null
Write-Host "    OK" -ForegroundColor Green

# ── Ergebnis prüfen ───────────────────────────────────────────────────────────
$rawUrl = "https://raw.githubusercontent.com/$gitHubRepo/main/manifest.json"
Write-Host "`n[Prüfe URL] Warte auf GitHub CDN ..." -ForegroundColor Yellow
Start-Sleep -Seconds 8
try {
    $resp = Invoke-WebRequest $rawUrl -UseBasicParsing -ErrorAction Stop
    Write-Host "    200 OK" -ForegroundColor Green
    Write-Host $resp.Content
} catch {
    Write-Host "    WARNUNG: Noch nicht erreichbar — ggf. kurz warten und manuell prüfen" -ForegroundColor DarkYellow
    Write-Host "    URL: $rawUrl" -ForegroundColor DarkYellow
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Release v$Version abgeschlossen!" -ForegroundColor Cyan
Write-Host "  Manifest : $rawUrl" -ForegroundColor Cyan
Write-Host "  Release  : $($release.html_url)" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

} catch {
    Write-Host "`n========================================" -ForegroundColor Red
    Write-Host "  FEHLER:" -ForegroundColor Red
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Zeile: $($_.InvocationInfo.ScriptLineNumber)" -ForegroundColor Red
    Write-Host "========================================`n" -ForegroundColor Red
} finally {
    Write-Host "Drücke Enter zum Beenden..." -ForegroundColor Gray
    Read-Host | Out-Null
}
