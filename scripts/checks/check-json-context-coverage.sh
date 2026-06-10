#!/usr/bin/env bash

# WeCMS M0-BE check-json-context-coverage
# Ensures DTO types used by endpoints are registered in at least one JsonContext.
# Scans all *JsonContext.cs files and all *Endpoints.cs files.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

echo "  Checking JsonSerializerContext coverage..."

TMPDIR=$(mktemp -d)
trap 'rm -rf "$TMPDIR"' EXIT

API_RESULT_REG="$TMPDIR/apiresult.txt"
DIRECT_REG="$TMPDIR/direct.txt"
MISSING="$TMPDIR/missing.txt"
: > "$MISSING"

# ── Extract all registered types from ALL JsonContext files ──
ctx_files=$(find "$REPO_ROOT/backend/src" -type f -name '*JsonContext.cs' -print0)
if [ -z "$ctx_files" ]; then
  echo "No JsonContext files found in backend/src/" >&2
  exit 1
fi

while IFS= read -r -d '' ctx_file; do
  # ApiResult<T> registrations
  rg --pcre2 -o 'typeof\(ApiResult<([^>]+)>\)' "$ctx_file" \
    | sed -E 's/.*typeof\(ApiResult<([^>]+)>\).*/\1/' \
    | tr -d ' \t\r' \
    | sort -u >> "$API_RESULT_REG" || true

  # Direct type registrations (non-ApiResult): typeof(TypeName)
  rg --pcre2 -o 'typeof\(([^)]+)\)' "$ctx_file" \
    | sed -E 's/.*typeof\((.*)\).*/\1/' \
    | tr -d ' \t\r' \
    | grep -v '^ApiResult<' \
    | sort -u >> "$DIRECT_REG" || true
done < <(find "$REPO_ROOT/backend/src" -type f -name '*JsonContext.cs' -print0)

sort -u -o "$API_RESULT_REG" "$API_RESULT_REG"
sort -u -o "$DIRECT_REG" "$DIRECT_REG"

# ── Scan all Endpoints.cs files for used DTO types ──
while IFS= read -r -d '' endpoint_file; do
  # 1) Response types: ApiResult<T> in code (e.g., ApiResult<LoginResponse>.Ok(...))
  used_apiresult=$(
    rg -o 'ApiResult<[^>]+>' "$endpoint_file" \
      | sed -E 's/ApiResult<([^>]+)>/\1/' \
      | tr -d ' \t\r' \
      | grep -v '^object\?$' \
      | sort -u || true
  )

  # 2) Request types: ParseRequestAsync<ConcreteType>(...) calls (require ≥2 chars to exclude generic T)
  used_requests=$(
    rg --pcre2 -o 'ParseRequestAsync<([A-Z][A-Za-z0-9_]+)>' "$endpoint_file" \
      | sed -E 's/ParseRequestAsync<([^>]+)>/\1/' \
      | tr -d ' \t\r' \
      | sort -u || true
  )

  for t in $used_apiresult; do
    if ! grep -Fxq "$t" "$API_RESULT_REG"; then
      echo "$t (response in $(basename "$endpoint_file"))" >> "$MISSING"
    fi
  done

  for t in $used_requests; do
    if ! grep -Fxq "$t" "$DIRECT_REG"; then
      echo "$t (request in $(basename "$endpoint_file"))" >> "$MISSING"
    fi
  done

done < <(find "$REPO_ROOT/backend/src" -type f -name '*Endpoints.cs' -print0)

if [ -s "$MISSING" ]; then
  echo "Endpoint types used but not registered in any JsonContext:" >&2
  sort -u "$MISSING" >&2
  exit 1
fi

echo "  All endpoint request/response DTOs are covered in JsonContext."
