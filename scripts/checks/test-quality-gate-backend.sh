#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

source "${REPO_ROOT}/scripts/quality-gate-backend.sh"

assert_equals() {
  local expected="$1"
  local actual="$2"
  local message="$3"

  if [[ "$expected" != "$actual" ]]; then
    echo "${message}: expected '${expected}', got '${actual}'" >&2
    exit 1
  fi
}

assert_resolved_rid() {
  local uname_s="$1"
  local uname_m="$2"
  local expected="$3"
  local actual

  actual="$(
    WECMS_AOT_PUBLISH_RID="" \
    WECMS_UNAME_S_OVERRIDE="$uname_s" \
    WECMS_UNAME_M_OVERRIDE="$uname_m" \
    resolve_publish_rid
  )"

  assert_equals "$expected" "$actual" "RID detection failed for ${uname_s}/${uname_m}"
}

assert_resolved_rid "Darwin" "arm64" "osx-arm64"
assert_resolved_rid "Darwin" "x86_64" "osx-x64"
assert_resolved_rid "Linux" "x86_64" "linux-x64"
assert_resolved_rid "Linux" "aarch64" "linux-arm64"

override_rid="$(
  WECMS_AOT_PUBLISH_RID="linux-x64" \
  WECMS_UNAME_S_OVERRIDE="Darwin" \
  WECMS_UNAME_M_OVERRIDE="arm64" \
  resolve_publish_rid
)"
assert_equals "linux-x64" "$override_rid" "Explicit RID override should win"

if WECMS_AOT_PUBLISH_RID="" WECMS_UNAME_S_OVERRIDE="Plan9" WECMS_UNAME_M_OVERRIDE="mips64" resolve_publish_rid >/dev/null 2>&1; then
  echo "Unsupported host should fail RID detection" >&2
  exit 1
fi

echo "quality-gate-backend RID detection tests passed."
