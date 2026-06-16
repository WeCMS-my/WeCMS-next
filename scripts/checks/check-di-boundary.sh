#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
src_root="$repo_root/backend/src"

fail() {
  printf 'check-di-boundary: %s\n' "$1" >&2
  exit 1
}

if rg -n 'new\s+\w*Repository\s*\(|new\s+(SqlSugarClient|SqlSugarScope|MySqlConnection|HttpClient)\s*\(|\bDateTime\.UtcNow\b|\bGuid\.NewGuid\s*\(|\bRandom\.Shared\b' \
  "$src_root/WeCms.Modules.System" "$src_root/WeCms.Modules.Cms" \
  --glob '!**/bin/**' --glob '!**/obj/**'; then
  fail 'business module code directly creates side-effect dependencies'
fi

if rg -n '[(,][[:space:]]*([[:alnum:]_.]+\.)?[[:alnum:]_]*Repository[[:space:]]+[[:alnum:]_]+' \
  "$src_root/WeCms.Modules.System" "$src_root/WeCms.Modules.Cms" \
  --glob '!**/bin/**' --glob '!**/obj/**' \
  | rg -v '[(,][[:space:]]*([[:alnum:]_.]+\.)?I[A-Z][[:alnum:]_]*Repository[[:space:]]+'; then
  fail 'business constructors depend on concrete repository implementations'
fi

printf 'check-di-boundary: ok\n'
