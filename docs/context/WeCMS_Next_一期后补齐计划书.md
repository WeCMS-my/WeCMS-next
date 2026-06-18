# WeCMS Next 一期后补齐计划书

## 1. 文档定位

本文档是一期后 hardening / 补齐阶段的稳定入口文件名，供 Codex / AI Agent / PR Review 优先引用。

当前详细任务正文维护在：

- `docs/context/WeCMS_Next_一期后建议补齐清单详细开发修复计划书_v1.1_任务说明增强版.md`

历史版本保留用于对照：

- `docs/context/WeCMS Next 一期后建议补齐清单详细开发修复计划书 v1.0.md`

## 2. 当前状态

WeCMS Next 一期已完成：

- M0-BE 后端底座。
- M1-BE 系统管理 API。
- M2-FE 基础系统前端管理端。

当前进入一期后 hardening / 补齐阶段。

## 3. 稳定执行边界

一期后补齐阶段必须遵守：

- 不把 CMS 内容能力回流到一期。
- 不引入 AI runtime。
- 不做旧 ThinkPHP runtime compatibility。
- 不做旧数据迁移或旧密码 hash 兼容。
- 不复制旧 AdminGate。
- 数据库访问只能在 `WeCms.Persistence`。
- 业务模块只能依赖接口和 `WeCms.Shared` 抽象。
- 所有写接口必须具备明确 HTTP Method、权限码、DTO 校验和 Audit Log。
- 高风险操作必须补充 Security Event，必要时要求当前密码、2FA 或 challenge。
- Refresh token 不允许回到 localStorage。

## 4. 推荐执行顺序

详细任务以 v1.1 任务说明增强版为准，稳定分组如下：

| 阶段 | 目标 |
| --- | --- |
| H0 | 文档与状态修复 |
| H1 | 优先安全 hardening：2FA、个人中心、安全中心、AdminGate / CSRF 拆解落地 |
| H2 | CMS 二期前建议补齐：i18n、菜单排序、字典状态、设置、文件策略、安全增强链路 |
| H3 | 全量质量门禁、差异复核、冻结基础系统 |

每次执行只能推进一个明确任务项。当前任务未完成测试、门禁和审计前，不得启动下一项。

## 5. H0 状态

H0 的目标是修正项目状态、ADR、验收边界和质量门禁说明，避免后续开发基线混乱。

H0 交付物：

- README 当前阶段更新。
- 一期完成状态说明。
- token storage ADR 更新为 HttpOnly Cookie 基线。
- 一期后补齐计划书稳定入口。
- AdminGate / CSRF 迁移 ADR。
- AdminGate / CSRF 迁移设计说明。

## 6. 质量门禁

后端：

```bash
bash scripts/quality-gate-backend.sh
```

前端：

```bash
bash scripts/quality-gate-frontend.sh
```

规则文档和流程文档修改可按 `AGENTS.md` 的规则文档例外处理，但 H0 计划要求 backend gate 与 frontend gate 均通过；若本地环境阻断，必须在任务结果中明确归类为环境阻断，不得伪造通过。
