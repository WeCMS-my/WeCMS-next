#!/usr/bin/env bash

# WeCMS database boundary architecture check.
# Ensures module layer does not bypass persistence boundary.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

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

echo "  Running DB boundary architecture tests..."

dotnet test \
  "$REPO_ROOT/backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj" \
  --no-restore \
  --no-build \
  --filter "FullyQualifiedName~WeCms.Tests.Architecture.PersistenceBoundaryTests" \
  --nologo \
  --verbosity minimal

echo "  DB boundary architecture checks passed."

