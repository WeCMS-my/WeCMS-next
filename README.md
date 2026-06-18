# WeCMS Next

> 从 ThinkPHP CMS 完整迁移重构到 .NET 10 + JIT publish/runtime + SqlSugar ORM + MySQL 的新一代 CMS 系统。

---

## 项目状态

**当前阶段：一期已完成，进入一期后 hardening / 补齐阶段。**

一期已完成 M0-BE 后端底座、M1-BE 系统管理 API、M2-FE 基础系统前端管理端，当前工作重点是安全 hardening、文档治理和一期后补齐项。

当前补齐阶段不回流 CMS 内容管理到一期，不做旧 ThinkPHP runtime compatibility，不做旧数据迁移，不引入 AI runtime，也不复制旧 AdminGate。旧系统 AdminGate / CSRF 中有价值的安全职责必须按新架构拆解到认证、授权、权限码、审计、限流、安全事件、IP 规则和 Cookie 型认证接口防护中。

## 技术栈

| 层 | 技术 |
|---|---|
| 后端 | ASP.NET Core Minimal APIs / .NET 10 / JIT publish/runtime |
| 数据访问实现 | WeCms.Persistence / SqlSugar ORM / MySQL |
| 前端 | SoybeanAdmin / Vue 3 管理端 |
| CI/CD | GitHub Actions |

## 快速开始

### 前置条件

- .NET 10 SDK
- Node.js >= 20.19.0
- pnpm >= 10.5.0
- Docker（用于本地 MySQL 开发环境）
- Bash（用于脚本执行）

### 1. 启动 MySQL

```bash
docker compose up -d mysql
```

### 2. 初始化数据库

```bash
bash scripts/db/reset-dev-db.sh
```

开发环境的 schema、基础权限和 `admin / Admin@123` 账号由后端启动时的 `DbMigrationRunner` 统一创建。
不要在主初始化流程中手工执行 `database/seeds/*.sql`，避免绕过运行时密码 hash 生成。

### 3. 运行后端

```bash
dotnet run --project backend/src/WeCms.Api --launch-profile http
```

### 4. 运行前端

```bash
pnpm --dir frontend/soybean-admin install --frozen-lockfile
pnpm --dir frontend/soybean-admin dev
```

### 5. 验证 API

```bash
curl http://localhost:5207/health/live
curl http://localhost:5207/health/ready
curl http://localhost:5207/api/v1/system/ping
curl http://localhost:5207/api/v1/system/version
curl http://localhost:5207/api/v1/system/db-check
```

验证初始化后的默认管理员可登录：

```bash
bash scripts/smoke-admin-login.sh
```

## 质量门禁

### Backend gate

```bash
bash scripts/quality-gate-backend.sh
```

`quality-gate-backend.sh` 是当前 backend 质量门禁入口。

默认运行 strict 模式。
如遇 NuGet 漏洞索引/缓存权限噪音，可在本地显式设置 `WECMS_NUGET_AUDIT_MODE=fallback` 做诊断；该模式会输出 warning，并且禁止在 CI / GitHub Actions 中使用。

CI（`backend-quality-gate.yml`）直接运行默认 strict gate。

当前 backend quality gate 以 JIT + Persistence 边界治理、系统管理 API 权限码、OpenAPI、seed 和迁移烟测为准。当前重点覆盖以下检查面：

1. `dotnet build -warnaserror`
2. `dotnet test`
3. `dotnet publish`（标准 JIT publish）
4. OpenAPI export
5. OpenAPI auth request body check
6. DB boundary / layer dependency / DI boundary 检查
7. 系统管理 OpenAPI / 权限覆盖检查
8. locked role seed / no SQL in modules 检查
9. generated test artifact / code-review 静态规则检查
10. migration / seed smoke test

### Frontend gate

```bash
bash scripts/quality-gate-frontend.sh
```

`quality-gate-frontend.sh` 是当前 SoybeanAdmin 前端质量门禁入口，覆盖：

1. `pnpm install --frozen-lockfile`
2. `pnpm lint`
3. `pnpm typecheck`
4. `pnpm build`
5. Vite proxy config test
6. no CMS frontend / generated contract / route permission / smoke fixture checks

## 项目结构

```text
backend/
  src/
    WeCms.Api/              # Host 层
    WeCms.Shared/            # 共享契约
    WeCms.Infrastructure/    # 非数据库基础设施适配
    WeCms.Persistence/       # 数据访问实现适配器层（SqlSugar ORM / MySQL）
    WeCms.Modules.System/    # 系统管理模块
    WeCms.Modules.Cms/       # CMS 内容模块
  tests/
    WeCms.Tests.Unit/
    WeCms.Tests.Integration/
    WeCms.Tests.Architecture/

database/
  migrations/                # 数据库迁移
  seeds/                     # 种子数据
  legacy-migration/          # 旧系统 Schema 对照（不执行迁移）

scripts/
  quality-gate-backend.sh    # 后端质量门禁
  quality-gate-frontend.sh   # 前端质量门禁
  checks/                    # 代码质量检查
  db/                        # 数据库操作脚本

docs/
  adr/                       # 架构决策记录
  context/                   # 项目上下文文档

artifacts/
  openapi/                   # OpenAPI 契约产物
  reports/                   # 报告
```

## 架构决策记录 (ADR)

| 编号 | 标题 | 状态 |
|---|---|---|
| [ADR-0005](docs/adr/0005-no-legacy-data-migration-and-frontend-deferred.md) | 旧系统不做数据迁移，不做兼容模式 | Accepted |
| [ADR-0007](docs/adr/0007-frontend-deferred-after-backend-complete.md) | 前端后移，后端 API 全部完成后再开发 | Accepted |
| [ADR-0009](docs/adr/0009-runtime-baseline-jit.md) | 运行时基线从 Native AOT 切换为 JIT | Accepted |
| [ADR-0010](docs/adr/0010-rebuild-from-zero.md) | M0-BE 从 0 重建后端工程 | Accepted |
| [ADR-0011](docs/adr/0011-jit-sqlsugar-persistence.md) | M0-BE JIT + SqlSugar Persistence 边界 | Accepted |
| [ADR-0012](docs/adr/0012-m0-be-frontend-deferred.md) | M0-BE 前端后移与 backend-only 边界 | Accepted |
| [ADR-0013](docs/adr/0013-m1-system-management-api-scope.md) | M1-BE 系统管理 API backend-only 边界 | Accepted |
| [ADR-0014](docs/adr/0014-refresh-token-storage-m2-fe.md) | 认证 token 存储最终基线：refresh cookie + access token memory | Accepted |
| [ADR-0016](docs/adr/0016-admingate-csrf-migration-strategy.md) | AdminGate / CSRF 迁移拆解策略 | Accepted |

## 核心原则

- 后端契约优先。
- 当前运行时基线为 `.NET 10 JIT publish/runtime`，不再将 Native AOT 作为现行门禁。
- WeCms.Persistence 是 SqlSugar ORM / MySQL 的唯一数据访问实现适配器层，不是传统 DAL；业务模块只依赖抽象。
- 所有数据库访问只能发生在 WeCms.Persistence；WeCms.Modules.* 不得持有 SQL、ORM Client、连接器或持久化实现依赖。
- 业务代码中的 Repository、UnitOfWork、时钟、Token、密码、随机数等有副作用依赖必须通过接口 + DI 获取。
- Refresh token 当前基线为 `HttpOnly; Secure; SameSite` Cookie，access token 仅保存在前端内存。
- 系统管理业务 Endpoint 必须绑定权限码，所有写操作必须记录审计。
- 高风险操作必须补充 Security Event，必要时要求当前密码、2FA 或 challenge。
- 旧系统不做数据迁移，不做兼容模式。
- CMS 内容管理、AI runtime、旧 ThinkPHP runtime compatibility 均不属于一期后补齐范围。
- 旧 AdminGate 不复制；相关职责必须拆解到新系统独立组件中。
- AI 接入是二期独立项目，一期不实现运行时 AI 功能。

## 文档

- [AGENTS.md](AGENTS.md) — AI 协作者项目级指令
- [code_review.md](code_review.md) — 代码审查基线
- [完整迁移重构计划 v3.0](docs/context/WeCMS%20Next%20%E5%AE%8C%E6%95%B4%E8%BF%81%E7%A7%BB%E9%87%8D%E6%9E%84%E8%AE%A1%E5%88%92%20v3.0.md)
- [M0-BE 后端-only 开发计划](docs/context/WeCMS%20Next%20M0-BE%20%E5%90%8E%E7%AB%AF-only%20%E5%BC%80%E5%8F%91%E8%AE%A1%E5%88%92.md)
- [M1-BE 后端-only 开发计划](docs/context/WeCMS%20Next%20M1-BE%20%E5%90%8E%E7%AB%AF-only%20%E5%BC%80%E5%8F%91%E8%AE%A1%E5%88%92%E4%B9%A6%20v1.0.md)
- [M1-BE 稳定入口](docs/context/WeCMS_Next_M1-BE_系统管理API开发计划.md)
- [一期完成状态说明](docs/context/WeCMS_Next_一期完成状态说明.md)
- [一期后补齐计划书](docs/context/WeCMS_Next_一期后补齐计划书.md)
- [AdminGate / CSRF 迁移设计说明](docs/context/WeCMS_Next_AdminGate_CSRF_迁移设计说明.md)
- [工程骨架验证文档](docs/context/WeCMS_工程骨架验证文档.md)
- [完整迁移重构计划（历史命名路径）](docs/context/WeCMS_Next_NET10_AOT_SoybeanAdmin_完整迁移重构计划.md)

## 许可证

Proprietary
