#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

printf '[1/6] backend quality gate\n'
bash scripts/quality-gate-backend.sh

printf '[2/6] frontend quality gate\n'
bash scripts/quality-gate-frontend.sh

printf '[3/6] production config docs\n'
bash scripts/checks/check-production-config-docs.sh

printf '[4/6] production template no secrets\n'
bash scripts/checks/check-production-template-no-secrets.sh

printf '[5/6] release runbooks\n'
bash scripts/checks/check-release-runbooks.sh

printf '[6/6] frontend production env\n'
bash scripts/checks/check-frontend-production-env.sh

printf 'quality-gate-production: ok\n'
