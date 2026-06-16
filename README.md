# WeCMS Next

> 从 ThinkPHP CMS 完整迁移重构到 .NET 10 + JIT publish/runtime + SqlSugar ORM + MySQL 的新一代 CMS 系统。

---

## 项目状态

**当前阶段：M1-BE（后端-only 系统管理 API）**

M1-BE 基于 M0-BE 后端底座继续交付系统管理 API，不操作 SoybeanAdmin 前端代码，不生成前端类型，不进入 CMS 内容 API。

## 技术栈

| 层 | 技术 |
|---|---|
| 后端 | ASP.NET Core Minimal APIs / .NET 10 / JIT publish/runtime |
| 数据访问实现 | WeCms.Persistence / SqlSugar ORM / MySQL |
| 前端 | SoybeanAdmin（后移，M1-BE 不开发） |
| CI/CD | GitHub Actions |

## 快速开始

### 前置条件

- .NET 10 SDK
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

### 4. 验证 API

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

## Backend 质量门禁

```bash
bash scripts/quality-gate-backend.sh
```

`quality-gate-backend.sh` 是当前唯一的 backend 质量门禁入口。

默认运行 strict 模式。
如遇 NuGet 漏洞索引/缓存权限噪音，可在本地显式设置 `WECMS_NUGET_AUDIT_MODE=fallback` 做诊断；该模式会输出 warning，并且禁止在 CI / GitHub Actions 中使用。

CI（`backend-quality-gate.yml`）直接运行默认 strict gate。

当前文档基线下，backend quality gate 以 JIT + Persistence 边界治理为准；M1-BE 会继续扩展系统管理 API 的权限码、OpenAPI 和 seed 覆盖检查。当前重点覆盖以下检查面：

1. `dotnet build -warnaserror`
2. `dotnet test`
3. `dotnet publish`（标准 JIT publish）
4. OpenAPI export
5. OpenAPI auth request body check
6. DB boundary / layer dependency / DI boundary 检查
7. 无前端变更完整性检查
8. code-review 静态规则检查

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

## 核心原则

- 后端契约优先。
- 当前运行时基线为 `.NET 10 JIT publish/runtime`，不再将 Native AOT 作为现行门禁。
- WeCms.Persistence 是 SqlSugar ORM / MySQL 的唯一数据访问实现适配器层，不是传统 DAL；业务模块只依赖抽象。
- 所有数据库访问只能发生在 WeCms.Persistence；WeCms.Modules.* 不得持有 SQL、ORM Client、连接器或持久化实现依赖。
- 业务代码中的 Repository、UnitOfWork、时钟、Token、密码、随机数等有副作用依赖必须通过接口 + DI 获取。
- 前端 SoybeanAdmin 不参与 M1-BE。
- M1-BE 只做系统管理 API，不做 CMS 内容 API、旧系统迁移或 AI runtime。
- M1-BE 所有业务 Endpoint 必须绑定权限码，所有写操作必须记录审计。
- 旧系统不做数据迁移，不做兼容模式。
- AI 接入是二期独立项目，一期不实现运行时 AI 功能。

## 文档

- [AGENTS.md](AGENTS.md) — AI 协作者项目级指令
- [code_review.md](code_review.md) — 代码审查基线
- [完整迁移重构计划 v3.0](docs/context/WeCMS%20Next%20%E5%AE%8C%E6%95%B4%E8%BF%81%E7%A7%BB%E9%87%8D%E6%9E%84%E8%AE%A1%E5%88%92%20v3.0.md)
- [M0-BE 后端-only 开发计划](docs/context/WeCMS%20Next%20M0-BE%20%E5%90%8E%E7%AB%AF-only%20%E5%BC%80%E5%8F%91%E8%AE%A1%E5%88%92.md)
- [M1-BE 后端-only 开发计划](docs/context/WeCMS%20Next%20M1-BE%20%E5%90%8E%E7%AB%AF-only%20%E5%BC%80%E5%8F%91%E8%AE%A1%E5%88%92%E4%B9%A6%20v1.0.md)
- [M1-BE 稳定入口](docs/context/WeCMS_Next_M1-BE_系统管理API开发计划.md)
- [工程骨架验证文档](docs/context/WeCMS_工程骨架验证文档.md)
- [完整迁移重构计划（历史命名路径）](docs/context/WeCMS_Next_NET10_AOT_SoybeanAdmin_完整迁移重构计划.md)

## 许可证

Proprietary
