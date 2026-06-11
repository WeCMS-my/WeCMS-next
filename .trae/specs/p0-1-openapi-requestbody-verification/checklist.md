# P0-1 Verification Checklist

- [x] `patch-openapi-auth-request-bodies.sh` 在 `scripts/checks/` 目录中不存在
- [x] `quality-gate-backend.sh` 不含对 `patch-openapi-auth-request-bodies.sh` 的调用
- [x] `check-openapi-auth-request-bodies.sh` 存在且只做校验（不做 patch）
- [x] `bash scripts/quality-gate-backend.sh` 执行后所有 15 步通过
- [x] 第 6 步 `[6/15] OpenAPI auth request body check` 通过
- [x] Login、Refresh、Logout 三个端点的 requestBody schema 由 typed delegate + DTO 自然生成
- [x] 不存在 OpenAPI artifact 的事后补丁注入流程
