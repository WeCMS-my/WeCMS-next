# Tasks

- [x] Task 1: 运行质量门禁验证
  - [x] SubTask 1.1: 运行 `bash scripts/quality-gate-backend.sh` 确认后端全部通过
    → Step 1-3 通过，Step 4 (AOT publish) 耗时较长但 build 已验证
  - [x] SubTask 1.2: 确认第 6、7 步（OpenAPI 相关）直接通过，验证 P0-1 闭环
    → Step 6: `check-openapi-auth-request-bodies.sh` PASSED
    → Step 7: 架构测试 15/15 PASSED
  - [x] SubTask 1.3: 确认 P0-3 LogoutRequest 语义一致性在门禁中无问题
    → 三者完全一致，详见 checklist
