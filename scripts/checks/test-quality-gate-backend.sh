#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
tmp_dir="$(mktemp -d)"
fake_bin="$tmp_dir/bin"
dotnet_log="$tmp_dir/dotnet.log"
strict_output="$tmp_dir/strict.out"
fallback_output="$tmp_dir/fallback.out"
ci_fallback_output="$tmp_dir/ci-fallback.out"
frontend_block_output="$tmp_dir/frontend-block.out"
frontend_scope_output="$tmp_dir/frontend-scope.out"
invalid_frontend_scope_output="$tmp_dir/invalid-frontend-scope.out"
real_git="$(command -v git)"

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

cat >"$fake_bin/git" <<EOF
#!/usr/bin/env bash
set -euo pipefail

if [[ "\${1:-}" == "-C" && "\${3:-}" == "status" && "\${4:-}" == "--short" && "\${5:-}" == "--" && "\${6:-}" == "frontend" ]]; then
  if [[ "\${QUALITY_GATE_TEST_FRONTEND_CHANGES:-}" == "true" ]]; then
    printf ' M frontend/soybean-admin/src/example.ts\n'
  fi
  exit 0
fi

exec "$real_git" "\$@"
EOF

chmod +x "$fake_bin/git"

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
  local frontend_scope="${6:-}"
  local frontend_changes="${7:-}"

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

  if [[ -n "$frontend_scope" ]]; then
    export WECMS_BACKEND_GATE_FRONTEND_SCOPE="$frontend_scope"
  else
    unset WECMS_BACKEND_GATE_FRONTEND_SCOPE
  fi

  if [[ -n "$frontend_changes" ]]; then
    export QUALITY_GATE_TEST_FRONTEND_CHANGES="$frontend_changes"
  else
    unset QUALITY_GATE_TEST_FRONTEND_CHANGES
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

run_gate "" "$strict_output" 1 "true" "true"
assert_contains "$dotnet_log" "restore backend/WeCms.slnx"
assert_not_contains "$dotnet_log" "-p:NuGetAudit=false"

run_gate "fallback" "$fallback_output" 0
assert_contains "$fallback_output" "quality-gate-backend: WARNING local-only fallback dotnet mode enabled with -p:NuGetAudit=false and NUGET_HTTP_CACHE_PATH="
assert_contains "$dotnet_log" "restore backend/WeCms.slnx -p:NuGetAudit=false"
assert_contains "$dotnet_log" "CACHE="
assert_contains "$dotnet_log" "build backend/WeCms.slnx -warnaserror --nologo --no-restore"
assert_contains "$dotnet_log" "publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false --nologo"
assert_contains "$fallback_output" "check-system-permission-coverage"
assert_contains "$fallback_output" "check-system-openapi-coverage"
assert_contains "$fallback_output" "check-write-endpoint-methods"
assert_contains "$fallback_output" "check-write-endpoint-permission-coverage"
assert_contains "$fallback_output" "check-write-endpoint-audit-coverage"
assert_contains "$fallback_output" "check-no-sql-in-modules"
assert_contains "$fallback_output" "check-security-event-coverage"
assert_contains "$fallback_output" "check-cookie-auth-origin-protection"
assert_contains "$fallback_output" "check-admingate-csrf-migration"
assert_contains "$fallback_output" "check-thinkphp-feature-delta"
assert_contains "$fallback_output" "check-foundation-freeze-baseline"
assert_contains "$fallback_output" "check-production-config-baseline"
assert_contains "$fallback_output" "check-security-baseline"
assert_contains "$fallback_output" "check-database-governance"
assert_contains "$fallback_output" "check-observability-baseline"
assert_contains "$fallback_output" "quality-gate-backend: ok"

run_gate "fallback" "$ci_fallback_output" 1 "true" "true"
assert_contains "$ci_fallback_output" "quality-gate-backend: WECMS_NUGET_AUDIT_MODE=fallback is local-only and must not be used in CI or release gates."

run_gate "fallback" "$frontend_block_output" 1 "" "" "" "true"
assert_contains "$frontend_block_output" "check-no-frontend-change: frontend changes are not allowed in M0-BE"
assert_contains "$frontend_block_output" "frontend/soybean-admin/src/example.ts"

run_gate "fallback" "$frontend_scope_output" 0 "" "" "includes-frontend" "true"
assert_contains "$frontend_scope_output" "check-no-frontend-change: skipped because WECMS_BACKEND_GATE_FRONTEND_SCOPE=includes-frontend"
assert_contains "$frontend_scope_output" "quality-gate-backend: ok"

run_gate "fallback" "$invalid_frontend_scope_output" 1 "" "" "frontend"
assert_contains "$invalid_frontend_scope_output" "quality-gate-backend: WECMS_BACKEND_GATE_FRONTEND_SCOPE must be backend-only or includes-frontend."

printf 'test-quality-gate-backend: ok\n'
