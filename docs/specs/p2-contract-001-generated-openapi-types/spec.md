# P2-CONTRACT-001 Generated OpenAPI Types

## Scope

Replace the M2-FE placeholder frontend contract types with a deterministic TypeScript artifact generated from `artifacts/openapi/wecms-api-v1.json`.

## Requirements

- `frontend/soybean-admin/src/api/types/generated.ts` must not be accepted because it contains placeholder text.
- The frontend gate must regenerate the TypeScript contract into a temporary file and fail when the committed file differs.
- Generated output must include component schema fields, required versus optional properties, nullable properties, arrays, dictionaries, and operation request/response metadata.
- The generator must have no runtime dependency on the frontend app and must not add an npm dependency without a separate package/license review.
- Existing frontend imports such as `ApiResult<TData>` and component DTO names must remain source-compatible where the OpenAPI contract allows it.

## Non-Goals

- Do not fix manual API client return-type drift in this task; P2-CONTRACT-002 owns client declarations.
- Do not change backend OpenAPI generation or endpoint contracts in this task.
- Do not introduce frontend generated service clients.

## Validation

- `bash scripts/checks/check-api-contract-generated.sh`
- `pnpm --dir frontend/soybean-admin typecheck`
- `pnpm --dir frontend/soybean-admin build`
- `bash scripts/quality-gate-frontend.sh`
