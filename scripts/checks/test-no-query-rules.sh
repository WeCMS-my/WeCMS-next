#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
FIXTURES_ROOT="${REPO_ROOT}/scripts/checks/fixtures/p2-002"

run_check() {
  local name="$1"
  local script_path="$2"
  local target_root="$3"
  local should_fail="$4"

  if [ "$should_fail" = "true" ]; then
    if "$script_path" --repo-root "$target_root"; then
      echo "$name: expected fail, got exit code 0" >&2
      exit 1
    fi
    echo "  PASS: $name"
    return
  fi

  if ! "$script_path" --repo-root "$target_root"; then
    echo "$name: expected pass, got failure" >&2
    exit 1
  fi

  echo "  PASS: $name"
}

dynamic_check="${REPO_ROOT}/scripts/checks/check-no-dynamic-query.sh"
select_check="${REPO_ROOT}/scripts/checks/check-no-select-star.sh"

ok_root="${FIXTURES_ROOT}/ok"
bad_root="${FIXTURES_ROOT}/bad"

echo "Running check-no-dynamic-query positive/negative fixtures..."
run_check "Dynamic query check should pass on OK fixtures" "$dynamic_check" "$ok_root" false
run_check "Dynamic query check should fail on BAD fixtures" "$dynamic_check" "$bad_root" true

echo "Running check-no-select-star positive/negative fixtures..."
run_check "SELECT * check should pass on OK fixtures" "$select_check" "$ok_root" false
run_check "SELECT * check should fail on BAD fixtures" "$select_check" "$bad_root" true

echo "Scripts self-test fixture checks passed."
