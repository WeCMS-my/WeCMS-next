#!/usr/bin/env bash

# WeCMS M0-BE check-openapi-auth-request-bodies
# Ensures OpenAPI contains requestBody for auth POST endpoints.

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

OPENAPI_FILE="$REPO_ROOT/artifacts/openapi/wecms-api-v1.json"

if [ ! -f "$OPENAPI_FILE" ]; then
  echo "OpenAPI artifact not found: $OPENAPI_FILE" >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required for this check." >&2
  exit 1
fi

required_paths=("/api/v1/auth/login" "/api/v1/auth/refresh" "/api/v1/auth/logout")

for route in "${required_paths[@]}"; do
  if ! jq -e --arg route "$route" '
      .paths[$route].post.requestBody.content["application/json"]? != null
    ' "$OPENAPI_FILE" >/dev/null; then
    echo "Missing request body schema for POST ${route} in OpenAPI artifact." >&2
    exit 1
  fi
done

echo "  OpenAPI auth request body check passed for login, refresh, logout."
