# WeCMS Next 一期完成状态说明

## 1. 文档定位

本文档记录 WeCMS Next 当前工程状态，作为进入一期后 hardening / 补齐阶段的状态基线。

当前状态为：

```text
一期完成：M0-BE + M1-BE + M2-FE
当前阶段：一期后 hardening / 补齐
```

本文档不重新打开一期范围，不把 CMS 内容管理能力、AI runtime、旧 ThinkPHP 数据迁移或旧系统 runtime compatibility 回流到一期。

## 2. 已完成范围

### M0-BE：后端底座

已形成以下基础能力：

- ASP.NET Core Minimal APIs。
- .NET 10 JIT publish/runtime。
- `WebApplication.CreateSlimBuilder(args)`。
- SqlSugar ORM 隔离在 `WeCms.Data.SqlSugar` 与 `WeCms.Modules.*.SqlSugar`。
- MySQL 开发与集成测试路径。
- 统一 `ApiResult` / 错误响应 / traceId。
- 健康检查与基础系统探针。
- OpenAPI 静态导出与契约检查。
- 后端质量门禁脚本。

### M1-BE：系统管理 API

已形成基础系统管理 API 闭环：

- 认证、刷新、退出、`/auth/me`。
- 用户、角色、菜单、权限。
- 部门、岗位、字典、系统设置。
- 登录日志、操作审计日志、安全事件查询。
- 文件基础能力。
- 系统管理权限码 seed 与菜单 seed。
- Endpoint 权限元数据与 OpenAPI 覆盖检查。
- 后端架构、数据库边界、DI 边界与 code review 检查。

### M2-FE：基础系统前端管理端

已形成 SoybeanAdmin / Vue 3 管理端基础闭环：

- 登录、刷新、退出、会话恢复。
- access token 仅保存在前端内存。
- refresh token 使用后端 `HttpOnly; Secure; SameSite=Strict` Cookie。
- 基础系统管理 API 接入。
- 动态菜单与按钮权限消费后端返回数据。
- 前端质量门禁脚本。

## 3. 当前安全与契约基线

- Refresh token 当前基线为 HttpOnly Cookie，不允许回到 localStorage。
- Access token 仅保存在前端内存，通过 `Authorization: Bearer` 调用业务 API。
- 所有业务 Endpoint 必须显式注册。
- 除 `AllowAnonymous` 接口外，业务 Endpoint 必须绑定权限码或内部访问策略。
- 所有写操作必须具备明确 HTTP Method、权限码、DTO 校验和 Audit Log。
- 高风险操作必须补充 Security Event，必要时要求当前密码、2FA 或 challenge。
- 数据库/ORM/连接器只能在 `WeCms.Data.SqlSugar` 与 `WeCms.Modules.*.SqlSugar` 边界内。
- 业务模块只能依赖接口和 `WeCms.Shared` 抽象。

## 4. 一期后补齐范围

一期后补齐只处理基础系统后台 hardening 和等价性缺口，主要包括：

- 2FA 双因素认证。
- 个人中心、修改密码、头像能力。
- 安全中心封禁、解封、批量解封。
- AdminGate / CSRF 职责拆解后的新架构落地。
- Cookie 型认证接口 Origin / CSRF 防护。
- IP 规则匹配、SecurityEventClassifier、Rate Limiting、PermissionVersion、安全响应头。
- i18n、菜单排序、字典状态、系统设置敏感配置、文件策略等基础后台补齐项。

这些项目是待补齐或待 hardening 能力，不应在审计中误写为已完成能力。

## 5. 明确排除

以下内容不属于一期后补齐范围：

- CMS 栏目、文章、页面、媒体、标签、SEO。
- AI 模块、AI Provider、Prompt、RAG、Vector Store、Agent Tool 或后端模型 API 调用。
- 旧 ThinkPHP 数据迁移。
- 旧密码 hash 兼容。
- 旧 Session / token runtime compatibility。
- 直接复制旧 AdminGate 或旧 PHP WAF。
- 运行时 DLL 插件。
- 大规模 UI 重构。

## 6. 关联文档

- [ADR-0014：Auth Token Storage Final State](../adr/0014-refresh-token-storage-m2-fe.md)
- [ADR-0016：AdminGate / CSRF Migration Strategy](../adr/0016-admingate-csrf-migration-strategy.md)
- [一期后补齐计划书](WeCMS_Next_一期后补齐计划书.md)
- [AdminGate / CSRF 迁移设计说明](WeCMS_Next_AdminGate_CSRF_迁移设计说明.md)
