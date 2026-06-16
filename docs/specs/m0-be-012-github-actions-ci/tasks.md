# M0-BE-012 GitHub Actions CI Tasks

- [x] Add `.github/workflows/backend-quality-gate.yml`.
- [x] Configure push, pull_request, and workflow_dispatch triggers.
- [x] Configure .NET 10 SDK.
- [x] Configure MySQL 8 service and health check.
- [x] Pass `WECMS_TEST_MYSQL_CONNECTION_STRING` to the gate.
- [x] Pass PR/base ref information for no-frontend-change checks.
- [x] Run local `scripts/quality-gate-backend.sh`.
- [x] Run workflow static audit.
- [x] Run sub agent final review.
