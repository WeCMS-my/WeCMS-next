# Checklist — fix-m0-audit9-high

## H5 — CORS 配置化白名单
- [x] appsettings.json 含 Cors:AllowedOrigins 配置节
- [x] appsettings.Development.json 配置 localhost:5173
- [x] Program.cs 从配置读取并调用 WithOrigins()
- [x] 未配置时启动 fail-fast

## H3 — 角色删除级联清理
- [x] RoleService.DeleteAsync 在事务中 DELETE sys_role_menu
- [x] RoleService.DeleteAsync 在事务中 DELETE sys_role_permission
- [x] 删除后调用 BumpPermissionVersion

## H4 — 用户名唯一性
- [x] UpdateUserRequest 不含 Username 字段（无需修改）
- [x] 代码添加注释说明原因

## H1 — Token 即时失效
- [x] ChangePasswordAsync 递增 permission_version
- [x] SetStatusAsync 禁用时递增 permission_version
- [x] PermissionEndpointFilter 比对 JWT permission_version 与 DB 值
- [x] 版本不匹配返回 401
- [x] Super-admin 跳过逻辑已恢复

## H2 — Refresh Token 会话上限
- [x] StoreRefreshToken 检查活跃 token 数
- [x] 超过 10 个时删除最旧的
- [x] 上限检查在事务内执行

## H6 — CollectDescendants N+1 消除
- [x] DeleteAsync 先一次查询全量菜单
- [x] CollectDescendants 改为纯内存递归
- [x] 仅执行 2 次 SQL（查全量 + 批量软删除）

## H7 — SortAsync 批量 UPDATE
- [x] SortAsync 用 CASE WHEN 单 SQL
- [x] 排序值从 0 开始递增
- [x] 仅执行 1 次 UPDATE

## 全量验证
- [x] dotnet build -warnaserror 通过
- [x] dotnet test 通过
- [x] dotnet publish -c Release -r win-x64 /p:PublishAot=true 通过
- [x] code_review.md 审查通过
