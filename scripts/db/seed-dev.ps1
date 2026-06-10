# WeCMS M0-BE Seed Dev DB (manual via MySQL CLI)
# Applies seed data from database/seeds/ in order.

$ErrorActionPreference = "Stop"

Write-Host "=== WeCMS Seed Dev DB ==="

$seedsDir = Join-Path $PSScriptRoot "..\..\database\seeds"

if (-not (Test-Path $seedsDir)) {
    Write-Error "Seeds directory not found: $seedsDir"
    exit 1
}

$files = Get-ChildItem -Path $seedsDir -Filter "*.sql" | Sort-Object Name

foreach ($file in $files) {
    Write-Host "Applying seed: $($file.Name)"
    Get-Content $file.FullName | docker compose exec -T mysql mysql -u root -pwecms-root-123 wecms_dev
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Seed failed: $($file.Name)"
        exit 1
    }
    Write-Host "  OK"
}

Write-Host "=== Seed Dev DB Complete ==="
