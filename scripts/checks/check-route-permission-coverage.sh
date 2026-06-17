#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
STATIC_ROUTES="${ROOT_DIR}/frontend/soybean-admin/src/router/static-routes.ts"

if [[ ! -f "${STATIC_ROUTES}" ]]; then
  echo "Missing static routes: ${STATIC_ROUTES}" >&2
  exit 1
fi

if grep -n 'path: "/system/' "${STATIC_ROUTES}" | grep -v 'permissions:'; then
  echo "All system routes must declare permission metadata." >&2
  exit 1
fi
