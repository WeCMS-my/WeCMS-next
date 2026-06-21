# WeCMS 工程落地执行计划与交付工件

> 说明：本文件已按 JIT 运行时基线与 S14 系统基础破坏性升级最终状态更新。
> 关联执行文档：`WeCMS_工程骨架验证文档.md`、`docs/dirs/system-foundation-development-guide.md`
> 说明：M0-BE backend-only 是历史阶段边界；当前 active source 已进入系统基础模块拆分后的最终结构。

---

## 1. 目标

把迁移计划转换为可执行的工程交付顺序与验收工件。

当前交付基线：

- 后端：ASP.NET Core Minimal APIs + .NET 10 + JIT publish/runtime
- 数据访问：SqlSugar ORM + `WeCms.Data.SqlSugar` + `WeCms.Modules.*.SqlSugar`
- 契约：OpenAPI
- 前端：SoybeanAdmin

---

## 2. 当前必须交付的工件

### 2.1 后端工件

- `backend/WeCms.slnx`
- `WeCms.Api`
- `WeCms.Shared`
- `WeCms.Infrastructure`
- `WeCms.Data.SqlSugar`
- `WeCms.Caching`
- `WeCms.EventBus`
- `WeCms.Aop`
- `WeCms.Modules.Identity`
- `WeCms.Modules.AccessControl`
- `WeCms.Modules.Organization`
- `WeCms.Modules.Configuration`
- `WeCms.Modules.Audit`
- `WeCms.Modules.Security`
- `WeCms.Modules.FileCenter`
- `WeCms.Modules.Platform`
- `WeCms.Modules.*.SqlSugar`
- `WeCms.Modules.Cms` 仅保留为二期内容模块占位；不参与系统基础 API、OpenAPI 或质量门禁功能覆盖
- Json serializer context
- OpenAPI artifact

### 2.2 文档工件

- `AGENTS.md`
- `code_review.md`
- `.trae/rules/wecms-engineering-principles.md`
- `docs/specs/<change-id>/`
- `docs/adr/`

### 2.3 验证工件

- build 结果
- test 结果
- publish 结果
- OpenAPI 产物
- 数据库边界 / DI 边界 / 分层边界审计结果
- 前端 typecheck / lint / build 结果（仅后续前端阶段涉及时提供）

---

## 3. 执行阶段

### 3.0 单任务串行闭环

执行顺序必须固定为：

1. 拆分任务列表
2. 选定当前唯一执行任务
3. 需要时使用 `sub agent` 做辅助分析或证据收集
4. 主 agent 完成改动
5. 运行当前任务测试
6. 运行当前任务门禁
7. 执行当前任务审计
8. 只有测试、门禁、审计全部通过，才允许切换到下一项任务
9. 全部任务完成后，对本次改动范围执行一次最终总审计

补充说明：

- `sub agent` 可以帮助提效，但不能把多个实现任务并行推进
- 主 agent 必须统一收口最终方案、验证结论和审计结论
- 若测试、门禁或审计任一失败，只能继续修复当前任务，不能顺带推进后续任务

### 阶段 A：工程底座

- 建 solution
- 建 Minimal API Host
- 建共享结果模型
- 建异常处理中间件
- 建 Json serializer context
- 建 Health endpoint

### 阶段 B：SqlSugar 数据平台

- 约束 `WeCms.Data.SqlSugar` 为 SqlSugar 数据平台层
- 约束 `WeCms.Modules.*.SqlSugar` 为模块持久化适配层
- 接入 SqlSugar ORM
- 建 repository port / adapter 结构
- 落数据库边界检查

### 阶段 C：契约交付

- 建 OpenAPI 产物
- 保证后端契约一致
- 前端从 OpenAPI 生成类型属于后续前端阶段

### 阶段 D：认证与系统模块

- Auth 最小闭环
- 用户/角色/菜单/权限
- 审计与安全事件

---

## 4. 当前门禁

后端：

```bash
dotnet build backend/WeCms.slnx -warnaserror
dotnet test backend/WeCms.slnx
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false
```

说明：

- 当前不再使用 Native AOT publish gate
- 当前发布要求是标准 JIT publish
- 当前系统基础升级允许已验收的 SoybeanAdmin 前端基础系统进入质量门禁；后端-only 任务仍不得顺带修改 `frontend/**`
- 规则制定或规则文档修改属于文档治理例外，可不执行本节门禁
- 上述例外仅适用于规则/流程文档本身；若伴随代码、测试、脚本或生成产物改动，则仍需完整执行门禁

---

## 5. 必须持续保持的约束

- Minimal API Only
- `CreateSlimBuilder`
- OpenAPI 契约优先
- SqlSugar / MySqlConnector / ORM Client 仅限 `WeCms.Data.SqlSugar` 与 `WeCms.Modules.*.SqlSugar`
- `WeCms.Modules.*` 不得引用 ORM / MySQL 连接器
- `WeCms.Modules.*` 不得持有 SQL 文本或持久化实现依赖
- Repository interface 只保留在模块层或 `WeCms.Shared`，implementation 只允许在 `WeCms.Modules.*.SqlSugar`
- `WeCms.Modules.System` 与 `WeCms.Persistence` 已退出 active source，不得重新引入
- Service / UseCase 获取 Repository、UnitOfWork、Clock、Token、密码、随机数等有副作用依赖时必须通过接口 + DI
- 禁止 `dynamic`
- 禁止 `SELECT *`
- 禁止拼接用户输入 SQL
- AI runtime 仍为一期禁止项

---

## 6. 交付验收表

| 类别 | 验收项 | 标准 |
|---|---|---|
| Build | `dotnet build` | 成功 |
| Test | `dotnet test` | 成功 |
| Publish | `dotnet publish` | 成功 |
| Contract | OpenAPI | 可生成 |
| ORM | SqlSugar 边界 | 通过 |
| Boundary | Data.SqlSugar / DI / Layer Audit | 通过 |
| Frontend | typecheck / lint / build | 涉及前端时必须通过 |
| Audit | 当前任务审计 | 通过 |
| Final Audit | 本次改动范围最终总审计 | 通过 |
