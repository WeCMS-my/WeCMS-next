# Runtime Baseline JIT Checklist

## Required Outcomes

- [x] Active governance documents no longer require Native AOT.
- [x] Active context documents no longer describe Native AOT as the current runtime baseline.
- [x] Backend project files no longer contain active AOT-only properties.
- [x] Backend quality gate no longer executes `/p:PublishAot=true`.
- [x] Historical AOT ADR/spec files are clearly marked as superseded or archived.

## Architecture Must Remain True

- [x] ASP.NET Core Minimal API remains the backend API model.
- [x] `WebApplication.CreateSlimBuilder(args)` remains the host bootstrap API.
- [x] Endpoints remain explicitly registered.
- [x] OpenAPI remains the contract source.
- [x] `WeCms.Persistence` remains the only production database adapter layer.
- [x] `WeCms.Modules.*` does not reference SqlSugar or MySQL provider types.
- [x] `dynamic`, `SELECT *`, and user-input SQL concatenation remain forbidden.

## Verification

- [x] `dotnet build backend/WeCms.sln -warnaserror` passes.
- [x] `dotnet test backend/WeCms.sln` passes.
- [x] `dotnet publish backend/src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 --self-contained false` passes.
