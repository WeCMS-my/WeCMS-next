# M2-FE-002 Checklist

- [x] Spec exists before Auth code.
- [x] Red auth check observed before implementation.
- [x] Login rejects blank username/password.
- [x] Login success stores access token in memory and auth state.
- [x] Login failure displays an error.
- [x] `/auth/me` restores user state.
- [x] Logout clears local state and redirects login.
- [x] 401 refresh queue has a single shared refresh promise.
- [x] Refresh/logout use the HttpOnly refresh-token cookie and do not send refresh-token JSON.
- [x] Refresh failure clears session.
- [x] Protected route redirects anonymous users.
- [x] Request client preserves backend `data` shape.
- [x] No password persistence or sensitive logging.
- [x] No token persistence in `localStorage` or `sessionStorage`.
- [x] Frontend gate passes.
- [x] No backend production code changed.
