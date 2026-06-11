#!/usr/bin/env bash

# WeCMS M0-BE endpoint permission integrity check
# Uses runtime architecture tests instead of source-text scanning.

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

echo "  Running PermissionMetadata architecture tests..."

dotnet test \
  "$REPO_ROOT/backend/tests/WeCms.Tests.Architecture/WeCms.Tests.Architecture.csproj" \
  --filter "FullyQualifiedName~WeCms.Tests.Architecture.PermissionMetadataScanTests.AllAuthenticatedEndpoints_ShouldHave_PermissionMetadata_OrBeExempt" \
  --nologo \
  --verbosity minimal

echo "  Endpoint permission metadata checks passed."
