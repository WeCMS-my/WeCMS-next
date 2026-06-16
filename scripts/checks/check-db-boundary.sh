#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
src_root="$repo_root/backend/src"

fail() {
  printf 'check-db-boundary: %s\n' "$1" >&2
  exit 1
}

command -v rg >/dev/null 2>&1 || fail 'rg is required. Install ripgrep before running this check.'

if rg -n 'SqlSugarCore|SqlSugarClient|SqlSugarScope|ISqlSugarClient|MySqlConnector|MySqlConnection|DbConnection|DbTransaction' \
  "$src_root/WeCms.Api" "$src_root/WeCms.Infrastructure" "$src_root/WeCms.Shared" "$src_root/WeCms.Modules.System" "$src_root/WeCms.Modules.Cms" \
  --glob '!**/bin/**' --glob '!**/obj/**'; then
  fail 'database/ORM tokens found outside WeCms.Persistence'
fi

if rg -n -i '\b(SELECT[[:space:]]+.+[[:space:]]+FROM|INSERT[[:space:]]+INTO|UPDATE[[:space:]]+[[:alnum:]_]+|DELETE[[:space:]]+FROM)\b' "$src_root/WeCms.Modules.System" "$src_root/WeCms.Modules.Cms" \
  --glob '!**/bin/**' --glob '!**/obj/**'; then
  fail 'SQL keywords found in WeCms.Modules.*'
fi

printf 'check-db-boundary: ok\n'
