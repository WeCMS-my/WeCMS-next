# M0-BE-012 GitHub Actions CI Checklist

- [x] Workflow exists at `.github/workflows/backend-quality-gate.yml`.
- [x] Workflow triggers on push to `main`.
- [x] Workflow triggers on pull requests to `main`.
- [x] Workflow supports `workflow_dispatch`.
- [x] Workflow sets up .NET 10.
- [x] Workflow starts MySQL 8.
- [x] Workflow runs `bash scripts/quality-gate-backend.sh`.
- [x] Workflow can generate OpenAPI through the backend gate.
- [x] Workflow does not run frontend commands.
- [x] Workflow does not run AOT/Dapper/IL trim gates.
