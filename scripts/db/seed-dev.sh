#!/usr/bin/env bash

# WeCMS M0-BE Seed Dev DB (manual via MySQL CLI)
# Applies seed data from database/seeds/ in order.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SEEDS_DIR="${REPO_ROOT}/database/seeds"

echo "=== WeCMS Seed Dev DB ==="

if [ ! -d "$SEEDS_DIR" ]; then
  echo "Seeds directory not found: $SEEDS_DIR" >&2
  exit 1
fi

shopt -s nullglob
files=("${SEEDS_DIR}"/*.sql)
mapfile -t files < <(printf '%s\n' "${files[@]}" | sort)
shopt -u nullglob

for file in "${files[@]}"; do
  echo "Applying seed: $(basename "$file")"
  docker compose exec -T mysql mysql -u root -pwecms-root-123 wecms_dev < "$file"
  echo "  OK"
done

echo "=== Seed Dev DB Complete ==="
