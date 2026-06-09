# Fix M0 Audit Round 10 — 10 MEDIUM Issues Spec

## Why
深度审计发现 10 个 MEDIUM 问题：Permissions.cs 常量不完整、敏感配置键白名单不足、排序字段未全部白名单、GetCurrentUser 独立查询、DB 连接池未显式配置、密码重置无安全事件记录、改密请求无确认字段。其中 3 个（M5/M6/M8）已确认为可接受，本 spec 修复剩余 7 个。

## What Changes

### MEDIUM 修复
- M1: `Permissions.cs` 补充缺失常量（dict/file/setting/log/security/i18n）
- M2: `SettingService.SensitiveKeys` 补充 `jwt_signing_key`, `db_password`, `smtp_user`, `sms_secret_key`
- M3: `RoleService`/`LogService`/`SecurityService`/`FileService`/`DictService` 列表方法增加排序字段白名单
- M4: `AuthService.GetCurrentUserAsync` 合并 roles+permissions 为单次查询
- M7: `DbConnectionFactory` 显式配置连接池参数（MaxPoolSize=100, MinPoolSize=0, ConnectionLifeTime=300）
- M9: `AuthManagementEndpoints.ForgotPasswordAsync` 记录安全事件
- M10: `ChangePasswordRequest` 新增 `ConfirmPassword` 字段，`ChangePasswordAsync` 校验一致性

## Impact
- Affected specs: fix-m0-audit9-high, fix-m0-audit8-critical
- Affected code: Permissions.cs, SettingService.cs, RoleService.cs, LogService.cs, SecurityService.cs, FileService.cs, DictService.cs, AuthService.cs, DbConnectionFactory.cs, AuthManagementEndpoints.cs, AuthDtos.cs

## ADDED Requirements

### Requirement: M1 — Permissions.cs 常量补全
系统 SHALL 在 `Permissions.cs` 中定义所有 `PermissionSyncService` 同步的权限码常量。

### Requirement: M2 — SensitiveKeys 白名单补全
系统 SHALL 将 `jwt_signing_key`, `db_password`, `smtp_user`, `sms_secret_key` 加入 `SettingService.SensitiveKeys`。

### Requirement: M3 — 排序字段白名单统一
系统 SHALL 在 `RoleService`、`LogService`、`SecurityService` 的列表方法中，将排序字段限定为白名单映射。

### Requirement: M4 — GetCurrentUser 合并查询
系统 SHALL 将 `GetCurrentUserAsync` 中的 roles 和 permissions 两次独立查询合并为一次或减少查询次数。

### Requirement: M7 — DB 连接池显式配置
系统 SHALL 在 `DbConnectionFactory` 中显式设置 `MaxPoolSize=100, MinPoolSize=0, ConnectionLifeTime=300`。

### Requirement: M9 — 密码重置记录安全事件
系统 SHALL 在 `ForgotPasswordAsync` 中调用 `ISecurityEventLogger` 记录密码重置请求。

### Requirement: M10 — 改密请求确认字段
系统 SHALL 在 `ChangePasswordRequest` 中新增 `ConfirmPassword` 字段，端点校验 `NewPassword == ConfirmPassword`。
