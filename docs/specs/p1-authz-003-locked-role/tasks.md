# P1-AUTHZ-003 Tasks

- [x] Task 1: Add `sys_role.is_locked` migration/schema coverage.
- [x] Task 2: Update role seed so `super_admin` is locked and has an enabled holder.
- [x] Task 3: Add `isLocked` to role DTO contracts and OpenAPI.
- [x] Task 4: Update `RoleRepository` to read/write `is_locked`.
- [x] Task 5: Block locked role mutation in `RoleService`.
- [x] Task 6: Add locked role holder protection in `UserService`.
- [x] Task 7: Add locked role query capabilities to `IUserRepository` and `UserRepository`.
- [x] Task 8: Update `JsonSerializerContext`, OpenAPI export checks, and contract tests.
- [x] Task 9: Add unit and integration tests for role/user locked-role rules.
- [x] Task 10: Add locked-role seed static check and wire it into `scripts/quality-gate-backend.sh`.
- [x] Task 11: Run backend quality gate and perform final audit.
