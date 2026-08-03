# ADR 0011: Smart Shuffle Jellyfin remote playback boundary

- Status: Proposed
- Date: 2026-08-03

## Context

The Media module previously deferred Jellyfin autoplay and every remote-control write.
Smart Shuffle needs one narrowly scoped write to start a server-validated episode on an
explicitly selected, currently connected Jellyfin client. Jellyfin Server 10.11.11
defines `POST /Sessions/{sessionId}/Playing` with `PlayNow`, `itemIds` and optional
`startPositionTicks`.

## Decision

Smart Shuffle remains inside the existing Media module and ASP.NET process. The browser
receives opaque BigBrain device and shuffle-session identifiers and never receives raw
Jellyfin session IDs, user IDs, credentials, provider URLs or arbitrary endpoint access.
The server revalidates the configured user, selected series and live remote-control
session before the only new upstream write: PlayNow for the exact next eligible episode.

Creation requires an explicit user click. Skip is another explicit action. Stop ends
BigBrain automation and does not stop current TV playback. A bounded background
coordinator may start a subsequent episode only for an already user-created active
shuffle session. Structured logs contain opaque BigBrain session IDs and safe device
display names, never credentials or raw Jellyfin identities.

The configured Jellyfin user identity is runtime-only and must match the selected live
session. Browser-provided series and device identifiers are always revalidated against
current server-side Jellyfin data. Provider errors are mapped to stable, sanitized
Problem Details categories; arbitrary item IDs, session IDs, URLs and commands are not
accepted from the browser.

MVP state is process-local, thread-safe and limited to one active session. An API restart
loses automation state and does not stop outstanding TV playback. Multi-replica operation
and durable recovery require a later decision and persistent store.

## Consequences

- Jellyfin remains behind a typed adapter; BigBrain does not proxy or stream media.
- User-specific episode state is mandatory; season 0, played and unplayable episodes are
  excluded, while saved playback position is passed as `startPositionTicks`.
- Provider failures and disappearing sessions become sanitized BigBrain failures.
- Automated tests use fake playback clients and never control a real TV.
- Manual end-to-end TV validation remains a separate operational gate tracked in BB-014.
- The decision remains Proposed pending architecture and security review.
