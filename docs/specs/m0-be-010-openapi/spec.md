# M0-BE-010 OpenAPI Export Spec

## Scope

Implement a deterministic backend-only OpenAPI export command:

```bash
dotnet run --project backend/src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-api-v1.json
```

## Requirements

- Export must run before normal `WebApplication.CreateSlimBuilder(args)` startup.
- Export must write `artifacts/openapi/wecms-api-v1.json`.
- Auth endpoints must include `requestBody`:
  - `POST /api/v1/auth/login`
  - `POST /api/v1/auth/refresh`
  - `POST /api/v1/auth/logout`
- Auth response schema must exist.
- System API paths must exist.
- `GET /api/v1/system/secure-ping` must include security metadata and permission metadata.
- Export must use `System.Text.Json`, not Newtonsoft.Json.

## Environment Constraint

The current local .NET 10 runtime hangs when creating `WebApplication.CreateSlimBuilder/CreateBuilder`.
Therefore M0-BE OpenAPI export must be a CLI-only static contract generator that executes before host builder creation.
Normal runtime startup still uses `WebApplication.CreateSlimBuilder(args)`.

## Checks

- `scripts/checks/check-openapi-auth-request-body.sh`
- `scripts/checks/check-openapi-endpoint-coverage.sh`
