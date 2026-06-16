#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
src_root="$repo_root/backend/src"

fail() {
  printf 'check-layer-dependency: %s\n' "$1" >&2
  exit 1
}

command -v rg >/dev/null 2>&1 || fail 'rg is required. Install ripgrep before running this check.'

project_refs() {
  local project="$1"
  local refs
  refs="$(rg --only-matching '<ProjectReference Include="[^"]+"' "$project" || true)"

  if [[ -z "$refs" ]]; then
    return 0
  fi

  printf '%s\n' "$refs" \
    | sed -E 's/.*Include="([^"]+)"/\1/' \
    | sed 's#\\#/#g' \
    | xargs -n1 basename 2>/dev/null \
    | sed 's/\.csproj$//' \
    | sort
}

assert_refs() {
  local project_name="$1"
  shift
  local project="$src_root/$project_name/$project_name.csproj"
  [[ -f "$project" ]] || fail "missing project $project"

  local expected actual
  expected="$(printf '%s\n' "$@" | sort)"
  actual="$(project_refs "$project")"

  if [[ "$actual" != "$expected" ]]; then
    fail "$project_name references do not match expected set. expected=[$(printf '%s ' "$@")] actual=[$(printf '%s ' $actual)]"
  fi
}

assert_refs WeCms.Api WeCms.Infrastructure WeCms.Modules.Cms WeCms.Modules.System WeCms.Persistence WeCms.Shared
assert_refs WeCms.Infrastructure WeCms.Shared
assert_refs WeCms.Modules.Cms WeCms.Shared
assert_refs WeCms.Modules.System WeCms.Shared
assert_refs WeCms.Persistence WeCms.Modules.Cms WeCms.Modules.System WeCms.Shared
assert_refs WeCms.Shared

printf 'check-layer-dependency: ok\n'
