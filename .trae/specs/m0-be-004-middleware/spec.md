# M0-BE-004: 实现 Middleware Spec

## Why
M0-BE 需要统一的异常处理和请求追踪机制。当前裸 Minimal API 在异常时直接暴露 Kestrel 默认错误页（含堆栈），且无 traceId 无法关联前后端日志。

## What Changes
- 新增 `RequestIdMiddleware`：每个请求注入/生成 `traceId`，写入响应头 `X-Trace-Id`
- 新增 `ExceptionMiddleware`：捕获未处理异常，映射为统一 `ApiResult` 错误响应
- 注册到 `Program.cs` 的 Middleware pipeline
- 新增测试验证 500 不泄露堆栈、traceId 贯穿、DomainException 映射正确

## Impact
- Affected specs: M0-BE-003 (Shared 契约层，已实现 ApiResult/DomainException/ApiCodes)
- Affected code: `backend/src/WeCms.Api/Program.cs`, `backend/src/WeCms.Api/Middleware/*`

## ADDED Requirements

### Requirement: RequestIdMiddleware
系统 SHALL 为每个 HTTP 请求注入唯一的 `traceId`。

#### Scenario: 有 traceId 标记请求
- **GIVEN** 客户端请求
- **WHEN** RequestIdMiddleware 处理请求
- **THEN** 响应头包含 `X-Trace-Id`
- **AND** 可通过 `HttpContext.TraceIdentifier` 读取

#### Scenario: 异常响应也含 traceId
- **GIVEN** 请求触发未处理异常
- **WHEN** ExceptionMiddleware 返回 500
- **THEN** 响应 body 中 `traceId` 字段非空
- **AND** 响应头 `X-Trace-Id` 存在

### Requirement: ExceptionMiddleware — 统一异常处理
系统 SHALL 捕获所有未处理异常并返回统一 `ApiResult` 结构。

#### Scenario: DomainException → 业务错误
- **GIVEN** Endpoint 抛出 `DomainException(2001, "业务错误")`
- **WHEN** ExceptionMiddleware 捕获
- **THEN** HTTP 状态码 200（业务错误不是 HTTP 错误）
- **AND** 响应 body `code` = 2001, `msg` = "业务错误"
- **AND** `data` = null

#### Scenario: 未处理异常 → 500
- **GIVEN** Endpoint 抛出 `InvalidOperationException`
- **WHEN** ExceptionMiddleware 捕获
- **THEN** HTTP 状态码 500
- **AND** 响应 body `code` = 5000 (ApiCodes.SystemError)
- **AND** `msg` = "系统内部错误"（不包含异常 Message）
- **AND** `data` = null
- **AND** 响应 body 不含堆栈、SQL、连接串、物理路径

#### Scenario: 正常响应不变
- **GIVEN** Endpoint 正常返回
- **WHEN** 经过 ExceptionMiddleware
- **THEN** 响应内容完全不变
- **AND** HTTP 状态码保持原始值

### Requirement: Middleware 注册
系统 SHALL 在 Program.cs 中按正确顺序注册中间件：
1. `UseRequestIdMiddleware()`
2. `UseExceptionMiddleware()`

## REMOVED Requirements
无。
