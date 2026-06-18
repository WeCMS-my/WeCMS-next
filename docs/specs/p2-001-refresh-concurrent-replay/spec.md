# P2-001 Refresh Concurrent Replay Semantics

## Goal

Clarify and harden refresh-token replay handling without changing the established tolerance for near-simultaneous browser refresh requests.

## Context

The current auth implementation already distinguishes two effective runtime outcomes:

1. A revoked refresh token reused long after rotation revokes the whole token family.
2. A revoked refresh token replayed within a short window after rotation is treated as concurrent replay and does not revoke the new active token.

This behavior is covered by existing unit and integration tests, so it is current repo-truth rather than an accidental gap.

The current gap is semantic:

- documentation does not explain the 2-second replay window or its threat model;
- concurrent replay and true token reuse both emit `auth.refresh_reuse`, which makes audit interpretation ambiguous.

## Decision

Keep the existing 2-second concurrent replay tolerance window.

Within that window:

- exactly one refresh succeeds;
- the rotated replacement token remains active;
- the rejected request is classified as `auth.refresh_concurrent_replay`.

Outside that window, or when rotation evidence is incomplete:

- the event remains `auth.refresh_reuse`;
- the whole refresh-token family is revoked.

## Required Changes

1. Add an ADR that documents:
   - the 2-second concurrent replay tolerance window;
   - why the system prefers avoiding false-positive family revocation for near-simultaneous browser refreshes;
   - the residual risk that a stolen token replayed inside that window is classified as concurrent replay rather than family-compromising reuse.
2. Update auth service behavior so concurrent replay emits `auth.refresh_concurrent_replay` instead of `auth.refresh_reuse`.
3. Keep family revocation semantics unchanged for true reuse outside the window.
4. Update unit and integration tests to prove the event split.

## Non-Goals

- Removing the 2-second tolerance window.
- Redesigning refresh-token storage or cookie transport.
- Changing frontend refresh queue behavior.
- Changing public HTTP request or response contracts.
