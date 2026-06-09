# Fix M0 Remaining Issues Spec

## Why
前四轮修复后仍有 23 个未修复问题（4 HIGH + 8 MEDIUM + 11 LOW），集中在代码质量、稳健性和前端适配。本轮将其全部清零。

## What Changes

### HIGH（4）
- #13: RefreshR SQL 添加注释说明 intentionally omitted `revoked_at IS NULL`
- #17: Menu Update COALESCE 改为允许设 null（新增额外字段 `ClearPath` 等）
- #18: BuildTree 使用 Dictionary 优化为 O(N)
- #19: IsDescendant CreateAsync 中跳过（新节点无子孙）

### MEDIUM（8）
- #26: 前端 authStore.logout 清理动态路由、tabs、缓存页面
- #27: 前端登录页处理 requiresTwoFactor 响应 → 跳转 2FA 页面
- #29: 关键 UPDATE 添加 `row_version` 乐观锁检查
- #33: AuthService 注入 IClock
- #34: PermissionSyncService 补全权限列表
- #35: Setting Update 忽略 req.Key，仅使用路由参数
- #37: Permissions 类移到 WeCms.Shared.Security 命名空间
- #38: /health/live 和 /health/ready 使用 ApiResult

### LOW（11）
- remove unnecessary ImplicitUsings nuance, add CORS, fix authStore null storage, etc.

## Impact
- Affected specs: all previous fixes
- Affected code: 30+ files
