# Remove MySQL Testing from CI Spec

## Why
CI 工作流中引入了 MySQL 服务容器为集成测试提供数据库，但这带来了几个问题：
1. MySQL 服务增加了 CI 运行时间和资源消耗。
2. 连接字符串配置容易出错（见 p1-003-fix-ci-mysql-connection-string）。
3. 架构测试和单元测试不依赖 MySQL，只有集成测试需要。
4. 集成测试应由开发者在本地有 MySQL 环境时运行，CI 只需验证构建、单元测试、架构约束和 AOT 发布。

## What Changes
- 从 `backend-quality-gate.yml` 移除 MySQL 服务容器定义。
- 从 `backend-quality-gate.yml` 移除 `ConnectionStrings__Default` 环境变量。
- 修改 `quality-gate-backend.sh` 中步骤 5/15 的 `dotnet test` 命令，排除需要 MySQL 的集成测试，只运行 Unit + Architecture 测试。

## Impact
- Affected specs: none
- Affected code:
  - `.github/workflows/backend-quality-gate.yml` — 移除 MySQL 服务和连接字符串配置
  - `scripts/quality-gate-backend.sh` — 修改 dotnet test 命令过滤掉集成测试

## MODIFIED Requirements
### Requirement: CI Quality Gate
CI 质量门禁 SHALL 验证构建、单元测试、架构约束、AOT 发布，但不再运行需要外部数据库的集成测试。

#### Scenario: CI 运行质量门禁
- **WHEN** 后端质量门禁在 CI 中运行
- **THEN** MySQL 服务容器不启动
- **AND** `dotnet test` 只运行 Unit 和 Architecture 测试项目，不运行 Integration 测试
- **AND** 构建、AOT 发布、OpenAPI 导出等步骤保持正常

### Requirement: Local Integration Testing
集成测试应由开发者在本地运行，需要本地 MySQL 环境。
