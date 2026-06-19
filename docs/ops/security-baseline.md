# WeCMS Next Security Baseline

This document defines the PH-1 production security baseline.

## TLS And Proxy

- Production TLS terminates at Nginx, Cloudflare, or a load balancer.
- Kestrel listens on internal HTTP only.
- The app emits HSTS outside Development.
- The app does not call HTTPS redirection by default, because TLS termination at the edge can otherwise create redirect loops.
- Forwarded headers are disabled unless `Security:ForwardedHeaders:Enabled=true`.
- Production forwarded headers require `KnownProxies` or `KnownNetworks`.
- `Security:ForwardedHeaders:ForwardLimit` is applied to cap trusted `X-Forwarded-*` depth (default/Production value: `1`, Production range: `1-32`).

See `docs/ops/deployment-reverse-proxy.md`.

## Cookie Policy

Refresh tokens are issued only through `__Host-wecms_refresh`.

- `HttpOnly=true`
- `Secure=true`
- `SameSite=Strict`
- `Path=/`
- no `Domain`
- append and delete use the same option factory
- `MaxAge` and `Expires` align with refresh-token lifetime on append

Split-domain deployments must keep HTTPS and explicit origins. If a future browser deployment requires cross-site cookies, it must get a separate ADR before changing `__Host-` or `SameSite`.

## CORS

- P1 Production v1 supports split-domain deployments with explicit origin whitelist enforced by ASP.NET CORS middleware.

Production CORS uses `Security:AllowedOrigins`.

- wildcard origins are forbidden
- localhost and loopback origins are forbidden in Production
- HTTP origins are forbidden in Production
- credentials are allowed only for explicit origins
- allowed methods: `GET`, `POST`, `PUT`, `PATCH`, `DELETE`, `OPTIONS`
- allowed headers: request headers are explicitly allowed by policy through ASP.NET Core CORS handling

Development may include localhost origins for Vite.

## CSP Rollout

Configuration keys:

- `Security:SecureHeaders:CspEnabled`
- `Security:SecureHeaders:CspReportOnlyEnabled`
- `Security:SecureHeaders:Csp`
- `Security:SecureHeaders:CspReportOnly`

Production rollout:

1. Report-only: keep `CspReportOnlyEnabled=true`.
2. Collect violations from browser and reverse-proxy logs.
3. Enforce: set `CspEnabled=true` with the reviewed `Csp` value.
4. Tighten: remove unsafe directives or replace them with nonce/hash strategies when frontend assets require it.

Production CSP must include:

- `object-src 'none'`
- `frame-ancestors 'none'` or an explicitly approved ancestor origin

## Rate Limiting

Rate limit policies are documented in `docs/ops/rate-limit-baseline.md`. Rejected requests return HTTP 429 with a generic response and write a security event.

## Verification

- `scripts/checks/check-security-baseline.sh`
- `scripts/checks/check-production-config-baseline.sh`
- `bash scripts/quality-gate-backend.sh`
- `bash scripts/quality-gate-frontend.sh`
