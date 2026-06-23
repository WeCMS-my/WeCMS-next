# Security Event Backpressure

## Scope

This spec covers the S-01 hardening task:

- Rate limit rejected requests must not synchronously write one security event per HTTP 429.
- Rate limit rejection recording must be buffered, aggregated, flushed in the background, and isolated from request response generation.

This spec does not implement S-02 through S-06. Those require separate serial tasks.

## Problem

`RateLimiterOptions.OnRejected` currently resolves `IRateLimitSecurityEventService` and awaits `RecordHitAsync` before writing the HTTP 429 response. Under hostile high-frequency traffic, each rejected request can trigger service resolution, database writes, alert evaluation, and logging on the rejection path. That turns the defense path into a pressure multiplier.

## Requirements

- `OnRejected` must only capture rejection metadata and enqueue it through an in-memory interface.
- If the buffer is full, unhealthy, or unavailable, the request must still return HTTP 429.
- Rejections must be aggregated by rate-limit policy, IP, path, method, and user when available.
- A fixed aggregation window must flush at most one summary security event per aggregate key.
- Flush must run in a hosted background service.
- Flush failures must not affect request handling.
- Repeated flush failures must open a short circuit breaker that skips database writes for a configured cooldown.
- Recovery after cooldown must resume flushing without process restart.
- No new database table or migration is introduced in this task; summary events continue to use `sys_security_event`.
- No MVC, Controller, EF Core, dynamic query/return, AI runtime, or frontend changes are allowed.

## Design

Add module-level abstractions:

- `IRateLimitHitBuffer`
- `IRateLimitHitAggregator`
- `RateLimitSecurityEventFlushHostedService`

The API `OnRejected` path calls `IRateLimitHitBuffer.TryRecord(...)` and immediately writes the 429 JSON response.

The buffer owns aggregation state in memory. A background hosted service periodically drains due aggregate windows and calls the existing `IRateLimitSecurityEventService` with a summary `RateLimitHitRecord`. Summary messages include the aggregated hit count.

The circuit breaker is local to the flush service. Consecutive flush failures open the breaker for a short cooldown. During the open state the request path still accepts/drops buffer entries according to buffer limits, but no database writes are attempted.

## Configuration

Initial defaults are code defaults:

- Window: 60 seconds.
- Flush interval: 10 seconds.
- Max aggregate keys: 4096.
- Failure threshold: 3 consecutive flush failures.
- Circuit breaker cooldown: 60 seconds.

Future tasks may expose these through configuration after production tuning.

## Validation

- Unit tests prove `OnRejected` calls the buffer and does not call `IRateLimitSecurityEventService` directly.
- Unit tests prove repeated hits with the same aggregate key flush as one summary event.
- Unit tests prove different policy/IP/path keys flush separately.
- Unit tests prove flush failure opens the circuit breaker and later recovers.
- Existing rate-limit policy coverage scripts must still pass.
- Backend quality gate should be run after implementation.
