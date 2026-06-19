# PH-6 Frontend Production Hardening

## Scope

PH-6 hardens SoybeanAdmin production configuration, API base URL rules, and user-facing error handling.

## Requirements

- Provide `.env.production.example` without real production hostnames.
- Document same-origin and split-domain deployment modes.
- Production split-domain API base must use HTTPS and must not use localhost.
- Empty API base is allowed only for same-origin deployments through the reverse proxy.
- 401 must redirect to login after refresh cannot recover the session.
- 403, 429, and 5xx must show generic user-facing messages and not expose backend exception details.
- Frontend gate must include a production environment/configuration check.

## Non-Goals

- No CMS phase-two frontend routes.
- No generated API type hand edits.
- No new UI pages beyond documentation and config checks.
