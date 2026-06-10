# Fix Architecture Compliance — Pagination + Sort Whitelist + Affected Rows

## Why
架构偏离检查发现 3 个合规问题：Dict/I18n/Setting 列表无分页、仅 UserService 有排序白名单、写操作未检查 affected rows。审计日志已在上轮修复。

## What Changes
- **P1**: DictService/SettingService/I18nService 列表方法添加分页参数
- **P2**: LogService/SecurityService 添加排序字段白名单保护
- **P3**: 所有 Service 写方法检查 affected rows

## Impact
- Affected specs: fix-m0-audit10-medium (sort whitelist), fix-m0-audit8-critical (audit)
- Affected code: DictService.cs, SettingService.cs, I18nService.cs, LogService.cs, SecurityService.cs, UserService.cs, RoleService.cs, MenuService.cs, FileService.cs, 对应 Endpoints

## ADDED Requirements

### P1 — 列表接口分页
DictService/SettingService/I18nService 列表方法 SHALL 接受 page/pageSize 参数，默认 20，最大 100。

### P2 — 排序白名单
LogService/SecurityService SHALL 有排序字段白名单，拒绝未知排序字段。

### P3 — Affected rows 检查
所有 UPDATE/DELETE 操作 SHALL 检查 affected rows > 0，为 0 时抛出明确异常。
