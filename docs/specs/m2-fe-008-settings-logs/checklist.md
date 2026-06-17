# Checklist

- [x] Route `/system/settings` declares `sys:setting:list`.
- [x] Routes `/system/logs/login`, `/system/logs/audit`, and `/system/logs/security` declare list permissions.
- [x] Sensitive setting values are never rendered raw.
- [x] Log pages contain no create, update, delete, status-toggle, or export mutation controls.
- [x] Frontend typecheck passes.
- [x] Frontend lint passes.
- [x] Frontend build passes.
- [x] `scripts/quality-gate-frontend.sh` passes.
- [x] Final task audit passes.
