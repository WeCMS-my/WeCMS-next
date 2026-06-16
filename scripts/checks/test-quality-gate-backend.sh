#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
tmp_dir="$(mktemp -d)"
fake_bin="$tmp_dir/bin"
dotnet_log="$tmp_dir/dotnet.log"
strict_output="$tmp_dir/strict.out"
fallback_output="$tmp_dir/fallback.out"
ci_fallback_output="$tmp_dir/ci-fallback.out"

cleanup() {
  rm -rf "$tmp_dir"
}

trap cleanup EXIT

mkdir -p "$fake_bin"

cat >"$fake_bin/dotnet" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

printf '%s\n' "$*" >> "${QUALITY_GATE_TEST_DOTNET_LOG:?}"
printf 'CACHE=%s\n' "${NUGET_HTTP_CACHE_PATH:-}" >> "${QUALITY_GATE_TEST_DOTNET_LOG:?}"

if [[ "${1:-}" == "restore" && "$*" != *"-p:NuGetAudit=false"* ]]; then
  printf 'error NU1900: Warning As Error: Error occurred while getting package vulnerability data: Access to the path '\''/tmp/vuln_index.dat-new'\'' is denied.\n' >&2
  exit 1
fi

if [[ "${1:-}" == "run" ]]; then
  args=("$@")
  for ((index=0; index<${#args[@]}; index++)); do
    if [[ "${args[$index]}" == "--export-openapi" ]]; then
      output_path="${args[$((index + 1))]}"
      source_path="$(cd "$(dirname "${QUALITY_GATE_TEST_OPENAPI_SOURCE:?}")" && pwd)/$(basename "${QUALITY_GATE_TEST_OPENAPI_SOURCE:?}")"
      resolved_output_path="$(cd "$(dirname "$output_path")" && pwd)/$(basename "$output_path")"
      if [[ "$source_path" != "$resolved_output_path" ]]; then
        cp "${QUALITY_GATE_TEST_OPENAPI_SOURCE:?}" "$output_path"
      fi
      break
    fi
  done
fi
EOF

chmod +x "$fake_bin/dotnet"

assert_contains() {
  local file="$1"
  local pattern="$2"
  if ! rg -q --fixed-strings -- "$pattern" "$file"; then
    printf 'assert_contains failed: %s missing %s\n' "$file" "$pattern" >&2
    cat "$file" >&2
    exit 1
  fi
}

assert_not_contains() {
  local file="$1"
  local pattern="$2"
  if rg -q --fixed-strings -- "$pattern" "$file"; then
    printf 'assert_not_contains failed: %s unexpectedly contains %s\n' "$file" "$pattern" >&2
    cat "$file" >&2
    exit 1
  fi
}

run_gate() {
  local mode="$1"
  local output_file="$2"
  local expected_status="$3"
  local ci_value="${4:-}"
  local gha_value="${5:-}"

  : >"$dotnet_log"

  export PATH="$fake_bin:$PATH"
  export QUALITY_GATE_TEST_DOTNET_LOG="$dotnet_log"
  export QUALITY_GATE_TEST_OPENAPI_SOURCE="$repo_root/artifacts/openapi/wecms-api-v1.json"
  export WECMS_TEST_MYSQL_CONNECTION_STRING='server=fake;uid=fake;pwd=fake;database=fake'

  if [[ -n "$mode" ]]; then
    export WECMS_NUGET_AUDIT_MODE="$mode"
  else
    unset WECMS_NUGET_AUDIT_MODE
  fi

  if [[ -n "$ci_value" ]]; then
    export CI="$ci_value"
  else
    unset CI
  fi

  if [[ -n "$gha_value" ]]; then
    export GITHUB_ACTIONS="$gha_value"
  else
    unset GITHUB_ACTIONS
  fi

  set +e
  bash "$repo_root/scripts/quality-gate-backend.sh" >"$output_file" 2>&1
  local status=$?
  set -e

  if [[ $status -ne $expected_status ]]; then
    printf 'expected exit %s but got %s\n' "$expected_status" "$status" >&2
    cat "$output_file" >&2
    exit 1
  fi
}

run_gate "" "$strict_output" 1
assert_contains "$dotnet_log" "restore backend/WeCms.slnx"
assert_not_contains "$dotnet_log" "-p:NuGetAudit=false"

run_gate "fallback" "$fallback_output" 0
assert_contains "$fallback_output" "quality-gate-backend: WARNING local-only fallback dotnet mode enabled with -p:NuGetAudit=false and NUGET_HTTP_CACHE_PATH="
assert_contains "$dotnet_log" "restore backend/WeCms.slnx -p:NuGetAudit=false"
assert_contains "$dotnet_log" "CACHE="
assert_contains "$dotnet_log" "build backend/WeCms.slnx -warnaserror --nologo --no-restore"
assert_contains "$dotnet_log" "publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false --nologo"
assert_contains "$fallback_output" "quality-gate-backend: ok"

run_gate "fallback" "$ci_fallback_output" 1 "true" "true"
assert_contains "$ci_fallback_output" "quality-gate-backend: WECMS_NUGET_AUDIT_MODE=fallback is local-only and must not be used in CI or release gates."

printf 'test-quality-gate-backend: ok\n'
