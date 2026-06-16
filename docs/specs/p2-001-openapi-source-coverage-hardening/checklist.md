# P2-001 OpenAPI Source Coverage Hardening Checklist

- [ ] Static OpenAPI export remains CLI-only and host-free.
- [ ] Source coverage no longer depends on a hardcoded list of three endpoint files.
- [ ] Endpoint mapping files under `WeCms.Modules.System` are scanned automatically.
- [ ] Exported OpenAPI paths are still checked against discovered source routes.
- [ ] No runtime DI/persistence/bootstrap dependency is introduced into export.
