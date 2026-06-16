# Plan: 移除 `is_super_admin` 权限绕过，统一走 RBAC

## Summary

当前系统在 `PermissionEndpointFilter` 中存在 `is_super_admin` 硬编码绕过：只要 JWT claim 中 `is_super_admin == "true"`，就**跳过所有 RBAC 4 表 JOIN 权限校验**，直接放行。这不满足"所有用户必须依据分配角色权限进行功能判断"的要求。

本计划移除该绕过机制，并补齐种子数据中缺少的 `sys_user_role` / `sys_role_permission` 关联，确保超级管理员走标准 RBAC 路径。

---

## Current State Analysis

### `is_super_admin` 在系统中的完整生命周期

```
1. DB 定义 (sys_user.is_super_admin tinyint(1) default 0)

2. 登录时 AuthService.LoginAsync 读取该字段
   ↓
3. 传入 TokenPrincipal.IsSuperAdmin
   ↓
4. TokenService.GenerateAccessToken 写入 JWT claim "is_super_admin"
   ↓
5. 每次请求，PermissionEndpointFilter 检查该 JWT claim：
   【if claim == "true" → 跳过 SQL 权限校验，直接放行】
   ↓
6. CurrentUserProvider 也暴露该属性供业务 guard 使用
```

### 受影响文件清单 (14 个文件)

| 文件 | 当前用途 | 变更类型 |
|---|---|---|
| `PermissionEndpointFilter.cs` | **权限绕过核心**：L28-30 跳过 RBAC 校验 | 删除绕过逻辑 |
| `TokenService.cs` | L55 读取、L78 写入 JWT claim | 移除 claim |
| `TokenPrincipal` (ITokenService.cs) | 携带 `IsSuperAdmin` 字段 | 移除字段 |
| `ICurrentUser.cs` | 暴露 `IsSuperAdmin` 属性 | 移除属性 |
| `CurrentUserProvider.cs` | 从 JWT claim 读取实现 | 移除实现 |
| `AuthService.cs` | 登录/2FA/Refresh 时查询和传递 `is_super_admin` | 移除查询列和传参 |
| `UserService.cs` | "最后超级管理员"保护逻辑 | 保留逻辑（改为按 sys_role 判断） |
| `UserDtos.cs` | `UserListItem`/`UserDetail` 包含 `IsSuperAdmin` | 保留（纯展示用途） |
| `sys_user` 表 (migration) | `is_super_admin` 列 | 保留列（不破坏 schema） |
| `sys_two_factor_ticket` 表 (migration) | `is_super_admin` 列 | 保留列 |
| `000001_base_seed.sql` | 种子数据 | **新增** `sys_user_role` 和 `sys_role_permission` |
| `000005_add_two_factor_ticket.sql` | 迁移文件 | 不修改（历史迁移，不再改动） |

### 关键发现：种子数据缺少角色分配

当前种子数据创建了：
- `super_admin` 角色
- `admin` 用户（`is_super_admin = 1`）
- 17 个权限码

但**没有**：
- `INSERT INTO sys_user_role` 把 admin 用户分配给 super_admin 角色
- `INSERT INTO sys_role_permission` 把权限分配给 super_admin 角色

这意味着移除绕过后，admin 用户将零权限。必须补齐种子数据。

---

## Proposed Changes

### 变更 1：移除 PermissionEndpointFilter 中的绕过 (核心)

**文件**: `backend/src/WeCms.Modules.System/Permissions/PermissionEndpointFilter.cs`

**改动**: 删除 L28-30（3 行）

```csharp
// 删除以下 3 行：
var isSuperClaim = user.FindFirst("is_super_admin")?.Value;
if (isSuperClaim == "true") return await next(context);
```

**理由**: 这 3 行是唯一绕过 RBAC 的位置。删除后所有用户走标准 4 表 JOIN 校验。

---

### 变更 2：从 JWT Token 中移除 `is_super_admin` claim

**文件**: `backend/src/WeCms.Infrastructure/Security/TokenService.cs`

**改动**:

1. `GenerateAccessToken` 方法 (L78)：删除 claim 写入行
   ```csharp
   // 删除：
   new Claim("is_super_admin", principal.IsSuperAdmin ? "true" : "false")
   ```

2. `ValidateAccessToken` 方法 (L55)：删除 claim 读取行
   ```csharp
   // 删除：
   var isSuperAdmin = result.Claims.FirstOrDefault(c => c.Type == "is_super_admin")?.Value == "true";
   ```
   并调整 `new TokenPrincipal(...)` 调用去掉最后一个参数。

**理由**: JWT claim 是绕过机制的载体，移除后可确保前端也无法通过修改 JWT 绕过权限。

---

### 变更 3：从 TokenPrincipal 中移除 `IsSuperAdmin`

**文件**: `backend/src/WeCms.Shared/Contracts/ITokenService.cs`

**改动**: 将 `TokenPrincipal` 从带 `IsSuperAdmin` 参数的 record 改为不再携带该字段。

```csharp
// 改前：
public sealed record TokenPrincipal(long UserId, string Username, string SecurityStamp, long PermissionVersion, bool IsSuperAdmin = false);

// 改后：
public sealed record TokenPrincipal(long UserId, string Username, string SecurityStamp, long PermissionVersion);
```

---

### 变更 4：从 ICurrentUser 中移除 `IsSuperAdmin`

**文件**: 
- `backend/src/WeCms.Shared/Contracts/ICurrentUser.cs`
- `backend/src/WeCms.Infrastructure/Security/CurrentUserProvider.cs`

**改动**: 删除 `IsSuperAdmin` 属性定义和实现。

---

### 变更 5：清理 AuthService 中的 `is_super_admin` 查询

**文件**: `backend/src/WeCms.Modules.System/Auth/AuthService.cs`

**改动**:

1. `LoginAsync` (L23)：从 `SELECT` 列表中删除 `is_super_admin` 列
2. `LoginAsync` (L35)：`sys_two_factor_ticket` INSERT 中删除 `is_super_admin` 列和值
3. `LoginAsync` (L48)：`new TokenPrincipal(...)` 去掉最后一个参数
4. `VerifyTwoFactorAndLoginAsync` (L59)：SELECT 列表中删除 `is_super_admin AS IsSuperAdmin`
5. `VerifyTwoFactorAndLoginAsync` (L87)：`new TokenPrincipal(...)` 去掉最后一个参数
6. `RefreshTokenAsync` (L106)：JOIN SELECT 中删除 `u.is_super_admin`
7. `RefreshTokenAsync` (L140)：`new TokenPrincipal(...)` 去掉最后一个参数
8. 内部 record `UserR` (L164)：移除 `bool IsSuperAdmin` 属性
9. 内部 record `RefreshR` (L165)：移除 `bool IsSuperAdmin` 属性
10. 内部 record `TwoFactorTicketData` (L167)：移除 `bool IsSuperAdmin` 属性

---

### 变更 6：UserService "最后超级管理员"保护逻辑

**文件**: `backend/src/WeCms.Modules.System/Users/UserService.cs`

**背景**: `DeleteAsync` (L40) 和 `SetStatusAsync` (L43) 中有"不能删除/禁用最后一个超级管理员"的保护逻辑，当前通过 `is_super_admin` 字段判断。

**决策**: 改为通过"是否拥有 `super_admin` 角色"来判断。这样更加语义准确：超级管理员指的是拥有 super_admin 角色的人，而非 `is_super_admin` 字段标记的人。

**改动**:
- `DeleteAsync` / `SetStatusAsync`：将 `SELECT is_super_admin FROM sys_user WHERE id=@Id` 改为检查用户是否拥有 `super_admin` 角色：

```sql
SELECT COUNT(1) FROM sys_user_role ur 
JOIN sys_role r ON r.id = ur.role_id 
WHERE ur.user_id = @Id AND r.code = 'super_admin' AND r.status = 'active' AND r.deleted_at IS NULL
```

并将"最后超级管理员计数"查询从：
```sql
SELECT COUNT(1) FROM sys_user WHERE is_super_admin=1 AND status='active' AND deleted_at IS NULL
```
改为：
```sql
SELECT COUNT(DISTINCT ur.user_id) FROM sys_user_role ur 
JOIN sys_role r ON r.id = ur.role_id 
JOIN sys_user u ON u.id = ur.user_id 
WHERE r.code = 'super_admin' AND r.status = 'active' AND r.deleted_at IS NULL 
AND u.status = 'active' AND u.deleted_at IS NULL
```

- 删除 `UserRow` 内部 record 中的 `int IsSuper` 字段（L45）

---

### 变更 7：补齐种子数据（关键）

**文件**: `database/seeds/000001_base_seed.sql`

**改动**: 新增以下两步 SQL：

```sql
-- 给 super_admin 角色分配所有权限
INSERT INTO sys_role_permission (role_id, permission_id)
SELECT r.id, p.id FROM sys_role r, sys_permission p
WHERE r.code = 'super_admin' AND p.status = 'active';

-- 给 admin 用户分配 super_admin 角色
INSERT INTO sys_user_role (user_id, role_id, created_at)
SELECT u.id, r.id, NOW()
FROM sys_user u, sys_role r
WHERE u.username = 'admin' AND r.code = 'super_admin';
```

**理由**: 没有这一步，移除绕过后的 admin 用户将零权限。

---

### 变更 8：DTO 中的 `IsSuperAdmin` 保留（仅展示）

**文件**: `backend/src/WeCms.Modules.System/Users/UserDtos.cs`

**决策**: 保留 `UserListItem` 和 `UserDetail` 中的 `IsSuperAdmin` 字段。

**理由**:
- 前端列表/详情页仍需展示"超级管理员"标识（UI badge）
- 这是一个展示字段，不被权限系统消费
- `CreateUserRequest` / `UpdateUserRequest` 原本就不包含此字段，前端无法写入

---

### 变更 9：code_review.md 更新

**文件**: `code_review.md`

移除 L504 和 L512 中关于 `is_super_admin` 的检查项（或更新为检查"是否以角色授权代替字段绕过"）。

---

### 不变更项

| 文件 | 原因 |
|---|---|
| `migrations/000001_init_system_tables.sql` | 历史迁移，保留 `is_super_admin` 列不删（避免 schema 断裂），run-time 不再依赖 |
| `migrations/000005_add_two_factor_ticket.sql` | 历史迁移，保留 `is_super_admin` 列，AuthService 不再写入 |
| 文档 `docs/context/*` | 不在本次范围，后续单独更新 |

---

## Assumptions & Decisions

1. **`is_super_admin` 列保留在 DB 中** —— 不删除列，避免破坏 schema 和迁移历史，但运行时不再依赖
2. **"最后超级管理员"保护改为按角色判断** —— 不再依赖字段值，改为检查 `sys_role.code = 'super_admin'` 的角色分配
3. **`IsSuperAdmin` 在前端 DTO 中保留** —— 纯 UI 展示用途
4. **本次不涉及新增 migration** —— 仅修改种子数据和源代码，不新增 SQL 迁移文件
5. **不涉及前端代码变更** —— 前端当前没有消费 `isSuperAdmin`，无需修改

---

## Verification Steps

### 实现后验证

```bash
# 1. 构建
dotnet build backend/WeCms.slnx -warnaserror

# 2. 单元测试
dotnet test backend/WeCms.slnx

# 3. JIT publish
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false

# 4. 前端 check（仅在前端阶段或修改 frontend/** 时执行）
pnpm --dir frontend/soybean-admin typecheck
pnpm --dir frontend/soybean-admin build
```

### 手动验证场景（需运行环境）

1. 不分配角色的用户 → 访问任何受保护 endpoint → 应返回 403
2. 拥有 `super_admin` 角色的用户 → 应通过标准 RBAC 校验（4 表 JOIN 返回 true）
3. 登录后 JWT payload 中不应再出现 `is_super_admin` claim
4. 禁用最后一个拥有 `super_admin` 角色的用户 → 应被阻止
5. 删除最后一个拥有 `super_admin` 角色的用户 → 应被阻止

---

## 文件变更汇总

| # | 文件 | 操作 | 行数估计 |
|---|---|---|---|
| 1 | `PermissionEndpointFilter.cs` | 删除 3 行 | -3 |
| 2 | `TokenService.cs` | 删除 2 行 | -2 |
| 3 | `ITokenService.cs` | 修改 1 行 | ±0 |
| 4 | `ICurrentUser.cs` | 删除 1 行 | -1 |
| 5 | `CurrentUserProvider.cs` | 删除 2 行 | -2 |
| 6 | `AuthService.cs` | 删除/修改 ~15 行 | -15 |
| 7 | `UserService.cs` | 修改 ~10 行 | ±0 |
| 8 | `000001_base_seed.sql` | 新增 ~10 行 | +10 |

**估计总 diff: ~30 行**，远低于 200 行阈值，无需 spec 三件套。
