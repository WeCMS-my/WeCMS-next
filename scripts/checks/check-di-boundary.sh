#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
src_root="$repo_root/backend/src"

fail() {
  printf 'check-di-boundary: %s\n' "$1" >&2
  exit 1
}

command -v rg >/dev/null 2>&1 || fail 'rg is required. Install ripgrep before running this check.'

module_paths=()
for path in "$src_root"/WeCms.Modules.*; do
  [[ -d "$path" ]] || continue
  [[ "$path" == *.SqlSugar ]] && continue
  module_paths+=("$path")
done

if [[ ${#module_paths[@]} -eq 0 ]]; then
  printf 'check-di-boundary: ok\n'
  exit 0
fi

if rg -n 'new\s+\w*Repository\s*\(|new\s+(SqlSugarClient|SqlSugarScope|MySqlConnection|HttpClient)\s*\(|\bDateTime\.UtcNow\b|\bGuid\.NewGuid\s*\(|\bRandom\.Shared\b' \
  "${module_paths[@]}" \
  --glob '!**/bin/**' --glob '!**/obj/**'; then
  fail 'business module code directly creates side-effect dependencies'
fi

if rg -n '\bGuid\.NewGuid\s*\(' "$src_root" \
  --glob '!**/bin/**' --glob '!**/obj/**' \
  --glob '!**/WeCms.Infrastructure/Id/SystemIdGenerator.cs'; then
  fail 'production code must use IIdGenerator instead of direct Guid.NewGuid'
fi

if rg -n '[(,][[:space:]]*([[:alnum:]_.]+\.)?[[:alnum:]_]*Repository[[:space:]]+[[:alnum:]_]+' \
  "${module_paths[@]}" \
  --glob '!**/bin/**' --glob '!**/obj/**' \
  | rg -v '[(,][[:space:]]*([[:alnum:]_.]+\.)?I[A-Z][[:alnum:]_]*Repository[[:space:]]+'; then
  fail 'business constructors depend on concrete repository implementations'
fi

if rg -n 'RequestServices\.GetRequiredService' \
  "${module_paths[@]}" \
  --glob '*Filter.cs' --glob '!**/bin/**' --glob '!**/obj/**'; then
  fail 'endpoint filters must use constructor injection instead of RequestServices lookup'
fi

printf 'check-di-boundary: ok\n'
