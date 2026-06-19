# PH-1 Security Baseline Checklist

- [ ] No CMS phase 2 feature added.
- [ ] No AI runtime added.
- [ ] No real production secret, token, database password, or production-only domain committed.
- [ ] `docs/ops/security-baseline.md` exists.
- [ ] `docs/ops/deployment-reverse-proxy.md` exists.
- [ ] `docs/ops/rate-limit-baseline.md` exists.
- [ ] `Security:ForwardedHeaders:*` documented and validated.
- [ ] CORS policy uses explicit origins and credentials.
- [ ] Cookie append/delete options are centralized.
- [ ] CSP report-only and enforce rollout is documented and configurable.
- [ ] `scripts/checks/check-security-baseline.sh` passes.
- [ ] `bash scripts/quality-gate-backend.sh` passes.
- [ ] `bash scripts/quality-gate-frontend.sh` passes.
