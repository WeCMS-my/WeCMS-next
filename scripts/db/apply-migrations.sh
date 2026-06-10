#!/usr/bin/env bash

# WeCMS M0-BE Apply Migrations (manual via MySQL CLI)
# Applies all SQL migration files from database/migrations/ in order.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
MIGRATIONS_DIR="${REPO_ROOT}/database/migrations"

echo "=== WeCMS Apply Migrations ==="

if [ ! -d "$MIGRATIONS_DIR" ]; then
  echo "Migrations directory not found: $MIGRATIONS_DIR" >&2
  exit 1
fi

shopt -s nullglob
files=("${MIGRATIONS_DIR}"/*.sql)
mapfile -t files < <(printf '%s\n' "${files[@]}" | sort)
shopt -u nullglob

for file in "${files[@]}"; do
  echo "Applying: $(basename "$file")"
  docker compose exec -T mysql mysql -u root -pwecms-root-123 wecms_dev < "$file"
  echo "  OK"
done

echo "=== Apply Migrations Complete ==="
