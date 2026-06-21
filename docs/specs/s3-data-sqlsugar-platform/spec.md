# S3 Data SqlSugar Platform Spec

## Scope

Implement the Sprint 3 Data.SqlSugar platform skeleton from `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md`.

This change moves shared SqlSugar platform capabilities out of the temporary `WeCms.Persistence` boundary without migrating every repository.

This change covers:

- UnitOfWork and transaction context migration to `WeCms.Data.SqlSugar`,
- multi-connection database option models and fail-fast option reading,
- SqlSugar connection registry for enabled named connections,
- SqlSugar client factory upgrades for default and named clients,
- migration runner and seed runner migration to the data platform,
- CodeFirst model registry, development runner, and schema validation skeleton.

## Requirements

- Keep `IUnitOfWork` in `WeCms.Shared.Data`.
- Data platform code may reference SqlSugar; modules must not directly reference SqlSugar or MySQL connector packages.
- Transaction commit, rollback, and disposal must be asynchronous and must not synchronously block async work.
- Exceptions inside transaction work must roll back and rethrow.
- Database configuration must fail fast for missing defaults, duplicate connection names, invalid database types, and invalid command timeouts.
- Disabled connections must not be returned by the registry.
- The factory must support default and named client creation before tenant-specific creation is implemented.
- Migration checksum drift behavior and safe admin password seed replacement must be preserved.
- CodeFirst initialization must be environment protected and must not run in production-like mode.
- Existing `WeCms.Persistence` may remain during migration, but new platform capabilities belong in `WeCms.Data.SqlSugar`.

## Non-Goals

- Migrating all module repositories in Sprint 3.
- Deleting `WeCms.Persistence` in Sprint 3.
- Adding tenant database provisioning.
- Running automatic DDL in production-like environments.
- Changing frontend generated contracts or SoybeanAdmin behavior.
- Introducing EF Core, Controller APIs, runtime endpoint scanning, or AI runtime code.

## Acceptance

- `WeCms.Data.SqlSugar` compiles with UnitOfWork, multi-connection options, registry, factory, migration, and CodeFirst skeleton.
- S3-T01 through S3-T06 focused tests pass.
- The full backend quality gate passes after each completed S3 task.
- Local audits pass for layer dependency, SqlSugar boundary, DB boundary, DI boundary, and no Controller.
- `WeCms.Persistence` remains only as a temporary migration boundary and does not receive new platform capabilities.
