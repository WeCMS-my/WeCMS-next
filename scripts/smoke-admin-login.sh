#!/usr/bin/env bash

# Resets the local development database, starts the API with DbMigrationRunner,
# and verifies admin/Admin@123 can log in.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
API_URL="${WECMS_SMOKE_API_URL:-http://localhost:5207}"
LOG_FILE="${TMPDIR:-/tmp}/wecms-admin-login-smoke.log"

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required for admin login smoke test." >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required for admin login smoke test." >&2
  exit 1
fi

cd "$REPO_ROOT"

echo "=== WeCMS Admin Login Smoke ==="
echo "Resetting local development database..."
bash scripts/db/reset-dev-db.sh --force

echo "Starting API at ${API_URL}..."
dotnet run --project backend/src/WeCms.Api --launch-profile http >"$LOG_FILE" 2>&1 &
api_pid=$!

cleanup() {
  kill "$api_pid" >/dev/null 2>&1 || true
}
trap cleanup EXIT

echo "Waiting for API readiness..."
for _ in {1..60}; do
  if curl -fsS "${API_URL}/health/ready" >/dev/null 2>&1; then
    break
  fi

  if ! kill -0 "$api_pid" >/dev/null 2>&1; then
    echo "API process exited before becoming ready. Log: $LOG_FILE" >&2
    exit 1
  fi

  sleep 1
done

if ! curl -fsS "${API_URL}/health/ready" >/dev/null 2>&1; then
  echo "API did not become ready within timeout. Log: $LOG_FILE" >&2
  exit 1
fi

echo "Logging in as admin..."
response="$(
  curl -fsS \
    -H 'Content-Type: application/json' \
    -d '{"username":"admin","password":"Admin@123"}' \
    "${API_URL}/api/v1/auth/login"
)"

code="$(printf '%s' "$response" | jq -r '.code')"
access_token="$(printf '%s' "$response" | jq -r '.data.accessToken // ""')"

if [[ "$code" != "0" || -z "$access_token" ]]; then
  echo "Admin login smoke failed. Response:" >&2
  printf '%s\n' "$response" >&2
  exit 1
fi

echo "=== Admin Login Smoke PASSED ==="
