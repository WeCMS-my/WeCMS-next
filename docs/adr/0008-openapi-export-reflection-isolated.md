# ADR-0008: OpenAPI Export Reflection Isolated to CLI Path

## Status

Accepted

## Context

WeCMS requires Native AOT publish and treats trim/AOT warnings as build failures.
The runtime API host must avoid reflection-heavy implementation paths.

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
- Native AOT publish followed by running the published binary with
  `--export-openapi`.

## Consequences

Reflection in `OpenApiExtensions` is an accepted exception, not a general pattern.
Any new reflection outside this file must be reviewed independently.

If .NET later exposes a stable public build-time OpenAPI export API, this ADR
must be revisited and the reflection path removed.


