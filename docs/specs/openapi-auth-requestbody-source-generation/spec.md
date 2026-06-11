# OpenAPI Auth RequestBody Source Generation

## 背景

当前 `Auth` 端点的 `login`、`refresh`、`logout` 依赖 `RequestDelegate` 包裹，OpenAPI requestBody 通过 `scripts/checks/patch-openapi-auth-request-bodies.sh` 进行补丁注入。这导致 DTO 与 OpenAPI 合同存在双源头，容易漂移。

## 目标

将 `Auth` 端点改为 typed minimal API delegate，让 OpenAPI requestBody 由源代码自然生成，不再依赖 bash patch。

## 范围

- `login`、`refresh`、`logout` 端点改为 typed delegate。
- 移除 OpenAPI patch 脚本和质量门禁中的 patch 步骤。
- 增加 OpenAPI contract test，校验 requestBody schema 与 DTO 保持一致。
- 保留现有 AOT、统一响应和权限行为。

## 非目标

- 不改动认证业务规则。
- 不引入兼容模式或运行时 fallback。
- 不调整前端代码。

## 验收标准

1. `LoginRequest`、`RefreshRequest`、`LogoutRequest` 改字段后，OpenAPI contract test 必须失败。
2. OpenAPI artifact 不再依赖 patch 注入 requestBody。
3. requestBody 的 required 标记和字段集合与 DTO 保持一致。
4. 质量门禁中不再出现 OpenAPI patch 步骤。
