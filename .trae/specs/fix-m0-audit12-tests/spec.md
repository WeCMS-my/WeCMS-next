# Fix M0 Audit Round 12 — CI/Test Coverage Spec

## Why
审计发现测试覆盖率严重不足：TokenService/AuthService/UserService/RoleService/MenuService/FileService 无测试，集成测试工程为空，quality-gate.sh 不存在。

## What Changes
- T1: TokenService 单元测试（GenerateTokenPair, ValidateAccessToken）
- T1: AuthService 单元测试（LoginAsync 成功/失败, RefreshTokenAsync, LogoutAsync）
- T1: PermissionEndpointFilter 缓存单元测试
- T2: Integration tests 骨架（至少 1 个 endpoint smoke test）
- T3: `scripts/quality-gate.sh`（等价于 quality-gate.ps1）

## ADDED Requirements
### T1 — TokenService 测试
系统 SHALL 有 TokenService 单元测试覆盖 token 生成和验证。

### T1 — AuthService 测试
系统 SHALL 有 AuthService 单元测试覆盖登录成功/失败/刷新/登出。

### T1 — PermissionEndpointFilter 测试
系统 SHALL 有权限过滤器单元测试覆盖缓存命中/未命中。

### T2 — 集成测试骨架
系统 SHALL 有至少 1 个集成测试（如 health check endpoint）。

### T3 — quality-gate.sh
系统 SHALL 有 `scripts/quality-gate.sh` 等价于 quality-gate.ps1。
