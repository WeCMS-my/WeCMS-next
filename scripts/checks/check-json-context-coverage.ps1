# WeCMS M0-BE check-json-context-coverage
# Ensures all ApiResult<T> types used in endpoint return values
# are registered in WeCmsJsonContext.cs with [JsonSerializable(typeof(ApiResult<T>))].
#
# This is a heuristic check: scans endpoint files for ApiResult<T> usage
# and verifies the type T appears in WeCmsJsonContext.cs.

$ErrorActionPreference = "Stop"
$RepoRoot = (Get-Item "$PSScriptRoot/../..").FullName

Write-Host "  Checking JsonSerializerContext coverage..."

$contextFile = "$RepoRoot/backend/src/WeCms.Api/Json/WeCmsJsonContext.cs"
if (-not (Test-Path $contextFile)) {
    Write-Error "WeCmsJsonContext.cs not found."
    exit 1
}

$contextContent = Get-Content $contextFile -Raw

# Extract types registered in [JsonSerializable(typeof(ApiResult<X>))]
$registeredTypes = [regex]::Matches($contextContent, 'typeof\(ApiResult<([^>]+)>\)') `
    | ForEach-Object { $_.Groups[1].Value }

# Scan endpoint files for ApiResult<T> usage in return statements
$endpointFiles = Get-ChildItem -Path "$RepoRoot/backend/src" -Recurse -Include "*.cs" `
    | Where-Object { $_.Name -match "Endpoints\.cs$|Dtos\.cs$" }

$missingTypes = @()
foreach ($file in $endpointFiles) {
    $fileContent = Get-Content $file.FullName -Raw
    $usedTypes = [regex]::Matches($fileContent, 'ApiResult<([^>]+)>') `
        | ForEach-Object { $_.Groups[1].Value } `
        | Where-Object { $_ -notmatch '^T>$' -and $_ -notmatch '^object\?' }

    foreach ($type in $usedTypes) {
        $cleanType = $type.Trim().TrimEnd('?')
        if ($cleanType -notin $registeredTypes) {
            # object? is already registered, skip
            if ($cleanType -eq "object?") { continue }
            $missingTypes += "$cleanType (from $($file.Name))"
        }
    }
}

if ($missingTypes.Count -gt 0) {
    $unique = $missingTypes | Select-Object -Unique
    Write-Error "Types used in ApiResult<T> but not registered in WeCmsJsonContext:"
    foreach ($t in $unique) {
        Write-Host "  $t"
    }
    exit 1
}

Write-Host "  All ApiResult<T> types covered in WeCmsJsonContext. (registered: $($registeredTypes -join ', '))"
