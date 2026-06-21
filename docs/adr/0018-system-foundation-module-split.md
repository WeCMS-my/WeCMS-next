# ADR-0018：系统基础模块拆分并最终删除 System God Module

## 状态

Accepted

## 背景

`WeCms.Modules.System` 在 M0/M1 早期承载了认证、用户、角色、权限、菜单、组织、字典、设置、国际化、日志、安全、文件和平台探针等系统基础能力。这个聚合模块在早期交付阶段降低了接入成本，但继续扩展会让它成为 God Module。

系统基础破坏性升级允许不兼容旧结构，因此本阶段不再把 `WeCms.Modules.System` 作为长期模块边界。

## 决策

1. `WeCms.Modules.System 最终删除`。
2. 迁移期间允许旧 WeCms.Modules.System 暂存，但只能作为过渡 allow-list。
3. 最终验收不得保留 WeCms.Modules.System。
4. 系统基础能力拆分为以下模块：
   - `WeCms.Modules.Identity`
   - `WeCms.Modules.AccessControl`
   - `WeCms.Modules.Organization`
   - `WeCms.Modules.Configuration`
   - `WeCms.Modules.Audit`
   - `WeCms.Modules.Security`
   - `WeCms.Modules.FileCenter`
   - `WeCms.Modules.Platform`
5. `WeCms.Modules.Cms 暂不启用`，不得把系统基础能力放入 CMS 模块。

## 拆分映射

| 旧 System 子域 | 新模块 |
|---|---|
| Auth | `WeCms.Modules.Identity` |
| TwoFactor | `WeCms.Modules.Identity` |
| Users | `WeCms.Modules.Identity` |
| Roles | `WeCms.Modules.AccessControl` |
| Permissions | `WeCms.Modules.AccessControl` |
| Menus | `WeCms.Modules.AccessControl` |
| Departments | `WeCms.Modules.Organization` |
| Posts -> Positions | `WeCms.Modules.Organization` |
| Dicts | `WeCms.Modules.Configuration` |
| Settings | `WeCms.Modules.Configuration` |
| I18n | `WeCms.Modules.Configuration` |
| Logs | `WeCms.Modules.Audit` |
| Security | `WeCms.Modules.Security` |
| Files | `WeCms.Modules.FileCenter` |
| System | `WeCms.Modules.Platform` |

## Posts -> Positions

系统岗位必须从 `Post` / `Posts` 破坏性重命名为 `Position` / `Positions`：

```text
Post -> Position
Posts -> Positions
sys_post -> sys_position
sys_user_post -> sys_user_position
PostService -> PositionService
IPostRepository -> IPositionRepository
PostPermissions -> PositionPermissions
```

原因：

- `Post` 在 CMS 语境中容易被理解为文章。
- 系统岗位领域应使用 `Position`。
- 后续 CMS 内容模块不得与系统岗位命名冲突。

## 依赖规则

目标模块只允许依赖 `WeCms.Shared` 和必要的跨模块 Contracts 抽象。业务模块不得引用持久化实现、数据库连接器、ORM Client 或 SQL 文本。

最终依赖治理必须由架构测试和质量门禁强制执行：

- `WeCms.Modules.*` 不得引用 `*.SqlSugar`。
- `WeCms.Modules.*` 不得引用数据平台实现。
- `WeCms.Modules.*` 不得包含 SQL 文本。
- `WeCms.Modules.Cms` 在系统基础升级期间不得进入 API 引用、OpenAPI 覆盖或质量门禁功能覆盖。

## 迁移策略

1. S0 阶段先建立 ADR、架构测试和门禁表达。
2. S1 阶段创建目标模块骨架。
3. S4-S8 按 Identity、AccessControl、Organization、Configuration、Audit/Security/FileCenter/Platform 串行迁移。
4. S9 关闭迁移期 allow-list，删除旧 `WeCms.Modules.System` 项目和 namespace。

## 验收

- 迁移期：架构测试必须明确识别旧 System 项目只允许暂存。
- 最终期：开启最终验收标志后，`WeCms.Modules.System` 存在即失败。
- 代码中不得保留系统岗位旧命名：`sys_post`、`UserPost`、`PostService`、`IPostRepository`、`PostPermissions`。
- 文档历史说明可以保留旧名称，但必须标明历史上下文。

## S14 最终状态

S14 最终清理验收时，`WeCms.Modules.System` 已从 active source、solution/project references 和 OpenAPI/质量门禁覆盖面中移除。系统基础能力由 Identity、AccessControl、Organization、Configuration、Audit、Security、FileCenter 和 Platform 模块承载。

## 关联

- `docs/context/WeCMS_next_系统基础破坏性升级技术书_v3_不引入Controller.md`
- `docs/context/WeCMS_next_系统基础破坏性升级开发计划书_v1.md`
