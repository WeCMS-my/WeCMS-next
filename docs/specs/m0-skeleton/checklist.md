 # M0 工程骨架 — Definition of Done
 
 ## 工程原则 ✅
 
 - [x] 新增有副作用服务类已暴露为 I* 接口（ITokenService、IPasswordHasher、IAuthService、IDbConnectionFactory）
 - [x] 有副作用依赖通过构造函数注入（AuthService、TokenService、DbConnectionFactory）
 - [x] 单类单一职责（TokenService 只关心令牌，PasswordHasher 只关心哈希，AuthService 只关心认证编排）
 - [x] 跨阶段模型使用 record（ApiResult、PagedResult、LoginResponse 等均为 record）
 - [x] 业务模块依赖抽象（Modules.System 依赖 IAuthService，不依赖具体基础设施）
 - [x] 改动文件均 ≤ 600 行（最大 AuthService.cs ~180 行）
 - [x] 命名空间匹配目录
 - [x] 跨工程引用未越过依赖矩阵
 - [x] 未引入隐式兼容兜底、静默 catch、legacy 分支、dead fallback
 
 ## Spec & TDD
 
 - [x] PR diff ≥ 200 行 → docs/specs/m0-skeleton/ 三件套已建立
 - [x] ApiResult、PagedResult、PasswordHasher 有单元测试
 - [x] ExceptionMiddleware 有单元测试（正常传递、401 拦截、500 兜底）
 - [x] 测试命名描述行为与条件
 
 ## 后端架构
 
 - [x] ASP.NET Core Minimal APIs
 - [x] .NET 10 + CreateSlimBuilder
 - [x] PublishAot=true, IsAotCompatible=true
 - [x] 无 MVC Controller / Razor
 - [x] Endpoint 显式注册，无运行时扫描
 - [x] Dapper + Dapper.AOT，无 EF Core
 - [x] 无 Query<dynamic>，无 SELECT *
 - [x] SQL 字段显式列出，参数化
 - [x] DTO 已加入 JsonSerializerContext
 
 ## 认证安全
 
 - [x] Access Token 短有效期（15 分钟）
 - [x] Access Token 不携带完整权限列表
 - [x] Refresh Token 随机生成（64 字节）
 - [x] Refresh Token 只保存 SHA-256 hash
 - [x] Refresh Token 支持轮换
 - [x] 无旧密码兼容（旧系统无生产数据）
 
 ## 权限
 
 - [x] PermissionMetadata + RequirePermission 扩展
 - [x] Permissions 常量类（sys:user:list 等）
 - [x] PermissionEndpointFilter（IEndpointFilter）
 
 ## 数据库
 
 - [x] 表命名符合 sys_ 前缀
 - [x] 字段命名使用 snake_case
 - [x] 包含审计字段（created_at、updated_at、deleted_at）
 - [x] 包含 row_version
 - [x] Migration 进入版本管理
 - [x] Base seed 包含超级管理员角色和权限码
 
 ## AI 边界
 
 - [x] 未创建 WeCms.Modules.Ai
 - [x] 未实现任何 AI runtime 能力
 - [x] 未调用任何 AI Provider API
 
 ## 前端
 
 - [x] 后端 DTO 类型在 service/generated/types.ts
 - [x] Request client 只处理 token/401/403，不重塑业务 data
 - [x] 路由守卫基于 token
 - [x] 登录页调用真实后端
 - [x] Dashboard 显示 /auth/me 返回的用户信息
 
 ## 待验证（需本地 dotnet/Node）
 
 - [ ] dotnet build backend/WeCms.sln -warnaserror
 - [ ] dotnet test backend/WeCms.sln
 - [ ] dotnet publish /p:PublishAot=true
 - [ ] pnpm --dir frontend/soybean-admin typecheck
 - [ ] pnpm --dir frontend/soybean-admin build
