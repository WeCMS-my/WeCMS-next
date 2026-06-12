#!/usr/bin/env bash

# WeCMS M0-BE Seed Dev DB
# Seed scripts are applied by DbMigrationRunner after schema migrations.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

echo "=== WeCMS Seed Dev DB ==="

dotnet run --project "${REPO_ROOT}/backend/src/WeCms.Api/WeCms.Api.csproj" -- --migrate-database

echo "=== Seed Dev DB Complete ==="
