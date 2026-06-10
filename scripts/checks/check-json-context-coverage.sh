#!/usr/bin/env bash

# WeCMS M0-BE check-json-context-coverage
# Ensures DTO types used by endpoints are registered in WeCmsJsonContext.cs.
# This script scans endpoint return types and request DTO parameters and
# verifies corresponding [JsonSerializable] registrations exist.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

CONTEXT_FILE="$REPO_ROOT/backend/src/WeCms.Api/Json/WeCmsJsonContext.cs"

if [[ ! -f "$CONTEXT_FILE" ]]; then
  echo "WeCmsJsonContext.cs not found: $CONTEXT_FILE" >&2
  exit 1
fi

echo "  Checking JsonSerializerContext coverage (bash fallback)..."

TMPDIR=$(mktemp -d)
trap 'rm -rf "$TMPDIR"' EXIT

API_RESULT_REG="$TMPDIR/apiresult.txt"
DIRECT_REG="$TMPDIR/direct.txt"
MISSING="$TMPDIR/missing.txt"
: > "$MISSING"

rg --pcre2 -o 'typeof\(ApiResult<([^>]+)>\)' "$CONTEXT_FILE" \
  | sed -E 's/.*typeof\(ApiResult<([^>]+)>\).*/\1/' \
  | tr -d ' \t\r' \
  | sort -u > "$API_RESULT_REG" || true

rg --pcre2 -o 'typeof\(([^)]+)\)' "$CONTEXT_FILE" \
  | sed -E 's/.*typeof\((.*)\).*/\1/' \
  | tr -d ' \t\r' \
  | grep -v '^ApiResult<' \
  | sort -u > "$DIRECT_REG" || true

while IFS= read -r -d '' endpoint_file; do
  used_apiresult=$(
    rg -o 'ApiResult<[^>]+>' "$endpoint_file" \
      | sed -E 's/ApiResult<([^>]+)>/\1/' \
      | tr -d ' \t\r' \
      | grep -v '^object\?$' \
      | sort -u || true
  )

  used_requests=$(
    rg --pcre2 -o '[A-Za-z_][A-Za-z0-9_]*Request[[:space:]]+[A-Za-z_][A-Za-z0-9_]*[[:space:]]*(,|\))' "$endpoint_file" \
      | sed -E 's/[[:space:]]+[A-Za-z_][A-Za-z0-9_]*[[:space:]]*(,|\)).*//' \
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

done < <(find "$REPO_ROOT/backend/src" -type f \( -name '*Endpoints.cs' -o -name '*Dtos.cs' \) -print0)

if [ -s "$MISSING" ]; then
  echo "Endpoint types used but not registered in WeCmsJsonContext:" >&2
  sort -u "$MISSING" >&2
  exit 1
fi

echo "  All endpoint request/response DTOs are covered in WeCmsJsonContext."
