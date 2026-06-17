#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
FRONTEND_DIR="${ROOT_DIR}/frontend/soybean-admin"

if [[ ! -d "${FRONTEND_DIR}" ]]; then
  echo "Missing frontend directory: ${FRONTEND_DIR}" >&2
  exit 1
fi

if grep -R -n -E '(/api/v1/cms|cms/article|cms/channel|cms/page|cms/tag)' "${FRONTEND_DIR}"; then
  echo "CMS frontend code is not allowed in M2-FE." >&2
  exit 1
fi
