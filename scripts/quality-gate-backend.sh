#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [[ -z "${WECMS_TEST_MYSQL_CONNECTION_STRING:-}" ]]; then
  printf 'quality-gate-backend: WECMS_TEST_MYSQL_CONNECTION_STRING is required for MySQL integration tests.\n' >&2
  exit 1
fi

command -v rg >/dev/null 2>&1 || {
  printf 'quality-gate-backend: rg is required. Install ripgrep before running the backend gate.\n' >&2
  exit 1
}

mysql_connection_string="$WECMS_TEST_MYSQL_CONNECTION_STRING"
unset WECMS_TEST_MYSQL_CONNECTION_STRING

cd "$repo_root"

openapi_path="artifacts/openapi/wecms-api-v1.json"

printf '[1/12] dotnet restore\n'
dotnet restore backend/WeCms.slnx

printf '[2/12] dotnet build -warnaserror\n'
dotnet build backend/WeCms.slnx -warnaserror --nologo

printf '[3/12] dotnet test\n'
dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --nologo
dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --nologo
export WECMS_TEST_MYSQL_CONNECTION_STRING="$mysql_connection_string"
dotnet test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --nologo

printf '[4/12] dotnet publish JIT\n'
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false --nologo

printf '[5/12] OpenAPI export\n'
dotnet run --project backend/src/WeCms.Api -- --export-openapi "$openapi_path"

printf '[6/12] OpenAPI auth request body check\n'
bash scripts/checks/check-openapi-auth-request-body.sh "$openapi_path"
bash scripts/checks/check-openapi-endpoint-coverage.sh "$openapi_path"

printf '[7/12] check-db-boundary\n'
bash scripts/checks/check-db-boundary.sh

printf '[8/12] check-layer-dependency\n'
bash scripts/checks/check-layer-dependency.sh

printf '[9/12] check-di-boundary\n'
bash scripts/checks/check-di-boundary.sh

printf '[10/12] check-no-frontend-change\n'
bash scripts/checks/check-no-frontend-change.sh

printf '[11/12] check-code-review\n'
bash scripts/checks/check-code-review.sh

printf '[12/12] migration/seed smoke test\n'
dotnet test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --filter MigrationAndSeedSmokeTests --nologo

printf 'quality-gate-backend: ok\n'
