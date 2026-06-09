# Tasks

## HIGH 修复（7 个）

- [x] Task 1: H5 — CORS 配置化白名单
  - 修改 `appsettings.json`：新增 `"Cors": { "AllowedOrigins": [] }`
  - 修改 `appsettings.Development.json`：新增 `"Cors": { "AllowedOrigins": ["http://localhost:5173"] }`
  - 修改 `Program.cs`：从配置读取 `Cors:AllowedOrigins`，非空时用 `WithOrigins()`，空数组时 fail-fast
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 2: H3 — 角色删除级联清理 sys_role_menu + sys_role_permission
  - 修改 `RoleService.DeleteAsync`：软删除角色后，DELETE FROM `sys_role_menu` WHERE role_id=@Id，DELETE FROM `sys_role_permission` WHERE role_id=@Id，并调用 BumpPermissionVersion
  - 将 DELETE 操作包裹在同一事务中
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 3: H4 — UserService.UpdateAsync 用户名唯一性校验
  - 当前 `UpdateUserRequest` 中无 `Username` 字段，无需修改。仅添加注释说明为何不需要此检查。
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 4: H1 + H2 — Token 即时失效 + Refresh Token 会话上限
  - 修改 `AuthManagementEndpoints.ChangePasswordAsync`：密码修改后递增 `permission_version`
  - 修改 `UserService.SetStatusAsync`：禁用用户时递增 `permission_version`
  - 修改 `AuthService.StoreRefreshToken`：检查用户活跃 token 数，超过 10 则删除最旧的
  - 修改 `PermissionEndpointFilter`：JWT 验证时比对 `permission_version` 与数据库值
  - **补丁**：修复 H1 实现中误删的 super-admin 跳过逻辑
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 5: H6 — MenuService.CollectDescendants 消除 N+1
  - 修改 `MenuService.DeleteAsync`：先一次查询所有未删除菜单，内存构建 parent→children 映射，再 BFS 收集后代 ID
  - 修改 UpdateAsync 中 `IsDescendant` 改为内存查找
  - 移除旧的 `CollectDescendants` 和 `IsDescendant` 方法
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 6: H7 — MenuService.SortAsync 批量 UPDATE
  - 修改 `MenuService.SortAsync`：使用 `CASE WHEN` 语法单 SQL 批量更新
  - 验证：`dotnet build -warnaserror` ✅

- [x] Task 7: 全量验证 + code_review.md
  - `dotnet build -warnaserror` ✅
  - `dotnet test` ✅
  - `dotnet publish -c Release -r win-x64 /p:PublishAot=true` ✅
  - 按 `code_review.md` 执行完整代码审查 ✅

# Task Dependencies
- Task 2-6 均可并行（无代码层面强依赖）
- Task 7 依赖 Task 1-6 全部完成

# 并行化建议
Tasks 1-6 可并行执行，Task 7 最后执行。
