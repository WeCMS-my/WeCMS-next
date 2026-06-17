# Checklist

- [x] `/system/files` declares `sys:file:list`.
- [x] Upload button is gated by `sys:file:upload`.
- [x] Preview/download controls are gated by `sys:file:download`.
- [x] Delete control is gated by `sys:file:delete`.
- [x] Frontend computes SHA-256 before upload.
- [x] Frontend rejects disallowed file types and files larger than 10 MB.
- [x] Frontend typecheck passes.
- [x] Frontend lint passes.
- [x] Frontend build passes.
- [x] `scripts/quality-gate-frontend.sh` passes.
- [x] Final task audit passes.
