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

project_exists() {
  local project_name="$1"
  [[ -f "$src_root/$project_name/$project_name.csproj" ]]
}

assert_refs() {
  local project_name="$1"
  shift
  local project="$src_root/$project_name/$project_name.csproj"
  [[ -f "$project" ]] || return 0

  local expected actual
  expected="$(printf '%s\n' "$@" | sort)"
  actual="$(project_refs "$project")"

  if [[ "$actual" != "$expected" ]]; then
    fail "$project_name references do not match expected set. expected=[$(printf '%s ' "$@")] actual=[$(printf '%s ' $actual)]"
  fi
}

api_refs=(WeCms.Infrastructure WeCms.Shared)
project_exists WeCms.Modules.System && api_refs+=(WeCms.Modules.System)
project_exists WeCms.Persistence && api_refs+=(WeCms.Persistence)
for project in \
  WeCms.Data.SqlSugar WeCms.Caching WeCms.EventBus WeCms.Aop \
  WeCms.Modules.Identity WeCms.Modules.Identity.SqlSugar \
  WeCms.Modules.AccessControl WeCms.Modules.AccessControl.SqlSugar \
  WeCms.Modules.Organization WeCms.Modules.Organization.SqlSugar \
  WeCms.Modules.Configuration WeCms.Modules.Configuration.SqlSugar \
  WeCms.Modules.Audit WeCms.Modules.Audit.SqlSugar \
  WeCms.Modules.Security WeCms.Modules.Security.SqlSugar \
  WeCms.Modules.FileCenter WeCms.Modules.FileCenter.SqlSugar \
  WeCms.Modules.Platform; do
  project_exists "$project" && api_refs+=("$project")
done

assert_refs WeCms.Api "${api_refs[@]}"
assert_refs WeCms.Infrastructure WeCms.Shared
assert_refs WeCms.Modules.Cms WeCms.Shared
assert_refs WeCms.Modules.System WeCms.Shared
assert_refs WeCms.Persistence WeCms.Modules.System WeCms.Shared
assert_refs WeCms.Data.SqlSugar WeCms.Shared
assert_refs WeCms.Caching WeCms.Shared
assert_refs WeCms.EventBus WeCms.Shared
assert_refs WeCms.Aop WeCms.Caching WeCms.EventBus WeCms.Shared
for module in Identity AccessControl Organization Configuration Audit Security FileCenter Platform; do
  assert_refs "WeCms.Modules.$module" WeCms.Shared
done
for module in Identity AccessControl Organization Configuration Audit Security FileCenter; do
  assert_refs "WeCms.Modules.$module.SqlSugar" "WeCms.Modules.$module" WeCms.Data.SqlSugar WeCms.Shared
done
assert_refs WeCms.Shared

printf 'check-layer-dependency: ok\n'
