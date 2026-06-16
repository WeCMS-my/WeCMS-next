# P2-002 NuGet Audit Fallback Policy Checklist

- [ ] `strict` remains the default gate mode.
- [ ] Local `fallback` still works and emits a warning.
- [ ] `fallback` is rejected in CI/GitHub Actions contexts.
- [ ] Gate regression tests cover the rejection path.
- [ ] README no longer documents unsupported `di` / `all` modes.
- [ ] CI spec and gate spec match the enforced fallback policy.
