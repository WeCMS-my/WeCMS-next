# WeCMS Next 一期后建议补齐清单详细开发修复计划书 v1.0

## 1. 文档定位

本文档用于指导 WeCMS Next 一期工程收口后的补齐、修复与安全增强开发。

一期工程已经完成基础系统后台与前端管理端主闭环。本文档不重新打开一期范围，不把 CMS 内容管理能力回流到一期。

本计划聚焦：

1. 补齐旧 ThinkPHP 系统中已经存在、但新系统一期尚未完全覆盖的系统能力。
2. 修复一期后发现的文档、ADR、功能等价性和安全 hardening 问题。
3. 将旧系统 AdminGate / CSRF 中有价值的安全控制拆解并落地到 WeCMS Next 的新架构中。
4. 在进入 CMS 二期前，优先消除基础系统后台的安全能力缺口。
5. 保持当前技术路线不变：.NET 10、ASP.NET Core Minimal APIs、SqlSugar、MySQL、Vue3 管理端。
6. 保持架构边界不变：数据库/ORM/连接器只能在 `WeCms.Data.SqlSugar` 与 `WeCms.Modules.*.SqlSugar` 边界内，业务模块只能依赖接口和 Shared 抽象。

---

## 2. 总体边界

### 2.1 本计划包含

| 范围                                             | 是否包含 |
| ---------------------------------------------- | ---: |
| 2FA 双因素认证补齐                                    |    是 |
| 个人中心 / 修改密码 / 头像能力补齐                           |    是 |
| 安全中心封禁、解封、批量解封能力补齐                             |    是 |
| AdminGate / CSRF 安全职责拆解与新系统落地                  |    是 |
| Origin / CSRF 防护策略                             |    是 |
| IP 规则统一匹配服务                                    |    是 |
| SecurityEventClassifier / Rate Limiting / 封禁联动 |    是 |
| PermissionVersion 权限变更刷新                       |    是 |
| 安全响应头与 CSP hardening                           |    是 |
| README / ADR / 一期状态文档修正                        |    是 |
| i18n 管理能力补齐                                    |    是 |
| 菜单排序能力补齐                                       |    是 |
| 字典状态切换补齐                                       |    是 |
| 设置敏感配置与 IP 规则 hardening                        |    是 |
| 文件上传策略分层与头像专用能力增强                              |    是 |
| 后端 API、前端页面、测试、质量门禁                            |    是 |

### 2.2 本计划不包含

| 范围                                      | 原因                                      |
| --------------------------------------- | --------------------------------------- |
| CMS 栏目、文章、页面、媒体、标签、SEO                  | 已整体进入二期                                 |
| AI 模块                                   | 一期明确不做                                  |
| 旧 ThinkPHP 数据迁移                         | 当前决策为从 0 初始化，不迁移旧数据                     |
| 旧密码 hash 兼容                             | 当前决策为不兼容旧密码                             |
| 旧 Session / token runtime compatibility | 当前决策为不做兼容模式                             |
| 直接复制旧 AdminGate                         | 新系统必须拆成认证、授权、权限码、限流、IP、2FA、审计、安全事件等独立组件 |
| 直接复制旧 PHP WAF                           | WAF 只能作为事件分类和限流辅助，不能作为主要业务安全边界          |
| 运行时 DLL 插件                              | 当前架构不采用                                 |
| 大规模 UI 重构                               | 当前前端以补齐功能为主，不做视觉大改                      |

---

## 3. 总体设计原则

### 3.1 不复制旧系统 AdminGate

旧系统 AdminGate 是后台安全总闸门，集中承担：

```text
WAF
配置读取
Session 检查
DB token 校验
2FA 检查
权限检查
IP 白名单
安全封禁
```

新系统不应新增一个等价的 `AdminGateMiddleware` 来复刻旧逻辑，而应拆分为：

| 旧系统职责               | WeCMS Next 落地组件                                    |
| ------------------- | -------------------------------------------------- |
| Session 登录检查        | ASP.NET Core Authentication                        |
| DB token 校验         | Refresh Token Repository + Token Family Revocation |
| URL 动态权限匹配          | RequirePermission + PermissionEndpointFilter       |
| 2FA pending session | Auth Challenge + TwoFactorService                  |
| WAF 特征检测            | SecurityEventClassifier                            |
| IP 白名单              | IIpRuleMatcher + IpAccessControlMiddleware         |
| 安全封禁                | SecurityBanService + SecurityBanMiddleware         |
| 操作日志                | Audit Middleware / AuditLogService                 |
| 配置读取                | SettingService + SettingCache                      |
| 登录失败限制              | Rate Limiting + SecurityBanService                 |
| 安全响应头               | SecureHeadersMiddleware                            |

### 3.2 不照搬旧系统 CSRF

旧系统是服务端模板 + Session 架构，CSRF 适合作为全局写请求保护。

WeCMS Next 是前后端分离 API 架构，应分类处理：

| API 类型                          | CSRF 策略                                                          |
| ------------------------------- | ---------------------------------------------------------------- |
| 使用 Authorization Bearer 的业务 API | 重点依赖 Bearer token、CORS、权限码、DTO 校验、Audit，不强制全局 CSRF               |
| 使用 HttpOnly Cookie 的认证 API      | 必须加强 SameSite、Origin / Referer 校验，必要时引入 double-submit CSRF token |
| 高风险写接口                          | 可叠加二次确认、当前密码验证、2FA 验证或短期 challenge                               |

### 3.3 所有写操作必须具备四要素

所有写操作必须同时满足：

```text
明确 HTTP Method
明确权限码
明确 DTO 校验
明确 Audit Log
```

高风险写操作还必须满足：

```text
Security Event
必要时要求当前密码 / 2FA / challenge
必要时吊销 refresh token family
```

---

## 4. 优先级总览

## P1：一期后必须优先补齐

| 编号    | 模块                    | 问题                                               | 优先级 | 建议阶段      |
| ----- | --------------------- | ------------------------------------------------ | --: | --------- |
| P1-01 | 2FA                   | 旧系统已有完整 TOTP / Backup Code / 管理员重置，新系统未覆盖        |  P1 | Sprint H1 |
| P1-02 | 个人中心                  | 旧系统有 Profile、修改密码、头像上传，新系统缺少自服务闭环                |  P1 | Sprint H1 |
| P1-03 | 安全中心                  | 当前仅有安全事件查询，缺少封禁、解封、批量解封                          |  P1 | Sprint H1 |
| P1-04 | 文档与 ADR               | README 阶段状态落后，ADR-0014 与最新 HttpOnly Cookie 实现不一致 |  P1 | Sprint H0 |
| P1-05 | AdminGate / CSRF 安全落地 | 旧系统安全总闸门需要拆解为新系统中间件、服务、权限、审计与限流能力                |  P1 | Sprint H1 |

## P2：建议在 CMS 二期前完成

| 编号    | 模块     | 问题                                                                           | 优先级 | 建议阶段      |
| ----- | ------ | ---------------------------------------------------------------------------- | --: | --------- |
| P2-01 | i18n   | 旧系统有多语言文案管理，新系统未覆盖                                                           |  P2 | Sprint H2 |
| P2-02 | 菜单排序   | 旧系统有菜单排序，新系统未看到独立批量排序 API                                                    |  P2 | Sprint H2 |
| P2-03 | 字典状态   | 旧系统字典值有 state，新系统未看到独立启停接口                                                   |  P2 | Sprint H2 |
| P2-04 | 系统设置   | 敏感配置加密、IP/CIDR 规则、缓存刷新需要复核并增强                                                |  P2 | Sprint H2 |
| P2-05 | 文件策略   | 上传策略分层、头像专用入口、安全响应头需增强                                                       |  P2 | Sprint H2 |
| P2-06 | 安全增强链路 | SecurityEventClassifier、Rate Limiting、PermissionVersion、安全响应头与 CSP hardening |  P2 | Sprint H2 |

---

## 5. 阶段安排

### Sprint H0：文档与状态修复

目标：先修正项目状态、ADR、验收边界，避免后续开发基线混乱。

交付项：

1. 更新 README 当前阶段。
2. 新增一期完成状态说明。
3. 更新 ADR-0014 或新增 ADR-0015，记录 refresh token 最终采用 HttpOnly Cookie。
4. 新增一期后补齐计划文档。
5. 新增 AdminGate / CSRF 迁移安全设计说明。
6. 更新 docs/context 中 M2-FE / 一期收口说明。
7. 确认 CMS 不进入一期补齐范围。
8. 更新质量门禁说明。

验收标准：

1. README 不再显示“当前阶段 M1-BE backend-only”。
2. 文档明确当前状态为“一期完成：M0-BE + M1-BE + M2-FE”。
3. ADR 明确 refresh token 不再由前端 localStorage 保存。
4. 文档明确 access token 仅内存保存。
5. 文档明确 HttpOnly Cookie 是当前实现基线。
6. 文档明确旧 AdminGate 不复制，必须拆解落地。
7. 文档明确旧 CSRF 不全局照搬，按 Cookie 型认证接口重点防护。
8. 文档明确 CMS 二期。
9. backend gate 与 frontend gate 均通过。

建议修改路径：

```text
README.md
docs/adr/0014-refresh-token-storage-m2-fe.md
docs/adr/0015-auth-cookie-token-final-state.md
docs/adr/0016-admingate-csrf-migration-strategy.md
docs/context/WeCMS_Next_一期完成状态说明.md
docs/context/WeCMS_Next_一期后补齐计划书.md
docs/context/WeCMS_Next_AdminGate_CSRF_迁移设计说明.md
```

---

# 6. P1-01：2FA 双因素认证补齐计划

## 6.1 当前差距

旧 ThinkPHP 系统已具备：

1. TOTP Secret 生成。
2. otpauth URI。
3. 二维码渲染。
4. 6 位 TOTP 校验。
5. 30 秒时间片。
6. 时间窗口容错。
7. Backup Code。
8. Backup Code hash 保存。
9. Secret 加密存储。
10. TOTP 重放保护。
11. 登录期间 2FA pending 状态。
12. 管理员重置 2FA。

新系统一期当前未形成对应 2FA API、数据库字段、前端页面和登录流程。

## 6.2 目标能力

新系统应实现：

1. 用户可在个人中心绑定 2FA。
2. 用户可启用 / 禁用 2FA。
3. 登录时如果用户启用 2FA，则进入二次验证流程。
4. 用户可使用 TOTP code 验证。
5. 用户可使用 backup code 验证。
6. 管理员可重置指定用户 2FA。
7. TOTP Secret 必须加密存储。
8. Backup Code 只存 hash。
9. TOTP 必须具备重放保护。
10. 所有高风险操作必须写 audit log 和 security event。
11. 不沿用旧系统 Session pending 方式，改用短有效期 Auth Challenge。

## 6.3 后端设计

### 6.3.1 新增表

推荐新增：

```text
sys_user_two_factor
  id
  user_id
  enabled
  secret_cipher
  confirmed_at
  last_totp_step
  recovery_codes_hash_json
  recovery_codes_used_count
  reset_required
  created_at
  updated_at
```

新增登录挑战表：

```text
sys_auth_challenge
  id
  challenge_id
  user_id
  challenge_type
  status
  expires_at
  consumed_at
  ip
  user_agent
  trace_id
  created_at
```

### 6.3.2 新增接口

个人 2FA：

```text
GET    /api/v1/account/2fa/status
POST   /api/v1/account/2fa/setup
POST   /api/v1/account/2fa/confirm
POST   /api/v1/account/2fa/disable
POST   /api/v1/account/2fa/recovery-codes/regenerate
```

登录 2FA：

```text
POST   /api/v1/auth/login
POST   /api/v1/auth/2fa/verify
POST   /api/v1/auth/2fa/recovery-code
```

管理员重置：

```text
POST   /api/v1/system/users/{id}/reset-2fa
```

### 6.3.3 登录流程调整

新的登录流程：

```text
1. 用户提交 username/password。
2. 后端校验密码。
3. 如果用户未启用 2FA：
   - 签发 access token。
   - 设置 refresh cookie。
   - 返回 LoginResponse。
4. 如果用户启用 2FA：
   - 不签发正式 access token。
   - 创建短有效期 two_factor_challenge_id。
   - 返回 requiresTwoFactor = true。
5. 前端跳转 2FA 验证页。
6. 用户提交 TOTP 或 recovery code。
7. 后端校验成功后：
   - 签发 access token。
   - 设置 refresh cookie。
   - 写 login log。
8. 校验失败：
   - 写 login log。
   - 写 security event。
   - 触发失败限制。
```

### 6.3.4 安全要求

1. TOTP Secret 不允许明文入库。
2. Recovery Code 不允许明文入库。
3. Recovery Code 只能展示一次。
4. 禁用 2FA 必须验证当前密码或 TOTP。
5. 管理员 reset 2FA 必须记录高风险审计日志。
6. TOTP 校验必须处理 `last_totp_step IS NULL OR last_totp_step <> usedStep`。
7. 2FA challenge 有效期建议 5 分钟。
8. 2FA challenge 只能使用一次。
9. 多次失败应写安全事件。
10. 不允许在 API response 中返回 secret 明文，除 setup 初始化阶段外。

## 6.4 前端设计

新增页面：

```text
/account/profile
/account/security
/auth/two-factor
/system/users reset 2FA 操作按钮
```

页面能力：

1. 查看 2FA 状态。
2. 开始绑定 2FA。
3. 展示二维码。
4. 输入 TOTP 确认启用。
5. 展示 recovery codes。
6. 禁用 2FA。
7. 重新生成 recovery codes。
8. 登录时跳转 2FA 验证页。
9. 管理员在用户管理中重置用户 2FA。

## 6.5 测试计划

单元测试：

1. TOTP code 正确校验。
2. TOTP code 错误拒绝。
3. 时间窗口容错。
4. TOTP 重放拒绝。
5. Recovery code 一次性使用。
6. Recovery code hash 校验。
7. Secret 加密 / 解密。
8. Challenge 过期拒绝。
9. Challenge 重复使用拒绝。

集成测试：

1. 未启用 2FA 用户登录成功。
2. 启用 2FA 用户登录返回 requiresTwoFactor。
3. 2FA 验证成功后签发 token。
4. 2FA 验证失败返回 401。
5. 管理员 reset 2FA 成功。
6. reset 2FA 写 audit log。
7. reset 2FA 写 security event。

前端测试：

1. 登录后触发 2FA 页面跳转。
2. 2FA 验证成功进入后台。
3. 2FA 验证失败显示错误。
4. 个人中心启用 2FA。
5. 用户管理 reset 2FA 按钮权限控制。

## 6.6 验收标准

1. 用户可启用 2FA。
2. 启用 2FA 后，登录必须完成二次验证。
3. Recovery Code 可用且一次性。
4. TOTP 重放被拒绝。
5. 管理员可重置用户 2FA。
6. 所有 2FA 高风险动作写 audit log。
7. 所有异常尝试写 security event。
8. OpenAPI 包含 2FA endpoint。
9. 前端页面可完整操作。
10. backend gate、frontend gate、OpenAPI gate 全部通过。

---

# 7. P1-02：个人中心 / 修改密码 / 头像能力补齐计划

## 7.1 当前差距

旧系统存在 `Profile` 控制器和头像上传入口。新系统当前用户管理偏管理员后台 CRUD，缺少登录用户自服务能力。

## 7.2 目标能力

用户应能：

1. 查看自己的资料。
2. 修改显示名称、邮箱、手机号等非敏感资料。
3. 修改自己的密码。
4. 上传 / 更换头像。
5. 查看自己的角色、权限、菜单。
6. 查看安全状态，例如是否启用 2FA。

## 7.3 后端接口

```text
GET    /api/v1/account/profile
PUT    /api/v1/account/profile
POST   /api/v1/account/password/change
POST   /api/v1/account/avatar
GET    /api/v1/account/security
```

## 7.4 权限策略

个人中心接口只要求登录，不要求系统权限码。

但以下操作必须写 audit log：

1. 修改密码。
2. 上传头像。
3. 修改安全设置。
4. 启用 / 禁用 2FA。

## 7.5 后端规则

修改密码：

1. 必须提交 oldPassword。
2. newPassword 必须满足密码策略。
3. oldPassword 校验失败返回 400 或 401。
4. 修改成功后吊销当前用户全部 refresh token family。
5. 修改成功后要求重新登录，或仅保留当前会话，需产品明确。
6. 写 security event。

头像上传：

1. 使用 AvatarUploadPolicy。
2. 只允许图片 MIME。
3. 限制大小。
4. 随机文件名。
5. 存储为 private 或 controlled public。
6. 返回 fileId 和 avatarUrl。
7. 可考虑图片重编码。

## 7.6 前端页面

新增：

```text
/account/profile
/account/security
```

功能：

1. 基础资料表单。
2. 修改密码弹窗。
3. 头像上传。
4. 2FA 状态入口。
5. 安全事件提示。

## 7.7 测试计划

后端测试：

1. 未登录访问 profile 返回 401。
2. 登录用户可获取 profile。
3. 修改 profile 成功。
4. 修改密码 oldPassword 错误失败。
5. 修改密码成功后 refresh token 被吊销。
6. 上传非图片头像失败。
7. 上传超大头像失败。
8. 上传合法头像成功。
9. 写 audit log。

前端测试：

1. Profile 页面加载。
2. 修改资料成功。
3. 修改密码成功提示重新登录。
4. 头像上传预览成功。
5. 错误信息正确展示。

## 7.8 验收标准

1. 登录用户可完整维护个人资料。
2. 登录用户可修改密码。
3. 登录用户可上传头像。
4. 修改密码后 token 处理符合安全策略。
5. 前端有完整页面入口。
6. 质量门禁通过。

---

# 8. P1-03：安全中心封禁 / 解封能力补齐计划

## 8.1 当前差距

当前新系统已实现 security event 查询，但未看到旧系统中的安全状态、封禁列表、解封、批量解封能力。

## 8.2 目标能力

实现安全中心：

1. 查看安全状态。
2. 查看封禁列表。
3. 查看安全事件。
4. 解封单个 IP / 账号。
5. 批量解封。
6. 查看失败登录统计。
7. 查看风险事件统计。
8. 配合登录失败限制。

## 8.3 后端数据模型

新增或确认已有表：

```text
sys_security_ban
  id
  ban_type              -- ip/user/device
  target                -- ip/userId/deviceId
  reason
  severity
  source
  expires_at
  revoked_at
  revoked_by
  revoke_reason
  created_at
  updated_at
```

可选统计表或实时查询：

```text
sys_security_counter
  id
  counter_type
  target
  window_start
  window_end
  count
  created_at
```

## 8.4 后端接口

```text
GET    /api/v1/system/security/status
GET    /api/v1/system/security/bans
GET    /api/v1/system/security/bans/{id}
POST   /api/v1/system/security/bans/{id}/unban
POST   /api/v1/system/security/bans/batch-unban
```

权限码：

```text
sys:security:status
sys:security:ban:list
sys:security:ban:detail
sys:security:ban:unban
sys:security:ban:batch-unban
```

## 8.5 前端页面

扩展当前安全事件页面为安全中心：

```text
/system/security
/system/logs/security
```

页面模块：

1. 安全概览卡片。
2. 封禁列表。
3. 安全事件列表。
4. 解封弹窗。
5. 批量解封操作。
6. 高风险操作二次确认。

## 8.6 安全要求

1. 解封操作必须写 audit log。
2. 批量解封必须限制数量。
3. 解封必须记录原因。
4. 不允许用户解封自己相关的高风险封禁，除非 super_admin。
5. 安全状态接口不能泄露敏感内部实现。
6. IP 规则必须支持 IPv4、IPv6、CIDR。
7. 解封操作必须写 security event。
8. 安全封禁应可被 SecurityBanMiddleware 使用。

## 8.7 测试计划

后端测试：

1. 无权限访问安全中心返回 403。
2. 有权限可查看 status。
3. 有权限可查看 bans。
4. 解封成功写 revoked_at。
5. 批量解封成功。
6. 解封不存在 ban 返回 404。
7. 重复解封返回业务错误。
8. 解封写 audit log。
9. 解封写 security event。

前端测试：

1. 安全中心页面展示。
2. 封禁列表筛选。
3. 单个解封。
4. 批量解封。
5. 无权限隐藏按钮。

## 8.8 验收标准

1. 安全中心具备查看、解封、批量解封能力。
2. 旧系统 Security 核心能力被覆盖。
3. 所有写操作有权限码。
4. 所有写操作有审计。
5. 前端按钮权限正确。
6. gate 全部通过。

---

# 9. P1-04：文档与 ADR 修复计划

## 9.1 当前差距

当前 README 和部分 ADR 与最新代码状态存在不一致：

1. README 仍显示 M1-BE backend-only。
2. 当前已经有 M2-FE 前端工程。
3. ADR-0014 仍描述 refresh token localStorage 风险。
4. 最新代码已切换为 HttpOnly Cookie refresh token。
5. 一期收口状态缺少正式文档。
6. 旧系统 AdminGate / CSRF 的新系统落地策略尚未形成正式 ADR。

## 9.2 修复目标

1. README 标记一期完成。
2. 明确当前阶段为一期收口后 hardening。
3. 更新 token storage ADR。
4. 新增一期完成状态说明。
5. 新增本计划书到 docs/context。
6. 新增 AdminGate / CSRF 迁移策略 ADR。
7. 更新质量门禁说明。

## 9.3 修改清单

```text
README.md
docs/adr/0014-refresh-token-storage-m2-fe.md
docs/adr/0015-auth-token-storage-final-state.md
docs/adr/0016-admingate-csrf-migration-strategy.md
docs/context/WeCMS_Next_一期完成状态说明.md
docs/context/WeCMS_Next_一期后补齐计划书.md
docs/context/WeCMS_Next_AdminGate_CSRF_迁移设计说明.md
```

## 9.4 验收标准

1. 文档没有把当前阶段描述为 M1-BE。
2. 文档明确 M0-BE、M1-BE、M2-FE 已完成一期闭环。
3. 文档明确 CMS 二期。
4. 文档明确 refresh token 使用 HttpOnly Cookie。
5. 文档明确 access token 仅内存保存。
6. 文档明确后续 P1/P2 hardening 清单。
7. 文档明确旧 AdminGate 不复制，必须拆解。
8. 文档明确旧 CSRF 不全局照搬。
9. 文档检查和质量门禁通过。

---

# 10. P1-05：AdminGate / CSRF 安全落地改造计划

## 10.1 当前差距

旧 ThinkPHP 系统的 AdminGate / CSRF 负责多项后台安全能力：

```text
CSRF
WAF
登录失败限制
Session 超时
DB token 校验
IP 白名单
安全中心封禁列表
2FA
敏感配置加密
文件上传检查
```

新系统当前已经具备认证、权限码、审计日志、安全事件查询等基础，但旧系统 AdminGate / CSRF 中的以下能力尚未系统化落地：

1. Cookie 型认证接口的 Origin / CSRF 防护策略。
2. 安全封禁中间件。
3. IP 白名单 / CIDR 统一匹配中间件。
4. 登录失败限制与封禁联动。
5. SecurityEventClassifier。
6. PermissionVersion 权限变更刷新。
7. 安全响应头与 CSP。
8. 所有写操作 method / permission / audit 的统一 gate 检查。

## 10.2 目标能力

本任务目标不是新增一个 `AdminGate`，而是实现一组明确组件：

```text
OriginValidationMiddleware
CookieCsrfProtectionFilter
IpAccessControlMiddleware
SecurityBanMiddleware
SecurityEventClassifier
RateLimitPolicy
SecureHeadersMiddleware
PermissionVersionService
AuditCoverageCheck
WriteEndpointMethodCheck
```

## 10.3 子任务拆分

### P1-05-001：Cookie 型认证接口 Origin / CSRF 防护

适用接口：

```text
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
POST /api/v1/auth/2fa/verify
POST /api/v1/auth/2fa/recovery-code
```

要求：

1. 校验 Origin。
2. 缺少 Origin 时按配置决定是否允许 Referer fallback。
3. 只允许当前前端 origin。
4. 不允许 wildcard origin。
5. 失败写 security event。
6. 可配置 Development 环境宽松策略，但 CI / Production 必须严格。
7. 对使用 HttpOnly refresh cookie 的接口优先启用。

建议配置：

```json
{
  "Security": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "https://admin.example.com"
    ],
    "RequireOriginForCookieAuth": true
  }
}
```

验收标准：

1. 合法 Origin 请求成功。
2. 缺失 Origin 的跨站模拟请求失败。
3. 非法 Origin 请求失败。
4. refresh/logout 均被覆盖。
5. 失败写 security event。
6. OpenAPI 不受破坏。

---

### P1-05-002：IP 白名单 / CIDR 统一匹配

新增服务：

```text
IIpRuleMatcher
IpRuleMatcher
IpAccessControlMiddleware
```

支持：

```text
IPv4
IPv6
CIDR
单 IP
逗号分隔
换行分隔
空白分隔
```

适用范围：

1. 后台 API。
2. 认证 API。
3. 可按配置决定是否覆盖 public health endpoint。
4. Development 可默认关闭。
5. Production 可按配置开启。

验收标准：

1. IPv4 精确匹配成功。
2. IPv4 CIDR 匹配成功。
3. IPv6 精确匹配成功。
4. IPv6 CIDR 匹配成功。
5. 逗号分隔解析成功。
6. 换行分隔解析成功。
7. 非法规则保存失败。
8. 不匹配 IP 返回 403。
9. 拒绝事件写 security event。

---

### P1-05-003：安全封禁中间件

新增：

```text
SecurityBanMiddleware
SecurityBanService
ISecurityBanRepository
```

覆盖：

1. IP ban。
2. User ban。
3. 可选 device/session ban。
4. 过期自动忽略。
5. revoked 自动忽略。
6. 命中 ban 返回 403 或 429。
7. 命中 ban 写 security event。

执行顺序建议：

```text
RequestId
Exception
SecureHeaders
IpAccessControl
SecurityBan
RateLimit
Authentication
Authorization
PermissionEndpointFilter
```

验收标准：

1. 被封禁 IP 无法访问后台 API。
2. 被封禁用户无法访问后台 API。
3. 过期封禁不生效。
4. 已解封记录不生效。
5. 命中封禁写 security event。
6. 安全中心可查询和解封。

---

### P1-05-004：登录失败限制与封禁联动

新增或增强：

```text
LoginFailurePolicy
LoginFailureCounter
RateLimitPolicy
SecurityBanService
```

策略：

1. 同一用户名短时间失败超过阈值，触发 user-level challenge 或 lock。
2. 同一 IP 短时间失败超过阈值，触发 IP-level rate limit。
3. 极端失败触发临时 ban。
4. 所有失败写 login log。
5. 达到阈值写 security event。
6. 成功登录后按策略清理失败计数。

验收标准：

1. 密码错误写 login log。
2. 多次错误触发 rate limit。
3. 达到阈值创建 security event。
4. 达到封禁阈值创建 ban。
5. 封禁后请求被 SecurityBanMiddleware 拦截。
6. 解封后可恢复访问。

---

### P1-05-005：写操作 Method / Permission / Audit 统一检查

新增脚本或测试：

```text
check-write-endpoint-methods.sh
check-write-endpoint-permission-coverage.sh
check-write-endpoint-audit-coverage.sh
```

检查目标：

1. `POST / PUT / PATCH / DELETE` 必须声明权限码，除明确 anonymous/internal endpoint。
2. 写操作不能使用 `GET`。
3. 写操作必须写 audit log。
4. 高风险写操作必须写 security event。
5. auth refresh/logout 允许 anonymous，但必须有 cookie/origin protection。
6. 文件上传必须显式 policy。

验收标准：

1. 检查脚本接入 backend quality gate。
2. 缺权限码时 gate 失败。
3. GET 写操作被发现时 gate 失败。
4. 缺 audit log 覆盖时 gate 失败。
5. 特例必须有 allowlist 和注释原因。

---

### P1-05-006：PermissionVersion 权限变更刷新

新增：

```text
PermissionVersionService
UserSecurityStampService
```

触发场景：

1. 用户角色变更。
2. 角色权限变更。
3. 角色菜单变更。
4. 权限禁用。
5. 菜单权限码变更。
6. 用户禁用。
7. 用户密码重置。
8. 管理员 reset 2FA。

策略：

1. 受影响用户 `permission_version` 增加。
2. 可选更新 `security_stamp`。
3. Auth `/me` 返回 permissionVersion。
4. access token 可携带 permissionVersion。
5. permissionVersion 不一致时要求刷新权限或重新登录。
6. 高风险变更可吊销 refresh token family。

验收标准：

1. 修改角色权限后，受影响用户 permissionVersion 增加。
2. 修改用户角色后，该用户 permissionVersion 增加。
3. 禁用角色后，受影响用户权限刷新。
4. 前端可感知权限变化。
5. 测试覆盖 role -> user 影响链。

---

### P1-05-007：安全响应头基础 hardening

新增：

```text
SecureHeadersMiddleware
```

建议默认响应头：

```text
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: no-referrer
Permissions-Policy: geolocation=(), microphone=(), camera=()
Content-Security-Policy: default-src 'self'
```

注意：

1. CSP 初期可先 report-only。
2. Vite dev 环境需要单独策略。
3. 文件 preview 需要单独处理 Content-Disposition 与 MIME。
4. 不要破坏 OpenAPI export。

验收标准：

1. 后台 API 返回安全响应头。
2. 文件下载 nosniff。
3. preview 只允许安全 MIME inline。
4. Dev 环境不阻塞前端调试。
5. Production 配置严格。

---

## 10.4 测试计划

后端测试：

1. 非法 Origin refresh 被拒。
2. 合法 Origin refresh 成功。
3. 非法 Origin logout 被拒。
4. 被封禁 IP 被拒。
5. 被封禁 user 被拒。
6. 过期 ban 不生效。
7. IP CIDR 匹配正确。
8. 登录失败触发 rate limit。
9. 登录失败触发 security event。
10. 权限变更增加 permissionVersion。
11. 写操作缺权限码 gate 失败。
12. 写操作缺 audit log gate 失败。
13. 安全响应头存在。

前端测试：

1. refresh/logout 正常。
2. 权限变化后重新拉取 me。
3. 被封禁用户显示明确错误。
4. 无权限按钮隐藏。
5. 安全中心可展示封禁和解封状态。

## 10.5 验收标准

1. 不新增旧式 AdminGate。
2. Cookie 型认证接口具备 Origin / CSRF 防护。
3. IP 规则统一解析。
4. 安全封禁中间件生效。
5. 登录失败限制与封禁联动。
6. 写操作 method / permission / audit gate 生效。
7. 权限变更后 permissionVersion 刷新。
8. 安全响应头生效。
9. 后端质量门禁通过。
10. 前端质量门禁通过。

---

# 11. P2-01：i18n 管理能力补齐计划

## 11.1 当前差距

旧系统支持数据库维护多语言文案、语言切换接口、runtime override 文件生成。新系统当前未看到等价 i18n 管理 API 与前端页面。

## 11.2 目标能力

1. 后台可维护多语言文案。
2. 支持 locale：`zh-CN`、`en-US`、`ms-MY`。
3. 支持按模块、key 搜索。
4. 支持新增、编辑、删除文案。
5. 支持前端拉取业务动态文案。
6. 支持菜单 i18n key。
7. 不再生成 PHP runtime override 文件。
8. 前端使用 Vue i18n 或 SoybeanAdmin 现有 i18n 机制接入。

## 11.3 数据模型

```text
sys_i18n_message
  id
  locale
  message_key
  message_value
  module
  remark
  status
  created_at
  updated_at
  deleted_at
```

唯一索引：

```text
locale + message_key
```

## 11.4 后端接口

```text
GET    /api/v1/system/i18n/messages
GET    /api/v1/system/i18n/messages/{id}
POST   /api/v1/system/i18n/messages
PUT    /api/v1/system/i18n/messages/{id}
DELETE /api/v1/system/i18n/messages/{id}
GET    /api/v1/i18n/messages?locale=zh-CN
POST   /api/v1/account/i18n/switch
```

权限码：

```text
sys:i18n:list
sys:i18n:detail
sys:i18n:create
sys:i18n:update
sys:i18n:delete
account:i18n:switch
```

## 11.5 前端页面

新增：

```text
/system/i18n
```

功能：

1. locale 筛选。
2. module 筛选。
3. keyword 搜索。
4. 新增文案。
5. 编辑文案。
6. 删除文案。
7. 批量导出可后置。

## 11.6 测试计划

1. 新增 i18n message。
2. 重复 locale + key 失败。
3. 编辑成功。
4. 删除成功。
5. 前端拉取指定 locale。
6. 权限不足返回 403。
7. 切换语言成功。
8. audit log 写入。

## 11.7 验收标准

1. 旧系统 i18n 核心管理能力被覆盖。
2. 不引入旧 PHP override 文件机制。
3. 前端可使用动态文案。
4. gate 通过。

---

# 12. P2-02：菜单排序能力补齐计划

## 12.1 当前差距

旧系统有 `ruleOrderBy` 菜单排序。新系统当前菜单 endpoint 已有 CRUD 与树，但未看到独立批量排序接口。

## 12.2 目标能力

1. 菜单支持拖拽排序。
2. 支持同级排序。
3. 支持批量提交 sort。
4. 支持 parent_id 调整时重新排序。
5. 写 audit log。
6. 前端菜单管理页面支持拖拽或排序字段编辑。

## 12.3 后端接口

```text
PUT /api/v1/system/menus/sort
```

Request：

```json
{
  "items": [
    {
      "id": 1,
      "parentId": 0,
      "sort": 10
    }
  ]
}
```

权限码：

```text
sys:menu:sort
```

## 12.4 规则

1. 所有 id 必须存在。
2. parentId 必须存在或为 root。
3. 不允许形成循环树。
4. 不允许移动系统锁定菜单。
5. 批量数量限制，例如最多 200。
6. 事务提交。

## 12.5 测试计划

1. 同级排序成功。
2. 跨父级移动成功。
3. 循环树失败。
4. 不存在菜单失败。
5. 无权限失败。
6. 写 audit log。
7. 前端拖拽保存成功。

## 12.6 验收标准

1. 菜单可批量排序。
2. 权限码覆盖。
3. 前端可操作。
4. OpenAPI 更新。
5. gate 通过。

---

# 13. P2-03：字典状态切换补齐计划

## 13.1 当前差距

旧系统字典值有 state 切换。新系统当前字典类型和值 CRUD 已有，但未看到独立 enable/disable endpoint。

## 13.2 目标能力

1. 字典类型可启用 / 禁用。
2. 字典值可启用 / 禁用。
3. 前端可通过开关操作。
4. 禁用类型时可选择是否级联禁用值。
5. 所有操作写 audit log。

## 13.3 后端接口

```text
POST /api/v1/system/dict-types/{id}/enable
POST /api/v1/system/dict-types/{id}/disable
POST /api/v1/system/dict-values/{id}/enable
POST /api/v1/system/dict-values/{id}/disable
```

权限码：

```text
sys:dict:type:enable
sys:dict:type:disable
sys:dict:value:enable
sys:dict:value:disable
```

## 13.4 测试计划

1. 启用字典类型成功。
2. 禁用字典类型成功。
3. 启用字典值成功。
4. 禁用字典值成功。
5. 禁用后公开查询不返回 disabled 值。
6. 权限不足返回 403。
7. 写 audit log。

## 13.5 验收标准

1. 字典状态与旧系统等价。
2. 前端开关可操作。
3. API 权限码完整。
4. gate 通过。

---

# 14. P2-04：系统设置安全 hardening 计划

## 14.1 当前差距

新系统已有 settings list/detail/update API，但需要进一步确认并增强：

1. 配置白名单。
2. 敏感配置加密。
3. SMTP 密码隐藏。
4. 配置缓存刷新。
5. IP / CIDR 白名单规则。
6. 设置变更审计。

## 14.2 目标能力

1. 所有 setting key 必须预定义。
2. 不允许任意 key 写入。
3. 敏感配置必须加密存储。
4. API 返回敏感配置必须脱敏。
5. IP 白名单支持 IPv4、IPv6、CIDR、逗号和换行分隔。
6. 保存配置后刷新缓存。
7. 修改安全相关配置写 security event。
8. IP 规则使用 P1-05 中同一个 `IIpRuleMatcher`。

## 14.3 后端设计

新增服务：

```text
ISettingDefinitionProvider
ISecretProtector
IIpRuleMatcher
ISettingCache
```

SettingDefinition 示例：

```text
key
group
valueType
isSensitive
isReadonly
validationRules
defaultValue
```

敏感 key：

```text
smtp_pass
auth_key
jwt_secret
storage_secret
```

## 14.4 接口调整

保留现有：

```text
GET /api/v1/system/settings
GET /api/v1/system/settings/{key}
PUT /api/v1/system/settings/{key}
```

新增：

```text
POST /api/v1/system/settings/validate-ip-rules
POST /api/v1/system/settings/reload-cache
```

## 14.5 测试计划

1. 未定义 key 更新失败。
2. readonly key 更新失败。
3. 敏感值入库加密。
4. 敏感值返回脱敏。
5. IP 精确匹配成功。
6. CIDR 匹配成功。
7. IPv6 匹配成功。
8. 非法 IP 规则保存失败。
9. 更新配置写 audit log。
10. 安全配置变更写 security event。

## 14.6 验收标准

1. 设置模块具备白名单保护。
2. 敏感配置不明文入库。
3. 敏感配置不明文返回。
4. IP 规则解析一致。
5. gate 通过。

---

# 15. P2-05：文件上传策略分层与头像专用能力增强计划

## 15.1 当前差距

新系统已有文件 list/upload/download/preview/delete，但需进一步增强：

1. 头像专用上传入口。
2. Avatar/Image/Document 策略分层。
3. MIME 与扩展名双校验。
4. 图片重编码。
5. nosniff 和安全响应头。
6. preview MIME 白名单。
7. 文件下载审计。

## 15.2 目标能力

1. 文件上传按业务策略执行。
2. 头像上传只能走 AvatarPolicy。
3. 图片上传走 ImagePolicy。
4. 文档上传走 DocumentPolicy。
5. 所有文件名随机化。
6. 所有路径防穿越。
7. 下载和预览必须鉴权。
8. 预览仅允许安全 MIME。
9. 危险文件必须强制下载，不允许 inline preview。

## 15.3 后端设计

新增策略接口：

```text
IFileUploadPolicy
IFileUploadPolicyResolver
AvatarUploadPolicy
ImageUploadPolicy
DocumentUploadPolicy
```

策略字段：

```text
allowedExtensions
allowedMimeTypes
maxSizeBytes
requireImageDecode
reencodeImage
allowPreview
storageScope
```

接口：

```text
POST /api/v1/account/avatar
POST /api/v1/system/files
GET  /api/v1/system/files/{id}/download
GET  /api/v1/system/files/{id}/preview
```

## 15.4 前端设计

1. 个人中心头像上传。
2. 文件管理上传弹窗增加 policy 类型。
3. 文件列表增加预览 / 下载按钮。
4. 非可预览文件隐藏 preview。
5. 上传失败展示具体错误。

## 15.5 测试计划

1. 合法头像上传成功。
2. 非图片头像上传失败。
3. 超大头像上传失败。
4. polyglot 图片被拒绝或重编码。
5. 危险扩展名被拒绝。
6. MIME 不匹配被拒绝。
7. 未授权下载返回 401。
8. 无权限下载返回 403。
9. preview 不允许危险 MIME inline。
10. 下载写 audit log。

## 15.6 验收标准

1. 上传策略分层完成。
2. 头像专用入口完成。
3. 文件安全能力不低于旧系统。
4. gate 通过。

---

# 16. P2-06：安全增强链路计划

## 16.1 当前差距

P1-05 负责把旧 AdminGate / CSRF 的必要安全职责落成可运行闭环。P2-06 负责进一步 hardening：

1. SecurityEventClassifier 细化。
2. Rate Limiting 策略分级。
3. PermissionVersion 与前端权限刷新体验完善。
4. 安全响应头和 CSP 从基础模式进入生产严格模式。
5. 审计日志覆盖率持续检查。
6. 安全事件与安全中心联动。

## 16.2 SecurityEventClassifier

新增：

```text
ISecurityEventClassifier
SecurityEventClassifier
SecurityEventRule
```

分类类型：

```text
login_failure
login_bruteforce
csrf_origin_rejected
ip_blocked
security_ban_hit
permission_denied
suspicious_payload
file_upload_rejected
two_factor_failed
two_factor_replay
settings_sensitive_changed
role_permission_changed
```

验收标准：

1. 每类事件有 severity。
2. 每类事件有 source。
3. 每类事件有 traceId。
4. 高风险事件能进入安全中心。
5. 分类规则可测试。

## 16.3 Rate Limiting 策略

策略分层：

```text
auth_login_policy
auth_refresh_policy
auth_2fa_policy
admin_write_policy
file_upload_policy
security_unban_policy
```

要求：

1. 登录接口严格限流。
2. refresh 接口限流。
3. 2FA 验证限流。
4. 文件上传限流。
5. 安全中心解封限流。
6. 命中限流写 security event。

## 16.4 PermissionVersion 前端体验

前端处理：

1. `/api/v1/auth/me` 返回 permissionVersion。
2. 前端 store 保存 permissionVersion。
3. API 响应出现 permission version mismatch 时重新拉取 `/me`。
4. 权限消失后自动移除菜单。
5. 当前路由无权限时跳转 403。
6. 不需要用户手动刷新页面。

## 16.5 CSP hardening

阶段策略：

```text
H2-06-A: Report-Only
H2-06-B: 阻断 inline script
H2-06-C: 限制 connect-src
H2-06-D: 生产 strict CSP
```

验收标准：

1. 开发环境不阻断 Vite。
2. 生产环境默认只允许 self。
3. 禁止任意第三方 script。
4. 文件预览不破坏 CSP。
5. 无 `v-html` 不可信渲染。

## 16.6 验收标准

1. SecurityEventClassifier 可用。
2. Rate Limiting 按接口分级。
3. PermissionVersion 前后端闭环。
4. CSP 可进入 report-only。
5. 安全响应头完整。
6. 安全中心可以展示分类后的高风险事件。
7. gate 通过。

---

# 17. 统一测试与质量门禁要求

## 17.1 后端必须新增测试

每个 P1/P2 模块至少包含：

```text
Unit Tests
Integration Tests
Architecture Tests 如涉及边界
OpenAPI Contract Tests
Permission Coverage Tests
Audit Log Tests
Security Event Tests
```

## 17.2 前端必须新增测试

每个前端页面至少包含：

```text
Typecheck
Lint
Route permission coverage
API contract compatibility
Smoke fixture
No v-html scan
No CMS frontend scan
```

## 17.3 每轮开发必须执行

后端：

```bash
bash scripts/quality-gate-backend.sh
```

前端：

```bash
bash scripts/quality-gate-frontend.sh
```

数据库：

```bash
bash scripts/db/reset-dev-db.sh
bash scripts/smoke-admin-login.sh
```

OpenAPI：

```bash
dotnet run --project backend/src/WeCms.Api -- --export-openapi artifacts/openapi/wecms-api-v1.json
```

## 17.4 新增安全门禁建议

新增：

```bash
bash scripts/checks/check-write-endpoint-methods.sh
bash scripts/checks/check-write-endpoint-permission-coverage.sh
bash scripts/checks/check-write-endpoint-audit-coverage.sh
bash scripts/checks/check-cookie-auth-origin-protection.sh
bash scripts/checks/check-security-headers.sh
```

## 17.5 禁止破坏项

1. 不允许 Modules 引用 Persistence。
2. 不允许 Modules 引用 SqlSugar。
3. 不允许业务层出现 SQL 字符串。
4. 不允许前端出现 CMS 代码。
5. 不允许 refresh token 回到 localStorage。
6. 不允许 `v-html` 渲染不可信内容。
7. 不允许绕过权限码。
8. 不允许写操作缺少 audit log。
9. 不允许高风险写操作缺少 security event。
10. 不允许敏感配置明文返回。
11. 不允许新增旧系统兼容模式。
12. 不允许复制旧 AdminGate。
13. 不允许把 WAF 作为主要业务安全边界。
14. 不允许 Cookie 型认证接口缺少 Origin / CSRF 防护。
15. 不允许 GET endpoint 产生业务写副作用。

---

# 18. 推荐执行顺序

## 第一批：H0 文档修复

```text
H0-001 更新 README 当前阶段
H0-002 更新 token storage ADR
H0-003 新增一期完成状态说明
H0-004 新增一期后补齐计划书
H0-005 新增 AdminGate / CSRF 迁移 ADR
```

## 第二批：H1 安全优先

```text
H1-001 Cookie 型认证接口 Origin / CSRF 防护
H1-002 IIpRuleMatcher 与 IpAccessControlMiddleware
H1-003 SecurityBan 表、服务与中间件
H1-004 安全中心 bans/status/unban
H1-005 登录失败限制与封禁联动
H1-006 写操作 method / permission / audit gate
H1-007 2FA 数据库与后端基础服务
H1-008 2FA 登录挑战流程
H1-009 2FA 个人中心接口
H1-010 2FA 前端页面
H1-011 管理员 reset 2FA
H1-012 个人中心 profile/password/avatar
```

## 第三批：H2 系统增强

```text
H2-001 i18n 数据库与 API
H2-002 i18n 前端页面
H2-003 菜单批量排序
H2-004 字典状态启停
H2-005 设置安全 hardening
H2-006 文件上传策略分层
H2-007 SecurityEventClassifier
H2-008 Rate Limiting 分级策略
H2-009 PermissionVersion 前后端闭环
H2-010 安全响应头与 CSP report-only
```

## 第四批：H3 总体验收

```text
H3-001 后端全量质量门禁
H3-002 前端全量质量门禁
H3-003 OpenAPI 合同复核
H3-004 权限码覆盖复核
H3-005 Audit log 覆盖复核
H3-006 Security event 覆盖复核
H3-007 Cookie 型认证接口 CSRF/Origin 覆盖复核
H3-008 旧 ThinkPHP AdminGate/CSRF 差异复核
H3-009 旧 ThinkPHP 功能差异复核
H3-010 CMS 二期启动前冻结基础系统
```

---

# 19. 每个任务的 Codex 执行模板

```text
你是 WeCMS Next 工程开发 Agent。

当前任务：
<填写任务编号与名称>

项目状态：
一期工程已完成，当前进入一期后 hardening / 补齐阶段。

硬性边界：
1. 不把 CMS 内容能力回流到一期。
2. 不做旧 ThinkPHP 数据迁移。
3. 不做旧密码 hash 兼容。
4. 不引入旧 Session / token runtime compatibility。
5. 不复制旧 AdminGate。
6. 不复制旧 PHP WAF 作为主要业务安全边界。
7. 不改变当前 .NET 10 + Minimal API + SqlSugar + Vue3 技术路线。
8. 数据库/ORM/连接器只能在 `WeCms.Data.SqlSugar` 与 `WeCms.Modules.*.SqlSugar` 边界内。
9. WeCms.Modules.* 不得引用 SqlSugar / Persistence / MySqlConnector。
10. 所有写操作必须绑定权限码并写 audit log。
11. 安全高风险操作必须写 security event。
12. Cookie 型认证接口必须有 Origin / CSRF 防护。
13. Refresh token 不允许存入 localStorage。
14. 不允许 GET endpoint 产生业务写副作用。

本轮允许修改：
<填写允许路径>

本轮禁止修改：
<填写禁止路径>

必须完成：
<填写功能点>

必须新增或更新测试：
<填写测试要求>

必须执行验证：
1. bash scripts/quality-gate-backend.sh
2. bash scripts/quality-gate-frontend.sh
3. OpenAPI export
4. 相关 smoke test

输出：
1. 修改文件清单
2. 新增文件清单
3. 删除文件清单
4. 测试结果
5. 风险说明
6. 下一轮建议
```

---

# 20. 最终完成态定义

一期后补齐计划完成后，WeCMS Next 基础系统后台应达到以下状态：

1. 登录、refresh、logout、me 完整。
2. Refresh token 使用 HttpOnly Cookie。
3. Cookie 型认证接口具备 Origin / CSRF 防护。
4. 用户、角色、权限、菜单、部门、岗位、字典、设置、文件、日志完整。
5. 2FA 与旧系统安全能力等价或更强。
6. 个人中心完整。
7. 安全中心具备查看、封禁、解封、批量解封能力。
8. IP 白名单 / CIDR 匹配统一。
9. 安全封禁中间件生效。
10. 登录失败限制与封禁联动。
11. i18n 管理具备基础后台能力。
12. 菜单排序和字典状态能力完整。
13. 设置敏感配置与 IP 规则安全可靠。
14. 文件上传安全能力不低于旧系统。
15. 所有写操作有明确 HTTP Method。
16. 所有写操作有权限码。
17. 所有写操作有 audit log。
18. 高风险操作有 security event。
19. SecurityEventClassifier 可用。
20. PermissionVersion 权限刷新闭环可用。
21. 安全响应头和 CSP hardening 可用。
22. 后端和前端质量门禁全部通过。
23. OpenAPI 契约稳定。
24. CMS 二期可以在稳定基础系统之上开始。

---

# 21. 推荐 PR 拆分

建议不要把所有补齐项混入一个大 PR。

推荐拆成：

```text
PR-01 docs-closeout-hardening-plan
PR-02 auth-cookie-origin-csrf-protection
PR-03 ip-rule-matcher-and-security-ban
PR-04 security-center-ban-unban
PR-05 write-endpoint-security-gates
PR-06 account-profile-password-avatar
PR-07 two-factor-auth-backend
PR-08 two-factor-auth-frontend
PR-09 i18n-management
PR-10 menu-dict-small-gaps
PR-11 settings-file-security-hardening
PR-12 security-event-rate-limit-permission-version
PR-13 final-acceptance-gate
```

其中 PR-02、PR-03、PR-04、PR-05、PR-07、PR-08 是进入 CMS 二期前最值得优先完成的安全基础能力。
