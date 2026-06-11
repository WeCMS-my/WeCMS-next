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
echo "[1/10] dotnet build -warnaserror"
run_with_dir "$REPO_ROOT" dotnet build backend/WeCms.slnx -warnaserror --nologo
echo "  PASSED"

# ── 2. AOT exception baseline check (ADR-0006) ──
echo "[2/10] AOT exception baseline check (ADR-0006)"
run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-aot-exception-baseline.sh"
echo "  PASSED"

# ── 3. AOT self-warning suppression check (ADR-0006) ──
echo "[3/10] AOT self-warning suppression check (ADR-0006)"
run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-no-self-aot-suppression.sh"
echo "  PASSED"

# ── 4. Native AOT Publish ──
echo "[4/10] dotnet publish (Native AOT)"
run_with_dir "$REPO_ROOT" dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true --nologo
echo "  PASSED"

# ── 5. Tests ──
echo "[5/10] dotnet test"
run_with_dir "$REPO_ROOT" dotnet test backend/WeCms.slnx --nologo --verbosity normal
echo "  PASSED"

# ── 6. OpenAPI Export ──
echo "[6/10] OpenAPI export"
run_with_dir "$REPO_ROOT" dotnet run --project backend/src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-api-v1.json --nologo
echo "  PASSED"

# ── 7. SQL: no SELECT * ──
echo "[7/10] check-no-select-star"
run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-no-select-star.sh"
echo "  PASSED"

# ── 8. SQL: no Query<dynamic> ──
echo "[8/10] check-no-dynamic-query"
run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-no-dynamic-query.sh"
echo "  PASSED"

# ── 9. Endpoint permission metadata scan (runtime architecture test) ──
echo "[9/10] check endpoint permissions (runtime architecture test)"
run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-endpoint-permissions.sh"
echo "  PASSED"

# ── 10. Integrity ──
echo "[10/10] check integrity"
run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-json-context-coverage.sh"
run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-no-frontend-change.sh"
echo "  PASSED"

echo "=== WeCMS M0-BE Backend Quality Gate PASSED ==="
