<#
.SYNOPSIS
    Hilfsskript fuer Code-Signing-Einrichtung.
    Sucht installierte Code-Signing-Zertifikate und traegt den Thumbprint
    automatisch in publish.config.json ein.

.EXAMPLE
    .\build\setup-codesigning.ps1
    .\build\setup-codesigning.ps1 -Thumbprint "ABC123..."
#>
param(
    [string]$Thumbprint = ""
)

$ErrorActionPreference = "Stop"

if (-not $PSScriptRoot) {
    $PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
}

$cfgFile = "$PSScriptRoot\publish.config.json"
$cfg = Get-Content $cfgFile -Raw | ConvertFrom-Json

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Code-Signing Einrichtung" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# ── Verfuegbare Code-Signing-Zertifikate anzeigen ───────────────────────────
Write-Host "Suche Code-Signing-Zertifikate im persoenlichen Zertifikatsspeicher ..." -ForegroundColor Yellow

$certs = Get-ChildItem Cert:\CurrentUser\My | Where-Object {
    $_.EnhancedKeyUsageList | Where-Object { $_.ObjectId -eq "1.3.6.1.5.5.7.3.3" }
}

if ($certs.Count -eq 0) {
    Write-Host "`n  Kein Code-Signing-Zertifikat gefunden." -ForegroundColor Red
    Write-Host "`n  Bitte ein OV- oder EV-Code-Signing-Zertifikat kaufen und installieren:" -ForegroundColor Yellow
    Write-Host "  Empfehlung: Sectigo (guenstigster OV-Anbieter)" -ForegroundColor White
    Write-Host "  URL:        https://sectigo.com/code-signing-certificates" -ForegroundColor Cyan
    Write-Host "  Preis:      ca. 90-120 EUR/Jahr (OV) | ca. 220-300 EUR/Jahr (EV)" -ForegroundColor White
    Write-Host "`n  Nach der Installation dieses Skript erneut ausfuehren." -ForegroundColor DarkGray
    Write-Host "`n  HINWEIS: EV-Zertifikat hebt SmartScreen-Warnung sofort auf." -ForegroundColor Green
    Write-Host "           OV-Zertifikat baut Reputation ueber mehrere Downloads auf." -ForegroundColor DarkGray
    exit 0
}

Write-Host "`n  Gefundene Code-Signing-Zertifikate:" -ForegroundColor Green
$i = 1
foreach ($cert in $certs) {
    $expiry  = $cert.NotAfter.ToString("dd.MM.yyyy")
    $issuer  = ($cert.Issuer -split ',')[0] -replace 'CN=',''
    $subject = ($cert.Subject -split ',')[0] -replace 'CN=',''
    $valid   = if ($cert.NotAfter -gt (Get-Date)) { "✅ Gueltig" } else { "❌ Abgelaufen" }
    Write-Host "  [$i] $subject" -ForegroundColor White
    Write-Host "      Aussteller : $issuer" -ForegroundColor DarkGray
    Write-Host "      Thumbprint : $($cert.Thumbprint)" -ForegroundColor DarkGray
    Write-Host "      Ablauf     : $expiry  $valid" -ForegroundColor DarkGray
    Write-Host ""
    $i++
}

# ── Thumbprint auswaehlen ────────────────────────────────────────────────────
if (-not $Thumbprint) {
    if ($certs.Count -eq 1) {
        $selected = $certs[0]
        Write-Host "  Nehme einziges Zertifikat: $($selected.Subject)" -ForegroundColor DarkGray
    } else {
        $idx = Read-Host "  Welches Zertifikat verwenden? [1-$($certs.Count)]"
        $selected = $certs[[int]$idx - 1]
    }
    $Thumbprint = $selected.Thumbprint
}

# Thumbprint bereinigen (certutil fuegt manchmal Leerzeichen ein)
$Thumbprint = $Thumbprint -replace '\s',''

# ── publish.config.json aktualisieren ───────────────────────────────────────
Write-Host "`n[1/2] Trage Thumbprint in publish.config.json ein ..." -ForegroundColor Yellow

# Timestamp-URL je nach Aussteller vorschlagen
$bestTimestamp = "http://timestamp.sectigo.com"
if ($selected) {
    $issuerStr = $selected.Issuer.ToLower()
    $bestTimestamp = switch -Wildcard ($issuerStr) {
        "*digicert*"   { "http://timestamp.digicert.com" }
        "*globalsign*" { "http://timestamp.globalsign.com/scripts/timstamp.dll" }
        "*comodo*"     { "http://timestamp.sectigo.com" }
        "*sectigo*"    { "http://timestamp.sectigo.com" }
        default        { "http://timestamp.sectigo.com" }
    }
}

$cfg.SigningCertThumbprint = $Thumbprint
$cfg.SigningTimestampUrl   = $bestTimestamp

$cfg | ConvertTo-Json -Depth 3 | Set-Content $cfgFile -Encoding UTF8
Write-Host "    OK  (Thumbprint: $Thumbprint)" -ForegroundColor Green
Write-Host "    Timestamp-URL: $bestTimestamp" -ForegroundColor DarkGray

# ── Signing testen ───────────────────────────────────────────────────────────
Write-Host "`n[2/2] Signing-Test mit Dummy-Datei ..." -ForegroundColor Yellow

$signtool = Get-Command "signtool.exe" -ErrorAction SilentlyContinue
if (-not $signtool) {
    $signtool = Get-Item "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" `
        -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
    if ($signtool) { $signtool = $signtool.FullName }
}

if (-not $signtool) {
    Write-Host "    WARNUNG: signtool.exe nicht gefunden." -ForegroundColor DarkYellow
    Write-Host "    Bitte Windows SDK installieren:" -ForegroundColor DarkYellow
    Write-Host "    https://developer.microsoft.com/windows/downloads/windows-sdk/" -ForegroundColor Cyan
} else {
    # Test-EXE kopieren (cmd.exe als harmlose Test-Datei)
    $testExe = "$env:TEMP\sign_test_$([System.IO.Path]::GetRandomFileName()).exe"
    Copy-Item "$env:SystemRoot\System32\cmd.exe" $testExe

    try {
        & $signtool sign `
            /sha1 $Thumbprint `
            /fd sha256 `
            /td sha256 `
            /tr $bestTimestamp `
            /d "Test" `
            $testExe 2>&1

        if ($LASTEXITCODE -eq 0) {
            Write-Host "    OK  – Signing funktioniert!" -ForegroundColor Green
        } else {
            Write-Host "    FEHLER beim Signing-Test (Exit: $LASTEXITCODE)" -ForegroundColor Red
            Write-Host "    Pruefen: Ist das Zertifikat im persoenlichen Speicher? Ist der private Schluessel vorhanden?" -ForegroundColor Yellow
        }
    } finally {
        Remove-Item $testExe -ErrorAction SilentlyContinue
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Einrichtung abgeschlossen!" -ForegroundColor Cyan
Write-Host "  Naechster Release: .\build\publish.ps1" -ForegroundColor White
Write-Host "  -> EXE + Installer werden automatisch signiert." -ForegroundColor White
Write-Host "========================================`n" -ForegroundColor Cyan
