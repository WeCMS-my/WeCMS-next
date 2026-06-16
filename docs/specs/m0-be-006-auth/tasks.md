# M0-BE-006 Auth Tasks

1. Add auth DTOs, repository abstraction, service abstraction, endpoint mapping, and token/password helpers in `WeCms.Modules.System/Auth`.
2. Add SqlSugar-backed auth repository in `WeCms.Persistence/Modules/System/Auth`.
3. Register auth services and repository through DI.
4. Register auth endpoints and JSON source-generation DTOs in `WeCms.Api`.
5. Add unit tests for validation, generic failed login, password verification, and token hash behavior.
6. Add integration tests for login success/failure audit and `me` authorization behavior.
7. Run task tests, backend quality gate, and task audit before closing.
