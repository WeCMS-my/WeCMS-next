1. Add failing unit and integration proofs for concurrent replay event classification.
2. Update auth service to emit `auth.refresh_concurrent_replay` for within-window concurrent replay.
3. Add or update ADR documentation for the replay window threat model and residual risk.
4. Run task-scoped backend tests and backend quality gate.
5. Audit auth replay event names and refresh-family revocation behavior.
