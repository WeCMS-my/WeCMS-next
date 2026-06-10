# Checklist

- [x] `MySqlConnector` NuGet 包已添加
- [x] `Dapper` NuGet 包已添加
- [x] `Dapper.AOT` NuGet 包已添加
- [x] `IDbConnectionFactory` 接口已创建
- [x] `DbConnectionFactory` 实现已创建，从配置读取连接字符串
- [x] `IUnitOfWork` 接口已创建
- [x] `UnitOfWork` 实现已创建（事务 begin/commit/rollback + 连接释放）
- [x] `SystemClock` 实现 `IClock`，返回 `DateTimeOffset.UtcNow`
- [x] `DapperDataExtensions.AddWeCmsData()` 扩展方法已创建
- [x] 代码中无 `Query<dynamic>` 调用
- [x] 代码中无 `SELECT *`
- [x] 数据访问方法均接收 `CancellationToken`
- [x] `dotnet build backend/WeCms.slnx -warnaserror` 通过
- [x] `dotnet test backend/WeCms.slnx` 单元测试 28/28 通过
- [x] `dotnet publish ... /p:PublishAot=true` 通过 (9.7 MB exe)
