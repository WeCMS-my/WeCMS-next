# Fix M0 Audit Round 3 — 16 Issues Spec

## Why
第三轮审计发现 16 个问题（2 CRITICAL 回归 + 4 HIGH + 6 MEDIUM + 4 LOW），需在 M0 关闭前修复。

## What Changes
- **REGRESSION**: 登录失败 HTTP 200（撤销 401，修复前端无限刷新循环）
- **REGRESSION**: 删除 TwoFactorEndpoints 遗留流程（上次只在 AuthService 删除）
- **REGRESSION**: 前端 logout 发送 X-Refresh-Token + 部分 Endpoint 注入接口
- email/phone COALESCE 防 NULL 覆盖
- PermissionSync 4 段权限码解析修复
- UserService status 白名单验证
- LogService COUNT 带过滤参数
- 修改密码后吊销 RT
- row_version 移除无意义的递增
- 2FA 登录审计日志
- BumpPermissionVersion 过滤软删除用户

## Impact
- Affected: AuthEndpoints.cs, TwoFactorEndpoints.cs, auth.ts, UserService.cs, PermissionSyncService.cs, LogService.cs, AuthManagementEndpoints.cs, RoleService.cs, SecurityEventLogger.cs, MenuService.cs
