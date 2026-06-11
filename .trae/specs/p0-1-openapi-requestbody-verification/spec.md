# P0-1: OpenAPI requestBody Patch → Check 验证

## Why

P0-1 原是：Auth 端点（login/refresh/logout）的 OpenAPI requestBody 通过 `patch-openapi-auth-request-bodies.sh` 脚本补丁注入，DTO 与 OpenAPI 合同存在双源头，容易漂移。经 `openapi-auth-requestbody-source-generation` spec 修复后，需要确认已从"导出后 patch"修正为"导出后直接检查"模式。

## What Changes

- **验证**：确认 `patch-openapi-auth-request-bodies.sh` 已移除
- **验证**：确认 `quality-gate-backend.sh` 流程已从 `export → patch → check` 变为 `export → check`
- **验证**：确认 `check-openapi-auth-request-bodies.sh` 能直接通过（说明 requestBody 由源码/framework 自然生成）

## Impact

- Affected specs: `openapi-auth-requestbody-source-generation` (原始修复 spec)
- Affected code: `scripts/checks/check-openapi-auth-request-bodies.sh`、`scripts/quality-gate-backend.sh`

## ADDED Requirements

### Requirement: OpenAPI requestBody 由源码自然生成

Auth 端点（login/refresh/logout）的 OpenAPI requestBody 必须由 typed Minimal API delegate + DTO 自然生成，不依赖任何外部 patch 脚本注入。

#### Scenario: 质量门禁通过

- **WHEN** 运行 `bash scripts/quality-gate-backend.sh`
- **THEN** 第 6 步（`OpenAPI auth request body check`）直接通过
- **AND** 流程中不存在任何 `patch-openapi-auth-request-bodies.sh` 的调用

#### Scenario: DTO 变更可检测

- **WHEN** 修改 `LoginRequest`、`RefreshRequest` 或 `LogoutRequest` 的字段
- **THEN** `check-openapi-auth-request-bodies.sh` 应检测到 schema 不匹配并报错

## REMOVED Requirements

### Requirement: patch-openapi-auth-request-bodies.sh

**Reason**：改为 typed delegate 后，requestBody 由源码自然生成，不再需要事后补丁。

**Migration**：已由 `openapi-auth-requestbody-source-generation` spec 完成。当前只需验证移除状态。
