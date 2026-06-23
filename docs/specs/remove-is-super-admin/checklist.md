# Remove isSuperAdmin Checklist

- [x] `super_admin` remains a role code, not a user field.
- [x] `sys_user.is_super_admin` is absent from reset baseline and seeds.
- [x] Auth and user DTOs no longer expose `isSuperAdmin`.
- [x] Access profile no longer accepts or caches by a super-admin flag.
- [x] Security-ban high-risk self-unban check uses role assignment.
- [x] Frontend generated types and users table no longer consume `isSuperAdmin`.
- [x] Architecture test blocks field reintroduction.
