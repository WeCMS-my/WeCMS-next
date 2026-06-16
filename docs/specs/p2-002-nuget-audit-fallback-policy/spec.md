# P2-002 NuGet Audit Fallback Policy Spec

## Scope

Harden the backend quality-gate policy around `WECMS_NUGET_AUDIT_MODE=fallback`.

This change covers:

- making `strict` remain the default,
- making fallback warnings explicit and local-only,
- blocking fallback in CI/release-gate contexts,
- aligning tests and user-facing docs with the actual gate entrypoint.

## Requirements

- `WECMS_NUGET_AUDIT_MODE` default remains `strict`.
- `fallback` remains an explicit opt-in mode.
- When `fallback` is used, the gate must print a clear local-only warning.
- `fallback` must be rejected when `CI=true` or `GITHUB_ACTIONS=true`.
- Gate regression tests must cover strict failure, local fallback success, and CI fallback rejection.
- README and spec docs must not claim unsupported `di` or `all` quality-gate modes.

## Non-Goals

- Reworking the entire gate into subcommands.
- Removing the local fallback capability entirely.
- Changing the current CI workflow away from strict mode.
