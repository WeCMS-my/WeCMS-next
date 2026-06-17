# WeCMS-next M2-FE 基础系统前端开发计划书

## 1. 阶段定位

M2-FE 是一期的前端阶段，目标是基于已验收的 M1-BE 系统管理 API，完成一个可登录、可授权、可操作的后台管理端。

M2-FE 不开发 CMS 内容管理功能。CMS 栏目、文章、页面、标签、发布、版本、回收站等全部移动到二期。

当前 M1-BE 已提供 Auth、Users、Roles、Menus、Permissions、Departments、Posts、Dicts、Settings、Logs、Files 等后端接口，后端入口已显式注册这些模块。

M2-FE 的阶段目标是：

```text
完成基础系统后台前端闭环：
登录 → 获取用户信息 → 动态菜单/路由 → 权限控制 → 系统管理页面 CRUD → 文件管理 → 日志查看。
```

---

## 2. M2-FE 范围

### 2.1 包含范围

M2-FE 包含以下前端能力：

* 登录 / 登出
* Token 存储与刷新
* `/auth/me` 当前用户态恢复
* 动态菜单
* 动态路由
* 权限按钮控制
* 用户管理
* 角色管理
* 菜单管理
* 权限管理
* 部门管理
* 岗位管理
* 字典管理
* 系统设置
* 登录日志
* 审计日志
* 安全事件
* 文件管理
* API 请求封装
* OpenAPI / 类型对齐
* 前端基础质量门禁

### 2.2 不包含范围

M2-FE 不包含：

```text
1. CMS 栏目管理
2. CMS 文章管理
3. CMS 页面管理
4. CMS 标签管理
5. CMS 发布管理
6. CMS 媒体库高级能力
7. 公开站点渲染
8. SEO 管理
9. AI 内容生成
10. 旧系统迁移
11. 多租户后台
12. 审批流
13. 评论系统
14. 会员系统
```

---

## 3. 技术原则

M2-FE 应遵循以下原则：

```text
1. 前端只消费 M1-BE 已验收 API。
2. 不新增 /api/v1/cms/* 调用。
3. 不绕过后端权限控制。
4. 前端权限只用于 UI 显示，最终授权以后端为准。
5. API 类型必须和 OpenAPI 保持一致。
6. 所有页面必须处理 loading / error / empty 状态。
7. 所有写操作必须有明确成功/失败反馈。
8. 所有危险操作必须二次确认。
9. 不在前端硬编码超级管理员逻辑。
10. 不在前端存储敏感明文信息。
```

---

## 4. 后端 API 基线

M2-FE 主要对接以下后端 API。

### 4.1 Auth

```text
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

后端 Auth Endpoint 已提供 login、refresh、logout、me。`login`、`refresh`、`logout` 允许匿名访问，`me` 需要认证。

当前 Auth 响应包含用户、角色、权限、菜单结构，其中 `LoginResponse` 包含 accessToken、refreshToken、expiresAt、user、roles、permissions、menus；`AuthMeResponse` 包含 user、roles、permissions、menus。

### 4.2 System Management

```text
/api/v1/system/users
/api/v1/system/roles
/api/v1/system/menus
/api/v1/system/permissions
/api/v1/system/depts
/api/v1/system/posts
/api/v1/system/dict-types
/api/v1/system/dict-values
/api/v1/system/settings
/api/v1/system/login-logs
/api/v1/system/audit-logs
/api/v1/system/security-events
/api/v1/system/files
```

### 4.3 Files

文件管理后端已提供列表、详情、上传、下载、预览、删除能力。下载和预览接口分别为：

```text
GET /api/v1/system/files/{id:long}/download
GET /api/v1/system/files/{id:long}/preview
```

并且都绑定 `FilePermissions.Download`。

---

## 5. 页面规划

## 5.1 登录页

### 路由

```text
/login
```

### 功能

* 用户名输入
* 密码输入
* 登录提交
* 登录失败提示
* 登录成功后保存 token
* 登录成功后初始化用户态
* 跳转首页

### API

```text
POST /api/v1/auth/login
```

### 输入校验

```text
username 必填
password 必填
禁止空白提交
```

### 状态处理

```text
loading
登录失败
账号禁用
密码错误
接口异常
```

### 验收标准

```text
1. 正确登录后进入后台。
2. 错误密码显示错误提示。
3. 登录成功后 accessToken / refreshToken 正确保存。
4. 刷新页面后可通过 /auth/me 恢复登录态。
```

---

## 5.2 首页 / 工作台

### 路由

```text
/dashboard
```

### 功能

M2-FE 阶段首页可以保持简单：

* 当前用户信息
* 当前角色
* 当前权限数量
* 系统快捷入口
* 后端健康状态，可选

### API

```text
GET /api/v1/auth/me
GET /api/v1/system/version
GET /health/ready
```

### 验收标准

```text
1. 登录后默认进入 dashboard。
2. 能显示当前用户。
3. 无权限时不显示不可访问入口。
```

---

## 5.3 用户管理

### 路由

```text
/system/users
```

### API

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

### 页面功能

* 用户列表
* 按关键词搜索
* 按状态筛选
* 按部门筛选
* 新建用户
* 编辑用户
* 删除用户
* 启用 / 禁用用户
* 重置密码
* 分配角色
* 分配岗位
* 查看详情

### 表格字段

```text
ID
用户名
显示名
邮箱
手机
部门
状态
是否超级管理员
最近登录时间
创建时间
操作
```

### 表单字段

```text
username
displayName
password
email
phone
deptId
roleIds
postIds
```

### 权限控制

```text
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

### 验收标准

```text
1. 用户列表分页正常。
2. 创建用户成功后列表刷新。
3. 编辑用户成功后详情更新。
4. 禁用用户后用户无法继续使用旧 refresh token。
5. 重置密码后用户旧 token 失效。
6. 不能在前端展示密码 hash。
7. 没有对应权限时隐藏按钮。
```

---

## 5.4 角色管理

### 路由

```text
/system/roles
```

### API

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

### 页面功能

* 角色列表
* 新建角色
* 编辑角色
* 删除角色
* 启用 / 禁用角色
* 分配权限
* 分配菜单
* 查看角色详情
* 展示 `isBuiltin`
* 展示 `isLocked`

### 表格字段

```text
ID
角色编码
角色名称
状态
是否内置
是否锁定
创建时间
操作
```

### 重要前端规则

```text
1. isLocked = true 时，前端禁用编辑、删除、启用、禁用、分配权限、分配菜单按钮。
2. isBuiltin = true 时，前端禁用删除按钮。
3. 这些只是 UI 保护，最终以后端校验为准。
```

### 权限控制

```text
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

### 验收标准

```text
1. locked role 操作按钮不可点击。
2. 强行调用 locked role 操作接口时显示后端错误。
3. 权限树展示正常。
4. 菜单树展示正常。
5. 分配权限 / 菜单后能保存并回显。
```

---

## 5.5 菜单管理

### 路由

```text
/system/menus
```

### API

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

### 页面功能

* 菜单树表格
* 新建目录 / 菜单 / 按钮
* 编辑菜单
* 删除菜单
* 启用 / 禁用菜单
* 图标字段
* 路由 path 字段
* component 字段
* permissionCode 字段

### 表单字段

```text
parentId
type
code
path
component
title
i18nKey
icon
sort
hidden
keepAlive
externalUrl
permissionCode
status
```

### 前端规则

```text
type = catalog / menu / button
button 类型可以不参与路由
catalog/menu 类型参与菜单树
builtin 菜单不允许删除、启用、禁用
```

### 验收标准

```text
1. 菜单树正确显示。
2. 新增子菜单后树更新。
3. 禁止形成前端循环父子关系。
4. 后端拒绝成环时前端正确提示。
5. 动态路由可根据菜单生成。
```

---

## 5.6 权限管理

### 路由

```text
/system/permissions
```

### API

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

### 页面功能

* 权限列表
* 权限树
* 新建权限
* 编辑权限
* 删除权限
* 启用 / 禁用权限
* 显示是否内置
* 显示是否已绑定角色

### 表格字段

```text
ID
权限码
名称
模块
描述
状态
是否内置
是否绑定角色
操作
```

### 前端规则

```text
isBuiltin = true 时禁用删除、启用、禁用按钮
isRoleBound = true 时删除前必须二次确认
```

### 验收标准

```text
1. 权限树按 module 分组。
2. 内置权限不可禁用。
3. 角色绑定权限可正确回显。
```

---

## 5.7 部门管理

### 路由

```text
/system/depts
```

### API

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

### 页面功能

* 部门树
* 新建部门
* 编辑部门
* 删除部门
* 启用 / 禁用部门

### 表单字段

```text
parentId
code
name
sortOrder
status
```

### 验收标准

```text
1. 部门树正确。
2. 有子部门时删除失败并提示。
3. 已分配用户的部门删除失败并提示。
4. 不允许选择自己或后代作为父部门。
```

---

## 5.8 岗位管理

### 路由

```text
/system/posts
```

### API

```text
GET    /api/v1/system/posts
GET    /api/v1/system/posts/{id}
POST   /api/v1/system/posts
PUT    /api/v1/system/posts/{id}
DELETE /api/v1/system/posts/{id}
POST   /api/v1/system/posts/{id}/enable
POST   /api/v1/system/posts/{id}/disable
```

### 页面功能

* 岗位列表
* 新建岗位
* 编辑岗位
* 删除岗位
* 启用 / 禁用岗位

### 表单字段

```text
code
name
sortOrder
status
```

### 验收标准

```text
1. 岗位列表分页正常。
2. 被用户引用岗位删除失败并提示。
3. 岗位可分配给用户。
```

---

## 5.9 字典管理

### 路由

```text
/system/dicts
```

建议页面结构：

```text
左侧：字典类型列表
右侧：当前类型下字典值列表
```

### API

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

### 页面功能

* 字典类型分页
* 字典类型新增 / 编辑 / 删除
* 字典值列表
* 字典值新增 / 编辑 / 删除
* 默认值显示
* 状态显示

### 验收标准

```text
1. 选择字典类型后加载对应字典值。
2. 系统字典类型不可删除。
3. 有字典值的字典类型不可删除。
4. 同一字典类型下 value 唯一。
```

---

## 5.10 系统设置

### 路由

```text
/system/settings
```

### API

```text
GET /api/v1/system/settings
GET /api/v1/system/settings/{key}
PUT /api/v1/system/settings/{key}
```

### 页面功能

* 设置列表
* 按关键词搜索
* 按 groupCode 筛选
* 编辑设置值
* 敏感配置隐藏值

### 前端规则

```text
1. isSensitive = true 时，列表和详情不展示真实 value。
2. 编辑敏感配置时只允许输入新值，不展示旧值。
3. valueType = boolean 时可使用 switch。
4. valueType = number 时使用数字输入。
5. valueType = json 时使用多行文本或 JSON 编辑器。
```

### 验收标准

```text
1. 敏感配置不泄露。
2. 修改敏感配置后显示成功，但不回显真实值。
3. valueType 错误由后端拒绝时前端正确提示。
```

---

## 5.11 日志管理

### 路由

```text
/system/logs/login
/system/logs/audit
/system/logs/security
```

### API

```text
GET /api/v1/system/login-logs
GET /api/v1/system/login-logs/{id}

GET /api/v1/system/audit-logs
GET /api/v1/system/audit-logs/{id}

GET /api/v1/system/security-events
GET /api/v1/system/security-events/{id}
```

### 页面功能

#### 登录日志

```text
用户名
IP
结果
原因
时间范围
详情
```

#### 审计日志

```text
用户
模块
资源
动作
结果
时间范围
详情
```

#### 安全事件

```text
事件类型
用户
IP
级别
消息
时间范围
详情
```

### 验收标准

```text
1. 所有日志列表分页正常。
2. 时间范围 from <= to。
3. 详情弹窗正确展示。
4. 日志页面只读，不提供修改能力。
```

---

## 5.12 文件管理

### 路由

```text
/system/files
```

### API

```text
GET    /api/v1/system/files
GET    /api/v1/system/files/{id}
POST   /api/v1/system/files
GET    /api/v1/system/files/{id}/download
GET    /api/v1/system/files/{id}/preview
DELETE /api/v1/system/files/{id}
```

### 页面功能

* 文件列表
* 文件上传
* 文件详情
* 文件下载
* 文件预览
* 文件删除

### 上传表单

```text
file
originalName
mimeType
sizeBytes
sha256
```

### 前端规则

```text
1. 上传前计算 sha256。
2. 上传前获取 file.size。
3. 上传前读取 file.type。
4. 上传时使用 multipart/form-data。
5. 限制文件类型：jpg/jpeg/png/webp/pdf/txt。
6. 限制文件大小：10MB。
```

### 验收标准

```text
1. 上传成功后列表刷新。
2. sha256 / mimeType / sizeBytes 与后端校验一致。
3. 图片可预览。
4. PDF 可预览或下载。
5. 无权限时隐藏上传 / 删除 / 下载按钮。
```

---

## 6. 前端权限设计

### 6.1 权限来源

前端权限来自：

```text
/auth/login
/auth/me
```

其中 `permissions` 只用于 UI 控制，不作为真实授权依据。

### 6.2 权限工具函数

建议提供：

```ts
hasPermission(code: string): boolean
hasAnyPermission(codes: string[]): boolean
hasAllPermissions(codes: string[]): boolean
```

### 6.3 按钮级控制

每个操作按钮必须绑定权限码。

示例：

```text
新建用户 -> sys:user:create
编辑用户 -> sys:user:update
删除用户 -> sys:user:delete
重置密码 -> sys:user:reset-password
分配角色 -> sys:user:assign-role
```

### 6.4 路由级控制

路由元信息建议包含：

```ts
meta: {
  title: string
  icon?: string
  permissions?: string[]
}
```

进入路由时检查权限。

---

## 7. 动态菜单 / 动态路由设计

### 7.1 菜单来源

优先使用：

```text
/auth/me 返回的 menus
```

如果当前后端 `menus` 暂时为空，则 M2-FE 可采用过渡方案：

```text
1. 登录后调用 /api/v1/system/menus/tree
2. 根据 permissionCode 和当前 permissions 过滤菜单
3. 生成前端路由
```

### 7.2 菜单字段映射

后端菜单字段：

```text
id
parentId
type
code
path
component
title
i18nKey
icon
sort
hidden
keepAlive
externalUrl
permissionCode
status
isBuiltin
children
```

前端路由映射：

```text
path -> route.path
component -> component loader
title -> meta.title
icon -> meta.icon
hidden -> meta.hideInMenu
keepAlive -> meta.keepAlive
permissionCode -> meta.permissions
externalUrl -> 外链
```

### 7.3 路由组件白名单

前端不能直接信任后端传入的 component 字符串动态 import 任意路径。

必须使用组件映射表：

```ts
const routeComponentMap = {
  'system/user/index': () => import('@/views/system/user/index.vue'),
  'system/role/index': () => import('@/views/system/role/index.vue')
}
```

如果后端 component 不在白名单中：

```text
1. 不注册该路由。
2. 控制台 warning。
3. 可显示 fallback 页面。
```

---

## 8. API 请求封装

### 8.1 请求基础配置

统一封装：

```text
baseURL
timeout
Authorization header
traceId header，可选
request interceptor
response interceptor
```

### 8.2 Token 刷新策略

建议：

```text
1. accessToken 过期或接口返回 Unauthorized 时，尝试 refresh。
2. refresh 成功后重放原请求。
3. refresh 失败后清空 token 并跳转 login。
4. 并发多个 401 时，只允许一个 refresh 请求执行，其余等待。
```

### 8.3 错误处理

统一处理后端 `ApiResult`：

```text
code
msg
data
traceId
```

前端错误提示建议：

```text
ValidationError -> 表单/消息提示
Unauthorized -> 清理会话并跳转登录
Forbidden -> 显示无权限
NotFound -> 显示资源不存在
BusinessError -> 显示业务错误
Conflict -> 显示重复/冲突
```

---

## 9. 类型生成计划

M2-FE 必须避免手写大量 API 类型导致漂移。

建议流程：

```text
1. 使用 artifacts/openapi/wecms-api-v1.json 作为类型来源。
2. 生成 frontend API types。
3. 对生成文件做固定目录管理。
4. quality gate 检查生成产物未过期。
```

如果一期暂时不接类型生成工具，也必须至少维护：

```text
src/api/types/generated.ts
```

并在 M2-FE 验收前与 OpenAPI 对齐。

---

## 10. 前端目录建议

建议结构：

```text
frontend/
  src/
    api/
      auth/
      system/
        users.ts
        roles.ts
        menus.ts
        permissions.ts
        depts.ts
        posts.ts
        dicts.ts
        settings.ts
        logs.ts
        files.ts
      request.ts
      types/
    stores/
      auth.ts
      user.ts
      permission.ts
      menu.ts
    router/
      static-routes.ts
      dynamic-routes.ts
      guards.ts
    views/
      login/
      dashboard/
      system/
        users/
        roles/
        menus/
        permissions/
        depts/
        posts/
        dicts/
        settings/
        logs/
        files/
    components/
      PermissionButton/
      DictSelect/
      UserSelect/
      RoleSelect/
      DeptTreeSelect/
      FileUpload/
      FilePreview/
    utils/
      permission.ts
      token.ts
      sha256.ts
```

---

## 11. 组件规划

### 通用组件

```text
PermissionButton
PermissionDropdown
StatusTag
StatusSwitch
ConfirmDelete
SearchForm
PagedTable
TreeTable
RoleSelect
PermissionTreeSelect
MenuTreeSelect
DeptTreeSelect
PostSelect
DictSelect
FileUpload
FilePreview
```

### 业务组件

```text
UserFormModal
ResetPasswordModal
AssignRolesModal
AssignPostsModal
RoleFormModal
AssignPermissionsModal
AssignMenusModal
MenuFormModal
PermissionFormModal
DeptFormModal
PostFormModal
DictTypeFormModal
DictValueFormModal
SettingEditModal
LogDetailDrawer
FileDetailDrawer
```

---

## 12. M2-FE 开发顺序

### Commit 1：前端基础工程接入

```text
安装依赖
配置环境变量
配置 request client
配置 token store
配置路由 guard
配置基础 layout
```

### Commit 2：Auth 闭环

```text
登录页
login API
refresh API
logout API
me API
token refresh queue
路由鉴权
用户态 store
```

### Commit 3：菜单与权限闭环

```text
permissions store
hasPermission 工具
动态菜单
动态路由
PermissionButton
菜单树渲染
```

### Commit 4：Users 页面

```text
用户列表
用户新增
用户编辑
删除/启用/禁用
重置密码
分配角色
分配岗位
```

### Commit 5：Roles / Permissions 页面

```text
角色管理
权限管理
权限树
角色分配权限
角色分配菜单
locked role UI 保护
builtin permission UI 保护
```

### Commit 6：Menus / Departments 页面

```text
菜单管理
菜单树
部门管理
部门树
防成环选择
```

### Commit 7：Posts / Dicts 页面

```text
岗位管理
字典类型管理
字典值管理
```

### Commit 8：Settings / Logs 页面

```text
系统设置
敏感设置 masking
登录日志
审计日志
安全事件
日志详情
```

### Commit 9：Files 页面

```text
文件列表
上传
sha256 计算
下载
预览
删除
```

### Commit 10：M2-FE 质量门禁与验收

```text
lint
typecheck
build
API contract check
route permission check
no CMS route check
E2E smoke，可选
```

---

## 13. 前端质量门禁计划

建议新增：

```text
scripts/quality-gate-frontend.sh
```

至少包含：

```text
pnpm install --frozen-lockfile
pnpm lint
pnpm typecheck
pnpm build
check-no-cms-frontend.sh
check-api-contract-generated.sh
check-route-permission-coverage.sh
```

### check-no-cms-frontend.sh

确保一期前端不出现：

```text
/api/v1/cms
cms/article
cms/channel
cms/page
cms/tag
```

### check-route-permission-coverage.sh

检查所有 system 页面路由必须有权限配置。

### check-api-contract-generated.sh

检查 OpenAPI 生成类型或手工类型未过期。

---

## 14. 验收标准

M2-FE 完成后必须满足：

```text
1. 可以登录后台。
2. 刷新页面后登录态可恢复。
3. access token 过期后可自动 refresh。
4. refresh 失败后跳转登录。
5. 用户信息、角色、权限可正确加载。
6. 菜单可按权限显示。
7. 无权限按钮不显示。
8. 强行访问无权限路由会被拦截。
9. 用户管理页面可完整 CRUD。
10. 角色管理页面可完整 CRUD 和分配权限/菜单。
11. locked role 前端操作按钮禁用。
12. 菜单管理页面可维护菜单树。
13. 权限管理页面可查看/维护权限。
14. 部门管理页面可维护部门树。
15. 岗位管理页面可维护岗位。
16. 字典管理页面可维护字典类型和值。
17. 系统设置页面不泄露敏感值。
18. 日志页面只读可查询。
19. 文件页面可上传、预览、下载、删除。
20. 前端 build 通过。
21. 前端 lint/typecheck 通过。
22. 不包含 CMS 功能入口。
23. 不调用 /api/v1/cms。
```

---

## 15. 后端支持项

M2-FE 阶段如果发现后端需要调整，只允许做小范围支持性修正。

允许：

```text
1. 修复 OpenAPI 字段不一致。
2. 补充前端必要字段。
3. 优化 /auth/me 的 menus 返回。
4. 修复文件上传 OpenAPI multipart 描述。
5. 修复前端接入发现的接口 bug。
6. 补充质量门禁。
```

不允许：

```text
1. 新增 CMS 表。
2. 新增 CMS API。
3. 新增 CMS 权限。
4. 开发文章/栏目/页面功能。
5. 修改 M1-BE 已验收安全规则。
```

---

## 16. 风险与处理

### 风险 1：动态菜单与 SoybeanAdmin 路由结构不匹配

处理：

```text
先实现静态系统路由 + 权限过滤；
再逐步接入后端菜单树；
保留 component 白名单映射。
```

### 风险 2：OpenAPI 类型生成成本超预期

处理：

```text
M2 初期可手写 API client；
但验收前必须完成类型对齐检查。
```

### 风险 3：登录刷新机制复杂

处理：

```text
优先实现单 refresh 队列；
并发 401 请求等待同一个 refresh Promise。
```

### 风险 4：文件 sha256 前端计算影响性能

处理：

```text
M2 文件限制 10MB；
浏览器端计算可接受；
大文件能力放后续阶段。
```

### 风险 5：权限按钮遗漏

处理：

```text
新增 route/button permission coverage 检查；
页面评审时逐项对照权限矩阵。
```

---

## 17. M2-FE 交付物

M2-FE 应交付：

```text
1. 前端基础工程
2. 登录/登出/刷新/会话恢复
3. 动态菜单/路由
4. 权限控制工具
5. 系统管理页面
6. 文件管理页面
7. 日志查看页面
8. API client
9. API 类型
10. 前端质量门禁脚本
11. M2-FE 验收报告
```

---

## 18. 最终结论

M2-FE 的核心不是继续扩后端功能，而是让 M1-BE 已完成的系统能力形成可操作后台。

最终目标：

```text
一期交付一个可登录、可授权、可管理用户/角色/权限/菜单/部门/岗位/字典/设置/日志/文件的基础后台系统。
```

CMS 功能整体移动到二期，不进入 M2-FE。
