# PH-1 Security Baseline Spec

## Goal

Harden the Phase 1 application security baseline for production deployment without adding CMS business features.

## Scope

- HTTPS / HSTS / reverse proxy strategy.
- Forwarded headers configuration and Production fail-fast checks.
- Production CORS whitelist policy.
- Refresh token cookie option centralization.
- Secure headers CSP report-only and enforce configuration.
- Rate limit production baseline documentation.
- Backend gate checks for security baseline artifacts.

## Non-Goals

- No CMS Articles / Channels / Pages / Media / Tags / Links.
- No AI runtime, provider, prompt, RAG, vector, or agent code.
- No legacy ThinkPHP runtime compatibility.
- No production secret values or real production domains.
- No cloud WAF or third-party alerting integration.

## Decisions

- TLS is terminated by Nginx / Cloudflare / load balancer. Kestrel listens on internal HTTP.
- The app does not blindly enable HTTPS redirection in Production to avoid proxy redirect loops.
- HSTS is emitted outside Development so the edge can pass the header to clients.
- Forwarded headers are disabled by default and require explicit known proxies or networks in Production.
- CORS is driven by `Security:AllowedOrigins`, uses explicit origins with credentials, and rejects wildcard behavior.
- CSP starts as report-only by default; enforce is controlled by `Security:SecureHeaders:CspEnabled`.

## Acceptance

- Production missing or unsafe forwarded headers configuration fails fast when enabled.
- Production CORS rejects empty, wildcard, localhost, and HTTP origins.
- Refresh cookie append and delete options share one factory.
- Secure headers can emit report-only and enforce CSP independently.
- Security docs and gate checks exist.
- Backend and frontend quality gates pass.
