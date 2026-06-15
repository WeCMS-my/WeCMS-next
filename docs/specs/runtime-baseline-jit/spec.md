# Runtime Baseline JIT Spec

## Status

Accepted for implementation.

## Change Id

`runtime-baseline-jit`

## Context

WeCMS Next will keep the current ASP.NET Core Minimal API architecture, `WebApplication.CreateSlimBuilder(args)`, backend contract-first delivery, and SqlSugar ORM as the database adapter direction.

The project no longer treats Native AOT as a required runtime or release gate. The backend runtime baseline changes to standard .NET 10 JIT publish and execution.

This change exists to remove every AOT-only rule, gate, ADR, and proof path that is now inconsistent with the intended runtime model, while preserving architecture boundaries that are still valid without AOT.

## Decision

1. Backend runtime baseline changes from `Native AOT Only` to `.NET 10 JIT publish/runtime`.
2. ASP.NET Core Minimal API remains the API programming model.
3. `WebApplication.CreateSlimBuilder(args)` remains the host bootstrap API unless a separate architecture decision changes it later.
4. SqlSugar remains the ORM direction and is still restricted to `WeCms.Persistence`.
5. Module-layer database boundary rules remain in force:
   - `WeCms.Modules.*` must not reference SqlSugar or MySQL provider types.
   - `WeCms.Modules.*` must not contain SQL text.
   - `WeCms.Persistence` remains the only production database adapter layer.
6. Native AOT specific project properties, analyzers, and release gates are removed from active policy and build configuration.

## In Scope

- Update repository rules and review checklists from AOT baseline to JIT baseline.
- Update current context documents so they describe JIT publish as the active runtime model.
- Remove AOT-specific `csproj` properties and warning policies.
- Replace backend quality gate AOT publish with normal JIT publish verification.
- Mark AOT-specific ADR and spec material as superseded or archived.

## Out of Scope

- Replacing Minimal API with MVC Controller architecture.
- Replacing `CreateSlimBuilder` with `CreateBuilder`.
- Replacing SqlSugar with another ORM.
- Relaxing database boundary rules.
- Changing frontend architecture.

## Active Architecture Rules After This Change

- Backend uses ASP.NET Core Minimal APIs on .NET 10.
- Host bootstrap uses `WebApplication.CreateSlimBuilder(args)`.
- Endpoints are registered explicitly.
- No runtime endpoint scanning.
- No MVC Controllers or Razor Pages.
- OpenAPI remains the backend-to-frontend contract source.
- SqlSugar usage is limited to `WeCms.Persistence`.
- `dynamic`, `SELECT *`, and user-input SQL concatenation remain forbidden.

## Acceptance Criteria

- `AGENTS.md`, `code_review.md`, `.trae/rules/wecms-engineering-principles.md`, and active `docs/context/*` no longer describe Native AOT as the required runtime baseline.
- `backend/src/*/*.csproj` no longer contain active AOT-only properties such as `PublishAot`, `IsAotCompatible`, or `EnableAotAnalyzer`.
- `scripts/quality-gate-backend.sh` verifies build, test, and standard publish instead of Native AOT publish.
- `docs/adr/0006-aot-trim-warnings-exception.md` is no longer an active policy baseline.
- `docs/specs/sqlsugar-aot-spike/*` is explicitly archived as historical AOT research.
- The backend verification commands below succeed or any failure is reported as current code truth rather than outdated AOT policy drift.

## Verification Commands

```bash
dotnet build backend/WeCms.sln -warnaserror
dotnet test backend/WeCms.sln
dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false
```

## Risks

- Large context documents may still contain stale AOT wording if they are only partially edited.
- Historical AOT research can confuse future work unless it is clearly archived.
- Removing AOT analyzers narrows one previous proof surface; architecture and boundary verification must remain explicit elsewhere.

