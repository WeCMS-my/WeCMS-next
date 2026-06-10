# WeCMS M0-BE check-json-context-coverage
# Ensures all DTO types used by endpoints are registered in WeCmsJsonContext.cs.
#
# This is a heuristic check: scans endpoint files for ApiResult<T> return types
# and endpoint handler parameters, then verifies discovered types appear in WeCmsJsonContext.cs.

$ErrorActionPreference = "Stop"
$RepoRoot = (Get-Item "$PSScriptRoot/../..").FullName

Write-Host "  Checking JsonSerializerContext coverage..."

$contextFile = "$RepoRoot/backend/src/WeCms.Api/Json/WeCmsJsonContext.cs"
if (-not (Test-Path $contextFile)) {
    Write-Error "WeCmsJsonContext.cs not found."
    exit 1
}

$contextContent = Get-Content $contextFile -Raw

# Extract response DTO types from [JsonSerializable(typeof(ApiResult<T>))]
$registeredApiResultTypes = [regex]::Matches($contextContent, 'typeof\(ApiResult<([^>]+)>\)') `
    | ForEach-Object { $_.Groups[1].Value }

# Extract directly registered DTO/request types from [JsonSerializable(typeof(T))]
$registeredDirectTypes = [regex]::Matches($contextContent, 'typeof\((?!ApiResult<)([^\)]+)\)') `
    | ForEach-Object { $_.Groups[1].Value }

# Scan endpoint files for response DTO usage in ApiResult<T> and request DTO in handler parameters
$endpointFiles = Get-ChildItem -Path "$RepoRoot/backend/src" -Recurse -Include "*.cs" `
    | Where-Object { $_.Name -match "Endpoints\.cs$|Dtos\.cs$" }

$missingTypes = @()
foreach ($file in $endpointFiles) {
    $fileContent = Get-Content $file.FullName -Raw

    # Response DTOs from ApiResult<T>
    $usedApiResultTypes = [regex]::Matches($fileContent, 'ApiResult<([^>]+)>') `
        | ForEach-Object { $_.Groups[1].Value } `
        | Where-Object { $_ -notmatch '^T>$' -and $_ -notmatch '^object\?' }

    # Request DTOs from endpoint method parameters (e.g. LoginRequest request)
    $requestTypes = [regex]::Matches($fileContent, '([A-Za-z_][A-Za-z0-9_]*Request)\s+\w+\s*(?:,|\))') `
        | ForEach-Object { $_.Groups[1].Value }

    foreach ($type in $usedApiResultTypes) {
        $cleanType = $type.Trim().TrimEnd('?')
        if ($cleanType -notin $registeredApiResultTypes) {
            $missingTypes += "$cleanType (response in $($file.Name))"
        }
    }

    foreach ($type in $requestTypes) {
        $cleanType = $type.Trim().TrimEnd('?')
        if ($cleanType -notin $registeredDirectTypes) {
            $missingTypes += "$cleanType (request in $($file.Name))"
        }
    }
}

if ($missingTypes.Count -gt 0) {
    $unique = $missingTypes | Select-Object -Unique
    Write-Error "Endpoint types used but not registered in WeCmsJsonContext:"
    foreach ($t in $unique) {
        Write-Host "  $t"
    }
    exit 1
}

Write-Host "  All endpoint request/response DTOs are covered in WeCmsJsonContext."
