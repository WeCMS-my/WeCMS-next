# M0-BE-008 Permissions Checklist

- [x] `secure-ping` binds `PermissionMetadata`.
- [x] `secure-ping` uses `RequireAuthorization`.
- [x] Missing login returns 401.
- [x] Disabled user returns 401.
- [x] Missing permission returns 403.
- [x] Granted permission is allowed.
- [x] Permission checks query Persistence.
