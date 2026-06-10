# Checklist

- [x] RequestIdMiddleware 已创建在 `backend/src/WeCms.Api/Middleware/RequestIdMiddleware.cs`
- [x] ExceptionMiddleware 已创建在 `backend/src/WeCms.Api/Middleware/ExceptionMiddleware.cs`
- [x] Program.cs 中 RequestId 中间件在 Exception 中间件之前注册
- [x] 正常 Endpoint 请求不被中间件改变
- [x] 抛出 DomainException 时返回对应 code 和 msg
- [x] 抛出未处理异常时 HTTP 状态码 = 500
- [x] 500 响应 body 中不包含异常 Message、堆栈、SQL、连接串、物理路径
- [x] 所有错误响应包含 traceId
- [x] 响应头 `X-Trace-Id` 存在
- [x] `dotnet build backend/WeCms.slnx -warnaserror` 通过
- [x] `dotnet test backend/WeCms.slnx` 全部通过
