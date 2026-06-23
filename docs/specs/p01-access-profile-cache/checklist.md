# Checklist

- [x] No public API or OpenAPI contract changed.
- [x] No new permission code or menu seed added.
- [x] No database schema or migration changed.
- [x] Cache key includes user id and permission version.
- [x] Cache key distinguishes super-admin and regular profile shape.
- [x] AccessControl does not reference `WeCms.Caching`.
- [x] TDD red/green proof exists for cache hit and version miss behavior.
