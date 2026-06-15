# WeCMS Next M0-BE 后端开发计划

> 技术栈：ASP.NET Core Minimal APIs / .NET 10 / JIT publish/runtime / SqlSugar ORM / MySQL

---

## 1. 文档目标

本文件用于约束后端-only 开发阶段的实现顺序、边界和验收要求。

当前主线：

- 保留 Minimal API
- 保留 `CreateSlimBuilder`
- 保留 SqlSugar ORM
- 把发布基线统一为 JIT publish/runtime

---

## 2. M0-BE 范围

```text
Minimal API Host
统一结果模型
异常处理中间件
Json serializer context
Health endpoint
Persistence / SqlSugar ORM
OpenAPI 导出
最小 Auth
权限元数据
后端验证脚本
```

---

## 3. 核心约束

1. Minimal API Only
2. `CreateSlimBuilder`
3. JIT publish/runtime
4. SqlSugar 只允许在 `WeCms.Persistence`
5. `WeCms.Modules.*` 禁止 SQL 文本与 ORM 直连
6. 禁止 `dynamic`
7. 禁止 `SELECT *`
8. OpenAPI 是前端类型来源

---

## 4. 推荐执行顺序

### M0-BE-001：建立 Host

- 创建 `WeCms.Api`
- 配置 `CreateSlimBuilder`
- 注册基础中间件和依赖

### M0-BE-002：建立契约与序列化

- `ApiResult`
- `PagedResult`
- `ApiCodes`
- `JsonSerializerContext`

### M0-BE-003：建立 Persistence / SqlSugar

- `WeCms.Persistence`
- 数据库配置
- repository port / adapter
- 边界检查

### M0-BE-004：建立 OpenAPI 与基础验证

- OpenAPI artifact
- 契约测试
- 质量门禁脚本

### M0-BE-005：建立最小 Auth / 权限元数据

- 登录
- 刷新
- 退出
- `/auth/me`
- 权限过滤器

---

## 5. 验收命令

```bash
dotnet build backend/WeCms.sln -warnaserror
dotnet test backend/WeCms.sln
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false
```

---

## 6. 验收标准

```text
[ ] Host 可启动
[ ] Endpoint 显式注册
[ ] DTO 已进入 JsonSerializerContext
[ ] SqlSugar ORM 查询成功
[ ] `WeCms.Persistence` 是唯一数据库适配层
[ ] OpenAPI 可生成
[ ] build / test / publish 成功
```

---

## 7. AI 协作要求

1. 先读规则与 context 文档。
2. ≥ 200 行变更或公共契约变更必须先建 spec。
3. 触碰 `.cs` 生产代码时遵循 TDD。
4. 未运行门禁不得宣称完成。
5. 不得把本次运行时基线切换扩大成 Controller 架构迁移。

