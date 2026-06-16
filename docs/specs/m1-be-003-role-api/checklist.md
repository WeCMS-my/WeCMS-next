# M1-BE-003 Checklist

- [x] Red test observed before implementation.
- [x] Role APIs require JWT and permission metadata.
- [x] Role code uniqueness is enforced.
- [x] Pagination validates `page` and `pageSize <= 100`.
- [x] System built-in roles cannot be deleted.
- [x] `super_admin` cannot be deleted.
- [x] `super_admin` cannot be disabled.
- [x] Permission/menu assignment ids are validated and deduplicated.
- [x] Writes record audit rows.
- [x] Modules contain no SQL or ORM types.
- [x] Persistence owns SQL and mapping.
- [x] JSON source-generation includes Role DTOs.
- [x] No frontend files changed.
