# WeCMS ThinkPHP 系统详细说明文档

> 文档版本：v1.0  
> 分析对象：`WeCMS-Thinkphp-Tailwind-main.zip`  
> 分析日期：2026-06-06  
> 分析方式：静态代码、目录结构、SQL 脚本、模板与中间件扫描。本文档不等同于运行时渗透测试或完整 QA 报告。

---

## 1. 文档目的

本文档用于在 **完整迁移到 ASP.NET Core Minimal APIs + .NET 10 Native AOT + Dapper / Dapper.AOT + SoybeanAdmin** 前，对现有 ThinkPHP CMS 进行系统化梳理，明确：

1. 当前系统的技术栈、目录结构与运行模型。
2. 当前系统已有业务模块与功能边界。
3. 当前认证、权限、菜单、日志、文件、安全、配置、i18n 等模块的实现方式。
4. 当前数据库表结构与迁移映射关系。
5. 当前系统可复用的业务资产与不建议延续的技术债。
6. 新项目重构时应保留、重做、废弃或迁移的内容。

本文档重点服务于“重构迁移设计”，不是单纯的 UI 或代码审计。

---

## 2. 系统定位

当前 WeCMS 是一套基于 **ThinkPHP 8 + 服务端模板 + TailwindCSS 风格后台 UI** 的传统后台 CMS 系统。

### 2.1 当前系统形态

```text
浏览器
  ↓
ThinkPHP 路由 / Controller
  ↓
Middleware：Session / CSRF / AdminGate / I18nOverride
  ↓
Service / Model
  ↓
MySQL
  ↓
View 模板渲染 HTML
```

### 2.2 当前前端形态

```text
服务端模板
+ TailwindCSS CDN
+ jQuery
+ SweetAlert2
+ iframe 后台主框架
+ 少量原生 fetch 封装 wecmsFetch
```

### 2.3 当前系统不是

```text
不是前后端分离 SPA
不是 API-first Headless CMS
不是 Vue / React 后台
不是 Token-first 的开放 API 系统
```

### 2.4 当前系统适合作为迁移参考的部分

| 内容 | 可参考价值 |
|---|---:|
| 用户、角色、菜单、权限模块 | 高 |
| 登录、验证码、失败限制、Session Token | 高 |
| 2FA TOTP 与 Backup Code 设计 | 高 |
| 文件上传、私有文件访问、防路径穿越 | 高 |
| 配置中心、敏感配置加密 | 中高 |
| 操作日志、登录日志 | 中 |
| i18n 数据库覆盖文件生成机制 | 中 |
| WAF / 安全中心 | 中 |
| Tailwind UI 视觉方向 | 中 |

---

## 3. 技术栈与依赖

### 3.1 Composer 依赖

`src/composer.json` 中声明的主要依赖如下：

```json
{
  "php": ">=8.3.0",
  "topthink/framework": "^8.1",
  "topthink/think-orm": "^4.0",
  "topthink/think-filesystem": "^2.0.2",
  "topthink/think-view": "^2.0.0",
  "topthink/think-captcha": "^3.0",
  "topthink/think-queue": "^3.0"
}
```

### 3.2 版本说明问题

项目 README 中存在 PHP 版本描述与 `composer.json` 不完全一致的情况。迁移前应以 `composer.json` 为准，即现有代码至少按 **PHP 8.3+** 评估。

### 3.3 前端依赖现状

当前项目未发现标准前端工程化文件：

```text
package.json
vite.config.*
tailwind.config.*
postcss.config.*
```

当前 TailwindCSS 是通过浏览器端 CDN 加载，而不是构建生成：

```html
<script src="https://cdn.tailwindcss.com"></script>
```

这说明当前 UI 更接近“Tailwind 原型化页面”，不是完整前端工程。

---

## 4. 目录结构说明

### 4.1 顶层结构

```text
WeCMS-Thinkphp-Tailwind-main/
  README.md
  Development_Standards.md
  docs/
  scripts/
  tests/
  demo/
  src/
```

### 4.2 `src` 目录结构

```text
src/
  app/
    controller/
    middleware/
    model/
    service/
    validate/
  config/
  data/
  extend/
  public/
  route/
  view/
  composer.json
```

### 4.3 控制器目录

```text
src/app/controller/
  EnumTypeDict.php
  EnumValueDict.php
  File.php
  I18n.php
  Index.php
  LogManage.php
  Login.php
  Menu.php
  Profile.php
  Role.php
  Security.php
  Setting.php
  TwoFactor.php
  Upload.php
  User.php
```

### 4.4 模型目录

```text
src/app/model/
  EnumTypeDictModel.php
  EnumValueDictModel.php
  FileModel.php
  MenuModel.php
  NodeModel.php
  SettingModel.php
  UserModel.php
  UserTypeModel.php
```

### 4.5 服务目录

```text
src/app/service/
  ApiResponse.php
  AuthService.php
  ConfigService.php
  EnumDictService.php
  FileMaintenanceService.php
  FileUploadService.php
  I18nOverrideBuilder.php
  LogManageService.php
  LogService.php
  PasswordService.php
  SecretCryptoService.php
  SecurityCenterService.php
  TwoFactorService.php
  WafService.php
```

### 4.6 中间件目录

```text
src/app/middleware/
  AdminGate.php
  Csrf.php
  I18nOverride.php
```

### 4.7 视图目录

当前模板大约 31 个，分布在：

```text
src/view/
  enum_type_dict/
  enum_value_dict/
  i18n/
  index/
  logmanage/
  menu/
  public/
  role/
  security/
  setting/
  twofactor/
  user/
  login.html
```

---

## 5. 应用启动与中间件模型

### 5.1 全局中间件

`src/app/middleware.php` 注册了全局中间件：

```php
\think\middleware\LoadLangPack::class,
\app\middleware\I18nOverride::class,
\think\middleware\SessionInit::class,
\app\middleware\Csrf::class,
```

说明：

1. 多语言包与数据库覆盖语言优先加载。
2. Session 是当前认证体系核心。
3. CSRF 保护全局启用，但只保护写方法。

### 5.2 Controller 级 AdminGate

大部分后台 Controller 继承 `BaseController`，由 `BaseController` 应用：

```php
protected $middleware = [\app\middleware\AdminGate::class];
```

`Login` 控制器不继承 `BaseController`，因此不受 `AdminGate` 保护。

### 5.3 现有请求保护链

```text
请求进入
  ↓
LoadLangPack
  ↓
I18nOverride
  ↓
SessionInit
  ↓
Csrf：POST/PUT/PATCH/DELETE token 检查
  ↓
AdminGate：WAF、配置、Session、Token、2FA、权限检查
  ↓
Controller Action
```

### 5.4 迁移含义

新系统不应直接延续 Session + 服务端模板模式，而应转换为：

```text
JWT / Cookie Auth
+ Minimal API Authorization
+ Permission Endpoint Filter
+ Rate Limiting
+ Audit Middleware
+ Typed DTO Validation
```

---

## 6. 路由与接口现状

### 6.1 显式路由

`src/route/app.php` 主要显式定义了登录与 2FA 相关路由，例如：

```text
GET  login/index
POST login/doLogin
GET  login/twoFactor
POST login/twoFactorVerify
GET  login/twoFactorSetup
POST login/twoFactorSetupInit
POST login/twoFactorSetupVerify
GET  login/twoFactorQr
GET  login/checkVerify
POST login/logOut
POST i18n/switch
GET  twoFactor/index
GET  twoFactor/qr
POST twoFactor/setup
POST twoFactor/enable
POST twoFactor/sessionverify
POST twoFactor/disable
POST twoFactor/reset
```

其他后台模块大多依赖 ThinkPHP 默认控制器路由。

### 6.2 Controller 方法清单

| Controller | 方法 |
|---|---|
| `EnumTypeDict` | `index`, `list`, `add`, `edit`, `del` |
| `EnumValueDict` | `index`, `list`, `add`, `edit`, `del`, `state` |
| `File` | `view`, `avatar`, `uploadimgs` |
| `I18n` | `index`, `list`, `save`, `switch`, `delete` |
| `Index` | `index`, `noauth`, `home`, `clear` |
| `LogManage` | `index`, `loginList`, `operate`, `operateList` |
| `Login` | `index`, `doLogin`, `twoFactor`, `twoFactorVerify`, `twoFactorSetup`, `twoFactorSetupInit`, `twoFactorQr`, `twoFactorSetupVerify`, `checkVerify`, `logOut` |
| `Menu` | `index`, `list`, `add`, `subadd`, `edit`, `del`, `ruleOrderBy`, `state` |
| `Profile` | `index`, `password` |
| `Role` | `index`, `add`, `edit`, `roleEdit`, `del`, `state`, `giveAccess`, `permissions` |
| `Security` | `index`, `status`, `list`, `unban`, `unbanBatch` |
| `Setting` | `index`, `save` |
| `TwoFactor` | `index`, `qr`, `setup`, `enable`, `sessionverify`, `disable`, `reset` |
| `Upload` | `uploadface` |
| `User` | `index`, `list`, `add`, `edit`, `del`, `state` |

### 6.3 写操作 Method 约束现状

部分写操作已经显式检查 POST，例如：

```text
Login::doLogin
Login::twoFactorVerify
Login::logOut
User::del
User::state
Role::del
Role::state
Role::giveAccess
Security::unban
Security::unbanBatch
TwoFactor::setup / enable / disable / reset
EnumTypeDict::del
EnumValueDict::del / state
```

但也有部分写操作未在方法内部强制 POST，或依赖前端 Ajax 行为，例如：

```text
I18n::save
I18n::delete
Menu::add
Menu::subadd
Menu::del
Menu::ruleOrderBy
Menu::state
Setting::save
Upload::uploadface
User::add
User::edit
Role::add
Role::edit
Role::roleEdit
```

迁移时应全部改为明确的 HTTP Method：

```text
GET    查询
POST   新增 / 动作
PUT    整体更新
PATCH  状态或部分更新
DELETE 删除
```

---

## 7. 认证系统说明

### 7.1 登录流程

当前登录核心位于：

```text
src/app/controller/Login.php
src/app/service/AuthService.php
src/app/service/PasswordService.php
```

典型流程：

```text
1. 用户提交用户名、密码、验证码。
2. AuthService 校验配置、验证码、失败次数。
3. 查询 think_admin 用户。
4. PasswordService::verify 校验 password_hash。
5. 校验用户状态。
6. 如果启用 2FA，则写入 twofa_pending_admin_id 等临时 Session。
7. 若不需要 2FA，生成 token。
8. token 写入 Session 与 think_admin.token。
9. 更新登录时间、IP、登录次数。
10. 进入后台。
```

### 7.2 密码模型

`PasswordService` 使用 PHP 标准函数：

```php
password_hash()
password_verify()
password_needs_rehash()
```

迁移时必须支持旧 PHP bcrypt hash。常见旧 hash 形态包括：

```text
$2y$...
$2a$...
$2b$...
```

建议新系统采用版本化密码格式：

```text
wecms.v1.pbkdf2.sha256.<iterations>.<salt>.<hash>
```

迁移策略：

```text
旧用户第一次登录：
  1. 用 LegacyPhpBcryptVerifier 验证旧 hash。
  2. 验证成功后立即改写为新 .NET AOT 友好的 PBKDF2 格式。
  3. 记录 password_migrated_at。
```

### 7.3 Token 模型

当前登录后会生成随机 token 并保存：

```text
Session：admin uid / username / token
DB：think_admin.token / token_expire_at
```

AdminGate 每次请求会比对 Session token 与数据库 token。

优点：

1. 可以通过更新数据库 token 强制下线。
2. 可以设置 token 过期时间。
3. 可避免旧 Session 长期有效。

不足：

1. 当前属于服务端 Session 模式，不适合前后端分离。
2. logout 逻辑建议同时清空 DB token。
3. 多设备登录模型不清晰。

新系统建议：

```text
Access Token 短有效期
+ Refresh Token Rotation
+ Refresh Token Hash 入库
+ security_stamp
+ permission_version
```

---

## 8. 双因素认证 2FA 模块说明

### 8.1 相关代码

```text
src/app/controller/TwoFactor.php
src/app/controller/Login.php
src/app/service/TwoFactorService.php
src/app/service/AuthService.php
```

### 8.2 当前能力

| 能力 | 状态 |
|---|---|
| TOTP Secret 生成 | 已实现 |
| otpauth URI | 已实现 |
| 二维码渲染 | 已实现 |
| 6 位 TOTP | 已实现 |
| 30 秒时间片 | 已实现 |
| 时间窗口容错 | 已实现 |
| Backup Code | 已实现 |
| Backup Code hash 保存 | 已实现 |
| Secret 加密存储 | 已实现 |
| Secret 旧格式兼容 | 已实现 |
| TOTP 重放保护 | 已实现 |
| 登录期间 2FA Pending Session | 已实现 |
| 管理员重置 2FA | 已实现 |

### 8.3 数据字段

`think_admin` 中相关字段：

```text
twofa_enabled
twofa_secret
twofa_secret_version
twofa_confirmed_at
twofa_backup_codes
twofa_last_totp_ts
```

### 8.4 迁移建议

2FA secret 是高敏感数据。建议新系统不要直接迁移旧 secret，而采用：

```text
1. 迁移用户基础资料。
2. 标记 two_factor_rebind_required = true。
3. 用户第一次登录新系统后，要求重新绑定 2FA。
4. 旧 backup code 全部失效。
```

只有在强业务要求“用户无感迁移 2FA”的情况下，才考虑解密旧 secret 并迁入新系统。该方式需要掌握旧 `auth_key` / `SECRET_KEY`，安全风险和测试成本较高。

### 8.5 发现的边界风险

TOTP 重放保护中存在一个需要重点验证的边界：

```text
如果 twofa_last_totp_ts 为 NULL，SQL 条件 <> usedTimeSlice 可能无法命中。
```

新系统应使用：

```sql
WHERE twofa_last_totp_ts IS NULL
   OR twofa_last_totp_ts <> @UsedTimeSlice
```

或者将默认值设为 `0`。

---

## 9. 权限与菜单系统说明

### 9.1 当前权限模型

当前采用 ThinkPHP Auth 风格 RBAC：

```text
think_admin
  ↓
think_auth_group_access
  ↓
think_auth_group.rules
  ↓
think_auth_rule
```

同时存在：

```text
think_admin.groupid
think_auth_group_access
```

这表示系统中同时存在单角色字段和多角色关系表。

### 9.2 当前核心表

| 表 | 说明 |
|---|---|
| `think_admin` | 后台用户 |
| `think_auth_group` | 角色 / 用户组 |
| `think_auth_group_access` | 用户角色关系 |
| `think_auth_rule` | 菜单与权限规则 |

### 9.3 `think_auth_group.rules` 问题

角色权限以 CSV 字符串保存：

```text
1,2,3,4,5
```

问题：

| 问题 | 影响 |
|---|---|
| 数据库无法建立外键约束 | 权限 ID 可失真 |
| 难以查询角色与权限关系 | SQL 分析困难 |
| 难以审计权限变更 | 不能准确比较增删项 |
| 不利于缓存失效 | 无法精确影响用户 |
| 不适合 API 权限模型 | 无权限码语义 |

新系统必须改为关系表。

### 9.4 `think_auth_rule` 菜单权限混用

当前 `think_auth_rule` 同时承载：

```text
菜单目录
页面菜单
按钮权限 / 操作权限
接口规则
```

字段包括：

```text
id
name
title
type
status
css
condition
pid
sort
lang_code
```

迁移时建议拆成：

```text
sys_menu
sys_permission
```

其中：

```text
菜单控制显示
权限控制接口和动作
按钮可作为 menu type = button，也可绑定 permission_code
```

### 9.5 当前 Auth 类

核心权限实现位于：

```text
src/extend/com/Auth.php
```

功能：

1. 根据用户 ID 查询用户角色。
2. 合并角色 `rules`。
3. 查询 `auth_rule`。
4. 对比当前 URL 与 rule name。
5. 支持 `condition` 条件表达式。

迁移时不建议延续动态 URL 匹配方式，应改为明确权限码：

```text
sys:user:list
sys:user:create
sys:user:update
sys:user:delete
```

---

## 10. 用户管理模块说明

### 10.1 相关代码

```text
src/app/controller/User.php
src/app/model/UserModel.php
src/app/validate/UserValidate.php
```

### 10.2 当前功能

| 功能 | 状态 |
|---|---|
| 用户列表 | 有 |
| 用户新增 | 有 |
| 用户编辑 | 有 |
| 用户删除 | 有 |
| 用户启用 / 禁用 | 有 |
| 密码 hash | 有 |
| 头像上传 | 有 |
| 角色分配 | 有 |
| 操作日志 | 有 |

### 10.3 当前问题

#### 10.3.1 字段白名单不足

`UserModel` 中新增和编辑存在保存请求参数的模式，容易形成 Mass Assignment 风险。

迁移时禁止使用“不加白名单的 DTO 到实体映射”。

#### 10.3.2 Session key 不统一

部分日志中使用：

```php
Session::get('uid')
session('uid')
```

但登录主流程保存的是：

```text
adminuid
username
```

新系统应封装当前用户上下文：

```text
ICurrentUser.UserId
ICurrentUser.Username
ICurrentUser.Roles
ICurrentUser.PermissionVersion
ICurrentUser.SecurityStamp
```

#### 10.3.3 删除用户为硬删除

当前删除更接近物理删除。新系统建议：

```text
soft delete
+ deleted_at
+ deleted_by
+ 审计日志
```

#### 10.3.4 系统账号保护不足

新系统必须内置保护：

```text
不能删除自己
不能禁用自己
不能删除超级管理员
不能禁用最后一个可登录管理员
不能删除最后一个超级管理员
重置 2FA / 密码必须记录高风险审计日志
```

---

## 11. 角色管理模块说明

### 11.1 相关代码

```text
src/app/controller/Role.php
src/app/model/UserTypeModel.php
```

### 11.2 当前功能

| 功能 | 状态 |
|---|---|
| 角色列表 | 有 |
| 角色新增 | 有 |
| 角色编辑 | 有 |
| 角色删除 | 有 |
| 角色启用 / 禁用 | 有 |
| 分配权限 | 有 |
| 权限树展示 | 有 |

### 11.3 当前问题

1. 角色权限以 CSV 保存。
2. 权限保存需要更严格地过滤、去重、排序和验证。
3. 修改角色权限后，在线用户权限可能不立即刷新。
4. 角色模型与用户 `groupid` 字段存在单角色/多角色混用。

### 11.4 迁移要求

新系统统一使用多角色模型：

```text
sys_user
sys_role
sys_user_role
```

角色权限拆为：

```text
sys_role_menu
sys_role_permission
```

角色权限变化后：

```text
1. 查找受影响用户。
2. 增加这些用户 permission_version。
3. 清理权限缓存。
4. 前端下次请求 /api/auth/me 或刷新 token 时获取最新权限。
```

---

## 12. 菜单管理模块说明

### 12.1 相关代码

```text
src/app/controller/Menu.php
src/app/model/MenuModel.php
src/app/model/NodeModel.php
```

### 12.2 当前功能

| 功能 | 状态 |
|---|---|
| 菜单列表 | 有 |
| 菜单新增 | 有 |
| 子菜单新增 | 有 |
| 菜单编辑 | 有 |
| 菜单删除 | 有 |
| 状态切换 | 有 |
| 排序 | 有 |
| 权限树复用 | 有 |

### 12.3 当前表

菜单依赖：

```text
think_auth_rule
```

### 12.4 迁移要求

新系统 `sys_menu` 建议字段：

```text
id
parent_id
type              -- catalog/menu/button/link
name              -- Soybean route name
path              -- 前端路由路径
component         -- Soybean route component key
title
i18n_key
icon
sort
hidden
keep_alive
external_url
permission_code
status
created_at
updated_at
deleted_at
```

`sys_menu` 不再直接代表接口权限。接口权限放在 `sys_permission`。

---

## 13. 文件上传与文件访问模块说明

### 13.1 相关代码

```text
src/app/controller/File.php
src/app/controller/Upload.php
src/app/model/FileModel.php
src/app/service/FileUploadService.php
src/app/service/FileMaintenanceService.php
```

### 13.2 当前优点

| 设计 | 说明 |
|---|---|
| 文件名随机化 | 降低覆盖与猜测风险 |
| 目录清洗 | 防止危险路径 |
| 扩展名限制 | 有基础白名单 |
| MIME 检查 | 有基础校验 |
| 图片类型检查 | 有 `getimagesize` 类校验 |
| 危险扩展名拦截 | 有 |
| 私有文件访问 | 不直接公开所有文件 |
| realpath 校验 | 防路径穿越 |
| nosniff | 响应头有安全增强 |

### 13.3 当前问题

1. 存储 disk 名称存在硬编码痕迹。
2. 配置中的 `storage_driver` 与实际上传服务的 disk 选择需要统一。
3. 文件在线预览策略需要按 MIME 更细化。
4. 图片建议重编码，降低 polyglot 风险。
5. 上传入口需要规则化，不应共用过宽默认配置。

### 13.4 迁移要求

新系统设计：

```text
IFileStorage
  LocalPrivateFileStorage
  S3CompatibleStorage -- 可选

FileUploadPolicy
  AvatarPolicy
  ImagePolicy
  DocumentPolicy
```

文件访问必须走授权接口：

```text
GET /api/system/files/{id}/download
GET /api/system/files/{id}/preview
GET /api/system/files/{id}/avatar
```

---

## 14. 配置管理模块说明

### 14.1 相关代码

```text
src/app/controller/Setting.php
src/app/model/SettingModel.php
src/app/service/ConfigService.php
src/app/service/SecretCryptoService.php
```

### 14.2 当前能力

| 能力 | 状态 |
|---|---|
| 配置白名单 | 有 |
| 敏感配置加密 | 有 |
| SMTP 密码隐藏 | 有 |
| 配置缓存 | 有 |
| 配置保存后刷新缓存 | 有 |
| 登录安全配置 | 有 |
| 上传配置 | 有 |
| IP 白名单配置 | 有 |

### 14.3 当前配置项示例

```text
web_site_title
web_site_description
admin_email
web_site_copy
home_page
home_page_name
list_rows
version
close
auth_key
reset_pw
admin_allow_ip
verify_type
login_retry_limit
session_timeout
login_lock_minutes
two_factor_auth
upload_allowed_types
upload_max_size_mb
storage_driver
smtp_host
smtp_port
smtp_user
smtp_pass
```

### 14.4 当前问题

`Setting::save` 支持 IP / CIDR / 逗号 / 空白分隔校验，但 `AdminGate` 中的后台 IP 白名单读取使用 `#` 分割并做精确匹配，两者存在格式不一致风险。

新系统应建立统一服务：

```text
IIpRuleMatcher
  Supports IPv4
  Supports IPv6
  Supports CIDR
  Supports comma / newline separators
```

---

## 15. 日志模块说明

### 15.1 相关代码

```text
src/app/controller/LogManage.php
src/app/service/LogManageService.php
src/app/service/LogService.php
```

### 15.2 当前日志表

```text
think_log
think_operate_log
```

### 15.3 当前能力

| 日志 | 说明 |
|---|---|
| 登录日志 | 记录登录用户、IP、状态、描述 |
| 操作日志 | 记录操作人、描述、IP、状态、类型 |
| 日志查看 | 后台列表页 |

### 15.4 新系统设计

建议拆为：

```text
sys_login_log
sys_audit_log
sys_security_event
```

其中 `sys_audit_log` 字段建议包括：

```text
id
trace_id
user_id
username
permission_code
module
action
http_method
path
query_string
ip
user_agent
request_body_summary
status_code
elapsed_ms
result
error_message
created_at
```

敏感字段必须脱敏：

```text
password
token
refreshToken
secret
smtp_pass
twofa_secret
backup_code
```

---

## 16. i18n 国际化模块说明

### 16.1 相关代码

```text
src/app/controller/I18n.php
src/app/service/I18nOverrideBuilder.php
src/app/middleware/I18nOverride.php
```

### 16.2 当前能力

1. 支持数据库维护多语言文案。
2. 支持语言切换接口。
3. 支持生成 runtime override 文件。
4. 支持 `zh-cn`、`en-us`、`ms-my` 等 locale。

### 16.3 迁移要求

新系统建议保留表：

```text
sys_i18n_message
```

字段：

```text
id
locale
message_key
message_value
module
remark
created_at
updated_at
```

对 SoybeanAdmin 前端：

```text
1. 后端返回 route.meta.i18nKey。
2. 前端使用 SoybeanAdmin 的 i18n 体系。
3. 系统级菜单可使用前端静态语言包。
4. 业务动态文案可通过 API 拉取并缓存。
```

---

## 17. 字典模块说明

后续 SQL dump 中存在：

```text
think_enum_type_dict
think_enum_value_dict
```

对应 Controller：

```text
EnumTypeDict
EnumValueDict
```

迁移到新系统建议改名为：

```text
sys_dict_type
sys_dict_value
```

字段建议：

```text
sys_dict_type:
  id
  code
  name
  description
  status
  sort
  created_at
  updated_at

sys_dict_value:
  id
  dict_type_id
  code
  name
  value
  sort
  status
  description
  created_at
  updated_at
```

---

## 18. 安全模块说明

### 18.1 相关代码

```text
src/app/controller/Security.php
src/app/service/SecurityCenterService.php
src/app/service/WafService.php
src/app/middleware/AdminGate.php
src/app/middleware/Csrf.php
```

### 18.2 当前安全能力

| 能力 | 状态 |
|---|---|
| CSRF | 有 |
| WAF 特征检测 | 有 |
| 登录失败限制 | 有 |
| Session 超时 | 有 |
| DB token 校验 | 有 |
| IP 白名单 | 有，但解析不一致 |
| 安全中心封禁列表 | 有 |
| 2FA | 有 |
| 敏感配置加密 | 有 |
| 文件上传检查 | 有 |

### 18.3 新系统安全原则

新系统不建议复制 PHP WAF 作为主要安全边界。应优先依赖：

```text
1. 明确路由与权限码。
2. 强类型 DTO。
3. 严格参数验证。
4. SQL 参数绑定。
5. 文件上传白名单。
6. Rate Limiting。
7. 审计日志。
8. IP / CIDR 规则。
9. 安全响应头。
10. 最小权限部署。
```

WAF 可作为补充的 `SecurityEventClassifier`，用于记录和限流，不应替代业务安全。

---

## 19. 数据库表结构概览

### 19.1 主 SQL 中的表

`src/data/wecms20260125.sql` 中主要包含：

```text
think_admin
think_auth_group
think_auth_group_access
think_auth_rule
think_config
think_file
think_i18n_message
think_log
think_mail_notify
think_msg_sender
think_notice
think_operate_log
```

后续 zip dump 中还包含：

```text
think_enum_type_dict
think_enum_value_dict
```

### 19.2 表说明

| 表名 | 说明 | 新系统建议表 |
|---|---|---|
| `think_admin` | 后台用户 | `sys_user` |
| `think_auth_group` | 角色 | `sys_role` |
| `think_auth_group_access` | 用户角色关系 | `sys_user_role` |
| `think_auth_rule` | 菜单与权限规则 | `sys_menu` + `sys_permission` |
| `think_config` | 系统配置 | `sys_setting` |
| `think_file` | 文件元数据 | `sys_file` |
| `think_i18n_message` | 国际化文案 | `sys_i18n_message` |
| `think_log` | 登录日志 | `sys_login_log` |
| `think_operate_log` | 操作日志 | `sys_audit_log` |
| `think_enum_type_dict` | 字典类型 | `sys_dict_type` |
| `think_enum_value_dict` | 字典值 | `sys_dict_value` |
| `think_notice` | 通知 | `sys_notice`，可后续迁移 |
| `think_mail_notify` | 邮件通知 | `sys_mail_notification`，可后续迁移 |
| `think_msg_sender` | 消息发送者 | `sys_message_sender`，可后续迁移 |

### 19.3 敏感数据问题

后续 SQL dump 中发现存在接近真实业务数据的字段：

```text
管理员账号
密码 hash
token
token_expire_at
twofa_secret
twofa_backup_codes
auth_key
SMTP 配置密文
邮箱 / 手机号
```

迁移要求：

```text
1. 备份文件只在受控环境使用。
2. 不允许作为公开 seed 数据。
3. 新项目只提供 schema.sql 与 seed-demo.sql。
4. seed-demo.sql 不包含 token、2FA secret、SMTP 密码、auth_key。
5. 生产迁移脚本与演示种子脚本完全隔离。
```

---

## 20. 现有 UI 架构说明

### 20.1 当前 UI 入口

```text
src/view/public/head.html
src/view/index/index.html
```

`head.html` 统一引入：

```text
jQuery
SweetAlert2
Tailwind CDN
Google Fonts / Material Symbols
CSRF token
wecmsFetch
滚动条样式
图标样式
```

`index/index.html` 是后台 Shell，内部通过：

```html
<iframe name="content_frame"></iframe>
```

加载业务页面。

### 20.2 当前 UI 问题

| 问题 | 迁移处理 |
|---|---|
| Tailwind CDN | 新项目改用 SoybeanAdmin 前端工程 |
| iframe Shell | 新项目改用 Vue Router |
| 服务端模板 | 新项目改用 SPA + API |
| jQuery | 新项目移除 |
| 内联 JS | 新项目改为模块化 TypeScript |
| 菜单服务端渲染 | 新项目改为动态路由 API |
| 按钮权限散落 | 新项目改为权限指令 / 组件封装 |

### 20.3 可复用的 UI 经验

1. 当前系统已经形成后台常见页面结构：标题、筛选、表格、分页、弹窗、操作按钮。
2. 当前菜单图标已偏向 Material Symbols；新项目可迁移到 Iconify 图标名。
3. 当前 Tailwind 视觉风格可作为 SoybeanAdmin 主题定制参考。

---

## 21. 旧权限路径到新权限码建议映射

| 旧 rule name | 新权限码 | 说明 |
|---|---|---|
| `/user/index` | `sys:user:page` | 用户管理页面 |
| `/user/list` | `sys:user:list` | 用户列表 |
| `/user/add` | `sys:user:create` | 新增用户 |
| `/user/edit` | `sys:user:update` | 编辑用户 |
| `/user/del` | `sys:user:delete` | 删除用户 |
| `/user/state` | `sys:user:change-status` | 启用禁用用户 |
| `/role/index` | `sys:role:page` | 角色页面 |
| `/role/add` | `sys:role:create` | 新增角色 |
| `/role/edit` | `sys:role:update` | 编辑角色 |
| `/role/del` | `sys:role:delete` | 删除角色 |
| `/role/state` | `sys:role:change-status` | 启用禁用角色 |
| `/role/giveAccess` | `sys:role:assign-permission` | 分配权限 |
| `/role/permissions` | `sys:role:permission-page` | 权限页 |
| `/menu/index` | `sys:menu:page` | 菜单页面 |
| `/menu/list` | `sys:menu:list` | 菜单列表 |
| `/menu/add` | `sys:menu:create` | 新增菜单 |
| `/menu/subadd` | `sys:menu:create-child` | 新增子菜单 |
| `/menu/edit` | `sys:menu:update` | 编辑菜单 |
| `/menu/del` | `sys:menu:delete` | 删除菜单 |
| `/menu/state` | `sys:menu:change-status` | 启用禁用菜单 |
| `/menu/ruleOrderBy` | `sys:menu:sort` | 菜单排序 |
| `/setting/index` | `sys:setting:view` | 查看设置 |
| `/setting/save` | `sys:setting:update` | 保存设置 |
| `/i18n/index` | `sys:i18n:page` | 多语言页面 |
| `/i18n/list` | `sys:i18n:list` | 文案列表 |
| `/i18n/save` | `sys:i18n:save` | 保存文案 |
| `/i18n/delete` | `sys:i18n:delete` | 删除文案 |
| `/i18n/switch` | `account:i18n:switch` | 切换语言 |
| `/logManage/index` | `sys:log:login-page` | 登录日志页 |
| `/logManage/loginList` | `sys:log:login-list` | 登录日志列表 |
| `/logManage/operate` | `sys:log:audit-page` | 操作日志页 |
| `/logManage/operateList` | `sys:log:audit-list` | 操作日志列表 |
| `/security/index` | `sys:security:page` | 安全中心 |
| `/security/status` | `sys:security:status` | 安全状态 |
| `/security/list` | `sys:security:list` | 安全列表 |
| `/security/unban` | `sys:security:unban` | 解封 IP |
| `/security/unbanBatch` | `sys:security:unban-batch` | 批量解封 |
| `/twoFactor/index` | `account:2fa:page` | 2FA 页面 |
| `/twoFactor/setup` | `account:2fa:setup` | 2FA 设置 |
| `/twoFactor/enable` | `account:2fa:enable` | 启用 2FA |
| `/twoFactor/disable` | `account:2fa:disable` | 禁用 2FA |
| `/twoFactor/reset` | `sys:user:reset-2fa` | 管理员重置 2FA |
| `/file/view` | `sys:file:view` | 查看文件 |
| `/file/avatar` | `sys:file:view-avatar` | 查看头像 |
| `/upload/uploadface` | `account:avatar:upload` | 上传头像 |
| `/enumTypeDict/index` | `sys:dict-type:page` | 字典类型页面 |
| `/enumTypeDict/list` | `sys:dict-type:list` | 字典类型列表 |
| `/enumTypeDict/add` | `sys:dict-type:create` | 新增字典类型 |
| `/enumTypeDict/edit` | `sys:dict-type:update` | 编辑字典类型 |
| `/enumTypeDict/del` | `sys:dict-type:delete` | 删除字典类型 |
| `/enumValueDict/index` | `sys:dict-value:page` | 字典值页面 |
| `/enumValueDict/list` | `sys:dict-value:list` | 字典值列表 |
| `/enumValueDict/add` | `sys:dict-value:create` | 新增字典值 |
| `/enumValueDict/edit` | `sys:dict-value:update` | 编辑字典值 |
| `/enumValueDict/del` | `sys:dict-value:delete` | 删除字典值 |
| `/enumValueDict/state` | `sys:dict-value:change-status` | 字典值状态 |

---

## 22. 迁移资产清单

### 22.1 必须迁移

```text
用户
角色
用户角色关系
菜单
权限
角色权限
系统配置
文件元数据
登录日志
操作日志
i18n 文案
字典类型和值
```

### 22.2 不直接迁移

```text
Session
token
token_expire_at
2FA secret
2FA backup codes
旧 WAF 内存状态
runtime 缓存
验证码状态
```

### 22.3 可选迁移

```text
notice
mail_notify
msg_sender
```

若当前业务未使用，可放入二期。

---

## 23. 当前系统主要技术债总结

| 编号 | 技术债 | 严重度 | 新系统处理 |
|---|---|---:|---|
| T01 | 服务端模板 + iframe 架构 | 高 | Vue Router SPA |
| T02 | Tailwind CDN | 中 | SoybeanAdmin 工程化前端 |
| T03 | jQuery 与内联 JS | 中 | TypeScript 模块化 |
| T04 | 权限 CSV 存储 | 高 | 关系表 |
| T05 | 菜单权限混表 | 高 | `sys_menu` + `sys_permission` |
| T06 | 单角色/多角色混用 | 高 | 统一多角色 |
| T07 | 写操作 Method 约束不统一 | 高 | Minimal API 显式 Method |
| T08 | 字段白名单不足 | 高 | DTO + 手工映射 |
| T09 | Session key 不统一 | 中 | `ICurrentUser` |
| T10 | SQL dump 包含敏感数据 | 高 | 清理与脱敏 |
| T11 | 配置 IP 白名单解析不一致 | 中 | `IIpRuleMatcher` |
| T12 | logout 不彻底 | 中 | Refresh Token Revoke |
| T13 | 文件存储 disk 硬编码 | 中 | `IFileStorage` |
| T14 | 2FA NULL 边界 | 中 | 数据库默认值或条件修正 |

---

## 24. 重构结论

当前 ThinkPHP 系统应作为新系统的 **业务蓝本与迁移源**，而不是作为目标架构继续演进。

新系统应保留：

```text
用户管理
角色管理
菜单管理
权限管理
登录安全策略
2FA 业务理念
文件私有访问策略
系统配置项
日志审计理念
i18n / 字典模块
安全中心思路
```

新系统应重做：

```text
认证方式
权限模型
菜单路由模型
API 契约
数据库关系模型
前端架构
文件存储抽象
审计中间件
配置加密与密钥管理
迁移脚本与种子数据
```

不建议保留：

```text
ThinkPHP 默认路由权限匹配
服务端模板后台
iframe 主框架
jQuery 页面交互
CSV 权限字段
Session token 认证模型
未脱敏 SQL dump
```
