#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
src_root="$repo_root/backend/src"

fail() {
  printf 'check-no-sql-in-modules: %s\n' "$1" >&2
  exit 1
}

command -v rg >/dev/null 2>&1 || fail 'rg is required. Install ripgrep before running this check.'

module_dirs=()
for module_dir in "$src_root"/WeCms.Modules.*; do
  [[ -d "$module_dir" ]] || continue
  [[ "$module_dir" != *.SqlSugar ]] || continue
  module_dirs+=("$module_dir")
done

if [[ "${#module_dirs[@]}" -gt 0 ]] && rg -n -i '\b(SELECT[[:space:]]+.+[[:space:]]+FROM|INSERT[[:space:]]+INTO|UPDATE[[:space:]]+[[:alnum:]_]+|DELETE[[:space:]]+FROM)\b' "${module_dirs[@]}" \
  --glob '!**/bin/**' --glob '!**/obj/**'; then
  fail 'SQL keywords found in WeCms.Modules.*'
fi

printf 'check-no-sql-in-modules: ok\n'
