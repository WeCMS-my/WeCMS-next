#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
src_root="$repo_root/backend/src"

fail() {
  printf 'check-di-boundary: %s\n' "$1" >&2
  exit 1
}

command -v rg >/dev/null 2>&1 || fail 'rg is required. Install ripgrep before running this check.'

if rg -n 'new\s+\w*Repository\s*\(|new\s+(SqlSugarClient|SqlSugarScope|MySqlConnection|HttpClient)\s*\(|\bDateTime\.UtcNow\b|\bGuid\.NewGuid\s*\(|\bRandom\.Shared\b' \
  "$src_root/WeCms.Modules.System" "$src_root/WeCms.Modules.Cms" \
  --glob '!**/bin/**' --glob '!**/obj/**'; then
  fail 'business module code directly creates side-effect dependencies'
fi

if rg -n '\bGuid\.NewGuid\s*\(' "$src_root" \
  --glob '!**/bin/**' --glob '!**/obj/**' \
  --glob '!**/WeCms.Infrastructure/Id/SystemIdGenerator.cs'; then
  fail 'production code must use IIdGenerator instead of direct Guid.NewGuid'
fi

if rg -n '[(,][[:space:]]*([[:alnum:]_.]+\.)?[[:alnum:]_]*Repository[[:space:]]+[[:alnum:]_]+' \
  "$src_root/WeCms.Modules.System" "$src_root/WeCms.Modules.Cms" \
  --glob '!**/bin/**' --glob '!**/obj/**' \
  | rg -v '[(,][[:space:]]*([[:alnum:]_.]+\.)?I[A-Z][[:alnum:]_]*Repository[[:space:]]+'; then
  fail 'business constructors depend on concrete repository implementations'
fi

if rg -n 'RequestServices\.GetRequiredService' \
  "$src_root/WeCms.Modules.System" "$src_root/WeCms.Modules.Cms" \
  --glob '*Filter.cs' --glob '!**/bin/**' --glob '!**/obj/**'; then
  fail 'endpoint filters must use constructor injection instead of RequestServices lookup'
fi

printf 'check-di-boundary: ok\n'
