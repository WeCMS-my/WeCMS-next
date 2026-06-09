# Tasks

- [x] Task 1: 安装 Dapper.AOT 包并声明模块
  - [x] 在 `WeCms.Api.csproj`、`WeCms.Infrastructure.csproj`、`WeCms.Modules.System.csproj` 中添加 `Dapper.AOT` PackageReference
  - [x] 在 `WeCms.Infrastructure/GlobalUsings.cs` 中添加 `[module: DapperAot]` 声明
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 2: 补全 WeCmsJsonContext 缺失的 DTO 类型
  - [x] 列出所有 Endpoint 使用的请求/响应 DTO 类型
  - [x] 在 `WeCmsJsonContext.cs` 中添加所有缺失的 `[JsonSerializable]` 属性
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 3: 修复 PermissionEndpoints.ListAsync 的 dynamic 查询
  - [x] 定义 `PermissionItem` record 类型
  - [x] 将 `QueryAsync` 改为 `QueryAsync<PermissionItem>`
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 4: 添加 app.UseRateLimiter() 调用
  - [x] 在 `Program.cs` 的 `app.UseCors(...)` 之后添加 `app.UseRateLimiter()`
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

- [x] Task 5: 修复 2FA 认证绕过
  - [x] 修改 `LoginResponse`：`AccessToken`/`RefreshToken` 改为 nullable，新增 `TwoFactorTicket` 字段
  - [x] 修改 `AuthService.LoginAsync`：2FA 启用时生成 `twoFactorTicket` 存入临时存储，不签发 token
  - [x] 修改 `TwoFactorEndpoints.VerifyAsync`：支持 `twoFactorTicket` 参数，验证通过后签发 token
  - [x] 修改 `TwoFactorVerifyRequest`：新增 `TwoFactorTicket` 字段
  - [x] 更新 `WeCmsJsonContext` 中相关类型
  - [x] 更新前端 `generated/types.ts` 中 `LoginResponse` 类型
  - [x] 验证：`dotnet build backend/WeCms.sln -warnaserror` 通过

# Task Dependencies
- Task 2 依赖 Task 3（PermissionItem 类型需在 JsonContext 中注册）
- Task 5 依赖 Task 2（新的 DTO 类型需在 JsonContext 中注册）
- Task 1、Task 4 无依赖，可并行执行
