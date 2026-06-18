#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
FRONTEND_SRC_DIR="${ROOT_DIR}/frontend/soybean-admin/src"

if [[ ! -d "${FRONTEND_SRC_DIR}" ]]; then
  echo "Missing frontend source directory: ${FRONTEND_SRC_DIR}" >&2
  exit 1
fi

if rg -n 'v-html|innerHTML|outerHTML|insertAdjacentHTML' "${FRONTEND_SRC_DIR}"; then
  echo "Unsafe raw HTML rendering is not allowed in the frontend." >&2
  exit 1
fi

echo "check-no-v-html: ok"
