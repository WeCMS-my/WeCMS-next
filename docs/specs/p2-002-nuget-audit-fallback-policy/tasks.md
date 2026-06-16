# P2-002 NuGet Audit Fallback Policy Tasks

1. Add a change-specific spec trio for fallback policy hardening.
2. Update `scripts/quality-gate-backend.sh` to reject fallback in CI/GitHub Actions and emit a stronger local-only warning.
3. Extend `scripts/checks/test-quality-gate-backend.sh` to cover CI fallback rejection.
4. Align README and quality-gate/CI spec docs with the real script entrypoint and fallback policy.
5. Run the gate regression test, then rerun the backend quality gate and audit before closing the task.
