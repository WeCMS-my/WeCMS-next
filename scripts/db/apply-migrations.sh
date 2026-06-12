#!/usr/bin/env bash

# WeCMS M0-BE Apply Migrations
# Routes database initialization through DbMigrationRunner.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

echo "=== WeCMS Apply Migrations ==="

dotnet run --project "${REPO_ROOT}/backend/src/WeCms.Api/WeCms.Api.csproj" -- --migrate-database

echo "=== Apply Migrations Complete ==="
