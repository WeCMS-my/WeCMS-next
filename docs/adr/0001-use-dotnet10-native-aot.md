 # ADR-0001: Use .NET 10 Native AOT
 
 > Status: accepted  
 > Date: 2026-06-08
 
 ## Context
 WeCMS Next 是从 ThinkPHP 到 .NET 的完整重构项目。需要选择 .NET 运行时和编译模式。
 
 ## Decision
 使用 .NET 10 + Native AOT Only 编译发布。
 
 ## Rationale
 - 更快的启动时间（冷启动 < 100ms）
 - 更小的部署体积（单个可执行文件）
 - 更低的内存占用
 - .NET 10 是当前最新 LTS-adjacent 版本
 - Native AOT 约束迫使团队从第一天就避免反射、动态代码生成等不可 AOT 的模式
 
 ## Consequences
 - 必须使用 CreateSlimBuilder
 - 禁止 MVC Controller、Razor、反射式序列化
 - 所有 DTO 必须加入 JsonSerializerContext
 - 第三方 NuGet 包必须在引入前验证 AOT 兼容性
 - 禁止 runtime code generation、动态代理、运行时 Endpoint 扫描
