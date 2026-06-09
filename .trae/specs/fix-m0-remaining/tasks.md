# Tasks

## HIGH（4）

- [x] Task 1: #13 RefreshR SQL 注释 + #19 IsDescendant 跳过
  - [x] AuthService.cs RefreshR SQL 添加注释
  - [x] MenuService.cs CreateAsync 移除无意义的 IsDescendant 调用
  - [x] 验证：`dotnet build -warnaserror`

- [x] Task 2: #17 Menu COALESCE + #18 BuildTree O(N)
  - [x] MenuService UpdateAsync：添加 COALESCE 设计注释
  - [x] BuildTree 改为 ILookup 单次遍历 O(N)
  - [x] 验证：`dotnet build -warnaserror`

## MEDIUM（8）

- [x] Task 3: #26 前端登出清理 + #27 登录页 2FA
  - [x] auth.ts logout：清理动态路由（保留 login/dashboard）
  - [x] login/index.vue：检测 requiresTwoFactor → 存 ticket → 跳转 /login/2fa
  - [x] 新增 views/login/2fa.vue 2FA 验证页面
  - [x] 新增 /login/2fa 路由
  - [x] 验证：后端 build 通过

- [x] Task 4: #29 row_version + #33 IClock + #34 Permissions + #35 Setting
  - [x] UserService/RoleService/MenuService UPDATE 加 row_version 递增
  - [x] AuthService：注入 IClock，替换 16 处 DateTime.UtcNow
  - [x] PermissionSyncService：补全 15 个权限码
  - [x] SettingDtos：移除 UpdateSettingRequest.Key
  - [x] TwoFactorServiceTests：适配 IClock 构造函数
  - [x] 验证：`dotnet build -warnaserror`

- [x] Task 5: #37 ApiCodes 命名空间 + #38 Health ApiResult
  - [x] Permissions.cs 移到 WeCms.Shared.Security
  - [x] SystemEndpoints /health 端点使用 ApiResult + HealthLiveResponse/HealthReadyResponse
  - [x] CommonResponses.cs 新增 HealthLiveResponse
  - [x] WeCmsJsonContext.cs 注册新类型
  - [x] 验证：`dotnet build -warnaserror`

## LOW（11）

- [x] Task 6: 前端/配置改进
  - [x] Program.cs 添加 CORS 中间件
  - [x] authStore setAuth 守卫 null accessToken
  - [x] 验证：`dotnet build -warnaserror`

# Dependencies
- Task 5（命名空间）影响所有引用 Permissions 的文件，优先执行
- Task 3 独立，可并行
- Task 1、2、4 无强依赖
