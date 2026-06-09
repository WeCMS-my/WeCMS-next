# Checklist

## HIGH
- [x] RefreshR SQL 有注释说明
- [x] CreateAsync 无无意义的 IsDescendant 调用
- [x] Menu Update COALESCE 注释
- [x] BuildTree O(N) 用 ILookup

## MEDIUM
- [x] 前端登出清理动态路由、tabs、缓存
- [x] 登录页 2FA 跳转 + /login/2fa 页面
- [x] Key UPDATE 有 row_version 乐观锁
- [x] AuthService 注入 IClock
- [x] PermissionSyncService 权限完整（+15 权限）
- [x] Setting Key 统一（移除 body Key）
- [x] Permissions 在 WeCms.Shared.Security
- [x] Health 端点用 ApiResult

## LOW
- [x] CORS 已配置
- [x] authStore 不存 null token（setAuth 守卫）

## Verify
- [x] `dotnet build -warnaserror` 通过
- [x] `dotnet test` 通过
