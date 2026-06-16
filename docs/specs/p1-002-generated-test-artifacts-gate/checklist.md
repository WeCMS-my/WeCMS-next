# P1-002 Generated Test Artifacts Gate Checklist

- [ ] Tracked files under `backend/tests/**/TestResults/**` are removed.
- [ ] `vstest.diag*.log` is ignored and removed from the worktree.
- [ ] `.gitignore` ignores `TestResults`, `.trx`, coverage, and `vstest` diagnostics with wildcard rules.
- [ ] Backend quality gate fails if tracked generated test artifacts appear again.
- [ ] The new scanner does not flag intentional checked-in contract artifacts.
- [ ] No build/test/publish/audit gate is relaxed.
