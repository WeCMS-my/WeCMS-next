#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
FRONTEND_DIR="${ROOT_DIR}/frontend/soybean-admin"

if ! command -v pnpm >/dev/null 2>&1; then
  echo "pnpm is required. Run: corepack prepare pnpm@10.5.0 --activate" >&2
  exit 1
fi

if [[ ! -f "${FRONTEND_DIR}/package.json" ]]; then
  echo "Missing frontend project: ${FRONTEND_DIR}/package.json" >&2
  exit 1
fi

pnpm --dir "${FRONTEND_DIR}" install --frozen-lockfile
pnpm --dir "${FRONTEND_DIR}" lint
pnpm --dir "${FRONTEND_DIR}" typecheck
pnpm --dir "${FRONTEND_DIR}" build
bash "${ROOT_DIR}/scripts/checks/check-no-cms-frontend.sh"
bash "${ROOT_DIR}/scripts/checks/check-api-contract-generated.sh"
bash "${ROOT_DIR}/scripts/checks/check-route-permission-coverage.sh"
bash "${ROOT_DIR}/scripts/checks/check-frontend-smoke-fixtures.sh"
