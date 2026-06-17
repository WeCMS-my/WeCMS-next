#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OPENAPI_FILE="${ROOT_DIR}/artifacts/openapi/wecms-api-v1.json"
TYPES_FILE="${ROOT_DIR}/frontend/soybean-admin/src/api/types/generated.ts"
GENERATOR="${ROOT_DIR}/scripts/generate-openapi-types.py"

if [[ ! -f "${OPENAPI_FILE}" ]]; then
  echo "Missing OpenAPI artifact: ${OPENAPI_FILE}" >&2
  exit 1
fi

if [[ ! -f "${TYPES_FILE}" ]]; then
  echo "Missing frontend API types: ${TYPES_FILE}" >&2
  exit 1
fi

if [[ ! -f "${GENERATOR}" ]]; then
  echo "Missing OpenAPI type generator: ${GENERATOR}" >&2
  exit 1
fi

if grep -q 'OpenAPI-aligned placeholder' "${TYPES_FILE}"; then
  echo "generated.ts must be generated from OpenAPI, not accepted as a placeholder." >&2
  exit 1
fi

generated_tmp="$(mktemp)"
cleanup() {
  rm -f "${generated_tmp}"
}
trap cleanup EXIT

python3 "${GENERATOR}" "${OPENAPI_FILE}" "${generated_tmp}"

if ! diff -u "${generated_tmp}" "${TYPES_FILE}" >/dev/null; then
  echo "generated.ts is stale or hand-edited. Regenerate it with:" >&2
  echo "python3 scripts/generate-openapi-types.py artifacts/openapi/wecms-api-v1.json frontend/soybean-admin/src/api/types/generated.ts" >&2
  diff -u "${generated_tmp}" "${TYPES_FILE}" >&2 || true
  exit 1
fi

required_api_clients=(
  auth.ts
  menu.ts
  users.ts
  roles.ts
  permissions.ts
  depts.ts
  posts.ts
  dicts.ts
  settings.ts
  logs.ts
  files.ts
)

for client in "${required_api_clients[@]}"; do
  if ! find "${ROOT_DIR}/frontend/soybean-admin/src/api" -name "${client}" -type f | grep -q .; then
    echo "Missing frontend API client: ${client}" >&2
    exit 1
  fi
done
