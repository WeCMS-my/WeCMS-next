# M0-BE-011 Quality Gate Checklist

- [x] Gate runs restore.
- [x] Gate runs build with `-warnaserror`.
- [x] Gate runs unit, architecture, and integration tests.
- [x] Gate runs JIT publish with `--self-contained false`.
- [x] Gate runs OpenAPI export.
- [x] Gate runs OpenAPI auth request body check.
- [x] Gate runs DB boundary check.
- [x] Gate runs layer dependency check.
- [x] Gate runs DI boundary check.
- [x] Gate runs no frontend change check.
- [x] Gate runs generated test artifact check.
- [x] Gate runs code-review rule check.
- [x] Gate runs migration/seed smoke test.
- [x] Gate excludes AOT/Dapper/IL trim checks.
- [x] Gate fails fast when ripgrep `rg` is unavailable.
