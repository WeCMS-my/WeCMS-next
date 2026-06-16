# M1-BE-005 Checklist

- [x] Red test observed before implementation.
- [x] Permission APIs require JWT and permission metadata.
- [x] Permission code uniqueness is enforced.
- [x] Permission tree endpoint groups permissions by module.
- [x] System built-in permissions cannot be deleted.
- [x] Delete is soft delete.
- [x] Role-bound permissions are not hard-deleted.
- [x] Writes record audit rows.
- [x] Modules contain no SQL or ORM types.
- [x] Persistence owns SQL and mapping.
- [x] JSON source-generation includes Permission DTOs.
- [x] No frontend files changed.
