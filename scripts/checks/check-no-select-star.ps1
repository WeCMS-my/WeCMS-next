# WeCMS M0-BE check-no-select-star
# Ensures no C# or SQL file contains SELECT *.

$ErrorActionPreference = "Stop"
$RepoRoot = (Get-Item "$PSScriptRoot/../..").FullName

Write-Host "  Checking for SELECT * in backend/src and database/..."

$violations = @(
    Get-ChildItem -Path "$RepoRoot/backend/src" -Recurse -Include "*.cs","*.sql" `
        | Select-String -Pattern 'SELECT\s+\*' -AllMatches `
        -Exclude "*generated*","*bin*","*obj*"
    Get-ChildItem -Path "$RepoRoot/database" -Recurse -Include "*.sql" `
        | Select-String -Pattern 'SELECT\s+\*' -SimpleMatch -AllMatches
) | Where-Object { $_ -ne $null }

if ($violations.Count -gt 0) {
    Write-Error "SELECT * violations found:"
    foreach ($v in $violations) {
        Write-Host "  $($v.Path):$($v.LineNumber)"
    }
    exit 1
}

Write-Host "  No SELECT * found."
