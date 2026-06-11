#!/usr/bin/env bash

# WeCMS M0-BE: Patch OpenAPI JSON to add requestBody for auth endpoints
# that use RequestDelegate wrappers (not typed delegates) for AOT compatibility.
#
# The built-in OpenAPI generator cannot infer requestBody from RequestDelegate
# endpoints even when .Accepts<T>() metadata is present. This script injects
# the correct requestBody schemas after OpenAPI export.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${REPO_ROOT:-$(cd "${SCRIPT_DIR}/../.." && pwd)}"

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

if [ ! -f "$OPENAPI_FILE" ]; then
  echo "OpenAPI artifact not found: $OPENAPI_FILE" >&2
  exit 1
fi

patched=0

# /api/v1/auth/login — LoginRequest { Username, Password }
if ! jq -e '.paths["/api/v1/auth/login"].post.requestBody' "$OPENAPI_FILE" >/dev/null 2>&1; then
  echo "  Patching requestBody for /api/v1/auth/login..."
  jq '
    .paths["/api/v1/auth/login"].post.requestBody = {
      "required": true,
      "content": {
        "application/json": {
          "schema": {
            "type": "object",
            "properties": {
              "username": { "type": "string", "description": "用户名" },
              "password": { "type": "string", "description": "密码" }
            },
            "required": ["username", "password"]
          }
        }
      }
    }
  ' "$OPENAPI_FILE" > "$OPENAPI_FILE.tmp" \
    && mv "$OPENAPI_FILE.tmp" "$OPENAPI_FILE"
  patched=1
fi

# /api/v1/auth/refresh — RefreshRequest { RefreshToken }
if ! jq -e '.paths["/api/v1/auth/refresh"].post.requestBody' "$OPENAPI_FILE" >/dev/null 2>&1; then
  echo "  Patching requestBody for /api/v1/auth/refresh..."
  jq '
    .paths["/api/v1/auth/refresh"].post.requestBody = {
      "required": true,
      "content": {
        "application/json": {
          "schema": {
            "type": "object",
            "properties": {
              "refreshToken": { "type": "string", "description": "刷新令牌" }
            },
            "required": ["refreshToken"]
          }
        }
      }
    }
  ' "$OPENAPI_FILE" > "$OPENAPI_FILE.tmp" \
    && mv "$OPENAPI_FILE.tmp" "$OPENAPI_FILE"
  patched=1
fi

# /api/v1/auth/logout — LogoutRequest { RefreshToken }
if ! jq -e '.paths["/api/v1/auth/logout"].post.requestBody' "$OPENAPI_FILE" >/dev/null 2>&1; then
  echo "  Patching requestBody for /api/v1/auth/logout..."
  jq '
    .paths["/api/v1/auth/logout"].post.requestBody = {
      "required": false,
      "content": {
        "application/json": {
          "schema": {
            "type": "object",
            "properties": {
              "refreshToken": { "type": "string", "description": "刷新令牌（可选）" }
            }
          }
        }
      }
    }
  ' "$OPENAPI_FILE" > "$OPENAPI_FILE.tmp" \
    && mv "$OPENAPI_FILE.tmp" "$OPENAPI_FILE"
  patched=1
fi

if [ "$patched" -eq 1 ]; then
  echo "  OpenAPI auth request bodies patched."
else
  echo "  OpenAPI auth request bodies already present (no patching needed)."
fi
