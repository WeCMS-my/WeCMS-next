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

assert_contains() {
  local haystack="$1"
  local needle="$2"
  local message="$3"

  if [[ "$haystack" != *"$needle"* ]]; then
    echo "${message}: expected to find '${needle}' in '${haystack}'" >&2
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

captured_commands=()
captured_rm_targets=()

rm() {
  captured_rm_targets+=("$(join_command "$@")")
  return 0
}

run_gate_step() {
  local step_id="$1"
  local title="$2"
  shift 2
  captured_commands+=("$step_id|$(join_command "$@")")
  return 0
}

WECMS_AOT_PUBLISH_RID="osx-arm64"
WECMS_NUGET_HTTP_CACHE_PATH="/tmp/wecms-test-cache"
nuget_http_cache_path="$WECMS_NUGET_HTTP_CACHE_PATH"
run_backend >/dev/null

captured_command_blob="$(printf '%s\n' "${captured_commands[@]}")"
assert_equals "-f $REPO_ROOT/artifacts/openapi/wecms-api-v1.json" "${captured_rm_targets[0]}" \
  "Regression harness should only intercept the OpenAPI artifact cleanup path"
assert_contains "$captured_command_blob" '[4/17] dotnet publish (Native AOT)|run_with_dir '"$REPO_ROOT"' run_dotnet_with_cache publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r osx-arm64 /p:PublishAot=true --nologo' \
  "Native AOT publish step should use cache-aware dotnet wrapper"
assert_contains "$captured_command_blob" '[5/17] dotnet test (Unit)|run_with_dir '"$REPO_ROOT"' run_dotnet_with_cache test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --nologo --verbosity normal' \
  "Unit test step should use cache-aware dotnet wrapper"
assert_contains "$captured_command_blob" '[6/17] dotnet test (Integration)|run_with_dir '"$REPO_ROOT"' run_dotnet_with_cache test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --nologo --verbosity normal' \
  "Integration test step should use cache-aware dotnet wrapper"
assert_contains "$captured_command_blob" '[7/17] OpenAPI export|run_with_dir '"$REPO_ROOT"' run_dotnet_with_cache run --project backend/src/WeCms.Api -- --export-openapi '"$REPO_ROOT"'/artifacts/openapi/wecms-api-v1.json --nologo' \
  "OpenAPI export step should use cache-aware dotnet wrapper"
assert_contains "$captured_command_blob" '[9/17] dotnet test (Architecture)|run_with_dir '"$REPO_ROOT"' run_dotnet_with_cache test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --nologo --verbosity normal' \
  "Architecture test step should use cache-aware dotnet wrapper"

dotnet_invocations=()

dotnet() {
  dotnet_invocations+=("cache=${NUGET_HTTP_CACHE_PATH:-}<args>$(join_command "$@")")
}

run_dotnet_with_cache test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --nologo
run_dotnet_with_cache run --project backend/src/WeCms.Api -- --export-openapi "$REPO_ROOT/artifacts/openapi/wecms-api-v1.json" --nologo
run_dotnet_with_cache publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r osx-arm64 /p:PublishAot=true --nologo

assert_equals "cache=/tmp/wecms-test-cache<args>test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --nologo" "${dotnet_invocations[0]}" \
  "Cache wrapper should pass NUGET_HTTP_CACHE_PATH to dotnet test"
assert_equals "cache=/tmp/wecms-test-cache<args>run --project backend/src/WeCms.Api -- --export-openapi $REPO_ROOT/artifacts/openapi/wecms-api-v1.json --nologo" "${dotnet_invocations[1]}" \
  "Cache wrapper should pass NUGET_HTTP_CACHE_PATH to dotnet run"
assert_equals "cache=/tmp/wecms-test-cache<args>publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r osx-arm64 /p:PublishAot=true --nologo" "${dotnet_invocations[2]}" \
  "Cache wrapper should pass NUGET_HTTP_CACHE_PATH to dotnet publish"

echo "quality-gate-backend RID detection tests passed."
