# M1-BE-014 OpenAPI Contract Spec

## Scope

Ensure all M1 backend APIs are represented in exported OpenAPI.

## Rules

- Every M1 path must be exported.
- Every POST and PUT endpoint with a body must declare a required `requestBody`.
- Every list endpoint must declare query parameter schemas.
- Every response schema reference must resolve.
- Export must be deterministic and runnable without database connectivity.
