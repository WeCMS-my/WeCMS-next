#!/usr/bin/env bash

# WeCMS M0-BE Backend Quality Gate
# Validates: build, test, AOT publish, OpenAPI export, code quality checks.
# M0-BE constraint: does NOT run pnpm or touch frontend/.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../" && pwd)"
json_mode=false
command_name="backend"
backend_checks=()
publish_rid="${WECMS_AOT_PUBLISH_RID:-}"
nuget_http_cache_path="${WECMS_NUGET_HTTP_CACHE_PATH:-${TMPDIR:-/tmp}/nuget-http-cache}"

json_escape() {
  local value="$1"
  value="${value//\\/\\\\}"
  value="${value//\"/\\\"}"
  value="${value//$'\n'/\\n}"
  value="${value//$'\r'/\\r}"
  value="${value//$'\t'/\\t}"
  printf '"%s"' "$value"
}

join_command() {
  local out=""
  local first=true part
  for part in "$@"; do
    if [[ "$first" == true ]]; then
      first=false
    else
      out+=" "
    fi
    out+="$(printf '%q' "$part")"
  done
  printf '%s' "$out"
}

json_array() {
  local -n arr=$1
  local output="["
  local first=true item

  for item in "${arr[@]}"; do
    if [[ "$first" == true ]]; then
      first=false
    else
      output+=","
    fi
    output+="$item"
  done
  output+="]"
  printf '%s' "$output"
}

run_with_dir() {
  local dir="$1"
  shift
  pushd "$dir" >/dev/null
  "$@"
  local rc=$?
  popd >/dev/null
  return $rc
}

run_dotnet_with_cache() {
  NUGET_HTTP_CACHE_PATH="$nuget_http_cache_path" dotnet "$@"
}

get_uname_s() {
  printf '%s\n' "${WECMS_UNAME_S_OVERRIDE:-$(uname -s)}"
}

get_uname_m() {
  printf '%s\n' "${WECMS_UNAME_M_OVERRIDE:-$(uname -m)}"
}

detect_host_publish_rid() {
  local uname_s
  local uname_m
  uname_s="$(get_uname_s)"
  uname_m="$(get_uname_m)"

  case "$uname_s" in
    Darwin)
      case "$uname_m" in
        arm64)
          printf 'osx-arm64\n'
          return 0
          ;;
        x86_64)
          printf 'osx-x64\n'
          return 0
          ;;
      esac
      ;;
    Linux)
      case "$uname_m" in
        x86_64|amd64)
          printf 'linux-x64\n'
          return 0
          ;;
        arm64|aarch64)
          printf 'linux-arm64\n'
          return 0
          ;;
      esac
      ;;
  esac

  return 1
}

resolve_publish_rid() {
  local explicit_publish_rid="${WECMS_AOT_PUBLISH_RID:-$publish_rid}"

  if [[ -n "$explicit_publish_rid" ]]; then
    printf '%s\n' "$explicit_publish_rid"
    return 0
  fi

  detect_host_publish_rid
}

published_api_executable_name() {
  case "$(get_uname_s)" in
    MINGW*|MSYS*|CYGWIN*)
      printf 'WeCms.Api.exe\n'
      ;;
    *)
      printf 'WeCms.Api\n'
      ;;
  esac
}

published_api_executable_path() {
  local rid="$1"
  printf '%s\n' "$REPO_ROOT/backend/src/WeCms.Api/bin/Release/net10.0/$rid/publish/$(published_api_executable_name)"
}

usage() {
  cat <<'EOF'
Usage:
  bash scripts/quality-gate-backend.sh [--json] [command]

Environment:
  WECMS_AOT_PUBLISH_RID  Override the Native AOT publish RID.
                         Default: current host RID (CI on Linux resolves to linux-x64).

Commands:
  backend   Run backend quality gate (build/test/publish/openapi/arch checks)
  di        Run DI boundary static scan and output fix list
  code-review
           Run code review checklist static scan (DI + file governance checks)
  all       Run backend gate then DI scan

If no command provided, runs backend.
EOF
}

run_step() {
  local step="$1"
  local title="$2"
  shift 2

  if [[ "$json_mode" == "false" ]]; then
    echo "$step"
  fi

  set +e
  "$@"
  local rc=$?
  set -e

  local cmd_desc
  cmd_desc="$(join_command "$@")"
  if (( rc == 0 )); then
    backend_checks+=("{\"step\":\"$title\",\"command\":$(json_escape "$cmd_desc"),\"status\":\"passed\"}")
  else
    backend_checks+=("{\"step\":\"$title\",\"command\":$(json_escape "$cmd_desc"),\"status\":\"failed\"}")
  fi

  if (( rc != 0 )); then
    return $rc
  fi

  return 0
}

run_gate_step() {
  local step_id="$1"
  local title="$2"
  shift 2

  if ! run_step "$step_id" "$title" "$@"; then
    if [[ "$json_mode" == "true" ]]; then
      print_json_output "failed"
    else
      echo "=== WeCMS M0-BE Backend Quality Gate FAILED ==="
    fi
    return 1
  fi

  if [[ "$json_mode" == "false" ]]; then
    echo "  PASSED"
  fi
  return 0
}

print_json_output() {
  local status="$1"
  printf '%s\n' "{\"command\":\"backend\",\"status\":\"$status\",\"checks\":$(json_array backend_checks)}"
}

run_code_review_scan() {
  echo "==> Run code-review checklist scan"
  run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-code-review.sh"
}

run_backend() {
  local resolved_publish_rid
  if ! resolved_publish_rid="$(resolve_publish_rid)"; then
    echo "Unable to determine Native AOT publish RID for host $(get_uname_s)/$(get_uname_m)." >&2
    echo "Set WECMS_AOT_PUBLISH_RID explicitly and rerun the quality gate." >&2
    return 1
  fi

  if [[ "$json_mode" == "false" ]]; then
    echo "=== WeCMS M0-BE Backend Quality Gate ==="
    echo "Native AOT publish RID: ${resolved_publish_rid}"
  fi

  mkdir -p "$nuget_http_cache_path"

  if ! run_gate_step "[1/17] dotnet build -warnaserror" "[1/17] dotnet build -warnaserror" \
    run_with_dir "$REPO_ROOT" dotnet build backend/WeCms.slnx -warnaserror --nologo; then
    return 1
  fi

  if ! run_gate_step "[2/17] AOT exception baseline check (ADR-0006)" "[2/17] AOT exception baseline check (ADR-0006)" \
    run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-aot-exception-baseline.sh"; then
    return 1
  fi

  if ! run_gate_step "[3/17] AOT self-warning suppression check (ADR-0006)" "[3/17] AOT self-warning suppression check (ADR-0006)" \
    run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-no-self-aot-suppression.sh"; then
    return 1
  fi

  if ! run_gate_step "[4/18] dotnet publish (Native AOT)" "[4/18] dotnet publish (Native AOT)" \
    run_with_dir "$REPO_ROOT" run_dotnet_with_cache publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r "$resolved_publish_rid" /p:PublishAot=true --nologo; then
    return 1
  fi

  local published_openapi_path="${TMPDIR:-/tmp}/wecms-openapi-aot-$$.json"
  if ! run_gate_step "[5/18] OpenAPI export (published Native AOT binary)" "[5/18] OpenAPI export (published Native AOT binary)" \
    run_with_dir "$REPO_ROOT" "$(published_api_executable_path "$resolved_publish_rid")" --export-openapi "$published_openapi_path"; then
    return 1
  fi

  if ! run_gate_step "[6/18] dotnet test (Unit)" "[6/18] dotnet test (Unit)" \
    run_with_dir "$REPO_ROOT" run_dotnet_with_cache test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --nologo --verbosity normal; then
    return 1
  fi

  if ! run_gate_step "[7/18] dotnet test (Integration)" "[7/18] dotnet test (Integration)" \
    run_with_dir "$REPO_ROOT" run_dotnet_with_cache test backend/tests/WeCms.Tests.Integration/WeCms.Tests.Integration.csproj --nologo --verbosity normal; then
    return 1
  fi

  # P0-1: 先删除旧 artifact，强制验证 export 能从零生成
  rm -f "$REPO_ROOT/artifacts/openapi/wecms-api-v1.json"

  if ! run_gate_step "[8/18] OpenAPI export" "[8/18] OpenAPI export" \
    run_with_dir "$REPO_ROOT" run_dotnet_with_cache run --project backend/src/WeCms.Api -- --export-openapi "$REPO_ROOT/artifacts/openapi/wecms-api-v1.json" --nologo; then
    return 1
  fi

  if ! run_gate_step "[9/18] OpenAPI auth request body check" "[9/18] OpenAPI auth request body check" \
    run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-openapi-auth-request-bodies.sh"; then
    return 1
  fi

  if ! run_gate_step "[10/18] dotnet test (Architecture)" "[10/18] dotnet test (Architecture)" \
    run_with_dir "$REPO_ROOT" run_dotnet_with_cache test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --nologo --verbosity normal; then
    return 1
  fi

  if ! run_gate_step "[11/18] check-no-select-star" "[11/18] check-no-select-star" \
    run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-no-select-star.sh"; then
    return 1
  fi

  if ! run_gate_step "[12/18] check-no-dynamic-query" "[12/18] check-no-dynamic-query" \
    run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-no-dynamic-query.sh"; then
    return 1
  fi

  if ! run_gate_step "[13/18] check endpoint permissions (runtime architecture test)" "[13/18] check endpoint permissions (runtime architecture test)" \
    run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-endpoint-permissions.sh"; then
    return 1
  fi

  if ! run_gate_step "[14/18] check layer dependency matrix" "[14/18] check layer dependency matrix" \
    run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-layer-dependency.sh"; then
    return 1
  fi

  if ! run_gate_step "[15/18] check db boundary (architecture)" "[15/18] check db boundary (architecture)" \
    run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-db-boundary.sh"; then
    return 1
  fi

  if ! run_gate_step "[16/18] check integrity - json context coverage" "[16/18] check integrity - json context coverage" \
    run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-json-context-coverage.sh"; then
    return 1
  fi

  if ! run_gate_step "[17/18] check integrity - no frontend change" "[17/18] check integrity - no frontend change" \
    run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-no-frontend-change.sh"; then
    return 1
  fi

  if ! run_gate_step "[18/18] check code-review" "[18/18] check code-review" \
    run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-code-review.sh"; then
    return 1
  fi

  if [[ "$json_mode" == "true" ]]; then
    print_json_output "passed"
    return 0
  fi

  echo "=== WeCMS M0-BE Backend Quality Gate PASSED ==="
}

run_di_scan() {
  echo "==> Run DI boundary scan"
  bash "${SCRIPT_DIR}/review-di.sh" "${REPO_ROOT}"
}

run_code_review() {
  run_code_review_scan
}

main() {
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --json)
        json_mode=true
        shift
        ;;
      backend|di|code-review|all)
        command_name="$1"
        shift
        ;;
      -h|--help|help)
        usage
        exit 0
        ;;
      *)
        echo "Unknown argument: $1" >&2
        usage
        exit 2
        ;;
    esac
  done

  case "$command_name" in
    backend)
      run_backend
      ;;
    di)
      run_di_scan
      ;;
    code-review)
      run_code_review
      ;;
    all)
      run_backend
      run_di_scan
      ;;
  esac
}

if [[ "${BASH_SOURCE[0]}" == "$0" ]]; then
  main "$@"
fi
