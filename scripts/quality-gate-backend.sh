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

usage() {
  cat <<'EOF'
Usage:
  bash scripts/quality-gate-backend.sh [--json] [command]

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
  if [[ "$json_mode" == "false" ]]; then
    echo "=== WeCMS M0-BE Backend Quality Gate ==="
  fi

  if ! run_gate_step "[1/15] dotnet build -warnaserror" "[1/15] dotnet build -warnaserror" \
    run_with_dir "$REPO_ROOT" dotnet build backend/WeCms.slnx -warnaserror --nologo; then
    return 1
  fi

  if ! run_gate_step "[2/15] AOT exception baseline check (ADR-0006)" "[2/15] AOT exception baseline check (ADR-0006)" \
    run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-aot-exception-baseline.sh"; then
    return 1
  fi

  if ! run_gate_step "[3/15] AOT self-warning suppression check (ADR-0006)" "[3/15] AOT self-warning suppression check (ADR-0006)" \
    run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-no-self-aot-suppression.sh"; then
    return 1
  fi

  if ! run_gate_step "[4/15] dotnet publish (Native AOT)" "[4/15] dotnet publish (Native AOT)" \
    run_with_dir "$REPO_ROOT" dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true --nologo; then
    return 1
  fi

  if ! run_gate_step "[5/15] dotnet test (Unit)" "[5/15] dotnet test (Unit)" \
    run_with_dir "$REPO_ROOT" dotnet test backend/tests/WeCms.Tests.Unit/WeCms.Tests.Unit.csproj --nologo --verbosity normal; then
    return 1
  fi

  if ! run_gate_step "[5b/15] dotnet test (Architecture)" "[5b/15] dotnet test (Architecture)" \
    run_with_dir "$REPO_ROOT" dotnet test backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj --nologo --verbosity normal; then
    return 1
  fi

  if ! run_gate_step "[6/15] OpenAPI export" "[6/15] OpenAPI export" \
    run_with_dir "$REPO_ROOT" dotnet run --project backend/src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-api-v1.json --nologo; then
    return 1
  fi

  if ! run_gate_step "[7/15] OpenAPI auth request body check" "[7/15] OpenAPI auth request body check" \
    run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-openapi-auth-request-bodies.sh"; then
    return 1
  fi

  if ! run_gate_step "[8/15] check-no-select-star" "[8/15] check-no-select-star" \
    run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-no-select-star.sh"; then
    return 1
  fi

  if ! run_gate_step "[9/15] check-no-dynamic-query" "[9/15] check-no-dynamic-query" \
    run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-no-dynamic-query.sh"; then
    return 1
  fi

  if ! run_gate_step "[10/15] check endpoint permissions (runtime architecture test)" "[10/15] check endpoint permissions (runtime architecture test)" \
    run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-endpoint-permissions.sh"; then
    return 1
  fi

  if ! run_gate_step "[11/15] check layer dependency matrix" "[11/15] check layer dependency matrix" \
    run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-layer-dependency.sh"; then
    return 1
  fi

  if ! run_gate_step "[12/15] check db boundary (architecture)" "[12/15] check db boundary (architecture)" \
    run_with_dir "$REPO_ROOT" bash "$SCRIPT_DIR/checks/check-db-boundary.sh"; then
    return 1
  fi

  if ! run_gate_step "[13/15] check integrity - json context coverage" "[13/15] check integrity - json context coverage" \
    run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-json-context-coverage.sh"; then
    return 1
  fi

  if ! run_gate_step "[14/15] check integrity - no frontend change" "[14/15] check integrity - no frontend change" \
    run_with_dir "$REPO_ROOT" "$SCRIPT_DIR/checks/check-no-frontend-change.sh"; then
    return 1
  fi

  if ! run_gate_step "[15/15] check code-review" "[15/15] check code-review" \
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
