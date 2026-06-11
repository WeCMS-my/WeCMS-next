#!/usr/bin/env bash

# WeCMS M0-BE normalize OpenAPI artifact
# Produces a deterministic artifact by fixing the server URL and sorting JSON keys.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo-root)
      REPO_ROOT="${2:?missing value for --repo-root}"
      shift 2
      ;;
    --openapi-file)
      OPENAPI_FILE="${2:?missing value for --openapi-file}"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      echo "Usage: $0 [--repo-root <path>] [--openapi-file <path>]" >&2
      exit 1
      ;;
  esac
done

OPENAPI_FILE="${OPENAPI_FILE:-$REPO_ROOT/artifacts/openapi/wecms-api-v1.json}"
FIXED_SERVER_URL="${WECMS_OPENAPI_SERVER_URL:-http://localhost:5000/}"

if [ ! -f "$OPENAPI_FILE" ]; then
  echo "OpenAPI artifact not found: $OPENAPI_FILE" >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required for OpenAPI normalization." >&2
  exit 1
fi

jq -S --arg server_url "$FIXED_SERVER_URL" '
  .servers = [{"url": $server_url}]
' "$OPENAPI_FILE" > "$OPENAPI_FILE.tmp"
mv "$OPENAPI_FILE.tmp" "$OPENAPI_FILE"

echo "  OpenAPI artifact normalized with server URL: $FIXED_SERVER_URL"
