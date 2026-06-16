# M1-BE-002 User API

## Goal

Implement backend-only user management APIs under `/api/v1/system/users`.

## Scope

This task includes:

- User list, detail, create, update, delete, enable, disable, reset password, assign roles, and assign posts APIs.
- User service business rules.
- User repository port in `WeCms.Modules.System`.
- User repository implementation in `WeCms.Persistence`.
- Supporting schema for user profile fields, department reference, posts, and user-post assignment.
- Permission metadata for every User API endpoint.
- JSON source-generation registration.
- Tests for user business rules, seed/schema smoke, and endpoint/permission source coverage.

This task does not include:

- Role management APIs.
- Department management APIs.
- Post management APIs.
- Frontend generated types or SoybeanAdmin pages.
- CMS content APIs.

## API Contract

All endpoints require JWT and the listed permission code.

```text
GET    /api/v1/system/users                    sys:user:list
GET    /api/v1/system/users/{id}               sys:user:detail
POST   /api/v1/system/users                    sys:user:create
PUT    /api/v1/system/users/{id}               sys:user:update
DELETE /api/v1/system/users/{id}               sys:user:delete
POST   /api/v1/system/users/{id}/enable        sys:user:enable
POST   /api/v1/system/users/{id}/disable       sys:user:disable
POST   /api/v1/system/users/{id}/reset-password sys:user:reset-password
PUT    /api/v1/system/users/{id}/roles         sys:user:assign-role
PUT    /api/v1/system/users/{id}/posts         sys:user:assign-post
```

List responses use the standard page shape:

```json
{
  "records": [],
  "page": 1,
  "pageSize": 20,
  "total": 100
}
```

## Business Rules

- `page >= 1`.
- `1 <= pageSize <= 100`.
- Username is required, normalized by trim, and unique.
- Email is optional, trimmed, and unique when present.
- Phone is optional, trimmed, and unique when present.
- Password hashes are never returned.
- A user cannot delete or disable themself.
- The last `super_admin` user cannot be deleted or disabled.
- User delete is soft delete.
- Writes record audit rows.
- Repository methods accept `CancellationToken`.

## Schema

Add fields to `sys_user`:

- `dept_id`
- `email`
- `phone`
- `security_stamp`
- `permission_version`
- `deleted_at`

Add supporting M1 user assignment tables:

- `sys_dept`
- `sys_post`
- `sys_user_post`

## Acceptance

- Unit tests cover self delete/disable and last super admin protection.
- Integration migration/seed smoke proves the new schema exists.
- User endpoint source scan proves every endpoint has authorization and permission metadata.
- Backend quality gate passes before moving to M1-BE-003.
