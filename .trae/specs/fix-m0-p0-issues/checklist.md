# Checklist

- [x] Dapper.AOT NuGet 包已添加到 WeCms.Api.csproj、WeCms.Infrastructure.csproj、WeCms.Modules.System.csproj
- [x] `[module: DapperAot]` 声明已添加到 WeCms.Infrastructure/GlobalUsings.cs
- [x] WeCmsJsonContext.cs 包含所有 Endpoint 使用的请求/响应 DTO 的 `[JsonSerializable]` 属性
- [x] PermissionEndpoints.ListAsync 使用 `QueryAsync<PermissionItem>` 强类型查询
- [x] Program.cs 中 `app.UseRateLimiter()` 已添加（在 ExceptionMiddleware 之后、UseAuthentication 之前）
- [x] LoginResponse 中 AccessToken/RefreshToken 改为 nullable，新增 TwoFactorTicket 字段
- [x] AuthService.LoginAsync 在 2FA 启用时不签发 token，改为生成 twoFactorTicket
- [x] TwoFactorEndpoints.VerifyAsync 支持 twoFactorTicket 参数，验证通过后签发 token
- [x] TwoFactorVerifyRequest 新增 TwoFactorTicket 字段
- [x] 前端 generated/types.ts 中 LoginResponse 类型已同步更新
- [x] `dotnet build backend/WeCms.sln -warnaserror` 通过
- [x] `dotnet test backend/WeCms.sln` 通过
