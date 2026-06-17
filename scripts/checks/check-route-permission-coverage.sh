#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
STATIC_ROUTES="${ROOT_DIR}/frontend/soybean-admin/src/router/static-routes.ts"

if [[ ! -f "${STATIC_ROUTES}" ]]; then
  echo "Missing static routes: ${STATIC_ROUTES}" >&2
  exit 1
fi

SYSTEM_ROUTE_COUNT="$(grep -c 'path: "/system/' "${STATIC_ROUTES}" || true)"
SYSTEM_PERMISSION_COUNT="$(grep -c 'sys:.*:.*' "${STATIC_ROUTES}" || true)"

if [[ "${SYSTEM_ROUTE_COUNT}" -gt "${SYSTEM_PERMISSION_COUNT}" ]]; then
  echo "All system routes must declare permission metadata." >&2
  exit 1
fi
