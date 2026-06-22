# Tasks

- [x] P0-1 确认 `RequireEndpointPermission` 扩展存在并可被调用端引用。
- [x] P1-2 在 API 组合根注册 `AddWeCmsCaching()` 并补充架构测试。
- [x] P1-1 将 `ApplicationServiceAopInterceptor` 从 no-op 改为消费 UnitOfWork/Cache/Audit 元数据。
- [x] P1-3 用 `SqlSugarAuditWriter` 替换默认 Noop 审计写入。
- [x] P1-4 新增并注册 Outbox dispatcher hosted service。
- [x] P1-5 增强 raw SQL / QueryFilter 边界架构测试。
- [x] P2-2 修复 Program 开发辅助逻辑同步等待与静默 catch。
- [x] 执行静态审计、raw SQL 边界脚本、git diff check。
