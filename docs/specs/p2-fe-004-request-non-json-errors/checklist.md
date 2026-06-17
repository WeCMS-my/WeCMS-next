# Checklist

- [x] `request.ts` does not call `response.json()` directly.
- [x] Empty response bodies are handled.
- [x] Non-JSON response bodies are handled with a bounded message.
- [x] 401 refresh flow still retries once.
- [x] Frontend typecheck/build and frontend quality gate pass.
