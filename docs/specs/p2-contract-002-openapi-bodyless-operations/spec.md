# P2-CONTRACT-002 OpenAPI Bodyless Operations Spec

## Scope

Align the committed OpenAPI contract with the real Minimal API handler shape for command-style system endpoints that do not accept a request body.

This change covers:

- removing synthetic `requestBody` declarations from bodyless `POST` operations such as `enable` and `disable`,
- keeping request bodies on endpoints that actually bind a DTO or form payload,
- correcting the OpenAPI coverage gate so it validates real body-bearing operations instead of every `POST`/`PUT`.

## Requirements

- `OpenApiExtensions.RegisteredDiscoveryEndpoints` must not assign a request-body schema to endpoints whose handlers only bind route values, framework context, or injected services.
- The exported OpenAPI document must omit `requestBody` for bodyless command-style operations under `/api/v1/system/**`.
- The exported OpenAPI document must continue to include `requestBody` for DTO-backed JSON operations and the multipart file upload endpoint.
- OpenAPI unit tests must assert both sides of the contract:
  - optional DTO fields remain present without being marked required,
  - bodyless command operations do not declare `requestBody`.
- `scripts/checks/check-system-openapi-coverage.sh` must require `requestBody` only for operations that really have one, and must fail if a known bodyless command operation declares one.

## Non-Goals

- Replacing the static exporter with runtime endpoint discovery.
- Refactoring handler signatures in `WeCms.Modules.System`.
- Broadening file download/preview permission coverage beyond preserving the current checks.
