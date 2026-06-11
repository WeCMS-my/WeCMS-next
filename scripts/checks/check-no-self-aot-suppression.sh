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

while IFS= read -r line; do
  if [[ "$line" == *"IL2026"* || "$line" == *"IL3050"* ]]; then
    matches=1
    echo "$line"
  fi
done < <(rg -n --no-heading -g '*.cs' -g '*.csproj' \
  "(NoWarn>|WarningsAsErrors>|#pragma warning disable IL2026|#pragma warning disable IL3050|#pragma warning disable 2026|#pragma warning disable 3050)" \
  "$REPO_ROOT/backend/src" "$REPO_ROOT/backend/tests")

if [ "$matches" -ne 0 ]; then
  echo "Detected self-owned IL2026/IL3050 suppression usage. ADR-0006 requires only Dapper assembly IL2104/IL3053 exceptions." >&2
  exit 1
fi

echo "  No self-owned IL2026/IL3050 suppressions detected in source."
