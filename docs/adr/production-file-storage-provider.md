# ADR: Production File Storage Provider Boundary

## Status

Accepted for PH-4.

## Decision

WeCMS Next keeps `local` as the only implemented provider in PH-4. The storage contract now includes `StoreAsync`, `OpenReadAsync`, `DeleteAsync`, `ExistsAsync`, and `GetMetadataAsync` so a later `s3-compatible`, `oss`, or `r2` adapter can be added without changing module code.

PH-4 does not introduce cloud SDK packages. This avoids adding credentials, network dependencies, and provider-specific runtime behavior before production deployment requirements are finalized.

## Consequences

- Production local storage must use a private absolute directory outside `wwwroot`.
- Local storage remains suitable for Development and small single-node deployments.
- Multi-node production deployments should plan an object storage adapter before horizontal scaling.
- `NoopFileScanService` is allowed only while `FileStorage:VirusScanEnabled=false`; enabling scanning in Production requires a real scanner implementation and a follow-up hardening task.
