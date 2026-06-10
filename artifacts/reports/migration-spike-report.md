# WeCMS Next M0 ThinkPHP 迁移 Spike 报告

> 文档类型：迁移探测报告（Spike Report）  
> 适用阶段：M0-BE-011  
> 生成日期：2026-06-10  
> 状态：**Spike 完成 — 旧系统为开发阶段，无生产数据，无需迁移。新系统从种子数据零起点启动。**

---

## 1. 结论（定版）

经确认：ThinkPHP 旧系统当前处于**开发阶段，从未部署到生产环境**。`think_admin`、`think_auth_group`、`think_auth_rule`、`think_config` 等表中**无真实用户业务数据**。

决策：

- **不执行任何数据迁移**。旧系统数据库仅作为 schema 设计参考，不作为数据迁移源。
- **新系统从零开始**。通过 `database/seeds/` 种子数据初始化：`000001_seed_m0_base_permissions.sql` + `000002_seed_m0_super_admin.sql`。
- **不实现兼容模式**。不保留旧密码哈希验证逻辑、不实现 `password_migrated_at` 兼容流程、不引入 legacy 分支。
- **旧表仅作 schema 设计参考**。新表结构已独立设计并进入 M0 migration，不受旧表 schema 约束。

---

## 2. Schema 对照（仅供参考）

本对照表仅用于**设计审计**，确认新系统表结构未遗漏旧系统已识别的核心概念。不用于实际数据映射。

| 旧表 (ThinkPHP) | 新表 (WeCMS Next) | 对照结论 |
|---|---|---|
| `think_admin` | `sys_user` | 新表已覆盖。新增 `permission_version`、`security_stamp`、`password_hash_algorithm`、`legacy_id`（预留） |
| `think_auth_group` | `sys_role` | 新表已覆盖。新增 `code`（唯一标识）、`is_system`、`is_builtin`。旧 CSV `rules` 字段已废弃，改用 `sys_role_permission` 关系表 |
| `think_auth_group_access` | `sys_user_role` | 新表已覆盖。新表支持多角色，不再依赖旧 `think_admin.groupid` 单字段 |
| `think_auth_rule` | `sys_menu` + `sys_permission` | 新表已拆分。旧单表混存菜单和权限的问题已在设计中解决 |
| `think_config` | `sys_setting`（二期） | M0 暂未创建，二期 M3 创建 |

---

## 3. 旧系统关键技术债（已在新系统解决）

| 编号 | 旧系统问题 | 新系统解决方案 |
|---|---|---|
| T04 | 角色权限以 CSV 存储 `rules = "1,2,3"` | `sys_role_permission` 关系表 |
| T05 | 菜单和权限混存在 `think_auth_rule` 单表 | `sys_menu` + `sys_permission` 独立表 |
| T06 | 用户-角色单字段 `groupid` | `sys_user_role` 多对多 |
| T07 | 权限标识为 URL 路径 `/user/index` | 权限码 `sys:user:list` |
| T09 | Session 认证 | JWT Bearer Token |
| T13 | 文件存储 disk 硬编码 | `IFileStorage` 抽象 |

---

## 4. 权限码命名对照（旧路径 → 新权限码）

旧系统权限路径仅作为新系统权限码设计的**参考来源**。新系统权限码已独立定义，不依赖旧系统数据。

| 旧路径 | 新权限码 | 说明 |
|---|---|---|
| `/user/index` | `sys:user:page` | 用户管理页面 |
| `/user/list` | `sys:user:list` | 用户列表 |
| `/user/add` | `sys:user:create` | 新增用户 |
| `/user/edit` | `sys:user:update` | 编辑用户 |
| `/user/del` | `sys:user:delete` | 删除用户 |
| `/role/index` | `sys:role:page` | 角色页面 |
| `/role/add` | `sys:role:create` | 新增角色 |
| `/role/edit` | `sys:role:update` | 编辑角色 |
| `/role/del` | `sys:role:delete` | 删除角色 |
| `/setting/index` | `sys:setting:view` | 查看设置 |

完整 50+ 项映射见 `docs/context/WeCMS_ThinkPHP_系统详细说明文档.md` §21。

---

## 5. 新系统种子数据初始化

新系统通过已有的 `database/seeds/` 初始化，无需依赖旧系统数据：

| 种子文件 | 内容 |
|---|---|
| `000001_seed_m0_base_permissions.sql` | 插入基础权限码（已执行） |
| `000002_seed_m0_super_admin.sql` | 创建 `super_admin` 角色 + `admin` 用户（密码 `Admin@123`）+ 角色-权限全量关联 |

---

## 6. 不引入兼容模式

| 不做 | 原因 |
|---|---|
| 不保留旧密码哈希验证逻辑 | 无旧用户需要兼容登录 |
| 不实现 `password_migrated_at` 升级流程 | 无旧 hash 需要升级 |
| 不引入 legacy 分支 | 无旧数据需要兼容 |
| 不迁移旧 token / 2FA secret / SMTP 密码 | 不存在此类旧数据 |
| 不在运行时代码中判断旧系统格式 | 无旧格式需要兼容 |

---

## 7. 风险结论

| 风险 | 状态 |
|---|---|
| 数据丢失 | **N/A** — 旧系统无生产数据 |
| 权限映射错误 | **N/A** — 新系统权限码独立设计，不依赖旧系统数据映射 |
| CSV 拆分错误 | **N/A** — 无 CSV 数据需要拆分 |
| 密码哈希不兼容 | **N/A** — 不保留旧密码哈希 |

**无阻塞性风险。**

---

## 8. Spike 交付物

| 文件 | 用途 |
|---|---|
| `artifacts/reports/migration-spike-report.md`（本文件） | M0 Spike 报告 |
| `database/legacy-migration/m0_spike_users_roles_permissions.sql` | Schema 对照 SQL（仅注释，不执行迁移） |
