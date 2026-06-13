# WeCMS Engineering Rules for Codex / AI Agents

**Document Version:** v1.0
**Project:** WeCMS Rebuild
**Stack:** .NET 10 Native AOT / ASP.NET Core Minimal API / SqlSugar / MySQL / Vue 3
**Scope:** Codex / Trae / Claude Code / Cursor / Copilot and all human contributors

---

# 1. Object-Oriented Programming / SOLID

## 1.1 Interface First

Any new service class with side effects must define an `I*` interface before implementation.

Side-effect services include:

```text
Database access
File IO
Network IO
Token generation
Password hashing
Cache access
Clock/time access
ID generation
Email sending
Storage access
Process interaction
Configuration loading
Audit writing
Login log writing
Permission checking
```

Examples:

```text
IPasswordHasher -> PasswordHasher
ITokenService -> JwtTokenService
IClock -> SystemClock
IIdGenerator -> SnowflakeIdGenerator
IFileStorage -> LocalFileStorage
ICacheService -> MemoryCacheService
IUnitOfWork -> SqlSugarUnitOfWork
IUserRepository -> SqlSugarUserRepository
```

Allowed exception:

```text
Pure value objects
Pure static helpers
DTOs
Enums
Constants
Small internal pure functions
```

Forbidden:

```csharp
public sealed class LoginUseCase
{
    private readonly PasswordHasher _hasher = new();
}
```

Required:

```csharp
public sealed class LoginUseCase(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUnitOfWork unitOfWork)
{
}
```

---

## 1.2 Constructor Injection Required

All dependencies must be passed through constructor injection.

Allowed:

```csharp
public sealed class CreateUserUseCase(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IIdGenerator ids,
    IClock clock,
    IUnitOfWork unitOfWork)
{
}
```

Forbidden:

```csharp
var db = new SqlSugarClient(...);
var hasher = new PasswordHasher();
var tokenService = new JwtTokenService();
```

Forbidden in business code:

```csharp
serviceProvider.GetRequiredService<T>();
serviceProvider.GetService<T>();
```

`IServiceProvider` usage is allowed only in composition root / framework integration code when unavoidable.

---

## 1.3 Single Responsibility

A class must not perform more than one major responsibility.

Forbidden combinations:

```text
Collect + Process + Output
Validate + Persist + Render
Query + Command + Audit + Permission
Login + User Management + Role Management
Content + Category + Tag + Media
```

Split immediately when responsibilities mix.

Correct examples:

```text
LoginUseCase
RefreshTokenUseCase
CreateUserUseCase
UpdateUserUseCase
DeleteUserUseCase
AssignRolePermissionsUseCase
PermissionChecker
AuditWriter
SqlSugarUserRepository
```

Forbidden examples:

```text
SystemService
CommonService
GlobalHelper
UserRoleMenuPermissionService
CmsManager
BaseServiceWithEverything
```

---

## 1.4 Thin Minimal API Endpoints

Minimal API endpoints are HTTP adapters only.

Allowed in endpoints:

```text
Route definition
Request binding
Authorization declaration
Calling one UseCase / Handler
Returning TypedResults
OpenAPI metadata
```

Forbidden in endpoints:

```text
Business rules
SQL
SqlSugarClient
Transaction handling
Password hashing
Token generation
Permission calculation
Large conditional workflows
Data mapping longer than trivial mapping
```

Required style:

```csharp
group.MapPost("/users", CreateUserEndpoint.Handle)
     .RequirePermission("system.user.create");
```

Endpoint handlers must delegate to focused UseCase / Handler classes.

---

## 1.5 Immutable Cross-Stage Models

Data models passed across stages should be immutable where practical.

Recommended:

```csharp
public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);
```

Recommended for DTOs and value models:

```text
record
readonly record struct
init-only properties
private setters where mutation is required only by ORM
```

Forbidden:

```text
Mutating DTOs during pipeline processing
Using one mutable object as request, domain model, database entity, and response
Adding UI-only state into backend DTOs
```

---

# 2. High Cohesion and Low Coupling

## 2.1 Dependency Matrix

Allowed project dependency direction:

```text
WeCms.Api
  -> WeCms.Modules.System
  -> WeCms.Modules.Cms
  -> WeCms.Infrastructure
  -> WeCms.Persistence

WeCms.Modules.System
  -> WeCms.Core
  -> WeCms.Contracts
  -> WeCms.Abstractions

WeCms.Modules.Cms
  -> WeCms.Core
  -> WeCms.Contracts
  -> WeCms.Abstractions

WeCms.Infrastructure
  -> WeCms.Core
  -> WeCms.Contracts
  -> WeCms.Abstractions

WeCms.Persistence
  -> WeCms.Core
  -> WeCms.Contracts
  -> WeCms.Abstractions
  -> WeCms.Infrastructure
  -> SqlSugarCore
```

Hard restrictions:

```text
Only WeCms.Persistence may reference SqlSugarCore.
Modules must not reference WeCms.Persistence.
Modules must not reference SqlSugarCore.
Contracts must not reference Infrastructure or Persistence.
Core must not reference business modules.
Abstractions must not reference implementation projects.
Infrastructure must not reference Persistence.
Persistence must not reference Api.
```

---

## 2.2 Project Responsibilities

### WeCms.Core

Allowed:

```text
Base entities
Result
Error
DomainException
Pagination
Constants
Enums
Guard
PermissionCode
```

Forbidden:

```text
SqlSugar
HTTP
JWT implementation
Database access
Business module logic
Infrastructure implementation
```

---

### WeCms.Contracts

Allowed:

```text
Request DTOs
Response DTOs
API contract models
Paged response contracts
OpenAPI-visible models
```

Forbidden:

```text
Database entities
SqlSugar attributes
Business behavior
UI-only fields
Repository interfaces
Service implementations
```

---

### WeCms.Abstractions

Allowed:

```text
Interfaces only
IUnitOfWork
IRepository
ICurrentUser
IClock
IIdGenerator
IPasswordHasher
ITokenService
IFileStorage
ICacheService
IAuditWriter
ILoginLogWriter
IPermissionChecker
```

Forbidden:

```text
Implementations
SqlSugar
Database entities
HTTP endpoints
Business use cases
```

---

### WeCms.Infrastructure

Allowed:

```text
JWT implementation
Password hashing
Current user accessor
Clock implementation
ID generator
Memory cache
Local file storage
Audit context
Configuration options
```

Forbidden:

```text
SqlSugar configuration
Database entities
Migration logic
Repository implementation
Business module logic
```

---

### WeCms.Persistence

Allowed:

```text
SqlSugarCore
Database entities
Table mappings
Migration
Seed
Repository implementations
UnitOfWork implementation
Soft delete filters
Audit field interceptors
```

Forbidden:

```text
API endpoints
Business use cases
Frontend code
Module logic
HTTP response handling
```

---

## 2.3 Single File Limit

Any `.cs` file longer than **600 lines** must be split.

Preferred limits:

```text
UseCase / Handler: ≤ 200 lines
Endpoint file: ≤ 150 lines
Repository: ≤ 300 lines
Entity: ≤ 200 lines
DTO file: ≤ 150 lines
Test file: ≤ 500 lines
```

If a file grows beyond the limit, split by responsibility before adding more logic.

---

## 2.4 Namespace Must Match Directory

Namespace must match directory structure.

`IDE0130` must be treated as an error.

Example:

```text
src/WeCms.Modules.System/Users/CreateUserUseCase.cs
```

Must use:

```csharp
namespace WeCms.Modules.System.Users;
```

---

## 2.5 InternalsVisibleTo Whitelist

`InternalsVisibleTo` is allowed only for the corresponding test project.

Allowed:

```csharp
[assembly: InternalsVisibleTo("WeCms.Tests.Unit")]
[assembly: InternalsVisibleTo("WeCms.Tests.Integration")]
```

Forbidden:

```text
Exposing internals to other production projects
Using InternalsVisibleTo to bypass architecture boundaries
```

---

## 2.6 Cross-Cutting Concerns Must Be Centralized

Cross-cutting logic must be centralized.

Examples:

```text
Error codes
Diagnostic codes
Slug generation
URL helpers
Date/time abstraction
ID generation
Password hashing
Token generation
Audit writing
ProblemDetails mapping
```

Do not duplicate cross-cutting logic inside business modules.

---

## 2.7 Third-Party Package Discipline

New NuGet packages are not allowed casually.

Any new NuGet dependency must explain:

```text
Why built-in .NET APIs are insufficient
Why this package is AOT-safe
Whether it supports trimming
Whether it introduces reflection-heavy behavior
Whether it is used only in the correct layer
```

Hard rule:

```text
AOT compatibility must be verified before accepting a new dependency.
```

---

# 2.5 Reject Implicit Compatibility

## 2.5.1 Boundary Is Contract

Validation should happen at system boundaries:

```text
HTTP request input
Configuration loading
Database migration input
File upload
External service response
Environment variables
OpenAPI contract
```

Internal modules should trust already-validated contracts.

Forbidden:

```text
Repeated defensive validation between internal modules
Silent defaulting of missing required values
Hidden fallback behavior
Legacy branches without active spec
```

---

## 2.5.2 Fail Fast

If required input is invalid, missing, or out of contract, fail fast.

Required:

```text
Throw clear exception
Return clear ProblemDetails
Use explicit error code
Log meaningful context
```

Forbidden:

```csharp
value ?? "default";
try { ... } catch { return null; }
if (legacy) { ... }
if (oldFormat) { ... }
catch { }
```

Forbidden patterns:

```text
Silent catch
Dead fallback
Legacy fallback
Auto migration of old API shape
Default values that hide missing config
```

---

## 2.5.3 No Automatic Legacy Migration

The rebuild project has no legacy compatibility obligation.

Forbidden:

```text
Old field fallback
Old route fallback
Old request body fallback
Old permission code fallback
Old menu shape fallback
Legacy database schema compatibility
```

If a breaking change is required after contract freeze, it must go through a spec.

---

## 2.5.4 Delete Means Delete

When removing an API, field, command, option, or module, remove it completely.

Forbidden:

```text
[Obsolete] forwarding
// removed: comments
Empty shell re-export
Hidden compatibility adapter
Legacy alias
```

Exception:

```text
Major-version migration compatibility is allowed only with a written spec under .trae/specs/<change-id>/.
```

---

# 3. Agile Development Rules

## 3.1 Spec First

Any change that meets one of the following conditions must create a spec first:

```text
≥ 200 lines of production code change
New public API
New module
New database table
New permission model
New migration strategy
API contract change after freeze
Architecture boundary change
New third-party dependency
```

Spec path:

```text
.trae/specs/<change-id>/
  spec.md
  tasks.md
  checklist.md
```

PR / task description must include:

```text
Spec: .trae/specs/<change-id>/
```

---

## 3.2 Small PR / Small Task

Single PR or Codex task diff should be ≤ **400 lines**.

If larger, the task must explain:

```text
Reason: oversized because ...
```

But the preferred action is to split the task.

Each task must have:

```text
One goal
One affected module
Clear acceptance criteria
Tests
No unrelated refactoring
No future-version implementation
```

---

## 3.3 Traceability

Every PR / Codex task must include one of:

```text
Closes #<issue>
Spec: .trae/specs/<change-id>/
Task: V0.x / T0.x.x
```

No untracked work is allowed.

---

## 3.4 Main Branch Must Stay Green

The `main` branch must always pass:

```bash
bash scripts/quality-gate.sh
```

Forbidden:

```text
Merge first, fix later
Known failing tests
Temporarily skipped quality gate
Temporary removal of tests
Lowering coverage threshold to pass
```

---

## 3.5 Clarify Ambiguous Requirements

If a requirement is unclear, ask before implementation.

AI agents must not write code based on guesses such as:

```text
I assume the user wants...
Probably this field means...
Maybe frontend needs...
```

For unclear requirements:

```text
AskUserQuestion
Create a spec
Document assumptions explicitly
```

---

## 3.6 Documentation Synchronization

When behavior changes, documentation must change in the same task.

Required documentation updates:

```text
Backend API behavior -> docs/api/
Architecture boundary -> docs/codex/architecture-boundaries.md
Task plan change -> docs/plans/
Developer rule change -> docs/codex/
User-visible behavior -> docs/user/
```

---

# 4. TDD Rules

## 4.1 Red → Green → Refactor Is Required

For `.cs` production code changes, follow:

### Red

Write a failing xUnit test first.

Run the specific test and confirm failure:

```bash
dotnet test --filter FullyQualifiedName~XxxTests
```

### Green

Write the smallest implementation that makes the test pass.

### Refactor

Refactor under test protection while keeping all tests green.

---

## 4.2 Bug Fix Must Start With Reproduction Test

Any bugfix must begin with a failing test that reproduces the bug.

Recommended commit message:

```text
test: reproduce <bug-id>
```

Only after the failing test exists may the fix be implemented.

---

## 4.3 Coverage Gate

The quality gate should enforce line coverage ≥ **80%**.

Forbidden:

```text
Lowering coverage to pass
Removing tests to pass
Marking code with ExcludeFromCodeCoverage to bypass gate
Skipping tests without spec
```

Temporary coverage changes require a separate spec and review.

---

## 4.4 Test Naming

Test names must follow:

```text
MethodUnderTest_Should<Behavior>_When<Condition>
```

Examples:

```text
Login_ShouldReturnTokenPair_WhenCredentialsAreValid
Login_ShouldFail_WhenPasswordIsInvalid
RefreshToken_ShouldRevokeOldToken_WhenRefreshSucceeds
CreateUser_ShouldFail_WhenUsernameAlreadyExists
```

---

## 4.5 Test-to-Class Mapping

Each production class should have a corresponding test class.

Examples:

```text
CreateUserUseCase.cs -> CreateUserUseCaseTests.cs
JwtTokenService.cs -> JwtTokenServiceTests.cs
SqlSugarUserRepository.cs -> SqlSugarUserRepositoryTests.cs
PermissionChecker.cs -> PermissionCheckerTests.cs
```

Edge cases may be split:

```text
CreateUserUseCaseEdgeCasesTests.cs
PermissionCheckerIntegrationTests.cs
```

---

## 4.6 Testability Is a Design Requirement

If logic is difficult to test, refactor it.

Forbidden:

```text
Skipping tests because logic is hard to test
Putting business logic in static global helpers
Hiding dependencies inside new expressions
Using service locator to avoid constructor parameters
```

---

# 5. Backend-First API Contract Rules

## 5.1 Frontend Development Is Postponed

Vue Admin frontend development must start only after backend API contract freeze.

Required version order:

```text
V0.1 Project Skeleton
V0.2 Core / Contracts / Abstractions
V0.3 Infrastructure
V0.4 Persistence + SqlSugar + AOT Validation
V0.5 Auth Backend API
V0.6 RBAC Backend API
V0.7 System Backend API
V0.8 CMS Backend API
V0.9 Backend API Contract Freeze
V0.10 Vue Admin
V1.0 MVP Stabilization
```

Forbidden:

```text
Developing frontend before V0.9
Creating final frontend API types before OpenAPI exists
Using mock data to define final API shape
Changing backend DTOs to fit frontend guesses
```

---

## 5.2 Backend Is Source of Truth

Frontend must follow:

```text
WeCms.Contracts
OpenAPI JSON
Actual backend response schema
PermissionCode catalog
Backend menu structure
ProblemDetails error structure
PageResult pagination structure
```

Frontend must not:

```text
Add API fields
Remove API fields
Rename API fields
Change field types
Change enum values
Invent permission codes
Invent menu fields
Invent backend statuses
```

---

## 5.3 Generated Types Must Not Be Edited

Generated frontend API types must not be manually edited.

Recommended path:

```text
frontend/admin/src/api/generated/
```

If generated types are wrong, fix the backend contract first.

---

# 6. AOT-First Rules

## 6.1 Native AOT Is a Hard Gate

Every backend version must preserve Native AOT compatibility.

Required command:

```bash
dotnet publish src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true
```

Forbidden:

```text
MVC Controllers
Session
Reflection-heavy runtime scanning
AutoMapper runtime mapping
Dynamic plugin loading
Runtime code generation
Unverified third-party packages
```

---

## 6.2 Explicit Registration Required

Use explicit registration.

Allowed:

```csharp
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddSystemModule();
builder.Services.AddCmsModule();

app.MapSystemModule();
app.MapCmsModule();
```

Forbidden:

```text
Scanning all assemblies for endpoints
Scanning all assemblies for services
Reflection-based convention registration without AOT verification
```

---

# 7. Definition of Done

A task or PR is complete only when all relevant items are true:

```text
[ ] Red → Green → Refactor followed, or explicitly N/A because no code logic changed
[ ] Task scope is minimal and traceable
[ ] Code builds successfully
[ ] Relevant tests pass
[ ] New or changed production class has corresponding tests
[ ] Native AOT publish does not regress
[ ] OpenAPI contract remains valid
[ ] New side-effect service has I* interface
[ ] Dependencies are injected through constructors
[ ] No service locator in business code
[ ] No module references WeCms.Persistence
[ ] No module references SqlSugarCore
[ ] SqlSugarCore exists only in WeCms.Persistence
[ ] File length limits are respected
[ ] Namespace matches directory
[ ] No implicit compatibility fallback
[ ] No silent catch
[ ] No dead fallback
[ ] No obsolete forwarding
[ ] ≥ 200 line or public API change has .trae/specs/<change-id>/ spec
[ ] Documentation updated when behavior or contract changed
[ ] Quality gate executed
```

Frontend task is complete only when:

```text
[ ] Backend API contract already exists
[ ] Frontend uses backend OpenAPI types
[ ] Generated API types are not manually edited
[ ] No mock data defines final API shape
[ ] pnpm typecheck passes
[ ] pnpm build passes
```

---

# 8. Quality Gate

The repository must provide:

```bash
bash scripts/quality-gate.sh
```

The gate should include:

```text
dotnet restore
dotnet build -c Release
dotnet test -c Release
coverage check >= 80%
dotnet format --verify-no-changes
OpenAPI export/check
architecture tests
AOT publish
frontend typecheck, only after frontend phase starts
frontend build, only after frontend phase starts
```

Before claiming completion, AI agents must actually run the quality gate and inspect the output.

Forbidden:

```text
Claiming success without running commands
Assuming tests pass
Ignoring failed quality gate
Removing checks to pass
Lowering coverage threshold to pass
```

---

# 9. AI Collaboration Hard Instructions

When an AI collaborator works on this repository, it must:

1. Read `AGENTS.md` and all referenced `docs/codex/*.md` files before editing.
2. Follow the approved version order.
3. Work on only one minimal task at a time.
4. Create `.trae/specs/<change-id>/` for ≥ 200 line changes or public API changes.
5. Use TDD for `.cs` production code changes.
6. Keep endpoints thin.
7. Use interfaces and constructor injection for side-effect services.
8. Preserve AOT compatibility.
9. Never reference SqlSugar outside `WeCms.Persistence`.
10. Never let frontend define backend data structures.
11. Run `bash scripts/quality-gate.sh` before claiming completion.
12. Report exactly which commands were run and whether they passed.
13. If a command fails, fix the failure before moving to another task.
14. Do not lower coverage, remove tests, or weaken gates to pass.
15. Ask for clarification when requirements are ambiguous.

---

# 10. Hard Red Lines

The following are strictly forbidden:

```text
God Service
CommonService dumping ground
GlobalHelper dumping ground
Endpoint with business logic
Endpoint with SQL
Manual new of side-effect dependencies
Service Locator in business code
Module referencing Persistence
Module referencing SqlSugarCore
DTO mixed with Entity
SqlSugar attributes in Contracts
Returning Persistence Entity from API
Implicit legacy compatibility
Silent catch
Dead fallback
[Obsolete] forwarding for removed APIs
Frontend before backend contract freeze
Frontend modifying backend API structure
Mock data defining final contract
Unverified AOT-unsafe package
```

---

# 11. Final Rule

If a proposed change violates any of the following, do not implement it:

```text
Object-oriented design
SOLID
High cohesion
Low coupling
Dependency injection
TDD
Small agile delivery
Backend-first API contract
Native AOT compatibility
Fail-fast contract enforcement
```
