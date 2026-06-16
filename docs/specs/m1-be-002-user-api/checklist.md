# M1-BE-002 Checklist

- [x] Red test observed before implementation.
- [x] User APIs require JWT and permission metadata.
- [x] Password hash is never returned.
- [x] Pagination validates `page` and `pageSize <= 100`.
- [x] Self delete and self disable are blocked.
- [x] Last super admin delete and disable are blocked.
- [x] Writes record audit rows.
- [x] Modules contain no SQL or ORM types.
- [x] Persistence owns SQL and mapping.
- [x] JSON source-generation includes User DTOs.
- [x] No frontend files changed.
