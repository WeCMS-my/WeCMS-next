#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

fail() {
  printf 'check-code-review: %s\n' "$1" >&2
  exit 1
}

scan() {
  local pattern="$1"
  local message="$2"
  shift 2

  if rg -n "$pattern" "$@" >/tmp/wecms-code-review-hit.txt; then
    cat /tmp/wecms-code-review-hit.txt >&2
    fail "$message"
  fi
}

cd "$repo_root"

scan 'ControllerBase|AddControllers|MapControllers|UseMvc|Razor' 'MVC/Razor is not allowed in backend M0-BE.' backend/src
scan 'EntityFramework|Microsoft\.EntityFrameworkCore' 'EF Core is not allowed.' backend/src backend/tests
scan '\bdynamic\b' 'dynamic is not allowed.' backend/src backend/tests
scan 'SELECT \*' 'SELECT * is not allowed.' backend/src backend/tests database
scan 'Newtonsoft' 'Newtonsoft.Json is not allowed in core backend paths.' backend/src
scan 'WeCms\.Modules\.Ai|OpenAI|IChatClient|Kernel|Prompt|Vector' 'AI runtime code is not allowed in M0-BE.' backend/src backend/tests
scan 'PublishAot|/p:PublishAot|Dapper|Dapper\.AOT|IL2026|IL3050' 'AOT/Dapper/IL trim gates are excluded from M0-BE JIT backend.' backend/src backend/tests

if find scripts -type f ! -path 'scripts/checks/check-code-review.sh' -print0 \
  | xargs -0 rg -n 'PublishAot|/p:PublishAot|Dapper|Dapper\.AOT|IL2026|IL3050' >/tmp/wecms-code-review-hit.txt; then
  cat /tmp/wecms-code-review-hit.txt >&2
  fail 'AOT/Dapper/IL trim gates are excluded from M0-BE JIT backend.'
fi

printf 'check-code-review: ok\n'
