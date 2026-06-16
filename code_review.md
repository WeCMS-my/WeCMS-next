# code_review.md — WeCMS Next Code Review Checklist

> 本文件是 WeCMS Next 项目的代码审查基线。  
> 审查目标不是只看代码能否运行，而是确认代码是否符合架构、安全、JIT 运行时基线、SqlSugar ORM、后端契约优先、AI 二期边界等硬约束。

---

## 0. Review 结论分级

| 结论 | 含义 |
|---|---|
| `BLOCK` | 阻断合并，存在 P0/P1 风险或违反硬约束 |
| `REQUEST_CHANGES` | 需要修改后再审查 |
| `APPROVE_WITH_NOTES` | 可合并，但有后续优化建议 |
| `APPROVE` | 可合并，无明显问题 |

---

## 1. P0 阻断项

出现任意一项，必须阻断合并：

```text
[ ] build / test / publish 失败
[ ] 使用 MVC Controller
[ ] 使用 Razor / Razor Pages
[ ] 使用 EF Core
[ ] 使用 dynamic 查询/返回
[ ] SQL 中出现 SELECT *
[ ] 拼接用户输入 SQL
[ ] 模块层出现 `SqlSugarClient` / `ISqlSugarClient` / `Ado` 原始 SQL API
[ ] WeCms.Modules.* 出现 SQL 文本
[ ] WeCms.Modules.* 引用 SqlSugar ORM / MySqlConnector
[ ] WeCms.Modules.* 直接引用 WeCms.Persistence 实现
[ ] WeCms.Api / WeCms.Infrastructure / WeCms.Shared 出现 SQL 文本、ORM Client、数据库连接或 Repository implementation
[ ] DTO 未加入 JsonSerializerContext
[ ] 业务 Endpoint 未绑定权限码且未显式 AllowAnonymous
[ ] 前端改写后端接口结构
[ ] request interceptor 重塑业务 data
[ ] Refresh Token 明文入库
[ ] password/token/secret/2FA 信息写入日志
[ ] 文件物理路径返回前端
[ ] 文件下载/预览无鉴权
[ ] 用户可删除或禁用自己
[ ] 系统可能删除最后一个超级管理员
[ ] 一期实现 AI runtime
[ ] 创建 WeCms.Modules.Ai 或 AI Provider
[ ] AI 代码直连 CMS 数据库或文件存储
[ ] 提交真实 secret、生产连接串、真实 token、生产数据 dump
[ ] 新增有副作用服务类没有 I* 接口
[ ] 业务类内部 new 出有副作用依赖
[ ] 单类承担复合职责且未拆分
[ ] 单个手写生产代码文件超过 600 行且无拆分说明
[ ] 命名空间与目录不一致
[ ] 跨工程引用违反依赖矩阵
[ ] 为隐式兼容加入静默 catch、默认值兜底、legacy 分支、dead fallback
[ ] ≥ 200 行变更或公共契约变更没有 docs/specs/<change-id>/ 三件套
[ ] 逻辑变更没有对应测试证据
[ ] bugfix 没有先提交可复现失败测试
[ ] 当前任务未完成测试、门禁、审计闭环就启动下一项任务
[ ] 为通过门禁而降低阈值、删除测试或绕过检查
```

M1-BE 额外阻断项：

```text
[ ] 修改 frontend/**、运行 pnpm 或生成前端 generated 类型
[ ] 在 M1-BE 中实现 CMS 内容 API、旧系统数据迁移、旧系统兼容模式、多租户或插件系统
[ ] M1-BE 业务 Endpoint 缺少权限码或 PermissionMetadata
[ ] M1-BE 写操作缺少审计记录
[ ] M1-BE 新增权限码、菜单、公共 API、数据库表、migration 或安全策略变更没有 spec 三件套
[ ] super_admin 未覆盖全部 M1-BE 系统管理权限 seed
```

---

## 2. 架构 Review

### 2.1 后端架构

检查项：

```text
[ ] 是否使用 ASP.NET Core Minimal APIs
[ ] 是否使用 .NET 10
[ ] 是否使用 JIT publish/runtime 基线
[ ] 是否使用 CreateSlimBuilder
[ ] 是否没有 MVC Controller
[ ] 是否没有 Razor / Razor Pages
[ ] Endpoint 是否显式注册
[ ] 是否没有运行时 Endpoint 扫描
[ ] 是否没有动态代理 AOP
[ ] 是否没有 runtime code generation
[ ] 是否没有在核心业务路径使用 Newtonsoft.Json
[ ] 新增 NuGet 包是否说明运行时兼容性、License、维护状态、替代方案
```

阻断条件：

```text
- 引入 Controller/MVC/Razor。
- 为了快速实现绕过 Minimal API 显式注册。
- 变更与 JIT 运行时基线冲突且无单独架构决策。
```

### 2.2 分层边界

```text
[ ] Endpoint 只处理 HTTP 绑定和返回
[ ] Service / UseCase 负责业务规则
[ ] WeCms.Persistence 中的 Repository 只负责 SQL 和数据映射
[ ] Repository interface 只定义在模块层或 Shared，Repository implementation 只存在于 WeCms.Persistence
[ ] 事务由 Service / UseCase 控制
[ ] Endpoint 中没有直接写 SQL
[ ] Repository 中没有 HTTP/权限/审计逻辑
[ ] DTO 没有跨模块随意复用
[ ] WeCms.Infrastructure 没有反向依赖业务模块
```

---

## 3. SqlSugar ORM Review

### 3.1 SQL 基线

```text
[ ] 是否使用 SqlSugar ORM
[ ] 是否没有 EF Core
[ ] 是否没有 dynamic 查询/返回
[ ] 是否没有 SELECT *
[ ] SQL 是否显式列出字段
[ ] 是否没有拼接用户输入
[ ] 排序字段是否白名单
[ ] 分页是否有上限
[ ] Repository 是否支持 CancellationToken
```

### 3.2 数据库边界

```text
[ ] 只有 WeCms.Persistence 引用了 SqlSugar ORM / MySqlConnector
[ ] WeCms.Modules.* 不直接处理 SQL 字符串
[ ] WeCms.Modules.* 未出现 SqlSugarClient / ISqlSugarClient / Ado 原始 SQL API
[ ] WeCms.Api / WeCms.Infrastructure / WeCms.Shared 未出现 SQL 文本、ORM Client、数据库连接或 Repository implementation
[ ] WeCms.Modules.* 不直接依赖持久化实现
[ ] WeCms.Persistence 只做数据访问适配，不承载业务规则、权限编排、审计编排或 HTTP 逻辑
```

---

## 4. OOP / SOLID Review

```text
[ ] 新增有副作用服务类是否先定义 I* 接口
[ ] 有副作用依赖是否通过构造函数注入
[ ] Repository、UnitOfWork、时钟、密码、Token、随机数等有副作用依赖是否通过接口 + DI 获取
[ ] 业务类内部是否没有 new 有副作用依赖
[ ] 单类是否只承担单一职责
[ ] 跨阶段模型是否优先使用 record / 只读属性 / 不可变集合
[ ] 业务模块是否依赖抽象而非具体基础设施实现
[ ] 逻辑是否可测试
```

---

## 5. Agile / Spec Review

```text
[ ] diff ≥ 200 行是否有 docs/specs/<change-id>/ 三件套
[ ] 新增公共 API / OpenAPI 契约是否有 spec
[ ] 新增数据库表 / migration 是否有 spec
[ ] 新增权限码、菜单、状态机是否有 spec
[ ] 修改认证、授权、Token、审计、文件上传、安全策略是否有 spec
[ ] PR diff 是否 ≤ 400 行，超出是否说明原因
[ ] PR 是否包含 Closes # 或 Spec: 链接
[ ] 文档是否同步更新
```

---

## 6. TDD Review

```text
[ ] 代码逻辑变更是否遵循 Red → Green → Refactor
[ ] bugfix 是否先提交可复现失败测试
[ ] 纯业务规则是否有单元测试
[ ] Repository / SQL 是否有集成测试
[ ] Minimal API 是否有 Endpoint 集成测试
[ ] OpenAPI / generated 类型是否有契约测试
[ ] 是否没有通过删除测试、降低覆盖率或删断言来过门禁
```

---

## 7. Definition of Done Review

```text
[ ] 已遵循 Red → Green → Refactor，或明确 N/A
[ ] 任务按单任务串行闭环推进，没有并行推进多个实现任务
[ ] 如使用 sub agent，主 agent 仍负责最终改动、验证与审计结论
[ ] 已运行 `scripts/quality-gate-backend.sh` 或等效命令
[ ] 新增有副作用服务类已暴露为 I* 接口并通过构造函数注入
[ ] 改动文件均 ≤ 600 行
[ ] 命名空间匹配目录
[ ] 跨工程引用未越过依赖矩阵
[ ] 未引入隐式兼容兜底、静默 catch、legacy 分支、dead fallback
[ ] ≥ 200 行或公共契约变更已有 spec 三件套
[ ] 文档已同步更新，如涉及
[ ] PR 描述含 Closes # 或 Spec: 链接
[ ] publish 已实际运行并通过
[ ] 前端 typecheck / build 已实际运行并通过，如涉及前端
[ ] 当前开发任务对应测试和质量门禁已实际运行并通过
[ ] 当前开发任务对应审计已实际运行并通过
[ ] 全部任务完成后已对本次改动范围执行最终总审计
[ ] 未实现一期禁止的 AI runtime 能力
```

规则文档例外：

```text
[ ] 仅当本次改动只涉及规则文档/流程文档时，才允许不跑 build/test/publish/typecheck/lint
[ ] 即使适用例外，也必须完成文档一致性检查与规则审计
[ ] 若混入生产代码、测试代码、脚本或生成产物改动，则不得使用该例外
```

---

## 8. CI/CD Review

后端必须通过：

```bash
dotnet build backend/WeCms.slnx -warnaserror
dotnet test backend/WeCms.slnx
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false
```

前端如涉及前端阶段或修改 `frontend/**`，必须通过：

```bash
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
```

检查项：

```text
[ ] publish 是否作为阻断门禁
[ ] warning 是否作为 error
[ ] OpenAPI 生成是否验证
[ ] M1-BE 系统管理 API paths 是否全部进入 OpenAPI
[ ] M1-BE POST / PUT 是否都有 requestBody schema
[ ] M1-BE 业务 Endpoint 是否全部有权限元数据覆盖
[ ] M1-BE 系统权限 seed / 菜单 seed 是否可幂等执行
[ ] SQL 规范扫描是否计划/已实现
[ ] generated 类型是否检查未手写
[ ] 是否没有把 secret 写入 CI
[ ] 是否没有连接生产数据库
```

禁止绕过：

```text
[ ] 调低覆盖率阈值
[ ] 删除测试或断言
[ ] 跳过 publish
[ ] 移除安全扫描
[ ] 临时关闭 lint / typecheck
[ ] 把失败项标记为后续再修但仍合并
```

---

## 9. AI 边界 Review

```text
[ ] 一期未实现 AI runtime
[ ] 未创建 WeCms.Modules.Ai
[ ] 未直连 CMS 数据库
[ ] 未直连 CMS 文件存储
[ ] 未写入 AI Provider Key
```

---

## 10. 最终合并门禁

```text
[ ] 架构 Review 通过
[ ] 安全 Review 通过
[ ] SQL Review 通过，如涉及 SQL
[ ] 前端 Review 通过，如涉及前端
[ ] dotnet build -warnaserror 通过
[ ] dotnet test 通过
[ ] dotnet publish 通过
[ ] pnpm typecheck 通过，如涉及前端
[ ] pnpm build 通过，如涉及前端
[ ] OpenAPI 生成通过，如涉及 API
[ ] 人工 Review 通过
```
