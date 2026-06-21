# S11 Cache And AOP Upgrade Tasks

## S11-T00 Spec Trio

Create Sprint 11 spec, tasks, and checklist before production code changes.

Required proof:

- `docs/specs/s11-cache-aop-upgrade/spec.md`
- `docs/specs/s11-cache-aop-upgrade/tasks.md`
- `docs/specs/s11-cache-aop-upgrade/checklist.md`
- documentation consistency audit

## S11-T01 Unified Cache Abstractions

Define cache abstractions in `WeCms.Caching`.

Required work includes:

- add `ICache`
- add `ICacheSerializer`
- add `ICacheKeyBuilder`
- add `ICacheInvalidator`
- add `CacheOptions`
- add `CacheEntryOptions`
- prefer async APIs
- support get, set, remove, get-or-create, prefix invalidation, and tag or equivalent invalidation
- require cache keys to include app, environment, tenant, module, resource, and version

Required proof includes:

- `CacheKeyBuilder_IncludesTenantModuleResource`
- `CacheKeyBuilder_IncludesAppEnvironmentAndVersion`
- `CacheSerializer_UsesSystemTextJson`
- `CacheInvalidator_RemovesByPrefix`

## S11-T02 MemoryCache Provider

Implement local memory cache provider and DI registration.

Required work includes:

- implement `MemoryCacheProvider`
- register `AddWeCmsCaching`
- document package compatibility, license, maintenance, and alternatives
- support absolute and sliding expiration where practical
- support null value caching policy
- support concurrent get-or-create without duplicate factory execution for the same key

Required proof includes:

- `MemoryCache_GetSetRemove`
- `MemoryCache_ExpiresEntries`
- `MemoryCache_CachesNull_WhenPolicyAllows`
- `MemoryCache_GetOrCreate_IsSingleFlightPerKey`
- `AddWeCmsCaching_RegistersMemoryCacheProviderAndAbstractions`

## S11-T03 Redis Provider Reservation

Reserve Redis configuration without hidden runtime dependency.

Required work includes:

- define Redis options
- add explicit unsupported or no-op provider boundary if Redis package is not introduced
- document TODO / ADR requirement before real Redis package adoption
- ensure business code does not depend on Redis directly

Required proof includes:

- `RedisProvider_IsExplicitlyDisabledUntilConfigured`
- `BusinessCode_DoesNotReferenceRedis`
- `RedisPackage_IsNotIntroducedWithoutAdr`

## S11-T04 AOP Attributes

Define AOP attributes in `WeCms.Aop`.

Required work includes:

- add `[UnitOfWork]`
- add `[Cacheable]`
- add `[CacheEvict]`
- add `[Audited]`
- support interceptor order through attribute or centralized interceptor ordering
- enforce usage only on Application Service interfaces or implementation methods
- forbid Repository and Endpoint Handler usage

Required proof includes:

- `AopAttributeUsageTests`
- `RepositoryTypes_AreNotAnnotatedForAop`
- `EndpointHandlers_AreNotAnnotatedForAop`

## S11-T05 TransactionInterceptor

Implement transaction AOP for Application Service interfaces.

Required work includes:

- support async `Task`
- support async `Task<T>`
- support `CancellationToken`
- commit on success
- rollback on exception
- rethrow original exception behavior
- forbid sync blocking (`Task.Wait`, `.Result`, `WaitAll`)
- define nested transaction behavior without distributed transactions

Required proof includes:

- `TransactionInterceptor_CommitsOnSuccess`
- `TransactionInterceptor_RollsBackAndRethrowsOnException`
- `TransactionInterceptor_DoesNotBlockSynchronously`
- `TransactionInterceptor_DoesNotCreateDistributedTransactions`

## S11-T06 CacheInterceptor

Implement cache AOP for Application Service query methods.

Required work includes:

- generate keys from `[Cacheable]`
- include parameter hash
- include tenant dimension
- execute target method on miss
- write cache on miss
- evict keys for `[CacheEvict]`
- do not cache exceptions
- support null caching policy

Required proof includes:

- `CacheInterceptor_ReturnsCachedValue`
- `CacheInterceptor_WritesOnMiss`
- `CacheInterceptor_EvictsAfterMutation`
- `CacheInterceptor_DoesNotCacheException`
- `CacheInterceptor_UsesTenantAwareKey`
- `CacheInterceptor_HonorsNullCachePolicy`

## S11-T07 Autofac Registration

Wire Autofac / DynamicProxy without widening interception scope.

Required work includes:

- add approved Autofac package references with license, maintenance, compatibility, and alternatives noted
- configure `UseServiceProviderFactory(new AutofacServiceProviderFactory())`
- register interceptors centrally
- enable interception only for Application Service interfaces
- forbid Repository, Endpoint Handler, Domain Entity, and infrastructure implementation interception
- update DI boundary tests

Required proof includes:

- `AutofacModule_RegistersApplicationServices`
- `RepositoryTypes_AreNotIntercepted`
- `EndpointHandlers_AreNotIntercepted`
- `AopRegistration_DoesNotScanEndpointsAtRuntime`

## S11-T08 Final Sprint 11 Audit

Run a total audit after S11-T01 through S11-T07 complete.

Required proof includes:

- `ICache` usable
- MemoryCache provider usable
- AOP transaction usable
- AOP cache usable
- Autofac registration usable
- no synchronous blocking
- repositories are not intercepted
- no S12/S13 scope drift
- no Controller/MVC/Razor/EF Core/dynamic/AI runtime capability
- final checklist complete
- full backend quality gate with MySQL
