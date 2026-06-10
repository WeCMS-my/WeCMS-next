# WeCMS M0-BE Apply Migrations (manual via MySQL CLI)
# Applies all SQL migration files from database/migrations/ in order.

$ErrorActionPreference = "Stop"

Write-Host "=== WeCMS Apply Migrations ==="

$migrationsDir = Join-Path $PSScriptRoot "..\..\database\migrations"

if (-not (Test-Path $migrationsDir)) {
    Write-Error "Migrations directory not found: $migrationsDir"
    exit 1
}

$files = Get-ChildItem -Path $migrationsDir -Filter "*.sql" | Sort-Object Name

foreach ($file in $files) {
    Write-Host "Applying: $($file.Name)"
    Get-Content $file.FullName | docker compose exec -T mysql mysql -u root -pwecms-root-123 wecms_dev
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Migration failed: $($file.Name)"
        exit 1
    }
    Write-Host "  OK"
}

Write-Host "=== Apply Migrations Complete ==="
