# WeCMS Next Quality Gate
# Runs: build, test, AOT publish, frontend typecheck/build
# Usage: pwsh scripts/quality-gate.ps1

param(
    [switch]$SkipAot,
    [switch]$SkipFrontend
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "=== WeCMS Quality Gate ===" -ForegroundColor Cyan

# ---- Backend build ----
Write-Host "`n[1/5] dotnet build..." -ForegroundColor Yellow
dotnet build "$root/backend/WeCms.sln" -warnaserror
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Write-Host "  PASS" -ForegroundColor Green

# ---- Backend test ----
Write-Host "`n[2/5] dotnet test..." -ForegroundColor Yellow
dotnet test "$root/backend/WeCms.sln" --no-build
if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
Write-Host "  PASS" -ForegroundColor Green

# ---- AOT publish ----
if (-not $SkipAot) {
    Write-Host "`n[3/5] dotnet publish AOT (linux-x64)..." -ForegroundColor Yellow
    dotnet publish "$root/backend/src/WeCms.Api/WeCms.Api.csproj" `
        -c Release -r linux-x64 /p:PublishAot=true --no-restore
    if ($LASTEXITCODE -ne 0) { throw "AOT publish failed" }
    Write-Host "  PASS" -ForegroundColor Green
} else {
    Write-Host "`n[3/5] AOT publish SKIPPED" -ForegroundColor DarkGray
}

# ---- Frontend typecheck ----
if (-not $SkipFrontend) {
    $feDir = "$root/frontend/soybean-admin"
    if (Test-Path "$feDir/package.json") {
        Write-Host "`n[4/5] pnpm typecheck..." -ForegroundColor Yellow
        pnpm --dir $feDir typecheck
        if ($LASTEXITCODE -ne 0) { throw "Typecheck failed" }
        Write-Host "  PASS" -ForegroundColor Green

        Write-Host "`n[5/5] pnpm build..." -ForegroundColor Yellow
        pnpm --dir $feDir build
        if ($LASTEXITCODE -ne 0) { throw "Build failed" }
        Write-Host "  PASS" -ForegroundColor Green
    } else {
        Write-Host "`n[4/5] Frontend SKIPPED (no package.json)" -ForegroundColor DarkGray
        Write-Host "[5/5] Frontend SKIPPED" -ForegroundColor DarkGray
    }
} else {
    Write-Host "`n[4/5] Frontend typecheck SKIPPED" -ForegroundColor DarkGray
    Write-Host "[5/5] Frontend build SKIPPED" -ForegroundColor DarkGray
}

Write-Host "`n=== Quality Gate PASSED ===" -ForegroundColor Green