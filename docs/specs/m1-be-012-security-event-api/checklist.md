# M1-BE-012 Checklist

- [x] Red test observed before implementation.
- [x] Security event APIs require JWT and permission metadata.
- [x] Security event list supports eventType, severity, user, ip, and date range filters.
- [x] Security event detail returns NotFound when missing.
- [x] No security event mutation endpoints are exposed.
- [x] Modules contain no SQL or ORM types.
- [x] Persistence owns SQL and mapping.
- [x] JSON source-generation includes SecurityEvent DTOs.
- [x] No frontend files changed.
