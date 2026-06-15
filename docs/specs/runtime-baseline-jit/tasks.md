# Runtime Baseline JIT Tasks

## Phase 0: Baseline

- [x] Confirm the user wants a repository-wide move from AOT baseline to JIT baseline.
- [x] Record current AOT-specific rule, doc, project, and script locations.
- [x] Confirm Minimal API, `CreateSlimBuilder`, and SqlSugar remain in scope.

## Phase 1: Governance Update

- [x] Add the `runtime-baseline-jit` spec set.
- [x] Add a new ADR for the runtime baseline change.
- [x] Update `AGENTS.md`.
- [x] Update `code_review.md`.
- [x] Update `.trae/rules/wecms-engineering-principles.md`.

## Phase 2: Context Update

- [x] Update `docs/context/WeCMS_Next_NET10_AOT_SoybeanAdmin_完整迁移重构计划.md`.
- [x] Update `docs/context/WeCMS_工程落地执行计划与交付工件.md`.
- [x] Update `docs/context/WeCMS_工程骨架验证文档.md`.
- [x] Update `docs/context/WeCMS_Next_M0-BE_后端-only_开发计划.md`.
- [x] Ensure active context docs describe JIT publish as current truth.

## Phase 3: Project Configuration

- [x] Remove `PublishAot` from active project configuration.
- [x] Remove `IsAotCompatible` from active project configuration.
- [x] Remove `EnableAotAnalyzer` from active project configuration.
- [x] Remove AOT-specific IL warning escalation tied only to Native AOT policy.

## Phase 4: Quality Gate

- [x] Replace Native AOT publish in `scripts/quality-gate-backend.sh` with normal publish verification.
- [x] Keep `dotnet build` and `dotnet test` in the backend gate.
- [x] Keep frontend drift protection if still applicable.

## Phase 5: Historical Material

- [x] Mark `docs/adr/0006-aot-trim-warnings-exception.md` as superseded.
- [x] Mark `docs/specs/sqlsugar-aot-spike/*` as archived historical AOT research.
- [x] Ensure historical files no longer read as active merge gates.

## Phase 6: Verification

- [x] Run `dotnet build backend/WeCms.sln -warnaserror`.
- [x] Run `dotnet test backend/WeCms.sln`.
- [x] Run `dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false`.
- [x] Review failures as current code truth and document them.
