# WeCMS-next M1-BE 最终验收结论

## 验收结论

M1-BE 已完成后端核心能力建设，并通过最终复审。当前后端具备进入 M2-BE 开发的基础条件。

结论：

```text
M1-BE：验收通过
状态：Accepted
下一阶段：可启动 M2-BE
```

## M1-BE 通过项

本轮 M1-BE 已完成并通过以下模块验收：

* Auth
* Users
* Roles
* Permissions
* Menus
* Departments
* Posts
* Dicts
* Settings
* Logs
* Files
* OpenAPI
* Migration / Seed
* Quality Gate

## 已关闭问题

以下问题已完成修复、复审并关闭：

* P1-AUTHZ-003：`super_admin` / locked role 权限可被降级问题
* P1-SEC：用户删除、禁用、重置密码后 refresh token 未吊销问题
* P1-SEC：软删除用户仍可能通过认证链路问题
* P2-CONTRACT：OpenAPI request / response 契约不一致问题
* P1/P3-HARDEN：locked role holder 并发保护问题
* P3-HARDEN：AssignRoles / Delete / Disable 并发测试矩阵补强

## 验收说明

M1-BE 当前已建立完整的后端系统管理基础能力，包括认证授权、用户管理、角色管理、权限管理、菜单管理、部门管理、岗位管理、字典管理、系统设置、日志审计、文件管理、OpenAPI 导出、数据库迁移与初始化种子数据。

其中，角色锁定机制已完成闭环：

* `is_locked` 字段已落地。
* locked role 不可被删除、禁用、启用、修改权限、修改菜单或修改核心信息。
* locked role 必须至少保留一个 enabled 且未删除的用户持有。
* 并发场景下，AssignRoles / Delete / Disable 均已覆盖回归测试。
* Repository 层已通过事务与 `FOR UPDATE` 保证并发安全。

同时，OpenAPI 契约、权限码覆盖、迁移种子、质量门禁和 GitHub Actions 均已通过验收要求。

## 最终判断

M1-BE 不再存在阻断 M2-BE 的遗留问题。

建议将当前通过 GitHub Actions 的 commit 作为 M1-BE 验收基线，并打 tag 固化，例如：

```text
m1-be-accepted
```

M2-BE 可以在该基线之上启动。
