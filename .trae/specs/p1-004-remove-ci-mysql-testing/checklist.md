# Checklist

- [x] `.github/workflows/backend-quality-gate.yml` 不再包含 `services.mysql` 定义
- [x] `.github/workflows/backend-quality-gate.yml` 不再包含真实 MySQL 连接字符串
- [x] `.github/workflows/backend-quality-gate.yml` 保留 dummy `ConnectionStrings__Default`（`User=none;Password=none;Database=none`），用于通过 `DbConnectionFactory` 构造函数
- [x] `scripts/quality-gate-backend.sh` 的步骤 5/15 只运行 Unit + Architecture 测试项目
- [x] CI 中各步骤（build、test、AOT publish、OpenAPI export）不再依赖实际 MySQL 连接
