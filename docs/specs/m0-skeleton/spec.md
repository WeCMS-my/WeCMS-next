 # M0 工程骨架验证 — Spec
 
 > Change ID: `m0-skeleton`  
 > Status: implemented  
 > PR: codex/m0-skeleton  
 > Diff: ~55 files, ~3000 lines
 
 ## 1. 概述
 
 创建 WeCMS Next 工程骨架，验证 ASP.NET Core Minimal APIs + .NET 10 Native AOT + Dapper + Dapper.AOT + MySQL + SoybeanAdmin 联通可行。
 
 ## 2. 需求
 
 ### 2.1 后端
 - .NET 10 Minimal API 项目，CreateSlimBuilder
 - Native AOT publish 配置（PublishAot=true，AOT 分析器）
 - 统一响应结构 ApiResult<T> + PagedResult<T>
 - 异常处理中间件（不泄露堆栈）
 - JsonSerializerContext 覆盖所有 DTO
 - MySQL 连接工厂 + Dapper.AOT 强类型查询
 - JWT 认证（Access Token 15min + Refresh Token hash 存储）
 - PBKDF2-SHA256 密码哈希，无旧格式兼容
 - 登录/刷新/登出/me 端点
 - PermissionMetadata + RequirePermission 扩展
 - 健康检查端点
 - 数据库迁移和种子 SQL
 
 ### 2.2 前端
 - Vue 3 + Vite + Pinia + Vue Router 项目
 - Axios 请求客户端，token 注入 + 自动刷新
 - 登录页面
 - Dashboard（显示当前用户信息）
 - 路由守卫
 
 ### 2.3 基础设施
 - Docker Compose（MySQL 8.4）
 - 10 张核心表的 migration
 - Base seed（超级管理员 + 基础权限码 + 菜单）
 
 ## 3. 非目标
 - 不实现完整 CMS 功能
 - 不实现 2FA
 - 不实现完整 SoybeanAdmin 主题
 - 不迁移旧数据
 - 不实现 AI runtime
 
 ## 4. 架构决策
 - ADR-0001: .NET 10 Native AOT
 - ADR-0002: Dapper + Dapper.AOT
 - ADR-0003: Backend Contract First
 - ADR-0004: AI as Independent Service (Phase 2)
 
 ## 5. 模块边界
 ```
 WeCms.Api → WeCms.Modules.System / .Cms → WeCms.Infrastructure → WeCms.Shared
 ```
 WeCms.Modules.Cms 为骨架占位，M4 之前不实现业务代码。
 
 ## 6. API 契约
 - POST /api/v1/auth/login — 登录
 - POST /api/v1/auth/refresh — 刷新令牌
 - POST /api/v1/auth/logout — 登出
 - GET /api/v1/auth/me — 当前用户
 - GET /api/v1/system/ping — 连通性检查
 - GET /api/v1/system/version — 版本信息
 - GET /api/v1/system/db-check — 数据库连接检查
 - GET /health/live — 存活检查
 - GET /health/ready — 就绪检查
 
 ## 7. 数据库表
 sys_user, sys_role, sys_user_role, sys_menu, sys_permission,
 sys_role_menu, sys_role_permission, sys_refresh_token,
 sys_login_log, sys_security_event
 
 ## 8. 验证门禁
 ```
 dotnet build backend/WeCms.sln -warnaserror
 dotnet test backend/WeCms.sln
 dotnet publish backend/src/WeCms.Api -c Release -r linux-x64 /p:PublishAot=true
 pnpm --dir frontend/soybean-admin typecheck
 pnpm --dir frontend/soybean-admin build
 ```
