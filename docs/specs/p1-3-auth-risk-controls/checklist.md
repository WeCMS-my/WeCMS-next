# Checklist

- [x] Login risk is checked before password verification.
- [x] username + IP repeated failures return `ApiCodes.TooManyRequests`.
- [x] IP-only repeated failures return `ApiCodes.TooManyRequests`.
- [x] username-only repeated failures return `ApiCodes.TooManyRequests`.
- [x] Rate-limited attempts record high-severity security events.
- [x] Captcha challenge API exists and is source-generated for AOT.
- [x] Login can require captcha before password verification proceeds.
- [x] 2FA-enabled users receive a backend challenge instead of tokens after password verification.
- [x] 2FA verification issues access and refresh tokens.
- [x] Refresh token reuse severity escalates after repeated events.
- [x] Modules do not contain SQL or direct Dapper calls.
