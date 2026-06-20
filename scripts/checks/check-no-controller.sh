#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
src_root="$repo_root/backend/src"

fail() {
  printf 'check-no-controller: %s\n' "$1" >&2
  exit 1
}

command -v rg >/dev/null 2>&1 || fail 'rg is required. Install ripgrep before running this check.'

if rg -n ': ControllerBase|: Controller|AddControllers\(|MapControllers\(|\[ApiController\]' \
  "$src_root" \
  --glob '!**/bin/**' --glob '!**/obj/**'; then
  fail 'Controller API surface found in production code'
fi

printf 'check-no-controller: ok\n'
