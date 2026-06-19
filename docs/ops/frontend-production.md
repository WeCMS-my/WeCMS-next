# Frontend Production

PH-6 defines the SoybeanAdmin production deployment baseline.

## Environment File

Use `frontend/soybean-admin/.env.production.example` as the safe template:

```text
VITE_API_BASE_URL=https://api.example.com
```

Do not commit real production hostnames if they reveal private topology. Do not store secrets in Vite environment variables.

## Deployment Modes

For P1 Production v1, split-domain deployment is supported and secured by `VITE_API_BASE_URL` + `Security:AllowedOrigins`; same-origin remains the simpler default.

Same-origin mode:

- `VITE_API_BASE_URL` is empty.
- The frontend and API share one public origin.
- The reverse proxy routes `/api` and `/health` to the backend.
- This mode is preferred for cookie-based refresh flows because browser credential behavior is simpler.

Split-domain mode:

- `VITE_API_BASE_URL` is the API origin.
- The value must be HTTPS.
- The value must not be `localhost`, loopback, or plain HTTP.
- Backend `Security:AllowedOrigins` must include the frontend origin.
- Cookie, CORS, CSP, and reverse proxy settings must be reviewed together.

## Error Handling

- 401: refresh once; if refresh fails or the retry remains unauthorized, clear in-memory token state and redirect to login.
- 403: show a generic no-permission message.
- 429: show a generic rate-limit message.
- 5xx: show a generic system-error message and do not expose backend exception details.

## Release Checklist

- Run `bash scripts/quality-gate-frontend.sh`.
- Confirm `.env.production.example` remains a template.
- Confirm deploy-specific `VITE_API_BASE_URL` follows same-origin or split-domain rules.
- Confirm frontend and backend are released as a matching OpenAPI/generated-type pair.
