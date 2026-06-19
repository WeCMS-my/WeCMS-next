# PH Residual Cleanup

## Scope

Close the remaining Production Hardening residuals that can be resolved inside the repository without pretending that a real production deployment has happened.

## Requirements

- Add a real configurable file scan adapter for Production use.
- Keep `NoopFileScanService` available only when virus scanning is disabled.
- Allow `FileStorage:VirusScanEnabled=true` only with a configured real scanner provider.
- Convert the real deployment residual into an explicit deployment evidence record template.
- Synchronize PH-0, PH-1, and PH-2 checklist state with already-passing gates.

## Non-Goals

- Do not execute real DNS, proxy, secret-manager, or production deployment operations.
- Do not add CMS phase-two features.
- Do not add AI runtime code.
