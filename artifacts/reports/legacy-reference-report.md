# WeCMS M0-BE Legacy Reference Report

## Scope

This report closes M0-BE-013. The ThinkPHP system is used only as a business and schema reference for the new backend. It is not a migration source for M0-BE runtime data.

Reference inputs:

- `docs/context/WeCMS_ThinkPHP_系统详细说明文档.md`
- `docs/context/WeCMS Next 完整迁移重构计划 v3.0.md`
- `docs/context/WeCMS Next M0-BE 后端-only 开发计划.md`
- `database/migrations/000001_init_identity.sql`
- `database/migrations/000002_init_permission.sql`
- `database/migrations/000003_init_auth_security.sql`

## Final Decision

M0-BE starts from an empty database and initializes only the new backend schema plus controlled seed data.

- No legacy data migration.
- No compatibility mode.
- No old password compatibility.
- No old token, session, 2FA secret, SMTP secret, or `auth_key` import.
- No legacy runtime branch in the backend.
- No frontend work in M0-BE.

## Reference Mapping

| ThinkPHP table | Legacy meaning | M0-BE reference target | Current M0-BE status |
|---|---|---|---|
| `think_admin` | Back-office user account, password hash, login/session related fields | `sys_user` | Implemented as empty-db identity table and seeded admin account |
| `think_auth_group` | Role/user group | `sys_role` | Implemented as role table with stable role code |
| `think_auth_group_access` | User-to-role relation | `sys_user_role` | Implemented as normalized relation table |
| `think_auth_rule` | Mixed menu, page, button, and URL permission rule | `sys_menu` + `sys_permission` | Split into menu metadata and explicit permission codes |
| `think_auth_group.rules` | CSV permission/rule id list | `sys_role_permission` | Replaced by normalized role-permission relation |
| `think_config` | System configuration | `sys_setting` | Deferred beyond M0-BE; reference only |

## User Reference

Old `think_admin` is only a reference for the shape of an administrative account. The new `sys_user` table keeps a clean identity model:

- `username`
- `display_name`
- `password_hash`
- `status`
- `is_super_admin`
- login timestamps and IP metadata

The old password model is not accepted by the new login flow. M0-BE seed creates the initial admin account through the new password hashing flow.

## Role Reference

Old `think_auth_group` maps conceptually to `sys_role`. The new model uses:

- immutable role code
- display name
- status
- timestamps

The old CSV-style `rules` field is not carried forward. Role permissions are stored through relational tables.

## Menu And Permission Reference

Old `think_auth_rule` mixed several concepts in one table:

- menu catalog
- page route
- button/action rule
- URL permission rule

M0-BE separates these responsibilities:

- `sys_menu` controls menu/route/button metadata.
- `sys_permission` controls backend action permissions.
- `sys_role_permission` assigns permissions to roles.

The backend does not use dynamic URL matching from the old Auth class. Business endpoints must declare explicit permission metadata or an internal/anonymous policy.

## Setting Reference

Old `think_config` is retained only as a later reference for `sys_setting`. M0-BE does not create `sys_setting`, does not import old configuration values, and does not import encrypted SMTP or application secrets.

## Sensitive Data Boundary

The legacy system documentation lists sensitive fields such as password hashes, tokens, 2FA secrets, backup codes, SMTP encrypted values, and `auth_key`. M0-BE deliberately excludes those values:

- Reference SQL contains schema notes only.
- Seed scripts contain only controlled development/bootstrap data.
- No production dump or secret is used.
- No old credential upgrade path is implemented.

## Acceptance Evidence

Expected M0-BE-013 artifacts:

- `artifacts/reports/legacy-reference-report.md`
- `database/legacy-reference/thinkphp_schema_reference.sql`

Required verification for closing M0-BE-013:

- Report includes `think_admin -> sys_user`.
- Report includes `think_auth_group -> sys_role`.
- Report includes `think_auth_rule -> sys_menu + sys_permission`.
- Report includes `think_config -> sys_setting`.
- Report explicitly states no data migration.
- Report explicitly states no compatibility mode.
- Report explicitly states no old password compatibility.
- Backend quality gate must be rerun after adding the artifacts.
- Final read-only M0-BE audit must be run after the task gate passes.
