# WeCMS Next H3 基础系统冻结基线

日期：2026-06-19

## 结论

H3 总体验收以基础系统后台和 SoybeanAdmin 管理端为冻结对象。当前基线允许后续 CMS 二期依赖认证、权限、文件、设置、日志、安全、i18n 和 OpenAPI 契约继续开发，但本基线本身不启动 CMS 二期。

本基线继续禁止：

- CMS 内容管理能力回流一期。
- AI runtime、AI Provider、Prompt / RAG / Vector Store / Agent Tool 运行时代码。
- 旧 ThinkPHP runtime compatibility。
- 复制旧 AdminGate。
- 业务模块直接访问 SqlSugar、MySQL 连接或 Persistence 具体实现。

## 冻结范围

当前冻结的基础系统能力：

- 认证：login、refresh、logout、me、HttpOnly refresh cookie、Cookie Origin / CSRF 防护。
- 账号自服务：个人资料、密码、头像、账号安全、账号 2FA。
- 系统管理：用户、角色、权限、菜单、部门、岗位、字典、设置、文件。
- 日志与安全：登录日志、审计日志、安全事件、安全封禁、IP 规则、限流、SecurityEventClassifier。
- 权限刷新：PermissionVersion 闭环。
- 前端管理端：登录、2FA 登录、账号页、系统管理页、安全中心、日志页、文件页、i18n 页。
- 契约与质量：OpenAPI、generated 类型、前后端质量门禁。

## 冻结产物

- OpenAPI：`artifacts/openapi/wecms-api-v1.json`
- H3 spec：`docs/specs/h3-final-acceptance/{spec.md,tasks.md,checklist.md}`
- 数据库迁移：`database/migrations/000001_init_identity.sql` 至 `database/migrations/000019_h2_security_event_classifier.sql`
- Seed：`database/seeds/000001_seed_base_permissions.sql` 至 `database/seeds/000010_seed_h2_setting_hardening_permissions.sql`
- 后端门禁：`scripts/quality-gate-backend.sh`
- 前端门禁：`scripts/quality-gate-frontend.sh`
- 差异复核：
  - `scripts/checks/check-cookie-auth-origin-protection.sh`
  - `scripts/checks/check-admingate-csrf-migration.sh`
  - `scripts/checks/check-thinkphp-feature-delta.sh`

## H3 验收项

- H3-001 后端全量质量门禁：通过。
- H3-002 前端全量质量门禁：通过。
- H3-003 OpenAPI 合同复核：通过。
- H3-004 权限码覆盖复核：通过。
- H3-005 Audit log 覆盖复核：通过。
- H3-006 Security event 覆盖复核：通过。
- H3-007 Cookie 型认证接口 CSRF/Origin 覆盖复核：通过。
- H3-008 旧 ThinkPHP AdminGate/CSRF 差异复核：通过。
- H3-009 旧 ThinkPHP 功能差异复核：通过。
- H3-010 CMS 二期启动前冻结基础系统：本文档建立冻结基线。

## 验证命令

H3 冻结前必须通过：

```bash
WECMS_BACKEND_GATE_FRONTEND_SCOPE=includes-frontend bash scripts/quality-gate-backend.sh
bash scripts/quality-gate-frontend.sh
bash scripts/checks/check-cookie-auth-origin-protection.sh
bash scripts/checks/check-admingate-csrf-migration.sh
bash scripts/checks/check-thinkphp-feature-delta.sh artifacts/openapi/wecms-api-v1.json
```

最近一次 H3 执行结果：

- 后端门禁：25/25 通过，包含 build、test、publish、OpenAPI、权限、审计、安全事件、Cookie Origin、AdminGate/CSRF 差异、旧功能差异、DB/layer/DI、migration/seed smoke。
- 前端门禁：通过，包含 lint、typecheck、build、配置测试、no-v-html、路由权限扫描、smoke fixture。
- 已知非阻断项：Vite 构建报告 `INEFFECTIVE_DYNAMIC_IMPORT` warning，不影响当前验收。

## CMS 二期入口约束

CMS 二期启动前必须以本基线为依赖边界：

- CMS 只能新增独立 CMS 内容模型、权限、菜单、OpenAPI、前端页面和迁移，不得修改冻结基线的认证与权限语义，除非另立 spec。
- CMS 不得直连旧 ThinkPHP 数据库或复用旧 runtime session/token/password hash。
- CMS 不得绕过 PermissionEndpointFilter、Audit Log、Security Event、rate limit 和文件安全策略。
- CMS 如需迁移旧数据，必须作为二期独立迁移项目，不能把 legacy compatibility 放入运行时主路径。

## 未完成项与风险

- 未创建 Git tag；本文件作为 release baseline。正式 tag 由发布负责人在人工 review 后执行。
- 生产 `Security:AllowedOrigins` 仍需由部署环境显式配置；代码和门禁禁止 wildcard 与生产关闭 Origin 校验。
- CMS 二期的内容管理功能未在本基线实现，属于明确后置范围。
