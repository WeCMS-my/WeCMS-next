#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
nuget_audit_mode="${WECMS_NUGET_AUDIT_MODE:-strict}"
nuget_http_cache_path="${NUGET_HTTP_CACHE_PATH:-}"

if [[ -z "${WECMS_TEST_MYSQL_CONNECTION_STRING:-}" ]]; then
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

mysql_connection_string="$WECMS_TEST_MYSQL_CONNECTION_STRING"
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

printf '[1/13] dotnet restore\n'
run_dotnet_gate_command restore backend/WeCms.slnx

printf '[2/13] dotnet build -warnaserror\n'
run_dotnet_gate_command build backend/WeCms.slnx -warnaserror --nologo --no-restore

printf '[3/13] dotnet test\n'
run_dotnet_gate_command test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --nologo --no-build --no-restore
run_dotnet_gate_command test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --nologo --no-build --no-restore
export WECMS_TEST_MYSQL_CONNECTION_STRING="$mysql_connection_string"
run_dotnet_gate_command test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --nologo --no-build --no-restore

printf '[4/13] dotnet publish JIT\n'
run_dotnet_gate_command publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false --nologo

printf '[5/13] OpenAPI export\n'
dotnet run --project backend/src/WeCms.Api --no-build --no-restore -- --export-openapi "$openapi_path"

printf '[6/13] OpenAPI auth request body check\n'
bash scripts/checks/check-openapi-auth-request-body.sh "$openapi_path"
bash scripts/checks/check-openapi-endpoint-coverage.sh "$openapi_path"

printf '[7/13] check-db-boundary\n'
bash scripts/checks/check-db-boundary.sh

printf '[8/13] check-layer-dependency\n'
bash scripts/checks/check-layer-dependency.sh

printf '[9/13] check-di-boundary\n'
bash scripts/checks/check-di-boundary.sh

printf '[10/13] check-no-frontend-change\n'
bash scripts/checks/check-no-frontend-change.sh

printf '[11/13] check-generated-test-artifacts\n'
bash scripts/checks/check-generated-test-artifacts.sh

printf '[12/13] check-code-review\n'
bash scripts/checks/check-code-review.sh

printf '[13/13] migration/seed smoke test\n'
run_dotnet_gate_command test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --filter MigrationAndSeedSmokeTests --nologo --no-build --no-restore

printf 'quality-gate-backend: ok\n'
