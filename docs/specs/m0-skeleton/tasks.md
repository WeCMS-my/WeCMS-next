 # M0 工程骨架 — Tasks
 
 ## M0-BE-001~002：项目结构 + AOT 配置 ✅
 - 创建 solution + 7 项目 + 2 测试项目
 - WeCms.Api.csproj: PublishAot=true, AOT 分析器, InterceptorsPreviewNamespaces
 - WeCms.Infrastructure.csproj: Dapper + Dapper.AOT + MySqlConnector NuGet
 - 依赖矩阵符合 AGENTS.md
 
 ## M0-BE-003~004：ApiResult + Program.cs ✅
 - ApiResult<T>.Ok(data) / .Fail(code, msg)
 - PagedResult<T>: Records, Page, PageSize, Total
 - ApiCodes: Success=0, Unauthorized=401, Forbidden=403, 等
 - CreateSlimBuilder + health endpoints
 
 ## M0-BE-005~006：Middleware + JSON Context ✅
 - ExceptionMiddleware: 401/500，不泄露堆栈
 - WeCmsJsonContext: 覆盖所有 Auth DTO
 
 ## M0-BE-007~008：数据库连接 ✅
 - IDbConnectionFactory + DbConnectionFactory（MySqlConnector）
 - DapperAotConfig [module: DapperAot]
 - GET /api/v1/system/db-check
 
 ## M0-BE-009~010：Migration + Seed ✅
 - 10 张核心表（sys_user 到 sys_security_event）
 - Base seed: 3 角色，13 权限，5 菜单
 
 ## M0-BE-011~012：认证基础设施 ✅
 - TokenService: JWT access + random refresh，HMAC-SHA256，15 分钟有效期
 - Pbkdf2PasswordHasher: 600k 迭代，格式 wecms.pbkdf2-sha256.v1...
 - 无旧密码兼容（旧系统无生产数据）
 
 ## M0-BE-013~016：认证端点 ✅
 - POST /auth/login — 查询 sys_user → 验证密码 → 生成令牌对 → 存储 refresh hash
 - POST /auth/refresh — 验证 hash → 撤销旧 → 生成新 → 存储新 hash
 - POST /auth/logout — 客户端清除令牌
 - GET /auth/me — 返回用户 + 角色 + 权限
 
 ## M0-BE-017：权限过滤器 ✅
 - PermissionMetadata(code)
 - RequirePermission("code") 扩展方法
 - PermissionEndpointFilter (IEndpointFilter)
 
 ## M0-BE-018：OpenAPI（待 dotnet CLI）
 - dotnet run -- --export-openapi artifacts/openapi/wecms-api-v1.json
 
 ## M0-FE-001~004：前端 ✅
 - Vue 3 + Vite + Pinia + Vue Router + Axios
 - Request client: token 注入，401 自动 refresh，403 → /403
 - Auth store: login/logout/fetchCurrentUser/hasPermission
 - Login page + Dashboard
 
 ## 质量门禁（待本地 dotnet）
 - dotnet build -warnaserror
 - dotnet test
 - dotnet publish /p:PublishAot=true
 - pnpm typecheck && pnpm build
