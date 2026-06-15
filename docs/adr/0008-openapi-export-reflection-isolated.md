# ADR-0008: OpenAPI Export Reflection Isolated to CLI Path

## Status

Accepted with historical AOT background.

## Context

This ADR was created when WeCMS still used Native AOT as the runtime baseline.
The current repository runtime baseline is JIT, but the decision to isolate the
OpenAPI export reflection path to a bounded CLI-only flow remains valid.

The .NET 10 `Microsoft.AspNetCore.OpenApi` package exposes the runtime OpenAPI
endpoint through `MapOpenApi()`, but the build-time document provider used by
`dotnet getdocument` is not available as a public compile-time API. The provider
type is `Microsoft.Extensions.ApiDescriptions.OpenApiDocumentProvider` and is
resolved by tooling through a known type name.

WeCMS still needs a deterministic local/CI command:

```bash
dotnet run --project backend/src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-api-v1.json
```

## Decision

Keep reflection only inside `OpenApiExtensions.ExportOpenApiAsync`, which is
reachable solely when the process is launched with `--export-openapi`.

The normal runtime path remains:

- `Program.cs` maps OpenAPI only in development.
- Production startup does not expose OpenAPI by default.
- Business endpoints do not call the export helper.

The export path must stay covered by:

- architecture tests documenting the reflection boundary;
- package-version checks that reject preview/stable ASP.NET Core package mixing;
- backend quality gate execution of `--export-openapi`;
- published-binary execution with `--export-openapi`.

## Consequences

Reflection in `OpenApiExtensions` is an accepted exception, not a general pattern.
Any new reflection outside this file must be reviewed independently.

If .NET later exposes a stable public build-time OpenAPI export API, this ADR
must be revisited and the reflection path removed.

