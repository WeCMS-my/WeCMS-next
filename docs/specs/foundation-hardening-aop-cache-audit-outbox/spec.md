# Spec: foundation-hardening-aop-cache-audit-outbox

## 背景

本次 hardening 处理系统基础设施中已识别的编译/运行风险：Endpoint 权限扩展确认、缓存 DI 组合根接通、AOP 拦截器由 no-op 改为消费事务/缓存元数据、Endpoint 审计默认落库、Outbox 后台派发启动、raw SQL 与 QueryFilter 边界门禁，以及 Program 开发辅助逻辑中的同步等待修复。

## 范围

- 不新增公共 HTTP API。
- 不新增数据库表或 migration，复用既有 `sys_audit_log` 与 `sys_outbox_message`。
- 不新增权限码、菜单、认证/Token 策略或前端 generated 类型。
- 不实现 AI runtime 能力。

## 设计

1. API 组合根注册 `AddWeCmsCaching()`，确保 AOP 缓存依赖可解析。
2. `ApplicationServiceAopInterceptor` 读取接口/实现方法与类型上的 AOP Attribute，支持 `Task`、`Task<T>`、`ValueTask`、`ValueTask<T>`，并串联 `TransactionInterceptor` 与 `CacheInterceptor`。
3. `WeCms.Modules.Audit.SqlSugar` 提供 `SqlSugarAuditWriter`，默认 `IAuditWriter` 写入 `sys_audit_log` 并检查 affected rows。
4. `WeCms.EventBus` 提供 `OutboxDispatcherHostedService`，通过 scope 解析 `IOutboxDispatcher` 并按配置轮询。
5. 架构测试限制 raw SQL/Ado API 只能出现在数据边界，业务模块不得直接绕过 QueryFilter。
6. Program 开发辅助端口探测使用 async/await，进程关闭异常记录 Debug 日志。

## 非目标

- 不重写所有 raw SQL Repository 为 Queryable。
- 不引入 CMS 内容 API。
- 不修改前端。
- 不新增 AI 相关代码。
