# Production Deployment Record

This record is completed during a real production release. It is not pre-filled by the hardening branch.

## Release Identity

- Commit SHA:
- Tag:
- Release owner:
- Release timestamp:

## Environment Evidence

- Secret manager reviewed without copying secret values:
- Reverse proxy / TLS deployed:
- DNS target confirmed:
- `appsettings` / environment variables reviewed against `docs/ops/production-configuration.md`:
- File storage path or provider verified:
- Virus scanner provider verified when `FileStorage:VirusScanEnabled=true`:

## Database Evidence

- Pre-release backup completed:
- Migration command executed:
- Migration version verified:
- Restore drill reference:

## Gate Evidence

- Backend quality gate result:
- Frontend quality gate result:
- Production readiness gate result:
- `/health/live`:
- `/health/ready`:
- Authorized `/health/dependencies`:
- Admin smoke login:

## Rollback Evidence

- Rollback target:
- Rollback owner:
- Rollback command or deployment action:
- Rollback health check:

## Decision

- Go / No-Go:
- Residual risk owner:
- Follow-up date:
