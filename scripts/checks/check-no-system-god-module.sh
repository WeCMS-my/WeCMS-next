#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
src_root="$repo_root/backend/src"
adr="$repo_root/docs/adr/0018-system-foundation-module-split.md"

fail() {
  printf 'check-no-system-god-module: %s\n' "$1" >&2
  exit 1
}

[[ -f "$adr" ]] || fail 'missing docs/adr/0018-system-foundation-module-split.md'

[[ ! -e "$src_root/WeCms.Modules.System/WeCms.Modules.System.csproj" ]] || fail 'final mode does not allow WeCms.Modules.System'
rg -q --fixed-strings '最终验收不得保留 WeCms.Modules.System' "$adr" || fail 'ADR does not document final removal'

printf 'check-no-system-god-module: ok\n'
