# S12 EventBus And Outbox Upgrade Spec

## Background

Sprint 12 follows the Sprint 11 cache and Application Service AOP upgrade. Sprint 12 owns the system foundation event bus and Outbox mechanism so later CMS content workflows can publish asynchronous events without introducing distributed transactions.

Primary source documents:

- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md`
- `docs/context/WeCMS_next_系统基础破坏性升级技术书_v3_不引入Controller.md`
- `AGENTS.md`
- `.trae/rules/wecms-engineering-principles.md`

## Current State

- `WeCms.EventBus` already exists and references `WeCms.Shared`.
- `WeCms.EventBus` currently contains only an assembly marker and project file.
- No EventBus abstractions, Outbox table, dispatcher, handler idempotency runtime, or system foundation events are implemented yet.

## Goals

- Define event abstractions in `WeCms.EventBus`.
- Implement a first-stage in-process EventBus with explicit handler registration.
- Add Outbox writer and repository boundaries without distributed transactions.
- Add `sys_outbox_message` database baseline or migration support.
- Implement a polling Outbox dispatcher with batch, retry, lock, and idempotency behavior.
- Add first system foundation events for identity, access control, configuration, i18n, and security-ban changes.
- Wire cache invalidation handlers for permission and configuration events.

## Non-Goals

- Do not implement CMS content publishing events or CMS runtime behavior.
- Do not implement external message brokers such as RabbitMQ, Kafka, Redis Streams, or cloud queues.
- Do not implement distributed transactions or `System.Transactions`.
- Do not implement Sprint 13 Swagger, Scalar, MiniProfiler UI changes, or OpenAPI UI work.
- Do not add frontend behavior.
- Do not add AI runtime capability.
- Do not use MVC Controller, Razor, Razor Pages, EF Core, runtime endpoint scanning, or business runtime code generation.
- Do not place SqlSugar, SQL text, database connections, or repository implementations inside `WeCms.EventBus`.

## Boundary Decisions

- `WeCms.EventBus` owns event contracts, handler contracts, EventBus abstractions, Outbox abstractions, dispatcher abstractions, and in-memory dispatch logic.
- Database entities and SqlSugar repository implementations for Outbox belong under the data infrastructure boundary, not inside `WeCms.EventBus`.
- Application Services publish system foundation events through `IEventBus` or `IOutboxWriter`; repositories do not publish domain events directly.
- Outbox consistency is same database and same transaction only. Cross-database consistency is not solved by distributed transactions.
- Dispatcher failures must be visible through retry state and logs; failures must not be silently swallowed.

## Functional Requirements

- Events include `id`, `type`, `occurredAt`, `traceId`, and `tenantId`.
- Handlers implement `IEventHandler<TEvent>` and accept `CancellationToken`.
- `IEventBus.PublishAsync` invokes registered handlers for the event type.
- Handler failure behavior is explicit and covered by tests.
- `IOutboxWriter` writes serialized event messages to `sys_outbox_message`.
- Outbox messages track `pending`, `processing`, `processed`, and `failed` states.
- Outbox repository supports locking pending messages, marking processed messages, and marking failed messages with retry metadata.
- Dispatcher supports batch size, retry delay, concurrency lock semantics, and idempotent handler execution.
- First system foundation events include user, role-permission, menu, setting, dict, i18n, and security-ban changes.
- Setting, dict, i18n, and permission events can trigger cache invalidation handlers.

## Acceptance Criteria

- Event abstraction tests prove event metadata and handler cancellation support.
- EventBus tests prove publish-to-handler and configured failure behavior.
- Outbox writer and repository tests prove write, lock, processed, failed, and retry behavior.
- Dispatcher tests prove pending dispatch, retry, and locked-message double-processing protection.
- Idempotency tests prove duplicate event handling is safe.
- System event tests prove permission and configuration events can evict cache.
- Full backend quality gate passes with MySQL after each implementation task.
- Final Sprint 12 audit passes with no CMS/frontend/S13/Controller/EF/dynamic/AI scope drift.

## Package Review

S12-T02 adds `Microsoft.Extensions.DependencyInjection.Abstractions`.

- Runtime compatibility: the package is a Microsoft .NET 10 extension package and is compatible with the current `net10.0` JIT runtime baseline.
- License: Microsoft .NET extension packages are distributed under the MIT license.
- Maintenance: the package is maintained by Microsoft as part of the .NET extensions stack.
- Alternatives considered: direct `IServiceProvider` usage without DI abstractions was rejected because S12-T02 requires explicit handler registration through DI; adding a custom service registry was rejected because it would duplicate the built-in DI contract already used by the rest of the backend.

S12-T04 adds `Microsoft.Extensions.Logging.Abstractions`.

- Runtime compatibility: the package is a Microsoft .NET 10 extension package and is compatible with the current `net10.0` JIT runtime baseline.
- License: Microsoft .NET extension packages are distributed under the MIT license.
- Maintenance: the package is maintained by Microsoft as part of the .NET extensions stack.
- Alternatives considered: swallowing dispatcher failures or recording only retry state without logs was rejected because S12 requires visible failure logging; using `Microsoft.AspNetCore.App` as a framework reference in `WeCms.EventBus` was rejected because the EventBus core should stay independent from ASP.NET endpoint hosting.
