# WeCMS M0-BE Reset Dev DB
# Drops and recreates the development database.
# Requires: docker compose, mysql client

param(
    [switch]$Force = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== WeCMS Reset Dev DB ==="

if (-not $Force) {
    Write-Warning "This will DROP and recreate the wecms_dev database. All data will be lost!"
    $confirm = Read-Host "Type 'yes' to continue"
    if ($confirm -ne "yes") {
        Write-Host "Aborted."
        exit 0
    }
}

Write-Host "Ensuring MySQL container is running..."
docker compose up -d mysql

Write-Host "Waiting for MySQL to be healthy..."
$maxRetries = 30
$retry = 0
while ($retry -lt $maxRetries) {
    $healthy = docker inspect --format='{{.State.Health.Status}}' wecms-mysql 2>$null
    if ($healthy -eq "healthy") {
        Write-Host "MySQL is healthy."
        break
    }
    $retry++
    Write-Host "Waiting... ($retry/$maxRetries)"
    Start-Sleep -Seconds 2
}

if ($retry -ge $maxRetries) {
    Write-Error "MySQL failed to become healthy."
    exit 1
}

Write-Host "Dropping and recreating database..."
docker compose exec -T mysql mysql -u root -pwecms-root-123 -e "DROP DATABASE IF EXISTS wecms_dev; CREATE DATABASE wecms_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

Write-Host "=== Reset Complete ==="
