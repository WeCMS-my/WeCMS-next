# PH-1 Security Baseline Checklist

- [x] No CMS phase 2 feature added.
- [x] No AI runtime added.
- [x] No real production secret, token, database password, or production-only domain committed.
- [x] `docs/ops/security-baseline.md` exists.
- [x] `docs/ops/deployment-reverse-proxy.md` exists.
- [x] `docs/ops/rate-limit-baseline.md` exists.
- [x] `Security:ForwardedHeaders:*` documented and validated.
- [x] CORS policy uses explicit origins and credentials.
- [x] Cookie append/delete options are centralized.
- [x] CSP report-only and enforce rollout is documented and configurable.
- [x] `scripts/checks/check-security-baseline.sh` passes.
- [x] `bash scripts/quality-gate-backend.sh` passes.
- [x] `bash scripts/quality-gate-frontend.sh` passes.

Validation evidence: `scripts/checks/check-security-baseline.sh`, backend gate, frontend gate, and production readiness gate.
