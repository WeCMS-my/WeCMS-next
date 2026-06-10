#!/usr/bin/env bash

# WeCMS M0-BE check-endpoint-permissions
# Ensures all business endpoints (MapGet/MapPost/MapPut/MapDelete) that
# require authorization also have PermissionMetadata.
#
# Checks:
#   - Endpoints with .RequireAuthorization() should have .RequirePermission()
#     OR be in the exempt list (e.g., /auth/logout, /auth/me).
#   - M0-BE known anonymous endpoints (health, system/ping, etc.) are excluded.

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

echo "  Checking endpoint permission metadata..."

exempt_auth_endpoints=("Auth_Logout" "Auth_Me")

violations=()

while IFS=0 read -r -d '' file; do
  # Determine whether endpoint is in known anonymous/exempt flow by file content
  is_exempt=false
  for ex in "${exempt_auth_endpoints[@]}"; do
    if grep -q "$ex" "$file"; then
      is_exempt=true
      break
    fi
  done

  mapfile -t lines < "$file"
  for idx in "${!lines[@]}"; do
    line="${lines[idx]}"
    if [[ "$line" == *".RequireAuthorization()" ]]; then
      start=$((idx + 1))
      end=$((idx + 4))
      block=$(sed -n "${start},${end}p" "$file")
      if [[ "$block" == *"PermissionMetadata"* || "$block" == *"RequirePermission"* ]]; then
        continue
      fi
      if [ "$is_exempt" = false ]; then
        violations+=("${file##*/}:$((idx + 1)) — RequireAuthorization without PermissionMetadata")
      fi
    fi
  done
done < <(find "$REPO_ROOT/backend/src" -type f -name '*Endpoints.cs' -print0)

if [ "${#violations[@]}" -ne 0 ]; then
  echo "Endpoints with RequireAuthorization but no PermissionMetadata found:"
  printf '  %s\n' "${violations[@]}" >&2
  echo "  Add .RequirePermission(...) or add to exempt list." >&2
  exit 1
fi

echo "  All authenticated endpoints have permission metadata or are exempt."
