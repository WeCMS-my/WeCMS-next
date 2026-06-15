# ADR-0005：旧系统不做数据迁移，不做兼容模式

## 状态

Accepted

## 背景

当前 ThinkPHP 旧系统仍处于开发阶段，未真实部署使用。旧系统中的用户、角色、权限、菜单、配置、日志、文件等数据均不属于生产业务数据。

因此，新系统不需要承担旧数据迁移、旧密码兼容、旧 token 兼容、旧 2FA secret 迁移等历史包袱。

## 决策

1. 旧系统仅作为业务模块、Schema 和权限模型设计参考。
2. 不迁移旧系统用户、角色、权限、菜单、配置、日志、文件等数据。
3. 新系统从 0 初始化基础种子数据。
4. 新系统不实现 legacy runtime compatibility。
5. 不保留旧密码 hash 登录兼容。
6. 不实现 `password_migrated_at` 登录升级流程。
7. 不迁移旧 token、session、2FA secret、backup code、SMTP 密码、auth_key。
8. `database/legacy-migration` 仅保留 Schema 对照和设计说明，不执行真实数据迁移。

## 影响

### 正向影响

- 降低迁移复杂度。
- 避免 legacy 分支污染新系统运行时代码。
- 简化 Auth 模块。
- 减少旧密码、旧 2FA、旧 secret 的安全风险。

### 代价

- 新系统上线时需要重新创建管理员、角色、权限和基础配置。
- 后续如需要导入旧数据，必须单独建立新的 migration spec。

## M0-BE 调整

M0-BE 保留：

- JIT 后端工程骨架
- MySQL / SqlSugar ORM
- 最小 Auth
- 权限元数据
- OpenAPI 后端契约输出
- seed 初始化
- Schema 对照报告

补充约束：

- 所有数据库访问必须集中在 `WeCms.Persistence`
- `WeCms.Modules.*` 只保留 repository port / 业务抽象，不保留 SQL、ORM Client、数据库连接或持久化实现依赖
- 业务服务只能通过接口 + DI 获取 Repository、UnitOfWork、密码、Token、时钟、随机数等有副作用依赖

M0-BE 移除：

- 真实旧数据迁移
- 旧密码兼容
- 旧 token 兼容
- 旧 2FA secret 迁移


