# ADR-0012：M0-BE 前端后移与 backend-only 边界

## 状态

Accepted

## 背景

ADR-0007 已确认前端 SoybeanAdmin 后移。M0-BE 计划进一步收紧为 backend-only：先完成后端工程底座、Auth、权限、System API、OpenAPI export、质量门禁和 CI，再进入后续前端阶段。

## 决策

1. M0-BE 不开发 `frontend/**`。
2. M0-BE 不运行 `pnpm`。
3. M0-BE 不生成前端 TypeScript generated 类型。
4. M0-BE 的 OpenAPI 产物只作为后端契约交付物，不驱动前端代码生成。
5. SoybeanAdmin 只作为后续前端阶段 UI 模板，不得反向修改 M0-BE 后端契约。

## 影响

- M0-BE 验证聚焦后端 build/test/publish、OpenAPI、数据库边界、DI 边界和代码审计。
- 前端 typecheck/lint/build 在 M0-BE 阶段为 N/A，除非后续任务明确修改前端。
- `check-no-frontend-change` 是后端质量门禁的一部分。

## 验收

- M0-BE 任务不修改 `frontend/**`。
- 本地和 CI 的 backend quality gate 能证明前端未被改动。
- README 和计划书记录此 backend-only 边界。
