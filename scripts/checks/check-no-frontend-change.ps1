# WeCMS M0-BE check-no-frontend-change
# Ensures no files under frontend/ were modified during M0-BE.
# This check verifies that the M0-BE backend-only constraint is respected.
#
# In CI: fails if frontend/ has any changes vs HEAD.
# Locally: warns if frontend/ has uncommitted changes.

$ErrorActionPreference = "Stop"
$RepoRoot = (Get-Item "$PSScriptRoot/../..").FullName

Write-Host "  Checking frontend/ for M0-BE violations..."

Push-Location "$RepoRoot"
try {
    $frontendChanges = git status --porcelain frontend/ 2>$null

    if ($frontendChanges) {
        Write-Error "M0-BE VIOLATION: frontend/ has changes. M0-BE must not modify frontend/*."
        Write-Host "  Changed files:"
        Write-Host $frontendChanges
        Write-Host "  If these changes are intentional, they belong in M0.5-FE, not M0-BE."
        exit 1
    }

    Write-Host "  No frontend/ changes detected."
}
catch {
    Write-Error "Cannot check frontend/ changes. Ensure git is available."
    exit 1
}
finally { Pop-Location }
