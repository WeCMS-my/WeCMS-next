# S7 Configuration Migration Tasks

Scope summary: migrate Settings, Dicts, and I18n into the Configuration module boundary and add a Configuration cache invalidation abstraction for write paths.

## S7-T00 Spec Trio

Add the S7 Configuration migration spec trio before production code changes.

Required proof includes:

- `docs/specs/s7-configuration-migration/spec.md`
- `docs/specs/s7-configuration-migration/tasks.md`
- `docs/specs/s7-configuration-migration/checklist.md`
- route and permission strategy documented as preserved public contract
- docs/rules audit

## S7-T01 Settings Migration

Move Settings DTOs, records, permissions, service, security rules, repository interface, and endpoint definitions into `WeCms.Modules.Configuration`.

Move `SettingRepository` into `WeCms.Modules.Configuration.SqlSugar`, preserve sensitive setting protections, preserve existing `/api/v1/system/settings` routes and `sys:setting:*` permission codes, and add write-path calls to the Configuration cache invalidation abstraction.

If the cache invalidation abstraction does not exist before S7-T01 starts, S7-T01 may introduce the minimal `IConfigurationCacheInvalidator` no-op abstraction needed for Settings write-path proof. S7-T04 remains the final cross-Settings/Dicts/I18n cache invalidation audit.

Required proof includes:

- `SettingServiceTests`
- `SettingSecurityTests`
- `SettingRepositoryIntegrationTests`
- `SettingCacheInvalidationTests`
- endpoint permission, audit, OpenAPI, DB boundary, layer, and DI gates

## S7-T02 Dicts Migration

Move Dict DTOs, records, permissions, service, repository interface, and endpoint definitions into `WeCms.Modules.Configuration`.

Move `DictRepository` into `WeCms.Modules.Configuration.SqlSugar`, preserve existing `/api/v1/system/dicts` routes and `sys:dict:*` permission codes, preserve type/value separation and status behavior, and add write-path calls to the Configuration cache invalidation abstraction.

Required proof includes:

- `DictServiceTests`
- `DictRepositoryIntegrationTests`
- `DictCacheInvalidationTests`
- endpoint permission, audit, OpenAPI, DB boundary, layer, and DI gates

## S7-T03 I18n Migration

Move I18n DTOs, records, permissions, message service, repository interface, and endpoint definitions into `WeCms.Modules.Configuration`.

Move `I18nMessageRepository` into `WeCms.Modules.Configuration.SqlSugar`, preserve public message read behavior, preserve user language switching policy, and add write-path calls to the Configuration cache invalidation abstraction.

Required proof includes:

- `I18nServiceTests`
- `I18nRepositoryIntegrationTests`
- `PublicI18nEndpointTests`
- `I18nCacheInvalidationTests`
- endpoint permission, audit, OpenAPI, DB boundary, layer, and DI gates

## S7-T04 Configuration Cache Invalidation Final Audit

Ensure `IConfigurationCacheInvalidator` in `WeCms.Modules.Configuration` covers setting, dict, and i18n invalidation.

The S7 implementation may be no-op, but every Settings, Dicts, and I18n write operation must call it. Real distributed cache integration remains later scope.

Required proof includes:

- `SettingWrite_CallsCacheInvalidator`
- `DictWrite_CallsCacheInvalidator`
- `I18nWrite_CallsCacheInvalidator`
- final S7 checklist and total audit
