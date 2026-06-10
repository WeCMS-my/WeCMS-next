# WeCMS M0-BE check-endpoint-permissions
# Ensures all business endpoints (MapGet/MapPost/MapPut/MapDelete) that
# require authorization also have PermissionMetadata.
#
# Checks:
#   - Endpoints with .RequireAuthorization() should have .RequirePermission()
#     OR be in the exempt list (e.g., /auth/logout, /auth/me).
#   - M0-BE known anonymous endpoints (health, system/ping, etc.) are excluded.

$ErrorActionPreference = "Stop"
$RepoRoot = (Get-Item "$PSScriptRoot/../..").FullName

Write-Host "  Checking endpoint permission metadata..."

# M0-BE exempt endpoints: require auth but no specific permission code needed
$exemptAuthEndpoints = @(
    "Auth_Logout",
    "Auth_Me"
)

# M0-BE known anonymous endpoints: explicitly AllowAnonymous
$anonymousEndpoints = @(
    "HealthLive",
    "HealthReady",
    "Ping",
    "Version",
    "DbCheck",
    "Login",
    "Refresh"
)

$violations = @()

# Scan C# endpoint files for RequireAuthorization without RequirePermission
$endpointFiles = Get-ChildItem -Path "$RepoRoot/backend/src" -Recurse -Include "*.cs" `
    | Where-Object { $_.Name -match "Endpoints\.cs$" }

foreach ($file in $endpointFiles) {
    $content = Get-Content $file.FullName -Raw
    $lines = Get-Content $file.FullName

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Check for RequireAuthorization
        if ($line -match '\.RequireAuthorization\(\)') {
            # Check if next line has RequirePermission or WithMetadata(PermissionMetadata)
            $nextLines = ($lines[$i..([Math]::Min($i+3, $lines.Count-1))] -join "`n")
            $hasPermission = $nextLines -match 'PermissionMetadata|RequirePermission'

            if (-not $hasPermission) {
                # Check if this endpoint is exempt
                $isExempt = $false
                foreach ($ex in $exemptAuthEndpoints) {
                    if ($content -match $ex) { $isExempt = $true; break }
                }
                if (-not $isExempt) {
                    $violations += "$($file.Name):$($i+1) — RequireAuthorization without PermissionMetadata"
                }
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Error "Endpoints with RequireAuthorization but no PermissionMetadata found:"
    foreach ($v in $violations) {
        Write-Host "  $v"
    }
    Write-Host "  Add .RequirePermission(...) or add to exempt list."
    exit 1
}

Write-Host "  All authenticated endpoints have permission metadata or are exempt."
