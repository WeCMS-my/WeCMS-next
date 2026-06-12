#!/usr/bin/env bash

# WeCMS M0-BE guardrail for self-owned AOT warning suppression.
# Self-owned code must not add IL2026 / IL3050 suppressions.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${REPO_ROOT:-$(cd "${SCRIPT_DIR}/../.." && pwd)}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo-root)
      REPO_ROOT="${2:?missing value for --repo-root}"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      echo "Usage: $0 [--repo-root <path>]" >&2
      exit 1
      ;;
  esac
done

matches=0

report_matches() {
  local description="$1"
  shift
  local output

  if ! output="$("$@")"; then
    return
  fi

  if [[ -n "$output" ]]; then
    matches=1
    printf '%s\n' "$output"
  fi
}

report_matches "NoWarn or pragma suppressions" \
  rg -n --no-heading -U -P -g '*.cs' -g '*.csproj' \
    '(?s)<(?:NoWarn|WarningsAsErrors)>[^<]*(?:IL2026|IL3050)[^<]*</(?:NoWarn|WarningsAsErrors)>|#pragma warning disable IL2026|#pragma warning disable IL3050|#pragma warning disable 2026|#pragma warning disable 3050' \
    "$REPO_ROOT/backend/src" "$REPO_ROOT/backend/tests"

report_matches "Attribute suppressions" \
  rg -n --no-heading -U -P -g '*.cs' \
    '(?s)\[(?:UnconditionalSuppressMessage|SuppressMessage)\s*\(\s*"[^"]*(?:Trimming|AOT)[^"]*"\s*,\s*"[^"]*(?:IL2026|IL3050)[^"]*"' \
    "$REPO_ROOT/backend/src" "$REPO_ROOT/backend/tests"

report_matches "Trim/AOT dependency preservation attributes" \
  rg -n --no-heading -g '*.cs' \
    '\[DynamicDependency\s*\(' \
    "$REPO_ROOT/backend/src" "$REPO_ROOT/backend/tests"

if [ "$matches" -ne 0 ]; then
  echo "Detected self-owned AOT/trim suppression or dependency-preservation usage. ADR-0006 requires only Dapper assembly IL2104/IL3053 exceptions." >&2
  exit 1
fi

echo "  No self-owned IL2026/IL3050 suppressions detected in source."
