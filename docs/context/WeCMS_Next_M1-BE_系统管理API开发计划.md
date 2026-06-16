# Context: WeCMS Next M1-BE System Management API Plan

本文件是 M1-BE 系统管理 API 阶段的稳定入口文件名，供 Codex / AI Agent / 自动化脚本优先读取。

当前仓库对应的真实正文文档是：

- `docs/context/WeCMS Next M1-BE 后端-only 开发计划书 v1.0.md`

关联 ADR：

- `docs/adr/0013-m1-system-management-api-scope.md`

阅读顺序要求：

1. 先阅读 `AGENTS.md`、`code_review.md`、`.trae/rules/wecms-engineering-principles.md`。
2. 再阅读 `docs/context/01-thinkphp-system.md`、`docs/context/02-next-migration-plan.md`、`docs/context/03-engineering-delivery.md`、`docs/context/04-m0-skeleton-validation.md` 及其指向的正文。
3. 最后完整阅读 `docs/context/WeCMS Next M1-BE 后端-only 开发计划书 v1.0.md`。

维护规则：

- 本文件只承担稳定路径与映射说明，不复制完整正文，避免双份事实源漂移。
- 如果 M1-BE 计划正文重命名，本文件必须同步更新。
- M1-BE 仍为 backend-only 阶段，不修改 `frontend/**`，不运行 `pnpm`，不生成前端 TypeScript generated 类型。
- M1-BE 只做系统管理 API，不做 CMS 内容 API，不做旧系统数据迁移，不做一期禁止的 AI runtime。
