# Checklist

## P1 — 数据完整性与安全逻辑

- [x] Logout 仅吊销当前 Refresh Token，非全部
- [x] Refresh Token 复用检测已实现，复用触发安全事件
- [x] TOTP 重放保护已实现（检查 two_factor_last_used_ts）
- [x] 文件上传双扩展名检测已实现
- [x] 文件存储使用可配置绝对路径
- [x] 菜单创建/更新循环检测已实现
- [x] 菜单删除级联软删除子孙节点
- [x] 删除/禁用用户检查最后一个超级管理员
- [x] 系统角色（is_system=1）不可删除
- [x] 字典类型删除级联软删除字典值
- [x] i18n 使用软删除（deleted_at）
- [x] db-check 端点不泄露异常详情
- [x] UserService.UpdateAsync 修改角色后递增 permission_version

## P2 — 代码质量与架构

- [x] 多步数据库操作有事务包裹（UserService.CreateAsync、UpdateAsync、RoleService.Assign*、AuthService.RefreshTokenAsync、ResetPasswordAsync）
- [x] is_super_admin 已放入 JWT claims，PermissionEndpointFilter 优先从 claims 读取
- [x] ICurrentUser 抽象已创建并注册
- [x] IClock 抽象已创建并注册
- [x] ISecurityEventLogger 改为必需依赖
- [x] GET /health/ready 端点已添加
- [x] AuthEndpoints 中内联 SQL 已提取到 AuthService
- [x] `dotnet build backend/WeCms.sln -warnaserror` 通过
- [x] `dotnet test backend/WeCms.sln` 通过
