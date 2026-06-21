# WeCMS Next AdminGate / CSRF 迁移设计说明

## 1. 文档定位

本文档说明旧 ThinkPHP AdminGate / CSRF 能力如何迁移到 WeCMS Next。

结论：

```text
不复制旧 AdminGate。
不全局照搬旧 CSRF。
按 WeCMS Next 新架构拆分为可测试、可审计、职责单一的组件。
```

对应 ADR：

- [ADR-0016：AdminGate / CSRF Migration Strategy](../adr/0016-admingate-csrf-migration-strategy.md)

## 2. 旧系统职责

旧 ThinkPHP AdminGate 集中承担：

- WAF。
- 配置读取。
- Session 检查。
- DB token 校验。
- 2FA 检查。
- 权限检查。
- IP 白名单。
- 安全封禁。
- 操作日志。

旧 CSRF 基于服务端模板 + Session 架构，适合作为全局写请求保护。

## 3. 新系统拆解策略

| 旧系统职责 | WeCMS Next 落地组件 |
| --- | --- |
| Session 登录检查 | ASP.NET Core Authentication |
| DB token 校验 | Refresh Token Repository + Token Family Revocation |
| URL 动态权限匹配 | `RequirePermission` + `PermissionEndpointFilter` |
| 2FA pending session | Auth Challenge + TwoFactorService |
| WAF 特征检测 | SecurityEventClassifier |
| IP 白名单 / 黑名单 | `IIpRuleMatcher` + IpAccessControlMiddleware |
| 安全封禁 | SecurityBanService + SecurityBanMiddleware |
| 操作日志 | Audit middleware / AuditLogService |
| 配置读取 | SettingService + SettingCache |
| 登录失败限制 | Rate Limiting + SecurityBanService |
| 安全响应头 | SecureHeadersMiddleware |

## 4. CSRF 策略

WeCMS Next 是前后端分离 API 架构，CSRF 按接口类型处理：

| API 类型 | 策略 |
| --- | --- |
| 使用 Authorization Bearer 的业务 API | 依赖 Bearer token、CORS、权限码、DTO 校验、Audit，不强制全局 CSRF |
| 使用 HttpOnly Cookie 的认证 API | 必须强化 SameSite、Origin / Referer 校验，必要时引入 double-submit CSRF token |
| 高风险写接口 | 可叠加当前密码、2FA 或短期 challenge |

当前 refresh token 已使用 `HttpOnly; Secure; SameSite=Strict` Cookie。Origin / Referer 校验属于 H1 hardening，不在 H0 文档修复中实现。

## 5. 写操作基线

所有写操作必须同时满足：

- 明确 HTTP Method。
- 明确权限码，或明确 `AllowAnonymous` / 内部访问策略。
- 明确 DTO 校验。
- 明确 Audit Log。

高风险写操作还必须满足：

- Security Event。
- 必要时要求当前密码、2FA 或 challenge。
- 必要时吊销 refresh token family。

## 6. 禁止项

- 不新增 `AdminGateMiddleware` 复刻旧系统。
- 不新增旧 ThinkPHP Session / token runtime compatibility。
- 不复制旧 PHP WAF 作为主要业务安全边界。
- 不把 CMS 内容 API、AI runtime、旧数据迁移混入一期补齐。
- 不让业务模块直接访问 SqlSugar、MySQL、`WeCms.Data.SqlSugar` 或 `WeCms.Modules.*.SqlSugar` 具体实现。

## 7. 后续实现要求

后续 H1/H2 任务每新增一个具体组件，都必须提供对应测试：

- Middleware / Endpoint 行为用集成测试。
- Service 规则用单元测试。
- Repository / SQL 用集成测试。
- OpenAPI / generated 类型用契约测试。
- 安全事件、审计日志、权限码覆盖必须纳入质量门禁或架构扫描。
