#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
STATIC_ROUTES="${ROOT_DIR}/frontend/soybean-admin/src/router/static-routes.ts"

if [[ ! -f "${STATIC_ROUTES}" ]]; then
  echo "Missing static routes: ${STATIC_ROUTES}" >&2
  exit 1
fi

violations="$(
  awk '
    /path: "\/system\// {
      route=$0
      in_route=1
      has_permission=0
      next
    }
    in_route && /permissions: \["sys:[^"]+"/ {
      has_permission=1
      next
    }
    in_route && /^  }/ {
      if (!has_permission) {
        print route
      }
      in_route=0
      has_permission=0
    }
  ' "${STATIC_ROUTES}"
)"

if [[ -n "${violations}" ]]; then
  echo "All system routes must declare permission metadata." >&2
  echo "${violations}" >&2
  exit 1
fi
