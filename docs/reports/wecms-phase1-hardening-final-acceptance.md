# WeCMS Next 一期 Hardening 最终验收记录

文档版本：v1.0
验收日期：2026-06-19
项目：WeCMS Next
阶段：一期 hardening / 一期后补齐收口
验收结论：PASS
最终审计结论：APPROVE

---

## 1. 验收结论

WeCMS Next 一期 hardening / 一期后补齐当前阶段已完成最终复审。

经多轮问题修复、静态审计、质量门禁检查与 GitHub Actions 验证，当前阶段结论为：

```text
APPROVE
Phase 1 hardening stable point accepted
Backend gate: PASS
Frontend gate: PASS
Known blocking issues: 0
Known residual audit items: 0
```

因此，本阶段可以作为 **WeCMS Next 一期 hardening 稳定验收点** 进行固化。

---

## 2. 验收范围

本次验收覆盖以下范围：

### 2.1 Backend

* ASP.NET Core Minimal APIs
* `.NET 10`
* JIT publish/runtime
* SqlSugar ORM / MySQL
* Persistence 数据访问边界
* System 模块基础后台能力
* Auth / Refresh Token / 2FA
* Users / Roles / Menus / Permissions
* Departments / Posts / Dicts / Settings
* Files / Logs / Security / I18n
* OpenAPI 导出与前后端契约
* Migration / Seed smoke test
* AdminGate / CSRF 职责迁移检查
* ThinkPHP feature delta 检查

### 2.2 Frontend

* SoybeanAdmin / Vue 3
* Vite dev proxy
* API contract generated types
* Route permission coverage
* No CMS frontend boundary
* No `v-html`
* Frontend smoke fixtures
* Access token memory-only handling
* Refresh token cookie flow

### 2.3 CI / Gate

* Backend quality gate
* Frontend quality gate
* GitHub Actions workflow
* MySQL integration test service
* Local / CI NuGet audit mode boundary
* DB boundary check
* DI boundary check
* Layer dependency check
* Generated artifact check
* Replace write affected rows check

---

## 3. 明确不属于本阶段范围

以下内容不属于本次一期 hardening 验收范围：

* CMS 内容管理模块完整实现
* 旧 ThinkPHP runtime compatibility
* 旧 ThinkPHP 数据迁移
* AI runtime / AI provider / Prompt / RAG / Vector / Agent
* 生产级部署运维体系
* 多租户能力
* 完整生产安全加固
* 外部 SSO / OAuth / 企业身份源集成

以上能力如需继续推进，应进入后续独立阶段或二期规划。

---

## 4. 最终修复闭环摘要

本阶段多轮复审中发现的问题已完成闭环，主要包括：

### 4.1 Auth / Token / 2FA

* Refresh Token 改为安全 Cookie 方向。
* Refresh Token 明文存储风险已规避。
* Refresh token rotation / reuse / concurrent replay 语义已拆分。
* AuthService 过重职责已拆分。
* 2FA key 不再写入 Development 配置。
* `Auth:AccessTokenSecret` 与 `Security:TwoFactor:SecretProtectionKey` 均保持 fail-fast，无静默 fallback。

### 4.2 文件安全

* 文件下载 / 预览已鉴权。
* 文件预览 Content-Disposition header 注入风险已修复。
* 文件名危险字符校验已补充。
* 上传文件 SHA256、MIME、size、policy 校验链路已保留。
* 上传失败后的物理文件 cleanup 不再静默吞异常，已记录 warning。

### 4.3 Repository / DB 一致性

* 主实体写入已补充 affected rows 检查。
* UserRole / UserPost / RolePermission / RoleMenu replace 写入已改为：

  * delete 使用 `ExecuteOptionalAsync`
  * insert 使用 `ExpectOneAsync`
* 用户绑定 role / post 已过滤 soft-deleted 数据。
* 角色绑定 permission / menu 已过滤 soft-deleted 数据。
* DB 边界仍由 Persistence 统一承载，Modules 不直接访问 ORM / SQL。

### 4.4 Role / Permission / Menu

* Role 删除后已 bump 用户 permission version。
* 权限版本刷新链路已覆盖前端状态刷新。
* locked role / super admin 保护逻辑已保留。
* 用户自删 / 自禁风险已阻断。

### 4.5 I18n

* I18n 审计日志字段已对齐 `sys_audit_log.ip_address`。
* I18n create 写入已补充 affected rows 检查。
* I18n DTO 已纳入 JsonSerializerContext。

### 4.6 CI / Gate

* Backend gate 恢复 MySQL integration test。
* GitHub Actions 已配置 MySQL service。
* push / pull_request 默认不跳过 MySQL integration test。
* workflow_dispatch 默认不跳过 MySQL integration test。
* Backend gate 已扩展到 27 步。
* Frontend gate 已覆盖 lint / typecheck / build / contract / route permission / smoke fixtures。
* `WECMS_TEST_MYSQL_ALLOWED_HOSTS` 明确属于 integration test 侧职责，不属于 `quality-gate-backend.sh` 门禁参数。

### 4.7 文档

* README 已移除不存在的 compose / reset 脚本引用。
* README 已补充 local MySQL 启动方式。
* README 已补充 user-secrets 配置方式。
* README 已明确 backend gate 不读取 user-secrets。
* README 已明确 CI strict / 本地 fallback 的 NuGet audit 行为。
* README 已说明 `WECMS_TEST_MYSQL_ALLOWED_HOSTS` 由 integration test 消费用于 host 白名单校验。

---

## 5. 最终质量门禁结果

本阶段最终确认：

```text
GitHub Actions backend-quality-gate: PASS
GitHub Actions frontend-quality-gate: PASS
```

本地或 CI 推荐最终确认命令：

```bash
bash scripts/quality-gate-backend.sh
bash scripts/quality-gate-frontend.sh
```

---

## 6. 当前代码状态判断

当前代码状态满足一期 hardening 验收要求：

| 检查项                                | 结果   |
| ---------------------------------- | ---- |
| Backend build gate                 | PASS |
| Backend test gate                  | PASS |
| Backend publish gate               | PASS |
| MySQL integration test             | PASS |
| Migration / seed smoke test        | PASS |
| OpenAPI export                     | PASS |
| Permission coverage                | PASS |
| Audit coverage                     | PASS |
| DB boundary                        | PASS |
| DI boundary                        | PASS |
| Layer dependency                   | PASS |
| Frontend lint                      | PASS |
| Frontend typecheck                 | PASS |
| Frontend build                     | PASS |
| Frontend generated contract        | PASS |
| Frontend route permission coverage | PASS |
| Known P0 issues                    | 0    |
| Known P1 issues                    | 0    |
| Known P2 issues                    | 0    |
| Known P3 residual items            | 0    |

---

## 7. 风险声明

本次验收结论基于以下事实：

1. 多轮静态审计问题已完成修复。
2. GitHub Actions backend-quality-gate 已通过。
3. GitHub Actions frontend-quality-gate 已通过。
4. 当前阶段不包含 CMS 完整后端能力。
5. 当前阶段不包含 AI runtime。
6. 当前阶段不包含旧 ThinkPHP runtime compatibility。
7. 当前阶段不包含旧数据迁移。

如后续新增功能、修改架构边界、调整数据库模型或引入 CMS / AI / 旧系统兼容能力，应重新进入对应阶段审计流程。

---

## 8. 稳定点建议

建议将当前 main 分支状态固化为一期 hardening 稳定点。

建议操作：

```bash
git status
git log -1 --oneline
git tag v1-phase1-hardening-stable
git push origin v1-phase1-hardening-stable
```

如需更精确版本号，可采用：

```bash
git tag v0.1.0-phase1-hardening
git push origin v0.1.0-phase1-hardening
```

最终 tag 名称由项目版本策略决定。

---

## 9. 后续阶段建议

本阶段完成后，后续工作建议分为三条线推进：

### 9.1 一期后 production hardening

* 生产环境配置模板
* Secret 管理策略
* 部署环境变量清单
* 数据库备份 / 恢复策略
* 日志留存策略
* 运行时监控
* 安全事件告警
* 文件存储生产化
* HTTPS / Cookie / CORS 生产策略

### 9.2 二期 CMS 模块规划

* Channels
* Articles
* Pages
* Media
* Tags
* Links
* Revisions
* Publish logs
* Recycle bin
* Site / SEO settings

### 9.3 前后端体验补齐

* 管理端菜单体验完善
* 表单交互完善
* 错误提示统一
* 权限变更后的前端状态刷新体验
* I18n 文案补齐
* 文件上传体验优化

---

## 10. 最终验收签署

验收结论：

```text
WeCMS Next Phase 1 hardening: PASS
Final review result: APPROVE
Backend quality gate: PASS
Frontend quality gate: PASS
Known residual items: 0
```

本阶段可关闭。

后续新增任务不得继续混入一期 hardening 收口范围，应进入新阶段计划、TaskSpec 或独立修复分支。
