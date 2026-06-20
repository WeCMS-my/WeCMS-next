#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
src_root="$repo_root/backend/src"
adr="$repo_root/docs/adr/0019-sqlsugar-data-platform.md"

fail() {
  printf 'check-sqlsugar-boundary: %s\n' "$1" >&2
  exit 1
}

command -v rg >/dev/null 2>&1 || fail 'rg is required. Install ripgrep before running this check.'
[[ -f "$adr" ]] || fail 'missing docs/adr/0019-sqlsugar-data-platform.md'

database_tokens='SqlSugarCore|SqlSugarClient|SqlSugarScope|ISqlSugarClient|MySqlConnector|MySqlConnection'
allowed_globs=(
  '--glob' '!**/bin/**'
  '--glob' '!**/obj/**'
  '--glob' '!**/WeCms.Data.SqlSugar/**'
  '--glob' '!**/WeCms.Modules.*.SqlSugar/**'
)

if [[ "${WECMS_ARCHITECTURE_FINAL_SQLSUGAR_PLATFORM:-false}" == "true" ]]; then
  [[ ! -e "$src_root/WeCms.Persistence/WeCms.Persistence.csproj" ]] || fail 'final mode does not allow WeCms.Persistence'
else
  allowed_globs+=( '--glob' '!**/WeCms.Persistence/**' )
  rg -q --fixed-strings '旧 WeCms.Persistence 不作为长期合法项目' "$adr" || fail 'ADR does not document Persistence removal'
fi

if rg -n "$database_tokens" "$src_root" "${allowed_globs[@]}"; then
  fail 'database/ORM tokens found outside allowed data projects'
fi

if rg -n 'WeCms\.Data\.SqlSugar|WeCms\.Modules\.[[:alnum:].]+\.SqlSugar' \
  "$src_root"/WeCms.Modules.* \
  --glob '!**/bin/**' --glob '!**/obj/**' --glob '!**/WeCms.Modules.*.SqlSugar/**'; then
  fail 'business modules reference data platform or module SqlSugar adapters'
fi

printf 'check-sqlsugar-boundary: ok\n'
