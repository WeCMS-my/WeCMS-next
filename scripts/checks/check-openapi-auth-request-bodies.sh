#!/usr/bin/env bash

# WeCMS M0-BE check-openapi-auth-request-bodies
# Ensures OpenAPI auth requestBody schemas stay aligned with DTOs.

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

check_route() {
  local route="$1"
  local expected_properties_json="$2"
  local expected_required_json="$3"

  jq -e \
    --arg route "$route" \
    --argjson expected_properties "$expected_properties_json" \
    --argjson expected_required "$expected_required_json" \
    '
      def resolved_schema($root; $request_body):
        ($request_body.content["application/json"].schema) as $schema
        | if $schema["$ref"]? then
            $root.components.schemas[($schema["$ref"] | split("/") | last)]
          else
            $schema
          end;

      .paths[$route].post.requestBody as $request_body
      | ($request_body.required == true)
      and (resolved_schema(. ; $request_body) as $schema
        | ($schema.type == "object")
        and (($schema.properties | keys_unsorted | sort) == ($expected_properties | sort))
        and ((($schema.required // []) | sort) == ($expected_required | sort))
      )
    ' "$OPENAPI_FILE" >/dev/null
}

check_route "/api/v1/auth/login" '["username","password"]' '["username","password"]' || {
  echo "Login request body schema does not match LoginRequest." >&2
  exit 1
}

check_route "/api/v1/auth/refresh" '["refreshToken"]' '["refreshToken"]' || {
  echo "Refresh request body schema does not match RefreshRequest." >&2
  exit 1
}

check_route "/api/v1/auth/logout" '["refreshToken"]' '["refreshToken"]' || {
  echo "Logout request body schema does not match LogoutRequest." >&2
  exit 1
}

echo "  OpenAPI auth request body check passed for login, refresh, logout."
