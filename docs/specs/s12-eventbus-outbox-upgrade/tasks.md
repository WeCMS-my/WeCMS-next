# S12 EventBus And Outbox Upgrade Tasks

## S12-T00 Spec Trio

Create Sprint 12 spec, tasks, and checklist before production code changes.

Required proof:

- `docs/specs/s12-eventbus-outbox-upgrade/spec.md`
- `docs/specs/s12-eventbus-outbox-upgrade/tasks.md`
- `docs/specs/s12-eventbus-outbox-upgrade/checklist.md`
- documentation consistency audit

## S12-T01 Event Abstractions

Define event abstractions in `WeCms.EventBus`.

Required work includes:

- add `IIntegrationEvent`
- add `IntegrationEventBase`
- add `IEventHandler<TEvent>`
- add `IEventBus`
- add `IOutboxWriter`
- add `IOutboxDispatcher`
- include id, type, occurredAt, traceId, and tenantId metadata
- support `CancellationToken` on handlers and publish APIs
- keep `WeCms.EventBus` free of SqlSugar, SQL text, and database connection types

Required proof includes:

- `IntegrationEventBase_ExposesRequiredMetadata`
- `EventHandler_RequiresCancellationToken`
- `EventBusAbstractions_DoNotReferenceDataInfrastructure`

## S12-T02 In-Memory EventBus

Implement first-stage in-process EventBus behavior.

Required work includes:

- register handlers explicitly through DI
- publish events to matching handlers
- define handler failure behavior through options or explicit policy
- do not scan Minimal API endpoints at runtime
- do not swallow handler exceptions unless an explicit tested policy allows it

Required proof includes:

- `EventBus_PublishesToHandlers`
- `EventBus_HandlerFailureDoesNotSwallowException_WhenConfigured`
- `EventBus_DoesNotScanEndpointsAtRuntime`

## S12-T03 Outbox Table And Repository

Implement Outbox persistence boundary.

Required work includes:

- add `sys_outbox_message` baseline or migration
- add Outbox entity under the data infrastructure boundary
- add Outbox repository abstraction and implementation
- support pending, processing, processed, and failed states
- support retry count and next available time
- write events with the current same-database transaction boundary
- avoid distributed transactions

Required proof includes:

- `OutboxWriter_WritesMessage`
- `OutboxRepository_LocksPendingMessages`
- `OutboxRepository_MarksProcessed`
- `OutboxRepository_MarksFailedWithRetry`
- `OutboxPersistence_DoesNotUseDistributedTransactions`

## S12-T04 Outbox Dispatcher

Implement polling dispatcher.

Required work includes:

- dispatch pending messages by batch size
- honor retry delay
- avoid double-processing locked messages
- support idempotent handler execution
- log failures without silent swallowing
- expose dispatcher through explicit DI registration

Required proof includes:

- `OutboxDispatcher_DispatchesPendingMessages`
- `OutboxDispatcher_RetriesFailedMessages`
- `OutboxDispatcher_DoesNotDoubleProcessLockedMessage`
- `EventHandlers_AreIdempotent`

## S12-T05 System Foundation Events

Publish the first system foundation events and connect cache invalidation handlers.

Required work includes:

- add `UserCreatedEvent`
- add `UserDisabledEvent`
- add `RolePermissionsChangedEvent`
- add `MenuChangedEvent`
- add `SettingChangedEvent`
- add `DictChangedEvent`
- add `I18nChangedEvent`
- add `SecurityBanCreatedEvent`
- publish events from Application Services
- write events to Outbox
- evict setting, dict, i18n, and permission caches through handlers

Required proof includes:

- `SettingChangedEvent_EvictsSettingCache`
- `RolePermissionsChangedEvent_EvictsAccessProfileCache`
- `EventHandlers_AreIdempotent`
- `SystemFoundationEvents_ArePublishedByApplicationServices`

## S12-T06 Final Sprint 12 Audit

Run a total audit after S12-T01 through S12-T05 complete.

Required proof includes:

- EventBus abstractions usable
- In-memory EventBus usable
- Outbox table usable
- Outbox writer and repository usable
- Dispatcher can process pending events
- Handler idempotency is enforced
- permission and configuration changes can trigger cache invalidation
- no distributed transaction usage
- no CMS/frontend/S13 scope drift
- no Controller/MVC/Razor/EF Core/dynamic/AI runtime capability
- final checklist complete
- full backend quality gate with MySQL
