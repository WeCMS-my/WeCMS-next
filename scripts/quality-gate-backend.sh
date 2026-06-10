#!/usr/bin/env bash

# WeCMS M0-BE Backend Quality Gate
# Validates: build, test, AOT publish, OpenAPI export, code quality checks.
# M0-BE constraint: does NOT run pnpm or touch frontend/.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../" && pwd)"

run_with_dir() {
  local dir="$1"
  shift
  pushd "$dir" >/dev/null
  "$@"
  local rc=$?
  popd >/dev/null
  return $rc
}

echo "=== WeCMS M0-BE Backend Quality Gate ==="

# ── 1. Build ──
echo "[1/7] dotnet build -warnaserror"
run_with_dir "$REPO_ROOT" dotnet build backend/WeCms.slnx -warnaserror --nologo
echo "  PASSED"

# ── 2. Tests ──
echo "[2/7] dotnet test"
run_with_dir "$REPO_ROOT" dotnet test backend/WeCms.slnx --nologo --verbosity normal
echo "  PASSED"

# ── 3. Native AOT Publish ──
echo "[3/7] dotnet publish (Native AOT)"
run_with_dir "$REPO_ROOT" dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true --nologo
echo "  PASSED"

# ── 4. OpenAPI Export ──
echo "[4/7] OpenAPI export"
run_with_dir "$REPO_ROOT" dotnet run --project backend/src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-api-v1.json --nologo
echo "  PASSED"

# ── 5. SQL: no SELECT * ──
echo "[5/7] check-no-select-star"
run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-no-select-star.sh"
echo "  PASSED"

# ── 6. SQL: no Query<dynamic> ──
echo "[6/7] check-no-dynamic-query"
run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-no-dynamic-query.sh"
echo "  PASSED"

# ── 7. Integrity ──
echo "[7/7] check integrity"
run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-endpoint-permissions.sh"
run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-json-context-coverage.sh"
run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-no-frontend-change.sh"
echo "  PASSED"

echo "=== WeCMS M0-BE Backend Quality Gate PASSED ==="

