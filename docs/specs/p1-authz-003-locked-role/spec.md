# P1-AUTHZ-003 Locked Role Authorization Guard

## Goal

Prevent privileged role loss by introducing a locked-role invariant:

- `sys_role.is_locked` marks roles whose definition and assignments are security-critical.
- Locked roles cannot be updated, deleted, disabled, or have permissions/menus modified through normal APIs.
- Every non-deleted locked role must have at least one enabled, non-deleted user holder.

This replaces the current `super_admin`-specific partial guard with a general locked-role rule.

## Scope

Backend-only M1-BE change. No frontend files, no generated frontend TypeScript, and no lock/unlock API.

Included:

- Add `sys_role.is_locked BOOLEAN NOT NULL DEFAULT FALSE`.
- Seed `super_admin` with `is_builtin = TRUE`, `is_locked = TRUE`, `status = enabled`.
- Ensure `admin` is enabled and holds `super_admin`.
- Expose `isLocked` on role list/detail DTOs and OpenAPI schemas.
- Ensure create/update role requests do not accept `isLocked` or `isBuiltin`.
- Explicitly write `is_locked = FALSE` when creating roles.
- Block locked role mutation in `RoleService`.
- Protect locked role holder invariant in `UserService`.
- Add repository queries needed for locked role holder checks.
- Add unit, integration, migration/seed smoke, static script, and quality-gate coverage.

Excluded:

- No `lock role` API.
- No `unlock role` API.
- No normal API path can modify `isLocked`.
- No frontend button control.
- No second-operator confirmation or approval workflow.
- No database trigger.
- No `super_admin` permission bypass.

## Data Contract

Role list and detail responses expose:

```csharp
bool IsLocked
```

`CreateRoleRequest` and `UpdateRoleRequest` must not expose:

```text
isLocked
isBuiltin
```

## Security Rules

Locked role mutation must return `ApiCodes.BusinessError` and must not change persisted data:

- `UpdateAsync`: `Locked role cannot be updated.`
- `DeleteAsync`: `Locked role cannot be deleted.`
- `DisableAsync`: `Locked role cannot be disabled.`
- `AssignPermissionsAsync`: `Locked role permissions cannot be modified.`
- `AssignMenusAsync`: `Locked role menus cannot be modified.`

Locked role holder invariant:

```text
For each sys_role row where is_locked = TRUE and deleted_at IS NULL,
there must be at least one sys_user row where:
  status = 'enabled'
  deleted_at IS NULL
and linked through sys_user_role.
```

Operations that would remove the last enabled holder of any locked role must return:

```text
Locked role must have at least one enabled user.
```

## Acceptance Criteria

- New database initialization creates `sys_role.is_locked` as `NOT NULL DEFAULT FALSE`.
- Seeds are idempotent and lock `super_admin`.
- `super_admin` receives every current M1-BE system permission without a static `IN` list.
- Role list/detail API and OpenAPI include `isLocked`.
- Locked roles cannot be updated, deleted, disabled, assigned permissions, or assigned menus.
- Deleting, disabling, or reassigning roles from the last enabled holder of a locked role is rejected.
- Adding another holder of a locked role is allowed.
- Removing a locked role from one user is allowed when another enabled holder remains.
- `scripts/quality-gate-backend.sh` includes the locked role seed/static check.
- Backend quality gate passes.
