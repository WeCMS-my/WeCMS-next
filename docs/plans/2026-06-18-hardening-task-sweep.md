# WeCMS Hardening Task Sweep Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Verify and fix the listed hardening issues one task at a time on top of the current WeCMS-next working tree, with test, gate, and audit closure after each task.

**Architecture:** Work strictly in serial order. For each task: perform root-cause verification first, write or identify the failing proof, make the minimal fix, run task-scoped tests, run the required gate(s), run a task-scoped audit, summarize results, then move to the next task. Because the current working tree is already dirty in overlapping areas, branch creation must preserve current repo-truth instead of switching to a detached clean snapshot.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, SqlSugar, MySQL, Vue 3, SoybeanAdmin, pnpm, shell gate scripts.

---

### Task 1: Branch And Baseline Safety

**Files:**
- Modify: none expected unless ignore/worktree safety requires it
- Inspect: `.gitignore`, `.worktrees/`, git status

**Step 1: Confirm working-tree safety**

Run: `git status --short --branch`
Expected: identify current branch and all pre-existing dirty files before any implementation

**Step 2: Verify worktree directory policy**

Run: `git check-ignore -q .worktrees && echo ignored || echo not-ignored`
Expected: `ignored`

**Step 3: Create the execution branch without losing current repo-truth**

Run: `git switch -c codex/hardening-task-sweep`
Expected: new branch created while preserving the current working tree

**Step 4: Record baseline risks**

Capture:
- current dirty files overlapping this task list
- whether task execution must accommodate existing edits

### Task 2: P0-001 Serializer Context TwoFactor Risk Verification

**Files:**
- Inspect: `backend/src/WeCms.Api/Json/WeCmsJsonSerializerContext.cs`
- Inspect: `backend/src/WeCms.Modules.System/Auth/AccountTwoFactorDtos.cs`
- Test: `backend/WeCms.slnx`

**Step 1: Verify DTO namespace ownership**

Run: `rg -n "namespace|AccountTwoFactor" backend/src/WeCms.Api/Json/WeCmsJsonSerializerContext.cs backend/src/WeCms.Modules.System/Auth/AccountTwoFactorDtos.cs -S`
Expected: prove whether DTOs are in `WeCms.Modules.System.Auth` or `WeCms.Modules.System.TwoFactor`

**Step 2: Reproduce the claimed build risk**

Run: `dotnet build backend/WeCms.slnx -warnaserror`
Expected: either fail with missing type/using evidence or pass and disprove the claim

**Step 3: Fix only if the failure is real**

Possible write scope:
- `backend/src/WeCms.Api/Json/WeCmsJsonSerializerContext.cs`

**Step 4: Verify task closure**

Run:
- `dotnet build backend/WeCms.slnx -warnaserror`
- `bash scripts/quality-gate-backend.sh`

**Step 5: Audit**

Run:
- `rg -n "AccountTwoFactor" backend/src/WeCms.Api/Json/WeCmsJsonSerializerContext.cs backend/src/WeCms.Modules.System -S`
- review for DTO source-generator coverage and namespace consistency

### Task 3: P1-001 UserRepository affected rows enforcement

**Files:**
- Modify: `backend/src/WeCms.Persistence/Modules/System/Users/UserRepository.cs`
- Add/Modify tests: likely `backend/tests/WeCms.Tests.Integration/**` or `backend/tests/WeCms.Tests.Unit/**` depending the existing coverage seam

**Step 1: Locate all unchecked write paths in scope**

Run: `rg -n "CreateAsync\\(|BumpPermissionVersionAsync|ExecuteCommandAsync|ExpectOneAsync" backend/src/WeCms.Persistence/Modules/System/Users/UserRepository.cs -S`

**Step 2: Write failing proof first**

Use the smallest existing test seam that can prove:
- `CreateAsync` fails fast when insert affected rows != 1
- `BumpPermissionVersionAsync` fails fast when update affected rows != 1

**Step 3: Implement the minimal fix**

Possible write scope:
- `backend/src/WeCms.Persistence/Modules/System/Users/UserRepository.cs`
- targeted test files only

**Step 4: Verify**

Run:
- task-scoped test command(s)
- `dotnet build backend/WeCms.slnx -warnaserror`
- `bash scripts/quality-gate-backend.sh`

**Step 5: Audit**

Run: `rg -n "ExecuteCommandAsync\\(" backend/src/WeCms.Persistence/Modules/System/Users/UserRepository.cs -S`

### Task 4: P1-002 File preview Content-Disposition hardening

**Files:**
- Modify: `backend/src/WeCms.Modules.System/Files/FileEndpoints.cs`
- Modify: related validation/service files if required by existing layering
- Add/Modify tests: file endpoint/service tests

**Step 1: Trace current file-name validation and preview header construction**

Run: `rg -n "originalName|ContentDisposition|filename=|preview" backend/src/WeCms.Modules.System backend/tests -S`

**Step 2: Write failing tests**

Cover:
- invalid file names with control/header-dangerous characters rejected
- preview uses safe header generation

**Step 3: Implement minimal hardening**

Keep validation at the system boundary and avoid ad hoc string concatenation.

**Step 4: Verify**

Run:
- task-scoped tests
- `dotnet build backend/WeCms.slnx -warnaserror`
- `bash scripts/quality-gate-backend.sh`

**Step 5: Audit**

Run: `rg -n "ContentDisposition|filename\\*" backend/src/WeCms.Modules.System/Files backend/tests -S`

### Task 5: P1-003 Local development port consistency

**Files:**
- Modify: `README.md`
- Modify: `backend/src/WeCms.Api/Properties/launchSettings.json`
- Modify: `frontend/soybean-admin/vite.config.ts`
- Modify: `frontend/soybean-admin/tests/vite-dev-proxy.test.mjs`
- Possibly modify related smoke/config tests

**Step 1: Verify all live port sources**

Run: `rg -n "5207|5261|5080|VITE_API_BASE_URL|applicationUrl" README.md backend/src/WeCms.Api/Properties/launchSettings.json frontend/soybean-admin -S`

**Step 2: Write/update failing proof first**

Prefer existing frontend config tests and any docs/config assertions already present.

**Step 3: Implement the minimal consistency fix**

Unify development defaults and document any intentional test-only exceptions explicitly.

**Step 4: Verify**

Run:
- `pnpm --dir frontend/soybean-admin typecheck`
- `pnpm --dir frontend/soybean-admin lint`
- `pnpm --dir frontend/soybean-admin build`
- `bash scripts/quality-gate-frontend.sh`
- if backend files changed meaningfully: `dotnet build backend/WeCms.slnx -warnaserror`

**Step 5: Audit**

Run: `rg -n "5207|5261|5080" README.md backend frontend scripts -S`

### Task 6: P1-004 Frontend VITE_API_BASE_URL fallback

**Files:**
- Modify: `frontend/soybean-admin/src/api/request.ts`
- Modify/Add tests: request/runtime tests if present

**Step 1: Reproduce the undefined base-url path**

Run: `sed -n '1,220p' frontend/soybean-admin/src/api/request.ts`

**Step 2: Write failing test**

Cover:
- unset `VITE_API_BASE_URL` -> relative `/api/...`
- set `VITE_API_BASE_URL` -> absolute base honored

**Step 3: Implement minimal fallback**

**Step 4: Verify**

Run:
- targeted frontend test(s)
- `pnpm --dir frontend/soybean-admin typecheck`
- `pnpm --dir frontend/soybean-admin lint`
- `pnpm --dir frontend/soybean-admin build`
- `bash scripts/quality-gate-frontend.sh`

**Step 5: Audit**

Run: `rg -n "VITE_API_BASE_URL" frontend/soybean-admin/src frontend/soybean-admin/tests -S`

### Task 7: P2-001 Refresh concurrent replay security semantics

**Files:**
- Inspect/Modify: auth service, ADR/docs, integration tests

**Step 1: Confirm current behavior from code and tests**

**Step 2: Decide if this is a code bug, policy gap, or both**

**Step 3: If change is required, create or update spec/ADR before implementation**

**Step 4: Add failing integration tests before implementation**

**Step 5: Verify**

Run:
- targeted backend tests
- `bash scripts/quality-gate-backend.sh`

### Task 8: P2-002 Migration SQL parser scope documentation

**Files:**
- Modify: migration docs/spec/ADR if needed
- Modify tests only if parser behavior changes

**Step 1: Verify current parser contract and real usage**

**Step 2: Decide if documentation-only hardening is sufficient**

**Step 3: If docs-only, run consistency audit; if code change, add failing tests first**

### Task 9: P2-003 AuthService complexity

**Files:**
- Inspect: `backend/src/WeCms.Modules.System/Auth/AuthService.cs`
- Possibly create spec first if refactor scope is large

**Step 1: Measure actual complexity and responsibilities**

**Step 2: Decide whether this remains an audit finding or becomes an implementation task in this sweep**

**Step 3: If implementation crosses large diff or auth/security boundaries, create spec three-piece first**

### Task 10: Final Range Audit

**Files:**
- Entire touched range

**Step 1: Re-run required backend and frontend quality gates based on actual touched surfaces**

**Step 2: Review git diff against the task list**

**Step 3: Confirm no skipped task, no unverifed success claim, no cross-boundary regression**

**Step 4: Summarize per-task outcome**

Required closeout fields:
- fixed / not fixed / stale report / environment blocked
- proof command(s)
- affected files
- residual risks
