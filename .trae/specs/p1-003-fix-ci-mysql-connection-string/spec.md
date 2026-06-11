# Fix CI MySQL Connection String Environment Variable Spec

## Why
The CI `backend-quality-gate.yml` sets `WeCMS__ConnectionStrings__Default` as the environment variable to override the MySQL connection string. However, ASP.NET Core's `GetConnectionString("Default")` resolves only `ConnectionStrings:Default` — the `WeCMS__` prefix maps to `WeCMS:ConnectionStrings:Default`, which is the wrong config key. As a result, the integration tests in CI fall back to `appsettings.Development.json` which uses the `wecms` MySQL user. The MySQL service container only creates a `root` user, causing `Access denied for user 'wecms'` and all 8 `ExceptionMiddlewareTests` fail.

## What Changes
- Fix the CI workflow environment variable from `WeCMS__ConnectionStrings__Default` to `ConnectionStrings__Default` so it correctly overrides the database connection string.

## Impact
- Affected specs: none (CI infra fix)
- Affected code: `.github/workflows/backend-quality-gate.yml`

## MODIFIED Requirements
### Requirement: CI MySQL Connection
The CI workflow SHALL provide a valid MySQL connection string that matches the `root` user and `wecms_dev_pass` password created by the MySQL service container, so that integration tests that require a database connection can start successfully.

#### Scenario: Integration tests connect to CI MySQL
- **WHEN** the backend quality gate workflow runs
- **THEN** the `ConnectionStrings__Default` environment variable SHALL be set to `Server=127.0.0.1;Port=3306;Database=wecms_dev;User=root;Password=wecms_dev_pass;Charset=utf8mb4;`
- **AND** the `ExceptionMiddlewareTests` integration tests SHALL pass
