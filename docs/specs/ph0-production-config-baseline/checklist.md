# PH-0 Production Configuration Baseline Checklist

- [x] Production Hardening plan is present in `docs/plans`.
- [x] Production configuration inventory exists.
- [x] Production template exists and uses safe placeholders.
- [x] Required production keys are documented.
- [x] Secrets are marked as forbidden in git.
- [x] Production missing `Auth:AccessTokenSecret` fails fast.
- [x] Production missing `Security:TwoFactor:SecretProtectionKey` fails fast.
- [x] Production `Database:SeedAdminPassword=Admin@123` fails fast.
- [x] Development is not tightened by PH-0 validator.
- [x] Development connection string placeholder is explicit.
- [x] README links production configuration docs.
- [x] Backend gate includes PH-0 production config check.
- [x] No CMS phase-two feature was added.
- [x] No AI runtime code was added.
- [x] No old ThinkPHP compatibility path was added.

Validation evidence: `scripts/checks/check-production-config-baseline.sh`, backend gate, frontend gate, and production readiness gate.
