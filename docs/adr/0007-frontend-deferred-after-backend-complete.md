# ADR-0007：前端后移，后端 API 全部完成后再开发

## 状态

Accepted

## 背景

WeCMS Next 采用后端契约优先策略。SoybeanAdmin 是 UI 模板，不是 API 契约来源。前端一切数据格式以后端 DTO / OpenAPI 为准。

在 M0 阶段初期，后端 API 契约尚未稳定，Auth、用户、角色、菜单、权限等核心模块仍在建设中。如果前端与后端并行开发，将导致：

- 前端频繁适配后端契约变更。
- 前端 mock 数据与真实 API 不一致。
- 前端 generated 类型反复重新生成。
- 前后端联调阻塞。

## 决策

1. 前端 SoybeanAdmin 开发整体后移，等后端全部 API 完成并稳定后再进入前端开发。
2. M0-BE 阶段不操作 `frontend/` 目录。
3. M0-BE 阶段不运行 `pnpm` 命令。
4. M0-BE 阶段不生成前端 TypeScript generated 类型。
5. M0-BE 阶段不修改 SoybeanAdmin request / route / store / view。
6. M0-BE 阶段 OpenAPI 仅作为后端契约产物输出到 `artifacts/openapi/`，不用于前端类型生成。

## 前端进入条件

后端全部 API 完成并稳定后，才进入前端开发。具体条件：

1. 后端 Auth API 完成（login / refresh / logout / me）。
2. 用户、角色、菜单、权限 API 完成。
3. 系统基础 API 完成（配置、字典、文件、日志等）。
4. CMS 内容 API 完成（栏目、文章、媒体等）。
5. OpenAPI 契约稳定，无 breaking change。
6. 后端 quality gate 全部通过。

## 阶段划分

```text
M0-BE：后端-only 工程底座重建（当前阶段）
  ↓ 后端全部 API 完成并稳定
M0.5-FE：SoybeanAdmin 接入验证
  ↓
M1：完整认证安全闭环（前后端联调）
M2：用户、角色、菜单、权限正式业务模块
M3：系统基础模块
M4：CMS 内容模块
```

## M0-BE 前端红线

M0-BE 阶段明确禁止：

```text
不操作 frontend/soybean-admin
不初始化 SoybeanAdmin
不修改前端 request 封装
不生成 frontend/src/service/generated
不运行 pnpm install / typecheck / lint / build
不做前端登录页
不做 Dashboard
不做动态路由
不做按钮权限
```

## 影响

### 正向影响

- 后端 API 可先稳定，再进入前端开发。
- 避免前后端并行开发导致的契约不一致。
- 前端 generated 类型一次性生成，减少反复。
- 降低 M0 阶段复杂度。

### 代价

- 前端开发启动时间后移。
- 无法在 M0 阶段验证前后端联通。
- 前端 UI 验收延后。

## 关联 ADR

- [ADR-0005：旧系统不做数据迁移，不做兼容模式](./0005-no-legacy-data-migration-and-frontend-deferred.md)
- [ADR-0006：Native AOT / Trim 警告例外管理](./0006-aot-trim-warnings-exception.md)


