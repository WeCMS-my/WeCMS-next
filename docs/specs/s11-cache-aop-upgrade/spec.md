# S11 Cache And AOP Upgrade Spec

## Background

Sprint 11 follows the Sprint 10 data-platform upgrade. Sprint 10 completed CodeFirst model registration, schema validation, QueryFilter, multi-connection / tenant connection resolution, and SQL audit primitives. Sprint 11 now owns the unified cache abstraction and Application Service AOP runtime for transactions and cache behavior.

Primary source documents:

- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md`
- `docs/context/WeCMS_next_系统基础破坏性升级技术书_v3_不引入Controller.md`
- `AGENTS.md`
- `.trae/rules/wecms-engineering-principles.md`

## Goals

- Define unified cache abstractions in `WeCms.Caching`.
- Implement a local `MemoryCache` provider with async-first APIs.
- Reserve Redis support through options and explicit unsupported or no-op boundaries without adding hidden runtime behavior.
- Define AOP attributes in `WeCms.Aop`.
- Implement transaction and cache interceptors for Application Service interfaces.
- Wire Autofac / DynamicProxy only for Application Service interfaces.
- Prove repositories, endpoint handlers, domain entities, and module infrastructure are not intercepted.

## Non-Goals

- Do not implement Sprint 12 EventBus, Outbox dispatcher, idempotent handler runtime, or distributed transactions.
- Do not implement Sprint 13 Swagger, Scalar, MiniProfiler UI changes, or OpenAPI UI work.
- Do not add CMS runtime behavior or frontend features.
- Do not add AI runtime capability.
- Do not use MVC Controller, Razor, Razor Pages, EF Core, runtime endpoint scanning, or business runtime code generation.
- Do not intercept repositories, endpoint handlers, domain entities, or database infrastructure implementations.
- Do not connect to Redis unless an explicit later task approves package, license, maintenance, and runtime configuration.

## Functional Requirements

- `ICache` supports async `Get`, `Set`, `Remove`, `GetOrCreate`, and prefix or tag invalidation.
- `ICacheSerializer` uses `System.Text.Json` first.
- `ICacheKeyBuilder` produces keys containing app, environment, tenant, module, resource, and version dimensions.
- `ICacheInvalidator` supports prefix invalidation and is safe for configuration invalidation use cases.
- `MemoryCacheProvider` supports expiration, null value policy, and concurrent `GetOrCreate`.
- Redis configuration is explicit and does not let business code reference Redis directly.
- `[UnitOfWork]`, `[Cacheable]`, `[CacheEvict]`, and `[Audited]` are only valid on Application Service interfaces or implementation methods.
- `TransactionInterceptor` supports `Task`, `Task<T>`, `CancellationToken`, commit on success, rollback and rethrow on failure, and no sync blocking.
- `CacheInterceptor` builds tenant-aware keys, returns cached values, writes on miss, evicts after mutation, does not cache exceptions, and honors null caching policy.
- Autofac registration enables interception only for Application Service interfaces.

## Acceptance Criteria

- Cache key proof includes tenant, module, resource, and version.
- Memory cache get/set/remove, prefix invalidation, expiration, null caching, and concurrent get-or-create pass tests.
- Redis remains explicit and unconnected unless a later task expands scope.
- AOP attribute usage tests reject repository and endpoint handler usage.
- Transaction interceptor commits on success and rolls back/rethrows on exception.
- Cache interceptor covers hit, miss, eviction, exception, tenant key, and null policy behavior.
- Autofac registration proves Application Services are intercepted and repositories are not.
- Full backend quality gate passes with MySQL after each implementation task.
- Final Sprint 11 audit passes with no S12/S13/Controller/EF/dynamic/AI scope drift.

## Package Review

S11-T02 adds `Microsoft.Extensions.Caching.Memory` and `Microsoft.Extensions.DependencyInjection.Abstractions`.

- Runtime compatibility: both packages are Microsoft .NET 10 extension packages and are compatible with the current `net10.0` JIT runtime baseline.
- License: Microsoft .NET extension packages are distributed under the MIT license.
- Maintenance: both packages are maintained by Microsoft as part of the .NET extensions stack.
- Alternatives considered: `Microsoft.AspNetCore.App` framework reference was rejected because `WeCms.Caching` should keep a narrow dependency surface; Redis packages are reserved for S11-T03 and must not be introduced in S11-T02.

S11-T07 adds `Autofac.Extensions.DependencyInjection` and `Autofac.Extras.DynamicProxy`.

- Runtime compatibility: `Autofac.Extensions.DependencyInjection` 11.0.1 includes a `net10.0` dependency group; `Autofac.Extras.DynamicProxy` 7.1.0 targets `netstandard2.0`/`netstandard2.1` and is compatible with the current `net10.0` JIT runtime baseline through NuGet restore/build validation.
- License: `Autofac.Extensions.DependencyInjection` and `Autofac.Extras.DynamicProxy` declare MIT license expressions; transitive `Castle.Core` 5.1.1 declares Apache-2.0.
- Maintenance: Autofac packages are maintained by Autofac Contributors with public Git repositories and release notes; Castle.Core is maintained by Castle Project Contributors.
- Alternatives considered: the built-in Microsoft DI container was rejected for this task because it does not provide interface interception; Scrutor decoration was rejected because it would require manual per-service decorators and would not provide the DynamicProxy capability explicitly allowed by the governing docs.
