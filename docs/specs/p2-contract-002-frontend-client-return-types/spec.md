# P2-CONTRACT-002 Frontend Client Return Types

## Scope

Align frontend API client return types with the accepted OpenAPI response data contract for command-style operations.

## Requirements

- If OpenAPI declares an operation response as `ApiResult<Object>` and the frontend does not consume response data, the client must return `Promise<ApiResult<unknown>>`.
- Create/update operations that return mutation response DTOs must keep their typed mutation response.
- Do not change backend endpoint contracts to match frontend assumptions.
- Do not broaden request interceptor behavior or reshape business `data`.

## Non-Goals

- No backend code changes.
- No UI workflow changes.
- No generated client implementation.

## Validation

- `pnpm --dir frontend/soybean-admin typecheck`
- `pnpm --dir frontend/soybean-admin build`
- `bash scripts/quality-gate-frontend.sh`
