#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

fail() {
  printf 'check-generated-test-artifacts: %s\n' "$1" >&2
  exit 1
}

command -v rg >/dev/null 2>&1 || fail 'rg is required. Install ripgrep before running this check.'

cd "$repo_root"

artifacts=()
while IFS= read -r -d '' path; do
  if [[ -e "$path" ]]; then
    artifacts+=("$path")
  fi
done < <(
  git ls-files -z \
    'backend/tests/**/TestResults/**' \
    '*.trx' \
    '*.coverage' \
    '*.coveragexml' \
    'vstest.diag*.log'
)

if ((${#artifacts[@]} > 0)); then
  printf '%s\n' "${artifacts[@]}" >&2
  fail 'generated test artifacts must not be tracked.'
fi

printf 'check-generated-test-artifacts: ok\n'
