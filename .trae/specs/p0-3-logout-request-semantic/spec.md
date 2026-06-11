# P0-3: LogoutRequest 语义一致性验证与修复 Spec

## Why
P0-3 是一个待复核项：`LogoutRequest` 的 DTO、handler 空值校验、OpenAPI `requestBody.required` 三者语义必须一致。之前无法通过 GitHub raw/html fetch 获取文件内容确认修复状态，需要本地复核。

## What Changes
- 复核 `LogoutRequest` 的 `RefreshToken` 是否为 `string`（非 `string?`）
- 复核 `/logout` handler 是否有 `string.IsNullOrWhiteSpace` 空值校验
- 复核 OpenAPI `requestBody.required` 是否与 DTO 一致

## Impact
- Affected specs: p0-1-openapi-requestbody-verification（已完成）
- Affected code: 
  - `backend/src/WeCms.Modules.System/Auth/AuthDtos.cs`
  - `backend/src/WeCms.Modules.System/Auth/AuthEndpoints.cs`
  - `backend/src/WeCms.Api/Extensions/AuthEndpointMappings.cs`
  - `artifacts/openapi/wecms-api-v1.json`

## Verification Result

### 判定标准：logout 必须传 refreshToken

应满足以下三点：

| # | 要求 | 当前状态 |
|---|------|----------|
| 1 | `public sealed record LogoutRequest(string RefreshToken);` | 符合 |
| 2 | handler 中有 `string.IsNullOrWhiteSpace(request.RefreshToken)` 空值校验 | 符合 |
| 3 | OpenAPI `requestBody.required: true` | 符合 |

### 详细验证

1. **DTO** (`AuthDtos.cs:13`):
   ```csharp
   public sealed record LogoutRequest(string RefreshToken);
   ```
   `RefreshToken` 为 `string`（不可空），与必须传参语义一致。

2. **Handler** (`AuthEndpoints.cs:56-68`):
   ```csharp
   public async Task LogoutAsync(LogoutRequest request, HttpContext httpContext)
   {
       var cancellationToken = httpContext.RequestAborted;
       if (string.IsNullOrWhiteSpace(request.RefreshToken))
       {
           throw new DomainException(ApiCodes.ValidationError, "刷新令牌不能为空");
       }
       await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
       await WriteJsonResponse(httpContext, ApiResult<object?>.Ok(null), typeof(ApiResult<object?>), cancellationToken);
   }
   ```
   存在 `string.IsNullOrWhiteSpace` 空值校验，fail-fast 抛出 `DomainException`。

3. **OpenAPI** (`artifacts/openapi/wecms-api-v1.json:219-238`):
   ```json
   "requestBody": {
     "required": true,
     "content": {
       "application/json": {
         "schema": {
           "type": "object",
           "properties": {
             "refreshToken": { "type": "string" }
           },
           "required": ["refreshToken"]
         }
       }
     }
   }
   ```
   `requestBody.required: true` 且 schema `required: ["refreshToken"]`，与 DTO 一致。

4. **Endpoint 注册** (`AuthEndpointMappings.cs:29-34`):
   ```csharp
   var logout = (RouteHandlerBuilder)group.MapPost("/logout", static (LogoutRequest request, HttpContext context) =>
       context.RequestServices.GetRequiredService<AuthEndpointHandlers>().LogoutAsync(request, context));
   logout.RequireAuthorization();
   logout.Produces<ApiResult<object?>>(StatusCodes.Status200OK);
   logout.Produces<ApiResult<object?>>(StatusCodes.Status400BadRequest);
   logout.WithName("Auth_Logout");
   ```
   正确使用 `LogoutRequest` 作为路由参数类型，且标记了 `RequireAuthorization()`。

## Conclusion
P0-3 **已修复**，无需代码变更。仅需运行质量门禁确认整体通过。

## REMOVED Requirements
无。
