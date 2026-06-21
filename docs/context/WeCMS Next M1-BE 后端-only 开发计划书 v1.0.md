# WeCMS Next M1-BE 后端-only 开发计划书 v1.0

## 0. 文档定位

文档类型：M1-BE 后端-only 开发执行计划
当前状态：历史阶段计划；S14 后当前系统基础边界以 `docs/dirs/system-foundation-development-guide.md`、ADR-0018 和 ADR-0019 为准
上级文档：WeCMS Next 完整迁移重构计划 v3.0
前置阶段：M0-BE 后端底座已完成
执行方式：Codex / Codex CLI / Codex App
后端技术栈：.NET 10 + ASP.NET Core Minimal APIs + SqlSugarCore + MySQL
编译模式：普通 JIT，不采用 Native AOT
数据库访问：S14 后只允许在 `WeCms.Data.SqlSugar` 与 `WeCms.Modules.*.SqlSugar` 边界内
前端策略：前端后移，M1-BE 不开发 SoybeanAdmin
旧系统策略：旧 ThinkPHP 仅作为业务参考，不迁移数据、不做兼容
阶段目标：完成系统管理核心 API，不做 CMS 内容 API，不做前端页面

---

# 1. M1-BE 阶段总目标

M1-BE 的目标是基于 M0-BE 后端底座，完成 **系统管理模块 API**。

M1-BE 完成后，后端应具备完整的后台系统管理能力：

```text
用户管理
角色管理
菜单管理
权限管理
部门管理
岗位管理
字典管理
系统设置
登录日志
操作日志
安全事件
文件基础能力
当前用户安全能力
```

M1-BE 仍然不做前端，不进入 SoybeanAdmin。

---

# 2. M1-BE 前置条件

进入 M1-BE 前，M0-BE 必须已经完成：

```text
[ ] backend solution 可 build
[ ] SqlSugarCore 只存在于 WeCms.Data.SqlSugar 与 WeCms.Modules.*.SqlSugar
[ ] Modules 无 SQL / ORM
[ ] Auth login / refresh / logout / me 可用
[ ] Refresh Token rotation 可用
[ ] PermissionMetadata 可用
[ ] secure-ping 权限可用
[ ] OpenAPI export 成功
[ ] Auth requestBody schema 存在
[ ] backend quality gate 通过
[ ] GitHub Actions 通过
[ ] frontend/** 无修改
```

如果 M0-BE 未完成，不应开始 M1-BE。

---

# 3. M1-BE 范围

## 3.1 本阶段必须完成

```text
1. 用户管理 API
2. 角色管理 API
3. 菜单管理 API
4. 权限管理 API
5. 部门管理 API
6. 岗位管理 API
7. 字典管理 API
8. 系统设置 API
9. 登录日志 API
10. 操作审计日志 API
11. 安全事件 API
12. 当前用户安全 API
13. 文件基础 API
14. 系统管理权限码 seed
15. 系统管理菜单 seed
16. OpenAPI 契约导出
17. 系统管理 API 权限元数据扫描
18. M1-BE quality gate
```

---

## 3.2 本阶段不做

```text
1. 不做 frontend/**
2. 不运行 pnpm
3. 不生成前端 TypeScript generated
4. 不接入 SoybeanAdmin 页面
5. 不做 CMS 栏目 / 文章 / 页面 / 媒体完整业务
6. 不做 AI 模块
7. 不做多租户
8. 不做插件系统
9. 不做完整文件存储策略扩展
10. 不做内容发布工作流
11. 不做旧系统数据迁移
12. 不做旧系统兼容模式
```

---

# 4. M1-BE 技术原则

## 4.1 继续沿用 M0-BE 架构

```text
.NET 10
ASP.NET Core Minimal APIs
SqlSugarCore
MySQL
System.Text.Json
OpenAPI
JWT Bearer
模块化单体
Clean Architecture 风格分层
普通 JIT 发布
```

---

## 4.2 数据库访问边界

S14 后数据库/ORM/连接器只能在：

```text
WeCms.Data.SqlSugar
WeCms.Modules.*.SqlSugar
```

禁止在以下项目中出现数据库操作：

```text
WeCms.Api
WeCms.Modules.Cms
WeCms.Infrastructure
WeCms.Shared
```

`WeCms.Modules.*` 只能包含：

```text
Endpoints
Services / UseCases
DTOs
Repository interfaces
Permission constants
Validation rules
Business rules
```

`WeCms.Data.SqlSugar` 与 `WeCms.Modules.*.SqlSugar` 才能包含：

```text
SqlSugarCore
Entity
Repository implementation
Migration
Seed
事务实现
数据库查询
```

---

## 4.3 DI 与接口规则

业务层只依赖接口，不依赖具体实现。

禁止业务代码：

```text
new Repository(...)
new SqlSugarClient(...)
new SqlSugarScope(...)
new MySqlConnection(...)
new JwtTokenService(...)
new Pbkdf2PasswordHasher(...)
DateTime.UtcNow
Guid.NewGuid()
Random.Shared
```

允许：

```text
new DTO
new record
new ValueObject
new List
new Dictionary
new ApiResult
```

---

# 5. M1-BE 目标项目结构

M1-BE 历史阶段完成后曾使用聚合 System 模块；S14 后当前建议结构如下：

```text
backend/src/WeCms.Modules.Identity/
  Users/
    UserEndpoints.cs
    UserService.cs
    UserDtos.cs
    IUserRepository.cs
    UserPermissions.cs

  Roles/
    RoleEndpoints.cs
    RoleService.cs
    RoleDtos.cs
    IRoleRepository.cs
    RolePermissions.cs

  Menus/
    MenuEndpoints.cs
    MenuService.cs
    MenuDtos.cs
    IMenuRepository.cs
    MenuPermissions.cs

  Permissions/
    PermissionEndpoints.cs
    PermissionService.cs
    PermissionDtos.cs
    IPermissionRepository.cs
    SystemPermissions.cs
    PermissionEndpointFilter.cs
    PermissionEndpointExtensions.cs

  Departments/
    DepartmentEndpoints.cs
    DepartmentService.cs
    DepartmentDtos.cs
    IDepartmentRepository.cs
    DepartmentPermissions.cs

  Posts/
    PostEndpoints.cs
    PostService.cs
    PostDtos.cs
    IPostRepository.cs
    PostPermissions.cs

  Dicts/
    DictEndpoints.cs
    DictService.cs
    DictDtos.cs
    IDictRepository.cs
    DictPermissions.cs

  Settings/
    SettingEndpoints.cs
    SettingService.cs
    SettingDtos.cs
    ISettingRepository.cs
    SettingPermissions.cs

  Logs/
    LoginLogEndpoints.cs
    AuditLogEndpoints.cs
    SecurityEventEndpoints.cs
    LogDtos.cs
    ILogRepository.cs
    LogPermissions.cs

  Files/
    FileEndpoints.cs
    FileService.cs
    FileDtos.cs
    IFileRepository.cs
    FilePermissions.cs

backend/src/WeCms.Modules.*.SqlSugar/
  Entities/
    SysUserEntity.cs
    SysRoleEntity.cs
    SysUserRoleEntity.cs
    SysMenuEntity.cs
    SysPermissionEntity.cs
    SysRolePermissionEntity.cs
    SysRoleMenuEntity.cs
    SysDeptEntity.cs
    SysPostEntity.cs
    SysDictTypeEntity.cs
    SysDictValueEntity.cs
    SysSettingEntity.cs
    SysLoginLogEntity.cs
    SysAuditLogEntity.cs
    SysSecurityEventEntity.cs
    SysFileEntity.cs

  Modules/System/
    Users/UserRepository.cs
    Roles/RoleRepository.cs
    Menus/MenuRepository.cs
    Permissions/PermissionRepository.cs
    Departments/DepartmentRepository.cs
    Posts/PostRepository.cs
    Dicts/DictRepository.cs
    Settings/SettingRepository.cs
    Logs/LogRepository.cs
    Files/FileRepository.cs
```

---

# 6. M1-BE 数据库表范围

M1-BE 基于 M0 表继续扩展。

## 6.1 M0 已有表

```text
sys_user
sys_role
sys_user_role
sys_menu
sys_permission
sys_role_permission
sys_refresh_token
sys_login_log
sys_security_event
sys_schema_migration
```

---

## 6.2 M1 新增表

```text
sys_role_menu
sys_dept
sys_post
sys_user_post
sys_dict_type
sys_dict_value
sys_setting
sys_file
```

说明：`sys_audit_log` 已在 M0-BE 审计闭环中建立，M1-BE 继续复用并扩展其写入与查询能力，不重复新增 migration。

---

## 6.3 暂不新增表

```text
cms_channel
cms_article
cms_page
cms_media
cms_tag
cms_site
cms_link
```

这些属于 M2-BE CMS 阶段。

---

# 7. M1-BE 权限码规范

权限码格式：

```text
模块:资源:动作
```

System 模块统一使用：

```text
sys:<resource>:<action>
```

---

## 7.1 用户权限

```text
sys:user:page
sys:user:list
sys:user:detail
sys:user:create
sys:user:update
sys:user:delete
sys:user:enable
sys:user:disable
sys:user:reset-password
sys:user:assign-role
sys:user:assign-post
```

---

## 7.2 角色权限

```text
sys:role:page
sys:role:list
sys:role:detail
sys:role:create
sys:role:update
sys:role:delete
sys:role:enable
sys:role:disable
sys:role:assign-permission
sys:role:assign-menu
```

---

## 7.3 菜单权限

```text
sys:menu:page
sys:menu:list
sys:menu:tree
sys:menu:detail
sys:menu:create
sys:menu:update
sys:menu:delete
sys:menu:enable
sys:menu:disable
```

---

## 7.4 权限管理权限

```text
sys:permission:page
sys:permission:list
sys:permission:tree
sys:permission:detail
sys:permission:create
sys:permission:update
sys:permission:delete
sys:permission:enable
sys:permission:disable
```

---

## 7.5 部门权限

```text
sys:dept:page
sys:dept:list
sys:dept:tree
sys:dept:detail
sys:dept:create
sys:dept:update
sys:dept:delete
sys:dept:enable
sys:dept:disable
```

---

## 7.6 岗位权限

```text
sys:post:page
sys:post:list
sys:post:detail
sys:post:create
sys:post:update
sys:post:delete
sys:post:enable
sys:post:disable
```

---

## 7.7 字典权限

```text
sys:dict:page
sys:dict:type:list
sys:dict:type:create
sys:dict:type:update
sys:dict:type:delete
sys:dict:value:list
sys:dict:value:create
sys:dict:value:update
sys:dict:value:delete
```

---

## 7.8 设置权限

```text
sys:setting:page
sys:setting:list
sys:setting:detail
sys:setting:update
```

---

## 7.9 日志权限

```text
sys:login-log:page
sys:login-log:list
sys:login-log:detail
sys:audit-log:page
sys:audit-log:list
sys:audit-log:detail
sys:security-event:page
sys:security-event:list
sys:security-event:detail
```

---

## 7.10 文件权限

```text
sys:file:page
sys:file:list
sys:file:detail
sys:file:upload
sys:file:delete
```

---

# 8. M1-BE API 设计

所有 API 前缀：

```text
/api/v1/system
```

所有业务接口默认：

```text
需要 JWT
需要权限码
返回 ApiResult<T>
```

---

## 8.1 用户管理 API

```text
GET    /api/v1/system/users
GET    /api/v1/system/users/{id}
POST   /api/v1/system/users
PUT    /api/v1/system/users/{id}
DELETE /api/v1/system/users/{id}
POST   /api/v1/system/users/{id}/enable
POST   /api/v1/system/users/{id}/disable
POST   /api/v1/system/users/{id}/reset-password
PUT    /api/v1/system/users/{id}/roles
PUT    /api/v1/system/users/{id}/posts
```

约束：

```text
不能删除自己
不能禁用自己
不能删除最后一个 super_admin
不能禁用最后一个 super_admin
username 唯一
email 如存在则唯一
phone 如存在则唯一
password 不返回前端
```

---

## 8.2 角色管理 API

```text
GET    /api/v1/system/roles
GET    /api/v1/system/roles/{id}
POST   /api/v1/system/roles
PUT    /api/v1/system/roles/{id}
DELETE /api/v1/system/roles/{id}
POST   /api/v1/system/roles/{id}/enable
POST   /api/v1/system/roles/{id}/disable
PUT    /api/v1/system/roles/{id}/permissions
PUT    /api/v1/system/roles/{id}/menus
```

约束：

```text
role code 唯一
系统内置角色不可删除
super_admin 不可删除
super_admin 不可禁用
不能移除最后一个 super_admin 的关键权限
```

---

## 8.3 菜单管理 API

```text
GET    /api/v1/system/menus
GET    /api/v1/system/menus/tree
GET    /api/v1/system/menus/{id}
POST   /api/v1/system/menus
PUT    /api/v1/system/menus/{id}
DELETE /api/v1/system/menus/{id}
POST   /api/v1/system/menus/{id}/enable
POST   /api/v1/system/menus/{id}/disable
```

约束：

```text
菜单 code 唯一
不允许形成循环父子关系
有子菜单时不可直接删除
系统内置菜单不可删除
```

---

## 8.4 权限管理 API

```text
GET    /api/v1/system/permissions
GET    /api/v1/system/permissions/tree
GET    /api/v1/system/permissions/{id}
POST   /api/v1/system/permissions
PUT    /api/v1/system/permissions/{id}
DELETE /api/v1/system/permissions/{id}
POST   /api/v1/system/permissions/{id}/enable
POST   /api/v1/system/permissions/{id}/disable
```

约束：

```text
permission code 唯一
系统权限不可删除
已绑定角色的权限不可硬删除
权限删除默认软删除
```

---

## 8.5 部门管理 API

```text
GET    /api/v1/system/depts
GET    /api/v1/system/depts/tree
GET    /api/v1/system/depts/{id}
POST   /api/v1/system/depts
PUT    /api/v1/system/depts/{id}
DELETE /api/v1/system/depts/{id}
POST   /api/v1/system/depts/{id}/enable
POST   /api/v1/system/depts/{id}/disable
```

约束：

```text
部门 code 唯一
不允许形成循环父子关系
有子部门不可删除
部门下有用户不可删除
```

---

## 8.6 岗位管理 API

```text
GET    /api/v1/system/posts
GET    /api/v1/system/posts/{id}
POST   /api/v1/system/posts
PUT    /api/v1/system/posts/{id}
DELETE /api/v1/system/posts/{id}
POST   /api/v1/system/posts/{id}/enable
POST   /api/v1/system/posts/{id}/disable
```

约束：

```text
岗位 code 唯一
岗位下有用户不可删除
```

---

## 8.7 字典管理 API

```text
GET    /api/v1/system/dict-types
GET    /api/v1/system/dict-types/{id}
POST   /api/v1/system/dict-types
PUT    /api/v1/system/dict-types/{id}
DELETE /api/v1/system/dict-types/{id}

GET    /api/v1/system/dict-types/{typeCode}/values
POST   /api/v1/system/dict-types/{typeCode}/values
PUT    /api/v1/system/dict-values/{id}
DELETE /api/v1/system/dict-values/{id}
```

约束：

```text
dict type code 唯一
dict value 在同一 type 下 value 唯一
系统内置字典不可删除
```

---

## 8.8 系统设置 API

```text
GET /api/v1/system/settings
GET /api/v1/system/settings/{key}
PUT /api/v1/system/settings/{key}
```

约束：

```text
setting key 唯一
敏感配置不返回明文
敏感配置更新必须记录审计日志
```

---

## 8.9 登录日志 API

```text
GET /api/v1/system/login-logs
GET /api/v1/system/login-logs/{id}
```

只读，不提供删除接口。

---

## 8.10 操作审计日志 API

```text
GET /api/v1/system/audit-logs
GET /api/v1/system/audit-logs/{id}
```

只读，不提供删除接口。

---

## 8.11 安全事件 API

```text
GET /api/v1/system/security-events
GET /api/v1/system/security-events/{id}
```

只读，不提供删除接口。

---

## 8.12 文件基础 API

```text
GET    /api/v1/system/files
GET    /api/v1/system/files/{id}
POST   /api/v1/system/files
DELETE /api/v1/system/files/{id}
```

M1 只做基础能力：

```text
文件元数据入库
文件大小限制
mime type 白名单
文件扩展名白名单
不返回物理路径
删除默认软删除
```

完整对象存储、本地存储策略、图片处理后移。

---

# 9. M1-BE 数据模型概要

## 9.1 sys_user 扩展字段

```text
dept_id
status
last_login_at
last_login_ip
security_stamp
permission_version
```

---

## 9.2 sys_dept

```text
id
parent_id
code
name
sort_order
status
created_at
updated_at
deleted_at
```

---

## 9.3 sys_post

```text
id
code
name
sort_order
status
created_at
updated_at
deleted_at
```

---

## 9.4 sys_user_post

```text
id
user_id
post_id
created_at
```

---

## 9.5 sys_dict_type

```text
id
code
name
description
is_system
status
sort_order
created_at
updated_at
deleted_at
```

---

## 9.6 sys_dict_value

```text
id
type_id
label
value
description
sort_order
status
is_default
created_at
updated_at
deleted_at
```

---

## 9.7 sys_setting

```text
id
key
value
value_type
group_code
name
description
is_sensitive
is_system
updated_at
updated_by
```

---

## 9.8 sys_audit_log

```text
id
user_id
username
module
resource
action
target_id
request_method
request_path
ip_address
user_agent
trace_id
result
detail
created_at
```

---

## 9.9 sys_file

```text
id
storage_provider
bucket
object_key
original_name
file_ext
mime_type
size_bytes
sha256
status
created_by
created_at
deleted_at
```

---

# 10. M1-BE 任务拆分

M1-BE 拆分为 17 个开发任务：

```text
M1-BE-000：M1 规则与 ADR 更新
M1-BE-001：System 权限码和菜单 seed
M1-BE-002：User API
M1-BE-003：Role API
M1-BE-004：Menu API
M1-BE-005：Permission API
M1-BE-006：Department API
M1-BE-007：Post API
M1-BE-008：Dict API
M1-BE-009：Setting API
M1-BE-010：LoginLog API
M1-BE-011：AuditLog API
M1-BE-012：SecurityEvent API
M1-BE-013：File 基础 API
M1-BE-014：OpenAPI 契约增强
M1-BE-015：Quality Gate 与 CI 更新
M1-BE-016：M1-BE 最终只读审计
```

---

# 11. M1-BE-000：M1 规则与 ADR 更新

## 目标

为 M1-BE 建立阶段边界和规则。

## 交付物

```text
docs/adr/0013-m1-system-management-api-scope.md
docs/context/WeCMS_Next_M1-BE_系统管理API开发计划.md
README.md
AGENTS.md
code_review.md
```

## 规则

```text
M1-BE 不做前端
M1-BE 不做 CMS 内容 API
M1-BE 不迁移旧数据
M1-BE 只做系统管理 API
M1-BE 所有数据库操作只能在 Persistence
M1-BE 所有接口必须有权限码
```

---

# 12. M1-BE-001：System 权限码和菜单 seed

## 目标

初始化系统管理模块权限码和菜单。

## 交付物

```text
database/seeds/000003_seed_m1_system_permissions.sql
database/seeds/000004_seed_m1_system_menus.sql
database/seeds/000005_seed_m1_role_permissions.sql
```

## 验收标准

```text
[ ] 所有 M1 API 都有权限码
[ ] super_admin 自动拥有全部权限
[ ] 系统菜单 seed 可幂等执行
[ ] 权限码 code 唯一
[ ] 菜单 code 唯一
```

---

# 13. M1-BE-002：User API

## 目标

完成用户管理后端 API。

## 交付物

```text
UserEndpoints.cs
UserService.cs
UserDtos.cs
IUserRepository.cs
UserRepository.cs
UserPermissions.cs
UserServiceTests.cs
UserRepositoryTests.cs
```

## 验收标准

```text
[ ] 用户分页
[ ] 用户详情
[ ] 新增用户
[ ] 编辑用户
[ ] 删除用户
[ ] 启用用户
[ ] 禁用用户
[ ] 重置密码
[ ] 分配角色
[ ] 分配岗位
[ ] 不能删除自己
[ ] 不能禁用自己
[ ] 不能删除最后一个 super_admin
[ ] 不能禁用最后一个 super_admin
```

---

# 14. M1-BE-003：Role API

## 目标

完成角色管理后端 API。

## 验收标准

```text
[ ] 角色分页
[ ] 角色详情
[ ] 新增角色
[ ] 编辑角色
[ ] 删除角色
[ ] 启用角色
[ ] 禁用角色
[ ] 分配权限
[ ] 分配菜单
[ ] 系统角色不可删除
[ ] super_admin 不可删除
[ ] super_admin 不可禁用
```

---

# 15. M1-BE-004：Menu API

## 目标

完成菜单管理后端 API。

## 验收标准

```text
[ ] 菜单列表
[ ] 菜单树
[ ] 菜单详情
[ ] 新增菜单
[ ] 编辑菜单
[ ] 删除菜单
[ ] 启用菜单
[ ] 禁用菜单
[ ] 不允许循环父子关系
[ ] 有子菜单不可删除
[ ] 系统菜单不可删除
```

---

# 16. M1-BE-005：Permission API

## 目标

完成权限管理后端 API。

## 验收标准

```text
[ ] 权限列表
[ ] 权限树
[ ] 权限详情
[ ] 新增权限
[ ] 编辑权限
[ ] 删除权限
[ ] 启用权限
[ ] 禁用权限
[ ] 系统权限不可删除
[ ] 已绑定角色权限不可硬删除
```

---

# 17. M1-BE-006：Department API

## 目标

完成部门管理后端 API。

## 验收标准

```text
[ ] 部门列表
[ ] 部门树
[ ] 部门详情
[ ] 新增部门
[ ] 编辑部门
[ ] 删除部门
[ ] 启用部门
[ ] 禁用部门
[ ] 不允许循环父子关系
[ ] 有子部门不可删除
[ ] 部门下有用户不可删除
```

---

# 18. M1-BE-007：Post API

## 目标

完成岗位管理后端 API。

## 验收标准

```text
[ ] 岗位分页
[ ] 岗位详情
[ ] 新增岗位
[ ] 编辑岗位
[ ] 删除岗位
[ ] 启用岗位
[ ] 禁用岗位
[ ] 岗位 code 唯一
[ ] 岗位下有用户不可删除
```

---

# 19. M1-BE-008：Dict API

## 目标

完成字典类型和字典值 API。

## 验收标准

```text
[ ] 字典类型分页
[ ] 字典类型详情
[ ] 新增字典类型
[ ] 编辑字典类型
[ ] 删除字典类型
[ ] 字典值列表
[ ] 新增字典值
[ ] 编辑字典值
[ ] 删除字典值
[ ] 系统字典不可删除
[ ] 同一 type 下 value 唯一
```

---

# 20. M1-BE-009：Setting API

## 目标

完成系统设置 API。

## 验收标准

```text
[ ] 设置列表
[ ] 设置详情
[ ] 更新设置
[ ] setting key 唯一
[ ] 敏感配置不返回明文
[ ] 敏感配置更新写审计日志
```

---

# 21. M1-BE-010：LoginLog API

## 目标

完成登录日志查询 API。

## 验收标准

```text
[ ] 登录日志分页
[ ] 登录日志详情
[ ] 支持 username / ip / result / date range 查询
[ ] 不提供删除接口
```

---

# 22. M1-BE-011：AuditLog API

## 目标

完成操作审计日志 API。

## 验收标准

```text
[ ] 操作日志分页
[ ] 操作日志详情
[ ] 支持 user / module / resource / action / result / date range 查询
[ ] 写操作自动记录 audit_log
[ ] 不提供删除接口
```

---

# 23. M1-BE-012：SecurityEvent API

## 目标

完成安全事件查询 API。

## 验收标准

```text
[ ] 安全事件分页
[ ] 安全事件详情
[ ] 支持 event_type / severity / user / ip / date range 查询
[ ] 不提供删除接口
```

---

# 24. M1-BE-013：File 基础 API

## 目标

完成文件元数据基础 API。

## 验收标准

```text
[ ] 文件列表
[ ] 文件详情
[ ] 文件上传元数据入库
[ ] 文件软删除
[ ] 不返回物理路径
[ ] 限制文件大小
[ ] 限制 mime type
[ ] 限制扩展名
```

---

# 25. M1-BE-014：OpenAPI 契约增强

## 目标

确保 M1 所有 API 都进入 OpenAPI。

## 验收标准

```text
[ ] 所有 M1 paths 出现在 OpenAPI
[ ] 所有 POST / PUT 有 requestBody
[ ] 所有列表接口有 query 参数 schema
[ ] 所有 response schema 正确
[ ] OpenAPI 稳定导出
```

---

# 26. M1-BE-015：Quality Gate 与 CI 更新

## 目标

让 M1-BE 的系统管理 API 纳入 CI。

## 新增检查

```text
[ ] check-system-permission-coverage
[ ] check-system-openapi-coverage
[ ] check-no-sql-in-modules
[ ] check-db-boundary
[ ] check-layer-dependency
[ ] check-di-boundary
[ ] check-no-frontend-change
```

---

# 27. M1-BE-016：最终只读审计

## 目标

完成 M1-BE 后只读复审。

## 审计范围

```text
代码结构
依赖边界
Persistence 边界
SqlSugar 使用位置
权限码覆盖
OpenAPI 覆盖
Auth 安全
User / Role / Menu 安全规则
日志与审计
CI 结果
前端是否未动
```

---

# 28. M1-BE 最终质量门禁

M1-BE 质量门禁必须包含：

```text
dotnet restore
dotnet build -warnaserror
dotnet test
dotnet publish -c Release --no-self-contained
OpenAPI export
OpenAPI endpoint coverage
OpenAPI requestBody coverage
Permission metadata coverage
System permission seed coverage
DB boundary
Layer dependency
DI boundary
No frontend change
Code review scan
Migration / seed smoke test
```

---

# 29. M1-BE 最终验收清单

```text
[ ] 用户 API 完成
[ ] 角色 API 完成
[ ] 菜单 API 完成
[ ] 权限 API 完成
[ ] 部门 API 完成
[ ] 岗位 API 完成
[ ] 字典 API 完成
[ ] 设置 API 完成
[ ] 登录日志 API 完成
[ ] 审计日志 API 完成
[ ] 安全事件 API 完成
[ ] 文件基础 API 完成
[ ] 所有 M1 API 有权限码
[ ] 所有 M1 API 有 PermissionMetadata
[ ] super_admin 拥有所有 M1 权限
[ ] OpenAPI 覆盖所有 M1 API
[ ] 所有 POST / PUT 有 requestBody schema
[ ] Modules 无 SQL / ORM
[ ] Persistence 是唯一数据库层
[ ] 所有业务依赖通过接口 + DI
[ ] 质量门禁通过
[ ] GitHub Actions 通过
[ ] frontend/** 无修改
```

---

# 30. M1-BE 完成定义

M1-BE 完成后，系统应达到：

```text
系统管理 API 基本完整
权限体系基本完整
菜单体系基本完整
用户 / 角色 / 权限闭环可用
后台管理所需基础数据 API 就绪
OpenAPI 契约稳定
前端可以基于 OpenAPI 开始设计，但仍不进入正式前端开发
```

M1-BE 完成后进入：

```text
M2-BE：CMS 内容 API
```

而不是进入前端。
