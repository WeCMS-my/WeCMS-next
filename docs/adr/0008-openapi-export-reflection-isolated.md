# ADR-0008: OpenAPI Export Isolated to CLI Path

## Status

Accepted with historical AOT background. Updated for M0-BE JIT static export.

## Context

This ADR was created when WeCMS still used Native AOT as the runtime baseline.
The current repository runtime baseline is JIT, but the decision to isolate the
OpenAPI export path to a bounded CLI-only flow remains valid.

The .NET 10 `Microsoft.AspNetCore.OpenApi` package exposes the runtime OpenAPI
endpoint through `MapOpenApi()`, but the current local runtime environment hangs
before host construction when creating `WebApplication.CreateSlimBuilder` or
`WebApplication.CreateBuilder`. M0-BE therefore cannot make the OpenAPI export
depend on runtime host startup.

WeCMS still needs a deterministic local/CI command:

```bash
dotnet run --project backend/src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-api-v1.json
```

## Decision

Keep OpenAPI export isolated inside `OpenApiExtensions.ExportOpenApiAsync`,
which is reachable solely when the process is launched with `--export-openapi`.
For M0-BE, the export path is a deterministic static contract generator that
runs before `WebApplication.CreateSlimBuilder(args)`.

The normal runtime path remains:

- Production startup does not expose OpenAPI by default.
- Business endpoints do not call the export helper.

The export path must stay covered by:

- architecture tests documenting the CLI-only export boundary;
- backend quality gate execution of `--export-openapi`;
- OpenAPI contract scripts that verify auth request bodies, endpoint coverage,
  security metadata, and `$ref` resolution.

## Consequences

The static export must stay synchronized with explicit Minimal API endpoint
registration and DTOs. Drift is controlled by contract tests and check scripts.

If the local .NET runtime issue is resolved or .NET exposes a stable public
build-time OpenAPI export API, this ADR must be revisited and the static
contract generator should be replaced.
