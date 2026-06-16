# P2-001 OpenAPI Source Coverage Hardening Spec

## Scope

Harden the static OpenAPI export safety net without changing the CLI-only export architecture.

This change covers:

- broadening source-based endpoint coverage discovery,
- keeping the static export path isolated from runtime host startup,
- reducing file-location drift risk when new endpoint mapping files are added.

## Requirements

- Keep `OpenApiExtensions.ExportOpenApiAsync` as a CLI-only static exporter that does not build the runtime host.
- Keep `RegisteredDiscoveryEndpoints` as the export input for M0-BE.
- Source coverage tests must no longer hardcode only three endpoint mapping files.
- Source coverage tests must scan endpoint mapping files under `backend/src/WeCms.Modules.System` automatically and compare discovered `MapGet/MapPost/MapPut/MapPatch/MapDelete` routes against exported OpenAPI paths.
- The hardening must not pull in DI, persistence, or runtime endpoint discovery.

## Non-Goals

- Replacing static `RegisteredDiscoveryEndpoints` with runtime `EndpointDataSource`.
- Expanding OpenAPI export to cover runtime-only metadata collection.
- Introducing host startup into the export path.
