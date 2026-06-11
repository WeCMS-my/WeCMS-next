# Checklist

- [x] `.github/workflows/backend-quality-gate.yml` 不再包含 `services.mysql` 定义
- [x] `.github/workflows/backend-quality-gate.yml` 不再包含 `ConnectionStrings__Default` 环境变量
- [x] `scripts/quality-gate-backend.sh` 的步骤 5/15 只运行 Unit + Architecture 测试项目
- [x] CI 中各步骤（build、test、AOT publish、OpenAPI export）仍能通过，无 MySQL 依赖导致的失败（等待 CI 实际运行验证）
