# P2-FE-004 Request Non-JSON Error Handling

## Scope

Make the frontend request wrapper robust when the backend or infrastructure returns empty or non-JSON responses.

## Requirements

- Do not call `response.json()` directly before checking parse safety.
- Empty responses must become an `ApiResult`-shaped error with status and status text.
- Non-JSON responses must become an `ApiResult`-shaped error with a bounded message.
- 401 refresh retry must still run when the original response is non-JSON.
- Blob request error handling must use the same safe API result parser.
- Do not reshape valid backend business `data`.

## Non-Goals

- No UI error state changes in this task.
- No auth storage model changes.
- No backend contract changes.

## Validation

- `pnpm --dir frontend/soybean-admin typecheck`
- `pnpm --dir frontend/soybean-admin build`
- `bash scripts/quality-gate-frontend.sh`
