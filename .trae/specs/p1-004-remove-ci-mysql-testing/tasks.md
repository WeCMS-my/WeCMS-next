# Tasks

- [x] Task 1: 从 CI workflow 移除 MySQL 服务容器
  - [x] 删除 `.github/workflows/backend-quality-gate.yml` 中 `services.mysql` 块（第 29-41 行）
  - [x] 删除 `.github/workflows/backend-quality-gate.yml` 中 `ConnectionStrings__Default` 环境变量（第 72-74 行）

- [x] Task 2: 修改质量门禁脚本，排除集成测试
  - [x] 将 `scripts/quality-gate-backend.sh` 第 167 行的 `dotnet test backend/WeCms.slnx` 改为只运行 Unit + Architecture 测试项目

# Task Dependencies
- 无依赖关系，Task 1 和 Task 2 可并行执行。
