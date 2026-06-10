#!/usr/bin/env bash

# WeCMS M0-BE check-no-frontend-change
# Ensures no files under frontend/ were modified during M0-BE.
# This check verifies that the M0-BE backend-only constraint is respected.
#
# In CI: fails if frontend/ has any changes vs HEAD.
# Locally: warns if frontend/ has uncommitted changes.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

echo "  Checking frontend/ for M0-BE violations..."

pushd "$REPO_ROOT" >/dev/null

frontend_changes=$(git status --porcelain frontend/ || true)
if [ -n "$frontend_changes" ]; then
  echo "M0-BE VIOLATION: frontend/ has changes. M0-BE must not modify frontend/*." >&2
  echo "  Changed files:" >&2
  echo "$frontend_changes" >&2
  echo "  If these changes are intentional, they belong in M0.5-FE, not M0-BE." >&2
  popd >/dev/null
  exit 1
fi

popd >/dev/null

echo "  No frontend/ changes detected."

