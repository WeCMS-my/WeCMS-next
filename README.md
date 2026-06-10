# WeCMS Next

> 从 ThinkPHP CMS 完整迁移重构到 .NET 10 + Native AOT + Dapper.AOT + SoybeanAdmin 的新一代 CMS 系统。

---

## 项目状态

**当前阶段：M0-BE（后端-only 工程底座重建）**

M0-BE 只交付一个可信后端底座，不操作 SoybeanAdmin 前端代码。

## 技术栈

| 层 | 技术 |
|---|---|
| 后端 | ASP.NET Core Minimal APIs / .NET 10 / Native AOT Only |
| 数据访问 | Dapper / Dapper.AOT / MySQL |
| 前端 | SoybeanAdmin（后移，M0-BE 不开发） |
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
bash scripts/db/apply-migrations.sh
bash scripts/db/seed-dev.sh
```

### 3. 运行后端

```bash
dotnet run --project backend/src/WeCms.Api
```

### 4. 验证 API

```bash
curl http://localhost:5000/health/live
curl http://localhost:5000/health/ready
curl http://localhost:5000/api/v1/system/ping
curl http://localhost:5000/api/v1/system/version
curl http://localhost:5000/api/v1/system/db-check
```

## M0-BE 质量门禁

```bash
bash scripts/quality-gate-backend.sh
```

门禁包含 7 步检查：

1. `dotnet build -warnaserror`
2. `dotnet test`
3. `dotnet publish (Native AOT)`
4. OpenAPI export
5. 无 `SELECT *` 检查
6. 无 `Query<dynamic>` 检查
7. 完整性检查（权限、JSON Context、前端变更）

## 项目结构

```text
backend/
  src/
    WeCms.Api/              # Host 层
    WeCms.Shared/            # 共享契约
    WeCms.Infrastructure/    # 基础设施
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
| [ADR-0006](docs/adr/0006-aot-trim-warnings-exception.md) | Native AOT / Trim 警告例外管理 | Accepted |
| [ADR-0007](docs/adr/0007-frontend-deferred-after-backend-complete.md) | 前端后移，后端 API 全部完成后再开发 | Accepted |

## 核心原则

- 后端契约优先。
- Native AOT 从第一天进入门禁。
- Dapper.AOT 从第一天进入数据访问规范。
- 前端 SoybeanAdmin 不参与 M0-BE。
- 旧系统不做数据迁移，不做兼容模式。
- AI 接入是二期独立项目，一期不实现运行时 AI 功能。

## 文档

- [AGENTS.md](AGENTS.md) — AI 协作者项目级指令
- [code_review.md](code_review.md) — 代码审查基线
- [M0-BE 开发计划](docs/context/WeCMS_Next_M0-BE_后端-only_开发计划.md)
- [工程骨架验证文档](docs/context/WeCMS_工程骨架验证文档.md)
- [完整迁移重构计划](docs/context/WeCMS_Next_NET10_AOT_SoybeanAdmin_完整迁移重构计划.md)

## 许可证

Proprietary
