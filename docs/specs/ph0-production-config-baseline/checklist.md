# PH-0 Production Configuration Baseline Checklist

- [ ] Production Hardening plan is present in `docs/plans`.
- [ ] Production configuration inventory exists.
- [ ] Production template exists and uses safe placeholders.
- [ ] Required production keys are documented.
- [ ] Secrets are marked as forbidden in git.
- [ ] Production missing `Auth:AccessTokenSecret` fails fast.
- [ ] Production missing `Security:TwoFactor:SecretProtectionKey` fails fast.
- [ ] Production `Database:SeedAdminPassword=Admin@123` fails fast.
- [ ] Development is not tightened by PH-0 validator.
- [ ] Development connection string placeholder is explicit.
- [ ] README links production configuration docs.
- [ ] Backend gate includes PH-0 production config check.
- [ ] No CMS phase-two feature was added.
- [ ] No AI runtime code was added.
- [ ] No old ThinkPHP compatibility path was added.
