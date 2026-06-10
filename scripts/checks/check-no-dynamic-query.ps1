# WeCMS M0-BE check-no-dynamic-query
# Ensures no C# file uses Query<dynamic> or QueryAsync<dynamic>.

$ErrorActionPreference = "Stop"
$RepoRoot = (Get-Item "$PSScriptRoot/../..").FullName

Write-Host "  Checking for Query<dynamic> in backend/src/..."

$violations = Get-ChildItem -Path "$RepoRoot/backend/src" -Recurse -Include "*.cs" `
    | Select-String -Pattern 'Query<dynamic>|QueryAsync<dynamic>' -SimpleMatch -AllMatches `
    -Exclude "*generated*","*bin*","*obj*"

if ($violations.Count -gt 0) {
    Write-Error "Query<dynamic> violations found:"
    foreach ($v in $violations) {
        Write-Host "  $($v.Path):$($v.LineNumber)"
    }
    exit 1
}

Write-Host "  No Query<dynamic> found."
