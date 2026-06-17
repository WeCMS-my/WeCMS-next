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

required_schemas=(
  AuthMeResponse
  LoginResponse
  UserSummaryDto
  UserDetailDto
  RoleSummaryDto
  RoleDetailDto
  PermissionSummaryDto
  MenuTreeDto
  DepartmentTreeDto
  PostSummaryDto
  DictTypeSummaryDto
  DictValueDto
  SettingSummaryDto
  LoginLogSummaryDto
  AuditLogSummaryDto
  SecurityEventSummaryDto
  FileSummaryDto
  FileDetailDto
)

for schema in "${required_schemas[@]}"; do
  if ! grep -q "\"${schema}\":" "${OPENAPI_FILE}"; then
    echo "OpenAPI artifact is missing required schema: ${schema}" >&2
    exit 1
  fi

  if ! grep -Eq "(interface|type) ${schema}\\b" "${TYPES_FILE}"; then
    echo "Frontend generated.ts is missing required schema declaration: ${schema}" >&2
    exit 1
  fi
done

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
