# code_review.md — WeCMS Next Code Review Checklist

> 本文件是 WeCMS Next 项目的代码审查基线。  
> Codex、DeepSeek、人工 Review、PR 审查、CI 门禁均应参考本文件。  
> 审查目标不是只看代码能否运行，而是确认代码是否符合架构、安全、AOT、SqlSugar ORM、后端契约优先、AI 二期边界等硬约束。

---

## 0. Review 结论分级

每次 Review 必须给出以下结论之一：

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
[ ] Native AOT publish 失败
[ ] 使用 MVC Controller
[ ] 使用 Razor / Razor Pages
[ ] 使用 EF Core
[ ] 使用 dynamic 查询/返回
[ ] SQL 中出现 SELECT *
[ ] 拼接用户输入 SQL
[ ] 模块层出现 `SqlSugarClient` / `ISqlSugarClient` / `Ado` 原始 SQL API 违反数据边界
[ ] WeCms.Modules.* 出现 SQL 关键字或 SQL 字符串
[ ] WeCms.Modules.* 引用 SqlSugar ORM / MySqlConnector
[ ] WeCms.Modules.* 直接引用 WeCms.Persistence 实现
[ ] DB-BOUNDARY-004：任何 `WeCms.Modules.*` 对持久化实现的依赖
[ ] DB-BOUNDARY-006：数据库边界突破（默认 `BLOCK`）
[ ] DTO 未加入 JsonSerializerContext 导致 AOT 风险
[ ] 业务 Endpoint 未绑定权限码且未显式 AllowAnonymous
[ ] 前端为了 SoybeanAdmin 改写后端接口结构
[ ] request interceptor 重塑业务 data
[ ] Refresh Token 明文入库
[ ] password/token/secret/2FA 信息写入日志
[ ] 文件物理路径返回前端
[ ] 文件下载/预览无鉴权
[ ] 用户可删除或禁用自己
[ ] 系统可能删除最后一个超级管理员
[ ] 一期实现 AI runtime 能力
[ ] 创建 WeCms.Modules.Ai 或 AI Provider
[ ] AI 代码直连 CMS 数据库或文件存储
[ ] 提交真实 secret、生产连接串、真实 token、生产数据 dump

[ ] 新增有副作用服务类没有 I* 接口
[ ] 业务类内部 new 出数据库/文件/网络/缓存/邮件/存储/时间/随机数等有副作用依赖
[ ] 单类同时承担采集、处理、输出、鉴权、SQL、审计等复合职责且未拆分
[ ] 单个手写生产代码文件超过 600 行且无拆分说明
[ ] C# 命名空间与目录不一致，IDE0130 未修复
[ ] 跨工程引用违反 WeCMS 依赖矩阵
[ ] InternalsVisibleTo 暴露给非测试生产工程
[ ] 为隐式兼容加入静默 catch、默认值兜底、legacy 分支、dead fallback 或无期限 Obsolete 转发
[ ] ≥ 200 行变更或公共契约变更没有 docs/specs/<change-id>/ 三件套
[ ] 代码逻辑变更没有对应 Red → Green → Refactor 测试证据
[ ] bugfix 没有先提交可复现失败测试
[ ] 为通过测试/覆盖率/AOT/安全门禁而降低阈值、删除测试或绕过检查
```

---


## 1.5 工程原则 Review（OOP / SOLID / Agile / TDD / DoD）

### 1.5.1 OOP / SOLID Review

检查项：

```text
[ ] 新增有副作用服务类是否先定义 I* 接口
[ ] 有副作用依赖是否通过构造函数注入
[ ] 业务类内部是否没有 new 数据库、文件、网络、缓存、邮件、存储、时钟、随机数等依赖
[ ] 单类是否只承担单一职责
[ ] 是否没有 God Class 或“采集 + 处理 + 输出”混合类
[ ] 跨阶段模型是否优先使用 record / 只读属性 / 不可变集合
[ ] 业务模块是否依赖抽象而非具体基础设施实现
[ ] 逻辑是否可测试，是否没有以“难测”为由免测
```

阻断条件：

```text
- 新增有副作用服务类没有接口。
- 业务代码内部直接 new 有副作用依赖。
- 单类职责严重复合，导致无法测试或难以审计。
```

---

### 1.5.2 高内聚低耦合 Review

检查项：

```text
[ ] 是否符合 WeCMS 依赖矩阵
[ ] WeCms.Persistence -> WeCms.Modules.System / WeCms.Modules.Cms 是否仅用于实现模块暴露的 repository port
[ ] WeCms.Shared 是否没有引用其它生产工程
[ ] WeCms.Infrastructure 是否没有反向引用 Api / Modules
[ ] WeCms.Persistence 是否只作为适配器层 / 数据访问实现层存在，而不是承载业务规则的传统 DAL
[ ] WeCms.Persistence 是否只实现模块或 Shared 暴露的持久化抽象
[ ] System / Cms 模块是否没有互相引用内部实现
[ ] InternalsVisibleTo 是否仅暴露给对应测试工程
[ ] 单个手写代码文件是否 ≤ 600 行
[ ] C# 命名空间是否匹配目录结构
[ ] 横切关注点是否沉淀到 Shared / Infrastructure
[ ] 新增第三方依赖是否说明必要性、AOT 兼容、License、替代方案
```

WeCMS 生产工程依赖矩阵：

```text
WeCms.Api -> WeCms.Modules.System / WeCms.Modules.Cms / WeCms.Infrastructure / WeCms.Persistence / WeCms.Shared
WeCms.Modules.System -> WeCms.Shared
WeCms.Modules.Cms -> WeCms.Shared
WeCms.Persistence -> WeCms.Shared / WeCms.Modules.System / WeCms.Modules.Cms
WeCms.Infrastructure -> WeCms.Shared
WeCms.Shared -> 不引用其它生产工程
```

`WeCms.Persistence` 引用 System/Cms 模块是受控的依赖倒置实现方向：模块定义 repository port，Persistence 提供 SqlSugar ORM / MySQL adapter。反方向 `WeCms.Modules.* -> WeCms.Persistence`、模块层 SQL、模块层数据库连接器或 ORM 引用仍然是阻断项。

阻断条件：

```text
- 跨工程引用越过依赖矩阵。
- WeCms.Persistence 承载业务规则、权限编排、审计编排或 HTTP 逻辑。
- 生产工程之间滥用 InternalsVisibleTo。
- 新增第三方依赖无说明且影响 AOT / 安全 / 体积。
```

数据库边界新增检查点（待自动化）

```text
[ ] DB-BOUNDARY-001：只有 WeCms.Persistence 引用了 SqlSugar ORM / MySqlConnector。
[ ] DB-BOUNDARY-002：WeCms.Modules.* 不直接处理 SQL 字符串。
[ ] DB-BOUNDARY-003：WeCms.Modules.* 未出现 SqlSugarClient / ISqlSugarClient / Ado 原始 SQL API。
[ ] DB-BOUNDARY-004：WeCms.Modules.* 不能直接依赖持久化实现，必须只依赖 Repository 抽象。
[ ] DB-BOUNDARY-005：WeCms.Modules.* 仅通过 IUnitOfWork 进行事务控制，不直接使用 DbConnection/DbTransaction。
[ ] DB-BOUNDARY-006：数据库边界突破默认 BLOCK。
[ ] WeCms.Persistence 是 SqlSugar ORM / MySQL 适配器层，不是传统 DAL；Repository 只负责 SQL 和数据映射。
```

---

### 1.5.3 拒绝隐式兼容 Review

检查项：

```text
[ ] 是否只在系统边界做校验
[ ] 内部契约违约是否 fail-fast
[ ] 是否没有 ?? defaultValue 掩盖配置缺失
[ ] 是否没有 try/catch 吞错返回 null / false / 空集合
[ ] 是否没有运行时 legacy 分支
[ ] 是否没有 dead fallback
[ ] 删除 API/字段/权限码/菜单 key 是否同步彻底删除
[ ] ThinkPHP 兼容逻辑是否仅存在于 legacy migration 工具中
[ ] 跨版本兼容是否有 docs/specs/<change-id>/ 说明迁移窗口和移除时间
```

阻断条件：

```text
- 用静默兜底掩盖配置、权限、数据或契约错误。
- 为旧系统兼容把 legacy 分支写入运行时业务路径。
- breaking change 没有 spec / migration / 文档说明。
```

---

### 1.5.4 Agile / Spec Review

检查项：

```text
[ ] diff ≥ 200 行是否有 docs/specs/<change-id>/ 三件套
[ ] 新增公共 API / OpenAPI 契约是否有 spec
[ ] 新增数据库表 / migration 是否有 spec
[ ] 新增权限码、菜单、状态机是否有 spec
[ ] 修改认证、授权、Token、审计、文件上传、安全策略是否有 spec
[ ] PR diff 是否 ≤ 400 行，超出是否说明原因
[ ] PR 是否包含 Closes # 或 Spec: 链接
[ ] 用户/开发者/运维/API/数据库文档是否同步更新
```

阻断条件：

```text
- 大改无 spec。
- 公共契约变更无 spec。
- PR 无 issue / spec 可追溯来源。
```

---

### 1.5.5 TDD Review

检查项：

```text
[ ] 代码逻辑变更是否遵循 Red → Green → Refactor
[ ] bugfix 是否先提交可复现失败测试
[ ] 每个生产类是否有对应测试或明确合理例外
[ ] 测试命名是否说明行为与条件
[ ] Repository / SQL 是否有集成测试
[ ] Minimal API 是否有 Endpoint 集成测试
[ ] OpenAPI / generated 类型是否有契约测试
[ ] 覆盖率是否 ≥ 80%
[ ] 是否没有通过 ExcludeFromCodeCoverage 绕过门禁
[ ] 是否没有删除断言或调低阈值来通过测试
```

阻断条件：

```text
- 逻辑变更无测试。
- bugfix 无复现测试。
- 为通过门禁降低测试或覆盖率要求。
```

---

### 1.5.6 Definition of Done Review

PR 打开评审前必须确认：

```text
[ ] 已遵循 Red → Green → Refactor，或明确 N/A
[ ] 已运行 scripts/quality-gate.sh 或等效命令
[ ] 新增有副作用服务类已暴露为 I* 接口并通过构造函数注入
[ ] 改动文件均 ≤ 600 行
[ ] 命名空间匹配目录
[ ] 跨工程引用未越过依赖矩阵
[ ] 未引入隐式兼容兜底、静默 catch、legacy 分支、dead fallback
[ ] ≥ 200 行或公共契约变更已有 spec 三件套
[ ] 文档已同步更新，如涉及
[ ] PR 描述含 Closes # 或 Spec: 链接
[ ] AOT publish 已实际运行并通过
[ ] 前端 typecheck / build 已实际运行并通过，如涉及前端
[ ] 当前开发任务对应测试和质量门禁已实际运行并通过，未通过未进入下一项任务
[ ] 未实现一期禁止的 AI runtime 能力
```

---

## 2. 架构 Review

### 2.1 后端架构

检查项：

```text
[ ] 是否使用 ASP.NET Core Minimal APIs
[ ] 是否使用 .NET 10
[ ] 是否启用 PublishAot
[ ] 是否使用 CreateSlimBuilder
[ ] 是否没有 MVC Controller
[ ] 是否没有 Razor / Razor Pages
[ ] Endpoint 是否显式注册
[ ] 是否没有运行时 Endpoint 扫描
[ ] 是否没有动态代理 AOP
[ ] 是否没有 runtime code generation
[ ] 是否没有在核心业务路径使用 Newtonsoft.Json
[ ] 新增 NuGet 包是否说明 AOT 兼容性
[ ] 代码是否保持模块化边界
```

阻断条件：

```text
- 引入 Controller/MVC/Razor。
- 引入不兼容 AOT 的库且未说明替代方案。
- 为了快速实现绕过 Minimal API 显式注册。
```

---

### 2.2 分层边界

检查项：

```text
[ ] Endpoint 是否只处理 HTTP 绑定和返回
[ ] Service / UseCase 是否负责业务规则
[ ] WeCms.Persistence 中的 Repository 是否只负责 SQL 和数据映射
[ ] 事务是否由 Service / UseCase 控制
[ ] 是否没有在 Endpoint 中直接写 SQL
[ ] 是否没有在 Repository 中处理 HTTP/权限/审计
[ ] DTO 是否没有跨模块随意复用
[ ] WeCms.Infrastructure 是否没有反向依赖业务模块
[ ] WeCms.Persistence 是否仅为数据访问适配器实现，不把自己变成业务层或传统 DAL
```

风险提示：

```text
- Endpoint 过胖，后续难测。
- Repository 处理业务逻辑，后续事务难控。
- 模块间循环引用，后续 AOT 和维护风险高。
```

---

## 3. Native AOT Review

检查项：

```text
[ ] `dotnet publish -c Release -r linux-x64 /p:PublishAot=true` 是否通过
[ ] 是否无 trim/AOT 警告或警告已合理处理
[ ] DTO 是否加入 JsonSerializerContext
[ ] Endpoint 输入输出类型是否可被 Source Generator 处理
[ ] 是否没有 runtime reflection scan
[ ] 是否没有动态加载程序集
[ ] 是否没有 runtime code generation
[ ] 是否没有依赖动态代理库
[ ] 是否没有自动从方法反射生成 schema 的逻辑
[ ] 新增第三方库是否通过 AOT publish 验证
```

阻断条件：

```text
- AOT publish 失败。
- 引入运行时反射扫描作为核心机制。
- DTO 未覆盖导致运行时序列化失败风险。
```

---

## 4. SqlSugar ORM Review

### 4.1 SQL 基线

检查项：

```text
[ ] 是否使用 SqlSugar ORM
[ ] 是否没有 EF Core
[ ] 是否没有 dynamic 查询/返回
[ ] 是否没有 SELECT *
[ ] SQL 是否显式列出字段
[ ] 是否使用命名参数
[ ] 是否没有拼接用户输入
[ ] 排序字段是否白名单
[ ] 筛选字段是否白名单
[ ] 分页参数是否后端校验
[ ] pageSize 是否限制最大 100
[ ] 所有查询方法是否带 CancellationToken
[ ] 写操作是否检查 affected rows
[ ] 批量操作是否限制最大数量
[ ] 是否避免 N+1 查询
```

### 4.2 SQL 安全风险

必须检查：

```text
[ ] ORDER BY 是否来自白名单映射
[ ] WHERE 条件是否参数化
[ ] LIKE 查询是否处理通配符和长度
[ ] 批量 id 是否限制数量并校验类型
[ ] 是否没有无 WHERE 的 UPDATE/DELETE
[ ] 是否没有直接拼接 table/column name
[ ] 是否没有允许前端传任意 SQL 字段名
```

阻断条件：

```text
- SELECT *。
- 动态 SQL 拼接用户输入。
- 无 WHERE 更新/删除。
- Repository 返回 dynamic。
```

---

## 5. 数据库 Migration Review

检查项：

```text
[ ] migration 是否进入版本管理
[ ] 表命名是否符合 sys_ / cms_ 前缀规则
[ ] 字段命名是否使用 snake_case
[ ] 是否包含主键
[ ] 是否包含必要唯一索引
[ ] 是否包含必要普通索引
[ ] 是否包含 created_at / created_by
[ ] 是否包含 updated_at / updated_by
[ ] 是否包含 deleted_at / deleted_by
[ ] 是否包含 row_version
[ ] 迁移自旧系统的核心表是否包含 legacy_id
[ ] 是否有 rollback 或回退说明
[ ] 大表变更是否评估锁表风险
[ ] 字段删除是否走 deprecated 流程
[ ] 数据回填是否分批
```

旧系统迁移检查：

```text
[ ] think_auth_group.rules CSV 是否拆成关系表
[ ] 旧 token 是否未迁移
[ ] 旧 2FA secret 是否默认未迁移
[ ] 旧 SMTP 密码/auth_key 是否未迁移
[ ] 旧密码 hash 迁移策略是否明确
[ ] 是否输出 row count 校验
[ ] 是否输出异常数据清单
[ ] 是否保留 legacy_id 方便回溯
```

---

## 6. API 契约 Review

检查项：

```text
[ ] 是否使用统一 ApiResult<T>
[ ] 是否使用统一 PagedResult<T>
[ ] 是否没有多套响应格式
[ ] 错误码是否来自 ApiCodes
[ ] 是否包含 traceId
[ ] 字段验证错误是否支持 fieldErrors
[ ] OpenAPI 是否可生成
[ ] OpenAPI 是否反映真实 DTO
[ ] 新增/修改 DTO 是否同步 JsonSerializerContext
[ ] 是否没有为了前端模板修改后端字段
[ ] 是否没有在前端 request 层改写 data
```

阻断条件：

```text
- 一个接口返回多种结构。
- 前端私自定义业务 DTO 替代后端 DTO。
- request interceptor 重塑后端业务 data。
```

---

## 7. Endpoint 权限 Review

### 7.1 权限码绑定

检查项：

```text
[ ] 除 AllowAnonymous 外，业务 Endpoint 是否绑定权限码
[ ] 权限码是否使用常量
[ ] 权限码是否符合 模块:资源:动作 命名
[ ] 新权限码是否进入权限矩阵
[ ] 新权限码是否能同步到 sys_permission
[ ] 写接口是否绑定审计标记
[ ] 高风险接口是否绑定限流策略
```

### 7.2 对象级授权

检查项：

```text
[ ] 带 id 的详情接口是否做对象级授权
[ ] 带 id 的修改接口是否做对象级授权
[ ] 带 id 的删除接口是否做对象级授权
[ ] 文件下载是否校验文件归属/可见范围
[ ] 日志查询是否按权限过滤敏感字段
[ ] 非超级管理员是否不能修改超级管理员
[ ] 用户是否不能删除自己
[ ] 用户是否不能禁用自己
[ ] 系统是否保护最后一个超级管理员
```

阻断条件：

```text
- 只有 RBAC 动作权限，没有对象级授权。
- 前端隐藏按钮替代后端权限。
- 允许用户通过改 id 操作其他对象。
```

---

## 8. 认证与 Token Review

检查项：

```text
[ ] Access Token 是否短有效期
[ ] Access Token 是否不携带完整权限列表
[ ] Refresh Token 是否高强度随机值
[ ] Refresh Token 是否只保存 hash
[ ] Refresh Token 是否支持轮换
[ ] Refresh Token 刷新后旧 token 是否失效
[ ] 登出是否吊销当前 Refresh Token
[ ] 修改密码是否吊销全部会话
[ ] 禁用用户是否吊销全部会话
[ ] 修改权限是否更新 permission_version
[ ] 登录成功是否记录登录日志
[ ] 登录失败是否记录安全事件
[ ] 登录接口是否限流
[ ] Refresh 接口是否限流
[ ] 验证码策略是否未泄露账号存在性
```

阻断条件：

```text
- Refresh Token 明文入库。
- Access Token 塞完整权限列表。
- 登录失败无审计。
- 修改密码/禁用用户后旧 token 仍可用。
```

---

## 9. 输入验证与 Mass Assignment Review

检查项：

```text
[ ] 请求 DTO 是否只包含允许字段
[ ] 是否没有直接把前端对象映射到数据库实体
[ ] 是否有字段白名单
[ ] 是否校验字符串长度
[ ] 是否校验枚举值
[ ] 是否校验 id 是否存在
[ ] 是否校验 roleIds/menuIds/permissionIds 有效且启用
[ ] 是否禁止前端提交系统字段，如 id、created_at、permission_version
[ ] 是否禁止非拥有 super_admin 角色的用户修改超级管理员
```

阻断条件：

```text
- Mass Assignment 可修改敏感字段。
- 前端可提交 permission_version/security_stamp 等系统字段。
```

---

## 10. 文件上传与访问 Review

检查项：

```text
[ ] 上传是否存储在非 WebRoot
[ ] 文件名是否系统生成
[ ] 原始文件名是否只作展示
[ ] 扩展名是否白名单
[ ] MIME 是否白名单
[ ] 是否拒绝可执行扩展名
[ ] 是否拒绝双扩展名
[ ] 是否限制文件大小
[ ] 文件下载是否鉴权
[ ] 文件预览是否鉴权
[ ] 是否没有把物理路径返回前端
[ ] 删除是否软删除
[ ] 删除是否记录审计
[ ] 富文本附件是否建立引用关系
[ ] 删除媒体是否检查引用关系
```

阻断条件：

```text
- 文件路径由前端传入。
- 物理路径返回前端。
- 上传文件可直接被 Web 访问。
- 下载无鉴权。
```

---

## 11. 日志、审计与脱敏 Review

检查项：

```text
[ ] 写操作是否记录审计
[ ] 权限变更是否记录变更前后差异
[ ] 用户状态变更是否记录审计
[ ] 文件上传/下载/删除是否记录审计
[ ] 配置变更是否记录审计
[ ] 超级管理员操作是否 highRisk
[ ] 是否包含 requestId / traceId
[ ] 是否记录 userId、ip、path、statusCode、elapsedMs
[ ] 是否脱敏 password/token/secret
[ ] 生产环境是否默认不记录 body
[ ] body 日志是否白名单、限大小、脱敏
```

阻断条件：

```text
- 日志包含 password、token、secret、2FA、SMTP 密码。
- 高风险操作无审计。
- 权限变更无审计。
```

---

## 12. 缓存一致性 Review

检查项：

```text
[ ] 权限缓存 key 是否包含 userId + permissionVersion
[ ] 修改用户角色是否更新 permission_version
[ ] 修改角色权限是否更新关联用户 permission_version
[ ] 修改菜单权限是否更新关联用户 permission_version
[ ] 禁用角色是否更新关联用户 permission_version
[ ] 配置缓存是否按 config_version 失效
[ ] 敏感配置是否不明文进入缓存
[ ] 用户态接口是否未使用共享 OutputCache
```

风险提示：

```text
- 权限缓存脏读会导致授权错误。
- 配置缓存不一致会导致安全策略失效。
```

---

## 13. 前端 SoybeanAdmin Review

检查项：

```text
[ ] 是否使用后端 OpenAPI/generated 类型
[ ] generated 目录是否未手写
[ ] 是否未使用 mock 类型作为正式契约
[ ] request interceptor 是否未重塑业务 data
[ ] 401 是否统一进入登录流程
[ ] 403 是否统一显示无权限页面
[ ] 动态路由是否来自后端菜单 DTO
[ ] component key 是否走白名单映射
[ ] 按钮权限是否来自 permissions
[ ] 前端是否没有硬编码后端 URL
[ ] 前端是否没有硬编码散落权限码，或已集中常量化
[ ] 是否没有 v-html 渲染未清洗内容
```

阻断条件：

```text
- 前端为了适配模板修改后端 DTO。
- 前端直接消费 mock 类型。
- 前端自行生成权限或菜单。
```

---

## 14. OpenAPI 与类型生成 Review

检查项：

```text
[ ] OpenAPI 是否生成成功
[ ] OpenAPI 是否作为构建产物保存
[ ] 前端类型是否从 OpenAPI 生成
[ ] OpenAPI diff 是否检查破坏性变更
[ ] 生产环境 OpenAPI UI 是否默认关闭或受控
[ ] 每个 Endpoint 是否有 summary/响应类型
[ ] 错误码文档是否同步
[ ] 权限码文档是否同步
```

阻断条件：

```text
- DTO 改了但 OpenAPI/generated 未更新。
- 前端手写类型覆盖 generated。
```

---

## 15. CI/CD Review

后端必须通过：

```bash
dotnet restore
dotnet build -warnaserror
dotnet test
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true
```

前端必须通过：

```bash
pnpm install --frozen-lockfile
pnpm typecheck
pnpm lint
pnpm build
```

检查项：

```text
[ ] AOT publish 是否作为阻断门禁
[ ] warning 是否作为 error
[ ] OpenAPI 生成是否验证
[ ] Endpoint 权限扫描是否计划/已实现
[ ] JsonSerializerContext 覆盖扫描是否计划/已实现
[ ] SQL 规范扫描是否计划/已实现
[ ] generated 类型是否检查未手写
[ ] 是否没有把 secret 写入 CI
[ ] 是否没有连接生产数据库
```

---



### 15.1 scripts/quality-gate.sh 等效门禁

在 `scripts/quality-gate.sh` 尚未创建前，PR 必须逐项运行等效命令：

```bash
dotnet build backend/WeCms.sln -warnaserror
dotnet test backend/WeCms.sln
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin lint
pnpm --dir frontend/soybean-admin build
```

质量门禁禁止通过以下方式绕过：

```text
[ ] 调低覆盖率阈值
[ ] 删除测试或断言
[ ] 跳过 AOT publish
[ ] 移除安全扫描
[ ] 临时关闭 lint / typecheck
[ ] 把失败项标记为后续再修但仍合并
```

### 15.2 单任务完成阻断规则

每个开发任务完成后，必须先运行该任务对应测试和质量门禁。只有测试和门禁全部通过，才允许进入下一项任务或开始新的开发任务。

Review 必须检查：

```text
[ ] 当前任务是否已运行对应测试。
[ ] 当前任务是否已运行对应质量门禁。
[ ] 测试和门禁是否全部通过。
[ ] 若测试或门禁失败，是否停留在当前任务修复而未推进下一项。
```

阻断条件：

```text
- 当前任务测试未运行或失败，但继续进入下一项任务。
- 当前任务质量门禁未运行或失败，但继续进入下一项任务。
- 将门禁失败标记为“后续修复”并继续推进后续任务。
```

---

## 16. AI 二期边界 Review

### 16.1 一期禁止内容

检查项：

```text
[ ] 是否未创建 WeCms.Modules.Ai
[ ] 是否未创建 AI Provider
[ ] 是否未创建 Prompt runtime
[ ] 是否未创建 RAG runtime
[ ] 是否未创建 Vector Store runtime
[ ] 是否未创建 Agent Tool runtime
[ ] 是否未在后端调用 DeepSeek/OpenAI/Azure OpenAI API
[ ] 是否未在前端增加 AI 页面
[ ] 是否未写入 AI Provider Key
```

### 16.2 二期架构边界

如涉及二期设计文档，检查：

```text
[ ] AI 是否作为独立项目
[ ] AI 是否只能通过 CMS Core API 获取 CMS 数据
[ ] AI 是否严禁直接连接 CMS 数据库
[ ] AI 是否严禁读取只读副本/视图/binlog/同步表
[ ] AI 是否严禁直接读取 CMS 文件存储
[ ] CMS Core 是否保留 AI Bridge/API-facing 边界
[ ] AI Service 是否拥有自己的 AI DB / Vector Store
[ ] AI 结果写回是否通过 CMS API
```

阻断条件：

```text
- 一期实现 AI runtime。
- AI 代码直连 CMS 数据库。
- AI 项目保存 CMS DB 连接串。
```

---

## 17. 旧系统迁移 Review

检查项：

```text
[ ] 是否基于 ThinkPHP 系统说明文档
[ ] 是否保留 legacy_id
[ ] 是否有旧表到新表映射
[ ] 是否有字段转换规则
[ ] 是否有异常数据报告
[ ] 是否有 row count 校验
[ ] 是否不迁移 token
[ ] 是否不迁移 2FA secret 或要求重新绑定
[ ] 是否不迁移 SMTP 密码/auth_key
[ ] 是否处理 think_auth_group.rules CSV
[ ] 是否把菜单和权限拆分
[ ] 是否保留迁移日志
```

阻断条件：

```text
- 迁移真实敏感数据到开发/测试环境。
- 直接把旧 CSV 权限模型带入新系统。
- 未记录无法自动映射的数据。
```

---

## 18. CMS 内容模块 Review

检查项：

```text
[ ] 栏目树是否防循环
[ ] 栏目是否有最大深度
[ ] 文章是否归属栏目
[ ] 内容状态是否由后端状态机控制
[ ] AI 生成内容是否未被一期实现
[ ] 发布/下架是否记录审计
[ ] 删除是否进入回收站
[ ] 富文本是否有 XSS 清洗策略
[ ] 富文本附件是否建立引用关系
[ ] 公开 API 是否与后台 API 分离
[ ] 公开 API 是否不返回后台字段
```

---

## 19. 高风险操作 Review

高风险操作包括：

```text
删除用户
禁用用户
重置密码
重置 2FA
授予超级管理员
修改角色权限
删除角色
修改登录安全配置
关闭安全策略
批量删除
批量导入
批量授权
```

检查项：

```text
[ ] 是否二次确认
[ ] 是否完整审计
[ ] 是否 highRisk 标记
[ ] 是否对象级授权
[ ] 是否限流
[ ] 是否可恢复或有回滚策略
[ ] 是否防止自己锁死系统
```

---

## 20. 事务、幂等、并发 Review

检查项：

```text
[ ] 事务是否由 Service/UseCase 层控制
[ ] 跨多个 Repository 的写操作是否同一事务
[ ] 权限变更与 permission_version 更新是否同一事务
[ ] 事务中是否没有外部 HTTP 调用
[ ] 事务中是否没有长耗时文件扫描
[ ] 创建/导入/批量操作是否有重复提交保护
[ ] Refresh Token 轮换是否防并发重复刷新
[ ] 关键表是否使用 row_version 或 updated_at 并发控制
[ ] 编辑提交是否携带 rowVersion
[ ] rowVersion 冲突是否返回 409 或统一冲突错误码
```

---

## 21. 性能 Review

检查项：

```text
[ ] 列表接口是否分页
[ ] pageSize 是否有限制
[ ] 查询是否有必要索引
[ ] 是否避免 N+1
[ ] 是否避免一次性加载全量数据
[ ] 大导出是否异步
[ ] 日志查询是否按时间范围限制
[ ] 文件上传是否有大小限制
[ ] AI 一期是否未引入高延迟 runtime 调用
```

---

## 22. Security Review Prompt

可用于 DeepSeek / Codex Review：

```text
你是 WeCMS 安全审查员。请审查当前变更。

必须检查：
1. 是否存在认证绕过。
2. 是否存在授权缺失。
3. 是否存在对象级授权缺失。
4. 是否存在 Mass Assignment。
5. 是否泄露 password、token、secret、2FA。
6. Refresh Token 是否只保存 hash。
7. 是否记录敏感日志。
8. 文件上传和下载是否安全。
9. 写操作是否审计。
10. 是否误实现 AI 一期禁止内容。
11. AI 是否可能直接访问 CMS 数据库。

输出：
- P0 阻断问题
- P1 必须修复
- P2 建议修复
- 是否允许合并
```

---

## 23. Architecture Review Prompt

```text
你是 WeCMS 架构守门员。请审查当前变更是否符合架构约束。

必须检查：
1. ASP.NET Core Minimal APIs。
2. .NET 10 Native AOT Only。
3. CreateSlimBuilder。
4. 禁止 MVC Controller。
5. 禁止 Razor。
6. 禁止 EF Core。
7. SqlSugar ORM。
8. 禁止 dynamic。
9. DTO 是否进入 JsonSerializerContext。
10. Endpoint 是否显式注册。
11. 前端是否以后端契约为准。
12. AI 是否仍为二期独立项目且未实现 runtime。

输出：
- 架构合规性
- AOT 风险
- 模块边界风险
- 必须修复项
- 是否允许编码/合并
```

---

## 24. SQL Review Prompt

```text
你是 WeCMS 数据库审查员。请审查 SQL migration 和 SqlSugar ORM SQL。

必须检查：
1. 是否存在 SELECT *。
2. 是否存在 SQL 注入风险。
3. 是否显式字段。
4. 是否有必要索引。
5. 是否包含审计字段、软删除字段、row_version。
6. 是否包含 legacy_id。
7. 是否有无 WHERE UPDATE/DELETE。
8. 排序字段是否白名单。
9. 分页是否限制。
10. 是否符合 SqlSugar ORM 强类型映射。

输出：
- 阻断问题
- 性能风险
- 索引建议
- 迁移风险
- 修改建议
```

---

## 25. Frontend Review Prompt

```text
你是 WeCMS SoybeanAdmin 前端审查员。请审查当前前端变更。

必须检查：
1. 是否使用后端 OpenAPI/generated 类型。
2. generated 目录是否未手写。
3. 是否未使用 mock 类型作为正式契约。
4. 是否未修改 ApiResult。
5. request interceptor 是否未重塑 data。
6. 动态路由是否来自后端菜单 DTO。
7. 按钮权限是否来自后端 permissions。
8. 401/403 是否统一处理。
9. 是否没有硬编码散落权限码。
10. 是否没有一期 AI 页面。

输出：
- 必须修复
- 建议修复
- 契约风险
- 安全风险
- 是否允许合并
```

---

## 26. PR Review 模板

每次 PR Review 应输出：

```markdown
## Review 结论

BLOCK / REQUEST_CHANGES / APPROVE_WITH_NOTES / APPROVE

## P0 阻断问题

- ...

## P1 必须修复

- ...

## P2 建议修复

- ...

## AOT 检查

- [ ] dotnet publish /p:PublishAot=true 通过
- [ ] DTO 已进入 JsonSerializerContext
- [ ] 无 runtime scan / dynamic proxy / code generation

## 数据访问检查

- [ ] 无 EF Core
- [ ] 无 dynamic
- [ ] 无 SELECT *
- [ ] SQL 参数化
- [ ] Repository 强类型

## 权限与安全检查

- [ ] Endpoint 权限码
- [ ] 对象级授权
- [ ] 审计日志
- [ ] 敏感信息脱敏

## 前端契约检查

- [ ] 后端契约优先
- [ ] generated 类型
- [ ] 无 mock 契约污染

## AI 边界检查

- [ ] 一期未实现 AI runtime
- [ ] 未创建 WeCms.Modules.Ai
- [ ] 未直连 CMS 数据库

## 验证命令

- ...
```

---

## 27. 最终合并门禁

合并前必须满足：

```text
[ ] 架构 Review 通过
[ ] 安全 Review 通过
[ ] SQL Review 通过，如涉及 SQL
[ ] 前端 Review 通过，如涉及前端
[ ] dotnet build -warnaserror 通过
[ ] dotnet test 通过
[ ] dotnet publish /p:PublishAot=true 通过
[ ] pnpm typecheck 通过，如涉及前端
[ ] pnpm build 通过，如涉及前端
[ ] OpenAPI 生成通过，如涉及 API
[ ] 人工 Review 通过
```

## DI 评审检查（补齐缺口）

- DI-001：业务模块不应直接 `new` 有副作用服务（Repository、DbClient、TokenService、PasswordHasher、FileStorage、EmailSender、CacheClient、HttpClient）。
- DI-002：业务模块构造器只依赖接口、配置值对象与纯参数；不得依赖具体实现类型。
- DI-003：Repository 接口在模块层定义，具体实现位于 Persistence 层并通过 DI 注册。
- DI-004：数据库 Client/ORM/SQL 文本仅出现在 Persistence 层，模块层不得出现 `new MySqlConnection` 等实例化行为。
- DI-005：时间、随机、ID、时钟、外部调用等副作用能力走接口注入，不在业务层写 `DateTime.UtcNow` / `Guid.NewGuid()` / `Random.Shared`。
- DI-006：禁止在业务层使用 Service Locator（`IServiceProvider` 运行时解析）替代构造函数注入。
- DI-007：跨层依赖（文件、邮件、缓存、Token、密码、审计、当前用户）需由抽象层解耦且可替换实现。
- DI-008：若出现上述 DI 反例，评审应标记为阻断项。
- DI-010：业务模块不得持有具体实现类型（包括 ctor 参数类型）。即便看起来像实现类，也应依赖接口。
- DI-011：`IUnitOfWork` 与仓储接口只用于服务层事务编排；仓储本身不得处理权限、审计、HTTP、外部调用。
- DI-012：数据库 SQL 与数据库客户端仅限 Persistence；模块层不得出现 `DbConnection`、`SqlSugar ORM`、原始 SQL 文本或数据访问实现细节。
- DI-013：允许 `new` 的对象仅限局部资源对象；若使用资源型对象（如 `CancellationTokenSource`）需有明确生命周期边界（`using` / `await using` 或明确释放）。

任何 AI 生成的代码都必须经过同样门禁，不得例外。

## DI 执行口径（审查清单）

- [ ] 业务类 constructor 参数是否全部是接口或配置/值对象（排除实体/DTO/记录）
- [ ] 是否出现 `new HttpClient`、`new MySqlConnection`、`new JwtTokenService`、`new Pbkdf2PasswordHasher`、`new AuthRepository` 等副作用对象
- [ ] 是否出现 `DateTime.UtcNow`、`Guid.NewGuid`、`Random.Shared`、`IServiceProvider.GetRequiredService`
- [ ] 模块层是否出现 SQL 文本、`SqlSugarClient`、`ISqlSugarClient`、`Ado.SqlQuery`、`DbConnection`/`DbTransaction`
- [ ] 是否出现 `using` 缺失的 `CancellationTokenSource` 等局部资源未释放
- [ ] 命名是否符合：`Services / UseCases` 使用 `...Service` / `...UseCase`，接口使用 `I...Service` / `I...Repository` / `IClock`

## DI 快速静态扫描清单（`rg` 示例）

可在审查前先跑快速脚本（按需调整路径）：

```bash
# 1) 构造参数是否使用具体实现（关键字过滤，需结合代码语境人工判断）
rg -n "new\\s+(?!var|\\[|\\()\\w+Service|new\\s+\\w+Repository|new\\s+\\w+Storage|new\\s+\\w*Client\\(" backend src frontend

# 2) 明确禁止的副作用对象（业务层）
rg -n "new\\s+(HttpClient|MySqlConnection|SqlSugarClient|DbConnectionFactory|JwtTokenService|Pbkdf2PasswordHasher|SmtpClient|FileStorage)\\s*\\(" backend/src
rg -n "DateTime\\.UtcNow|Guid\\.NewGuid\\(\\)|Random\\.Shared" backend src frontend

# 3) Service Locator / 运行时服务解析
rg -n "IServiceProvider\\.GetRequiredService|GetService\\s*<" backend/src

# 4) 模块层数据库边界（排除持久化目录后检查模块目录）
rg -n "SqlSugar|SqlSugarClient|ISqlSugarClient|MySqlConnector|DbConnection|DbTransaction|Ado\\.SqlQuery|\\bSELECT\\s+\\*\\b|\\bUPDATE\\s+\\w+\\b" backend/src/WeCms.Modules

# 5) SQL 文本（粗筛）
rg -n "\"\\s*(SELECT|INSERT|UPDATE|DELETE|INSERT INTO)\\b|FROM\\s+\\w+\" backend/src/WeCms.Modules

# 6) 可疑具体实现注入到业务层 ctor（人工确认）
rg -n "class\\s+\\w+\\(.*\\b(new\\s+)?(AuthRepository|UserRepository|RoleRepository|PermissionRepository|FileRepository|EmailRepository)\\b" backend/src/WeCms.Modules -g "*.cs"
```



