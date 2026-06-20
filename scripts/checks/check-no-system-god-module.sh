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

if [[ "${WECMS_ARCHITECTURE_FINAL_SYSTEM_SPLIT:-false}" == "true" ]]; then
  [[ ! -e "$src_root/WeCms.Modules.System/WeCms.Modules.System.csproj" ]] || fail 'final mode does not allow WeCms.Modules.System'
else
  rg -q --fixed-strings '迁移期间允许旧 WeCms.Modules.System 暂存' "$adr" || fail 'ADR does not document transition allowance'
  rg -q --fixed-strings '最终验收不得保留 WeCms.Modules.System' "$adr" || fail 'ADR does not document final removal'
fi

printf 'check-no-system-god-module: ok\n'
