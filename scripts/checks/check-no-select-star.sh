#!/usr/bin/env bash

# WeCMS M0-BE check-no-select-star
# Ensures no C# or SQL file contains SELECT *.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo-root)
      REPO_ROOT="${2:?missing value for --repo-root}"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      echo "Usage: $0 [--repo-root <path>]" >&2
      exit 1
      ;;
  esac
done

echo "  Checking for SELECT * in backend/src and database/..."

if rg -i -n --glob '!**/generated/**' --glob '!**/bin/**' --glob '!**/obj/**' \
    -e 'SELECT\s+\*' "$REPO_ROOT/backend/src" "$REPO_ROOT/database"; then
  echo "SELECT * violations found." >&2
  exit 1
fi

echo "  No SELECT * found."
