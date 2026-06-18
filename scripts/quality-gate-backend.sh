#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
nuget_audit_mode="${WECMS_NUGET_AUDIT_MODE:-strict}"
nuget_http_cache_path="${NUGET_HTTP_CACHE_PATH:-}"
frontend_scope="${WECMS_BACKEND_GATE_FRONTEND_SCOPE:-backend-only}"

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
    if isinstance(value, str) and value.strip():
        print(value)
        break
PY
)"
fi

if [[ -z "$mysql_connection_string" ]]; then
  printf 'quality-gate-backend: WECMS_TEST_MYSQL_CONNECTION_STRING is required for MySQL integration tests.\n' >&2
  exit 1
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

printf '[1/20] dotnet restore\n'
run_dotnet_gate_command restore backend/WeCms.slnx

printf '[2/20] dotnet build -warnaserror\n'
run_dotnet_gate_command build backend/WeCms.slnx -warnaserror --nologo --no-restore

printf '[3/20] dotnet test\n'
run_dotnet_gate_command test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --nologo --no-build --no-restore
run_dotnet_gate_command test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --nologo --no-build --no-restore
export WECMS_TEST_MYSQL_CONNECTION_STRING="$mysql_connection_string"
run_dotnet_gate_command test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --nologo --no-build --no-restore

printf '[4/20] dotnet publish JIT\n'
run_dotnet_gate_command publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false --nologo

printf '[5/20] OpenAPI export\n'
dotnet run --project backend/src/WeCms.Api --no-build --no-restore -- --export-openapi "$openapi_path"

printf '[6/20] OpenAPI auth request body check\n'
bash scripts/checks/check-openapi-auth-request-body.sh "$openapi_path"
bash scripts/checks/check-openapi-endpoint-coverage.sh "$openapi_path"

printf '[7/20] check-system-openapi-coverage\n'
bash scripts/checks/check-system-openapi-coverage.sh "$openapi_path"

printf '[8/20] check-write-endpoint-methods\n'
bash scripts/checks/check-write-endpoint-methods.sh "$openapi_path"

printf '[9/20] check-write-endpoint-permission-coverage\n'
bash scripts/checks/check-write-endpoint-permission-coverage.sh "$openapi_path"

printf '[10/20] check-write-endpoint-audit-coverage\n'
bash scripts/checks/check-write-endpoint-audit-coverage.sh "$openapi_path"

printf '[11/20] check-system-permission-coverage\n'
bash scripts/checks/check-system-permission-coverage.sh

printf '[12/20] check-locked-role-seed\n'
bash scripts/checks/check-locked-role-seed.sh

printf '[13/20] check-no-sql-in-modules\n'
bash scripts/checks/check-no-sql-in-modules.sh

printf '[14/20] check-db-boundary\n'
bash scripts/checks/check-db-boundary.sh

printf '[15/20] check-layer-dependency\n'
bash scripts/checks/check-layer-dependency.sh

printf '[16/20] check-di-boundary\n'
bash scripts/checks/check-di-boundary.sh

printf '[17/20] check-no-frontend-change\n'
if [[ "$frontend_scope" == "backend-only" ]]; then
  bash scripts/checks/check-no-frontend-change.sh
else
  printf 'check-no-frontend-change: skipped because WECMS_BACKEND_GATE_FRONTEND_SCOPE=includes-frontend\n'
fi

printf '[18/20] check-generated-test-artifacts\n'
bash scripts/checks/check-generated-test-artifacts.sh

printf '[19/20] check-code-review\n'
bash scripts/checks/check-code-review.sh

printf '[20/20] migration/seed smoke test\n'
run_dotnet_gate_command test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --filter MigrationAndSeedSmokeTests --nologo --no-build --no-restore

printf 'quality-gate-backend: ok\n'
