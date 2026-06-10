# WeCMS M0-BE Backend-only Quality Gate
# Validates: build, test, AOT publish, OpenAPI export, code quality checks.
# M0-BE constraint: does NOT run pnpm or touch frontend/.
param(
    [switch]$SkipAot = $false,
    [switch]$SkipOpenApi = $false
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Get-Item "$PSScriptRoot/..").FullName

Write-Host "=== WeCMS M0-BE Backend Quality Gate ==="

# ── 1. Build ──
Write-Host "[1/7] dotnet build -warnaserror"
Push-Location "$RepoRoot"
try {
    dotnet build backend/WeCms.slnx -warnaserror --nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    Write-Host "  PASSED"
}
finally { Pop-Location }

# ── 2. Tests ──
Write-Host "[2/7] dotnet test"
Push-Location "$RepoRoot"
try {
    dotnet test backend/WeCms.slnx --nologo --verbosity normal
    if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
    Write-Host "  PASSED"
}
finally { Pop-Location }

# ── 3. Native AOT Publish ──
if (-not $SkipAot) {
    Write-Host "[3/7] dotnet publish (Native AOT)"
    Push-Location "$RepoRoot"
    try {
        dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj `
            -c Release -r linux-x64 /p:PublishAot=true --nologo
        if ($LASTEXITCODE -ne 0) { throw "AOT publish failed" }
        Write-Host "  PASSED"
    }
    finally { Pop-Location }
}
else {
    Write-Host "[3/7] dotnet publish (Native AOT) — SKIPPED (--SkipAot)"
}

# ── 4. OpenAPI Export ──
if (-not $SkipOpenApi) {
    Write-Host "[4/7] OpenAPI export"
    Push-Location "$RepoRoot"
    try {
        dotnet run --project backend/src/WeCms.Api `
            -- --export-openapi artifacts/openapi/wecms-api-v1.json --nologo
        if ($LASTEXITCODE -ne 0) { throw "OpenAPI export failed" }
        Write-Host "  PASSED"
    }
    finally { Pop-Location }
}
else {
    Write-Host "[4/7] OpenAPI export — SKIPPED (--SkipOpenApi)"
}

# ── 5. SQL: no SELECT * ──
Write-Host "[5/7] check-no-select-star"
Push-Location "$RepoRoot"
try {
    & "$PSScriptRoot/checks/check-no-select-star.ps1"
    Write-Host "  PASSED"
}
finally { Pop-Location }

# ── 6. SQL: no Query<dynamic> ──
Write-Host "[6/7] check-no-dynamic-query"
Push-Location "$RepoRoot"
try {
    & "$PSScriptRoot/checks/check-no-dynamic-query.ps1"
    Write-Host "  PASSED"
}
finally { Pop-Location }

# ── 7. Integrity ──
Write-Host "[7/7] check integrity"
Push-Location "$RepoRoot"
try {
    & "$PSScriptRoot/checks/check-endpoint-permissions.ps1"
    & "$PSScriptRoot/checks/check-json-context-coverage.ps1"
    & "$PSScriptRoot/checks/check-no-frontend-change.ps1"
    Write-Host "  PASSED"
}
finally { Pop-Location }

Write-Host "=== WeCMS M0-BE Backend Quality Gate PASSED ==="
