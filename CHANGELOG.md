# Changelog

All notable project foundation changes are summarized here.

## [0.2.0-foundation] - 2026-06-23

### Added

- Added foundation-stable final acceptance report under `docs/reports/`.
- Added `v0.2.0-foundation` release note under `docs/releases/`.
- Added this changelog as the release history entrypoint.

### Changed

- Updated README current-state links to point at the foundation-stable acceptance and release note.
- Consolidated the accepted baseline around `.NET 10`, JIT publish/runtime, Minimal APIs, SqlSugar data-platform boundaries, SoybeanAdmin foundation frontend, OpenAPI contract delivery, and frozen quality gates.

### Frozen

- `AGENTS.md`, `code_review.md`, `.trae/rules/wecms-engineering-principles.md`, and the quality gate scripts are treated as the current foundation baseline surface.
- `WeCms.Modules.System` and `WeCms.Persistence` remain outside active source and must not be reintroduced.

### Excluded

- CMS content APIs.
- Legacy ThinkPHP runtime compatibility.
- Legacy data migration.
- Plugin runtime.
- AI runtime.

## Earlier Stable Points

- `phase1-accepted`
- `v1-phase1-hardening-stable`
- `v1-system-admin-production`
