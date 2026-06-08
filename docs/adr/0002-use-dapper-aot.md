 # ADR-0002: Use Dapper + Dapper.AOT
 
 > Status: accepted  
 > Date: 2026-06-08
 
 ## Context
 数据访问层需要选择 ORM 或微 ORM。EF Core 是 .NET 生态中最流行的 ORM，但与 Native AOT 兼容性有限。
 
 ## Decision
 使用 Dapper + Dapper.AOT 作为数据访问层，搭配 MySqlConnector。
 
 ## Rationale
 - Dapper 是轻量级微 ORM，不依赖运行时反射和代码生成
 - Dapper.AOT 提供编译时 SQL 分析和强类型映射
 - MySqlConnector 是纯 .NET MySQL 驱动，AOT 兼容
 - 手写 SQL 提供完全的性能控制和查询可见性
 - 避免 EF Core 的变更追踪、延迟加载等不可 AOT 特性
 
 ## Consequences
 - 禁止 Query<dynamic>、SELECT *、SQL 拼接
 - SQL 必须显式列出字段
 - 排序字段必须后端白名单映射
 - 所有 Repository 方法必须支持 CancellationToken
 - 分页参数必须后端校验，pageSize ≤ 100
 - 必须使用 [module: DapperAot] 启用编译时分析
