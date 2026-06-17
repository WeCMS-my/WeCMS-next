#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OPENAPI_FILE="${ROOT_DIR}/artifacts/openapi/wecms-api-v1.json"
TYPES_FILE="${ROOT_DIR}/frontend/soybean-admin/src/api/types/generated.ts"

if [[ ! -f "${OPENAPI_FILE}" ]]; then
  echo "Missing OpenAPI artifact: ${OPENAPI_FILE}" >&2
  exit 1
fi

if [[ ! -f "${TYPES_FILE}" ]]; then
  echo "Missing frontend API types: ${TYPES_FILE}" >&2
  exit 1
fi

if ! grep -q 'OpenAPI-aligned placeholder' "${TYPES_FILE}"; then
  echo "generated.ts must declare its current OpenAPI alignment status." >&2
  exit 1
fi

if ! grep -q 'LoginResponse' "${TYPES_FILE}" || ! grep -q 'AuthMeResponse' "${TYPES_FILE}"; then
  echo "generated.ts must include current auth response contracts." >&2
  exit 1
fi
