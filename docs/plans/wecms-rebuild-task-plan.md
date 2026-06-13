# WeCMS 前后端开发顺序与接口契约约束补充说明

## 1. 核心原则调整

本项目采用 **Backend First / API Contract First** 开发模式。

前端开发必须后置，只有在后端 API、DTO、OpenAPI 合同、权限码、菜单结构、分页结构、错误结构稳定后，才允许进入前端页面开发。

前端不得根据页面需要自行发明数据结构，也不得绕过后端合同直接修改字段、接口、枚举或权限码。

核心原则：

```text
后端数据结构是唯一真实来源
OpenAPI 是前后端协作合同
Contracts DTO 是接口字段来源
前端 TypeScript 类型必须以后端 OpenAPI 生成或严格同步
前端不得自行增加 / 修改 / 删除接口字段
前端不得自行定义后端不存在的业务状态
前端不得自行模拟后端权限结构作为最终实现
```

---

# 2. 开发版本顺序调整

原计划中的 Vue Admin 开发阶段需要整体后移。

调整后的版本顺序如下：

```text
V0.1 工程骨架
V0.2 Core / Contracts / Abstractions
V0.3 Infrastructure 基础设施
V0.4 Persistence + SqlSugar + AOT 验证
V0.5 Auth 后端 API
V0.6 RBAC 后端 API
V0.7 System 后端 API
V0.8 CMS 后端 API
V0.9 Backend API Contract Freeze
V0.10 Vue Admin 前端开发
V1.0 MVP 稳定版
```

关键变化：

```text
Vue Admin 不再放在 System API 与 CMS API 中间
前端必须等待 Auth / RBAC / System / CMS 后端 API 完成后再开发
V0.9 专门作为后端 API 合同冻结版本
```

---

# 3. 前端开发启动条件

前端开发必须满足以下条件后才允许启动：

```text
后端 Auth API 完成
后端 RBAC API 完成
后端 System API 完成
后端 CMS API 完成
OpenAPI 文档可稳定导出
所有 Request DTO 已确认
所有 Response DTO 已确认
分页结构已确认
错误结构已确认
权限码已确认
菜单结构已确认
接口路径已确认
接口 HTTP Method 已确认
接口鉴权规则已确认
```

如果以上任一项未完成，前端不得进入正式页面开发。

---

# 4. API 合同冻结规则

## 4.1 合同来源

前后端唯一合同来源为：

```text
WeCms.Contracts
OpenAPI JSON
后端接口实际返回结构
```

前端不得以以下内容作为最终合同来源：

```text
页面临时字段
前端 mock 数据
设计稿中的虚拟字段
开发者自行命名的字段
旧系统接口结构
口头约定
```

---

## 4.2 DTO 变更规则

后端 DTO 一旦进入 Contract Freeze，不允许随意变更。

如需变更，必须走以下流程：

```text
1. 提出 API Contract Change Request
2. 说明变更原因
3. 标记影响接口
4. 标记影响前端页面
5. 修改 WeCms.Contracts
6. 更新 OpenAPI
7. 更新后端测试
8. 前端重新生成或同步类型
9. 前后端联调验证
```

禁止：

```text
前端自行新增字段
前端自行删除字段
前端自行重命名字段
前端自行改变字段类型
前端自行改变枚举值
前端自行改变分页结构
前端自行改变错误结构
```

---

# 5. 前端数据类型规则

## 5.1 TypeScript 类型来源

前端 TypeScript 类型必须来自后端合同。

推荐方式：

```text
OpenAPI -> TypeScript Client / Types
```

允许：

```text
根据 OpenAPI 生成 API 类型
根据 OpenAPI 生成请求方法
根据后端 DTO 手动同步类型，但必须严格一致
```

禁止：

```text
页面中随意定义接口返回类型
为了 UI 方便修改后端字段名
把后端不存在的字段写进 API 类型
把 number 改成 string
把 enum 改成任意字符串
把 nullable 字段当成必填字段
```

---

## 5.2 ViewModel 与 DTO 分离

如果前端页面确实需要额外 UI 状态，可以建立 ViewModel，但必须和后端 DTO 分离。

允许：

```ts
type UserListItemDto = {
  id: number
  username: string
  status: string
}

type UserListItemViewModel = UserListItemDto & {
  checked: boolean
  loading: boolean
}
```

要求：

```text
DTO 表示后端接口数据
ViewModel 表示前端 UI 状态
ViewModel 不得反向污染后端 DTO
提交给后端时必须转换回后端 Request DTO
```

禁止：

```text
把 checked / loading / expanded 等 UI 字段提交给后端
要求后端为了前端临时 UI 状态增加字段
直接修改 DTO 来适配页面状态
```

---

# 6. Mock 数据规则

前端正式开发阶段不允许以 mock 数据决定最终结构。

允许使用 mock 的场景：

```text
仅用于页面静态布局预览
仅用于视觉占位
仅在 OpenAPI 已存在的情况下按合同 mock
```

禁止：

```text
根据 mock 反推后端接口
根据 mock 自行增加字段
mock 字段与 OpenAPI 不一致
长期保留 mock 作为数据来源
```

Mock 数据必须满足：

```text
字段名与 OpenAPI 一致
字段类型与 OpenAPI 一致
枚举值与后端一致
分页结构与后端一致
错误结构与后端一致
```

---

# 7. 菜单与权限规则

前端菜单必须以后端返回为准。

前端不得自行维护最终菜单树。

后端负责：

```text
菜单 ID
菜单名称
路由路径
组件标识
图标
排序
父级关系
权限码
是否隐藏
状态
```

前端负责：

```text
渲染菜单
根据后端菜单生成路由
根据权限码控制按钮显示
处理 401 / 403
```

禁止：

```text
前端硬编码最终菜单
前端自行增加权限码
前端自行判断超级管理员逻辑
前端绕过后端权限显示敏感入口
```

---

# 8. 分页结构统一

所有分页接口统一以后端结构为准。

标准结构：

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "total": 100
}
```

前端不得改成：

```json
{
  "list": [],
  "current": 1,
  "size": 20,
  "totalCount": 100
}
```

如 UI 组件字段不同，由前端 Adapter 转换：

```text
后端 PageResult<T>
  -> 前端 TableDataSource
```

不得要求后端为了某个 UI 组件改变全局分页结构。

---

# 9. 错误结构统一

后端错误统一使用 ProblemDetails。

前端必须按 ProblemDetails 处理错误。

标准结构：

```json
{
  "type": "https://wecms/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "traceId": "xxx"
}
```

前端不得自行要求后端改为：

```json
{
  "code": 500,
  "message": "error"
}
```

如果前端需要统一提示，由前端错误适配层处理。

---

# 10. V0.9：Backend API Contract Freeze 版本

## 10.1 版本目标

在前端正式开发前，冻结后端 API 合同。

本版本是前端开发启动门禁。

---

## 10.2 任务清单

### T0.9.1 导出完整 OpenAPI

产物：

```text
artifacts/openapi/wecms-api-v1.json
```

验收：

```text
Auth API 存在
System API 存在
CMS API 存在
所有 Request Schema 存在
所有 Response Schema 存在
ProblemDetails Schema 存在
分页 Schema 存在
```

---

### T0.9.2 检查接口路径

检查：

```text
/api/v1/auth/*
/api/v1/system/*
/api/v1/cms/*
```

要求：

```text
路径命名稳定
HTTP Method 正确
不存在临时接口
不存在 mock 接口
不存在未授权后台接口
```

---

### T0.9.3 检查 DTO 字段

检查：

```text
Auth DTO
User DTO
Role DTO
Menu DTO
Permission DTO
Content DTO
Category DTO
Tag DTO
Media DTO
```

要求：

```text
字段命名稳定
字段类型稳定
nullable 明确
枚举明确
列表结构明确
分页结构明确
```

---

### T0.9.4 检查权限码

检查：

```text
system.user.*
system.role.*
system.menu.*
system.permission.*
system.audit_log.*
system.login_log.*
cms.content.*
cms.category.*
cms.tag.*
cms.media.*
```

要求：

```text
权限码集中定义
权限码已 seed
接口已绑定权限码
前端只读取后端权限码
```

---

### T0.9.5 检查菜单结构

要求后端提供稳定菜单结构：

```text
id
parentId
name
path
component
icon
sort
permissionCode
hidden
enabled
children
```

前端不得自行扩展最终菜单字段。

---

### T0.9.6 生成前端 API 类型

方式：

```text
OpenAPI -> TypeScript types
```

产物：

```text
frontend/admin/src/api/generated/
```

要求：

```text
生成类型不得手改
如需变更，必须回到后端 Contracts 修改
```

---

### T0.9.7 建立 API 合同变更记录

产物：

```text
docs/api-contract-changelog.md
```

内容：

```text
变更日期
变更接口
变更字段
变更原因
影响页面
是否破坏兼容
```

---

## 10.3 V0.9 完成标准

```text
OpenAPI 完整
DTO 稳定
权限码稳定
菜单结构稳定
分页结构稳定
错误结构稳定
前端 API 类型可生成
后端集成测试通过
后端 AOT publish 通过
```

只有 V0.9 通过后，才允许进入 Vue Admin 正式开发。

---

# 11. V0.10：Vue Admin 前端开发版本

## 11.1 版本目标

基于后端已冻结 API 合同开发 Vue 3 管理端。

前端开发不得修改后端数据结构。

---

## 11.2 前端任务清单

### T0.10.1 初始化 Vue Admin 工程

内容：

```text
Vue 3
Vite
TypeScript
Pinia
Vue Router
UI 组件库
```

---

### T0.10.2 接入 OpenAPI 生成类型

要求：

```text
从 artifacts/openapi/wecms-api-v1.json 生成 TypeScript 类型
生成文件不得手动修改
```

---

### T0.10.3 实现 HTTP Client

要求：

```text
严格使用后端接口路径
严格使用后端 Request DTO
严格使用后端 Response DTO
统一 Authorization Header
统一 ProblemDetails 错误处理
```

---

### T0.10.4 实现登录页面

接口来源：

```text
POST /api/v1/auth/login
GET  /api/v1/auth/me
```

不得自定义登录返回结构。

---

### T0.10.5 实现 Token 刷新

接口来源：

```text
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
```

---

### T0.10.6 实现动态菜单

接口来源：

```text
GET /api/v1/auth/me
或后端指定菜单接口
```

规则：

```text
菜单以后端返回为准
前端只负责渲染
```

---

### T0.10.7 实现用户管理页面

接口来源：

```text
GET    /api/v1/system/users
POST   /api/v1/system/users
PUT    /api/v1/system/users/{id}
DELETE /api/v1/system/users/{id}
POST   /api/v1/system/users/{id}/enable
POST   /api/v1/system/users/{id}/disable
```

---

### T0.10.8 实现角色管理页面

接口来源：

```text
GET    /api/v1/system/roles
POST   /api/v1/system/roles
PUT    /api/v1/system/roles/{id}
DELETE /api/v1/system/roles/{id}
POST   /api/v1/system/roles/{id}/permissions
```

---

### T0.10.9 实现菜单管理页面

接口来源：

```text
GET    /api/v1/system/menus
POST   /api/v1/system/menus
PUT    /api/v1/system/menus/{id}
DELETE /api/v1/system/menus/{id}
```

---

### T0.10.10 实现日志页面

接口来源：

```text
GET /api/v1/system/audit-logs
GET /api/v1/system/login-logs
```

---

### T0.10.11 实现 CMS 内容页面

接口来源：

```text
GET    /api/v1/cms/contents
GET    /api/v1/cms/contents/{id}
POST   /api/v1/cms/contents
PUT    /api/v1/cms/contents/{id}
DELETE /api/v1/cms/contents/{id}
POST   /api/v1/cms/contents/{id}/publish
POST   /api/v1/cms/contents/{id}/unpublish
```

---

### T0.10.12 实现 CMS 分类 / 标签 / 媒体页面

接口来源：

```text
/api/v1/cms/categories
/api/v1/cms/tags
/api/v1/cms/media
```

---

## 11.3 V0.10 完成标准

```text
前端所有 API 类型来自 OpenAPI
前端无手写后端 DTO
前端无自定义业务字段污染
前端无 mock 数据决定结构
所有页面使用后端真实接口
pnpm typecheck 通过
pnpm build 通过
前后端联调通过
```

---

# 12. 修订后的 Sprint 划分

## Sprint 1：后端骨架

对应：

```text
V0.1
V0.2
```

---

## Sprint 2：后端基础设施与持久化

对应：

```text
V0.3
V0.4
```

---

## Sprint 3：后端认证与权限

对应：

```text
V0.5
V0.6
```

---

## Sprint 4：后端 System API

对应：

```text
V0.7
```

---

## Sprint 5：后端 CMS API

对应：

```text
V0.8
```

---

## Sprint 6：后端 API 合同冻结

对应：

```text
V0.9
```

---

## Sprint 7：Vue Admin 前端开发

对应：

```text
V0.10
```

---

## Sprint 8：MVP 稳定化

对应：

```text
V1.0
```

---

# 13. 最终红线

本项目正式执行时，必须遵守以下红线：

```text
前端开发后置
后端 API 先行
OpenAPI 先冻结
前端类型以后端为准
前端不得自定义接口结构
前端不得自行增加字段
前端不得自行删除字段
前端不得自行修改字段类型
前端不得自行修改权限码
前端不得自行维护最终菜单结构
前端不得以 mock 数据倒逼后端设计
```

一句话定型：

> 后端定义数据结构，OpenAPI 固化接口合同，前端严格消费合同，不反向污染后端。
