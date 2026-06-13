Backend validation:

dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet publish src/WeCms.Api/WeCms.Api.csproj -c Release -r linux-x64 /p:PublishAot=true

Frontend validation, only after frontend phase starts:

pnpm install
pnpm typecheck
pnpm lint
pnpm build

Contract validation:

- OpenAPI must export successfully.
- All request bodies must have schema.
- All response DTOs must be visible in OpenAPI.
- ProblemDetails must be used for errors.