#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ -n "${CI:-}" || -n "${GITHUB_ACTIONS:-}" ]]; then
  nuget_audit_mode="${WECMS_NUGET_AUDIT_MODE:-strict}"
else
  nuget_audit_mode="${WECMS_NUGET_AUDIT_MODE:-fallback}"
fi
nuget_http_cache_path="${NUGET_HTTP_CACHE_PATH:-}"
frontend_scope="${WECMS_BACKEND_GATE_FRONTEND_SCOPE:-backend-only}"
run_mysql_integration_tests=true

read_connection_value() {
  local key="$1"
  local connection_string="$2"

  python3 - "$key" "$connection_string" <<'PY'
import sys

target = sys.argv[1].strip().lower()
parts = [item.strip() for item in sys.argv[2].split(';') if item.strip()]
for item in parts:
    if '=' not in item:
        continue
    name, value = item.split('=', 1)
    if name.strip().lower() == target:
        print(value.strip())
        break
PY
}

case "${WECMS_SKIP_MYSQL_INTEGRATION_TESTS:-false}" in
  1|true|TRUE|True|yes|YES|on|ON|y|Y)
    run_mysql_integration_tests=false
    ;;
  *)
    run_mysql_integration_tests=true
    ;;
esac

if [[ "$run_mysql_integration_tests" == true ]]; then
  mysql_connection_string="${WECMS_TEST_MYSQL_CONNECTION_STRING:-}"
  if [[ -z "$mysql_connection_string" ]]; then
    mysql_connection_string="$(python3 - "$repo_root/backend/src/WeCms.Api/appsettings.Development.json" "$repo_root/backend/src/WeCms.Api/appsettings.json" <<'PY'
import json
import sys
from pathlib import Path

for path_text in sys.argv[1:]:
    path = Path(path_text)
    if not path.is_file():
        continue
    data = json.loads(path.read_text(encoding="utf-8"))
    connection_strings = data.get("ConnectionStrings") or {}
    value = connection_strings.get("Test") or connection_strings.get("Default")
    if isinstance(value, str) and value.strip() and "__SET_BY_USER_SECRETS__" not in value:
        print(value)
        break
PY
)"
  fi

  if [[ -z "$mysql_connection_string" ]]; then
    printf 'quality-gate-backend: WECMS_TEST_MYSQL_CONNECTION_STRING is required for MySQL integration tests.\n' >&2
    exit 1
  fi

  mysql_database="$(read_connection_value "database" "$mysql_connection_string" | tr '[:upper:]' '[:lower:]')"
  if [[ "$mysql_database" != "wecms_dev" ]]; then
    run_mysql_integration_tests=false
    printf 'Integration tests skipped: WECMS_TEST_MYSQL_CONNECTION_STRING database=%q is not allowed for project gate (expected wecms_dev).\n' "${mysql_database:-<empty>}"
  fi
else
  mysql_connection_string=""
fi

command -v rg >/dev/null 2>&1 || {
  printf 'quality-gate-backend: rg is required. Install ripgrep before running the backend gate.\n' >&2
  exit 1
}

if [[ "$nuget_audit_mode" != "strict" && "$nuget_audit_mode" != "fallback" ]]; then
  printf 'quality-gate-backend: WECMS_NUGET_AUDIT_MODE must be strict or fallback.\n' >&2
  exit 1
fi

if [[ "$frontend_scope" != "backend-only" && "$frontend_scope" != "includes-frontend" ]]; then
  printf 'quality-gate-backend: WECMS_BACKEND_GATE_FRONTEND_SCOPE must be backend-only or includes-frontend.\n' >&2
  exit 1
fi

if [[ "$nuget_audit_mode" == "fallback" ]]; then
  if [[ "${CI:-}" == "true" || "${GITHUB_ACTIONS:-}" == "true" ]]; then
    printf 'quality-gate-backend: WECMS_NUGET_AUDIT_MODE=fallback is local-only and must not be used in CI or release gates.\n' >&2
    exit 1
  fi

  if [[ -z "$nuget_http_cache_path" ]]; then
    nuget_http_cache_path="${TMPDIR:-/tmp}/wecms-nuget-http-cache"
  fi

  mkdir -p "$nuget_http_cache_path"
  export NUGET_HTTP_CACHE_PATH="$nuget_http_cache_path"
  printf 'quality-gate-backend: WARNING local-only fallback dotnet mode enabled with -p:NuGetAudit=false and NUGET_HTTP_CACHE_PATH=%s.\n' "$NUGET_HTTP_CACHE_PATH"
fi

unset WECMS_TEST_MYSQL_CONNECTION_STRING

cd "$repo_root"

openapi_path="artifacts/openapi/wecms-api-v1.json"

run_dotnet_gate_command() {
  if [[ "$nuget_audit_mode" == "fallback" ]]; then
    dotnet "$@" -p:NuGetAudit=false
    return 0
  fi

  dotnet "$@"
}

printf '[1/32] dotnet restore\n'
run_dotnet_gate_command restore backend/WeCms.slnx

printf '[2/32] dotnet build -warnaserror\n'
run_dotnet_gate_command build backend/WeCms.slnx -warnaserror --nologo --no-restore

printf '[3/32] dotnet test\n'
run_dotnet_gate_command test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --nologo --no-build --no-restore
run_dotnet_gate_command test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --nologo --no-build --no-restore
if [[ "$run_mysql_integration_tests" == true ]]; then
  export WECMS_TEST_MYSQL_CONNECTION_STRING="$mysql_connection_string"
  run_dotnet_gate_command test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --settings backend/tests/WeCms.Tests.Integration/serial.runsettings --nologo --no-restore
else
  printf 'Integration tests skipped because WECMS_SKIP_MYSQL_INTEGRATION_TESTS is enabled.\n'
fi

printf '[4/32] dotnet publish JIT\n'
run_dotnet_gate_command publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false --nologo

printf '[5/32] OpenAPI export\n'
dotnet run --project backend/src/WeCms.Api --no-build --no-restore -- --export-openapi "$openapi_path"

printf '[6/32] OpenAPI auth request body check\n'
bash scripts/checks/check-openapi-auth-request-body.sh "$openapi_path"
bash scripts/checks/check-openapi-endpoint-coverage.sh "$openapi_path"

printf '[7/32] check-system-openapi-coverage\n'
bash scripts/checks/check-system-openapi-coverage.sh "$openapi_path"

printf '[8/32] check-write-endpoint-methods\n'
bash scripts/checks/check-write-endpoint-methods.sh "$openapi_path"

printf '[9/32] check-write-endpoint-permission-coverage\n'
bash scripts/checks/check-write-endpoint-permission-coverage.sh "$openapi_path"

printf '[10/32] check-write-endpoint-audit-coverage\n'
bash scripts/checks/check-write-endpoint-audit-coverage.sh "$openapi_path"

printf '[11/32] check-system-permission-coverage\n'
bash scripts/checks/check-system-permission-coverage.sh

printf '[12/32] check-locked-role-seed\n'
bash scripts/checks/check-locked-role-seed.sh

printf '[13/32] check-rate-limit-policy-coverage\n'
bash scripts/checks/check-rate-limit-policy-coverage.sh

printf '[14/32] check-security-event-coverage\n'
bash scripts/checks/check-security-event-coverage.sh

printf '[15/32] check-cookie-auth-origin-protection\n'
bash scripts/checks/check-cookie-auth-origin-protection.sh

printf '[16/32] check-admingate-csrf-migration\n'
bash scripts/checks/check-admingate-csrf-migration.sh

printf '[17/32] check-thinkphp-feature-delta\n'
bash scripts/checks/check-thinkphp-feature-delta.sh "$openapi_path"

printf '[18/32] check-foundation-freeze-baseline\n'
bash scripts/checks/check-foundation-freeze-baseline.sh

printf '[19/32] check-production-config-baseline\n'
bash scripts/checks/check-production-config-baseline.sh

printf '[20/32] check-security-baseline\n'
bash scripts/checks/check-security-baseline.sh

printf '[21/32] check-database-governance\n'
bash scripts/checks/check-database-governance.sh

printf '[22/32] check-observability-baseline\n'
bash scripts/checks/check-observability-baseline.sh

printf '[23/32] check-file-storage-production\n'
bash scripts/checks/check-file-storage-production.sh

printf '[24/32] check-no-sql-in-modules\n'
bash scripts/checks/check-no-sql-in-modules.sh

printf '[25/32] check-db-boundary\n'
bash scripts/checks/check-db-boundary.sh

printf '[26/32] check-layer-dependency\n'
bash scripts/checks/check-layer-dependency.sh

printf '[27/32] check-di-boundary\n'
bash scripts/checks/check-di-boundary.sh

printf '[28/32] check-no-frontend-change\n'
if [[ "$frontend_scope" == "backend-only" ]]; then
  bash scripts/checks/check-no-frontend-change.sh
else
  printf 'check-no-frontend-change: skipped because WECMS_BACKEND_GATE_FRONTEND_SCOPE=includes-frontend\n'
fi

printf '[29/32] check-generated-test-artifacts\n'
bash scripts/checks/check-generated-test-artifacts.sh

printf '[30/32] check-code-review\n'
bash scripts/checks/check-code-review.sh
printf '[31/32] check-replace-write-affected-rows\n'
bash scripts/checks/check-replace-write-affected-rows.sh

printf '[32/32] migration/seed smoke test\n'
if [[ "$run_mysql_integration_tests" == true ]]; then
  run_dotnet_gate_command test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --settings backend/tests/WeCms.Tests.Integration/serial.runsettings --filter MigrationAndSeedSmokeTests --nologo --no-build --no-restore
else
  printf 'Migration/seed smoke test skipped because WECMS_SKIP_MYSQL_INTEGRATION_TESTS is enabled.\n'
fi

printf 'quality-gate-backend: ok\n'
