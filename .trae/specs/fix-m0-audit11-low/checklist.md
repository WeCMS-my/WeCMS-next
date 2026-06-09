# Checklist

## L1
- [x] GetDownloadInfoAsync 改用 record 替代 Tuple

## L3
- [x] .gitignore 含 `**/obj/` (already present at line 3)

## L4
- [x] SystemClock 封装 TimeProvider.System

## L5
- [x] /403 路由使用独立组件

## L6
- [x] refresh 失败时通知 subscribers

## 全量验证
- [x] dotnet build -warnaserror 通过
- [x] dotnet test 通过
- [x] dotnet publish /p:PublishAot=true 通过
- [x] code review 通过
