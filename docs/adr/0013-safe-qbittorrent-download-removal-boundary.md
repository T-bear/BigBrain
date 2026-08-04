# ADR 0013: Safe qBittorrent download removal boundary

- Status: Proposed
- Date: 2026-08-04

## Context

BigBrain needs an intentionally narrow write capability for removing one unwanted entry from the user's existing qBittorrent queue. qBittorrent's Web API accepts multiple hashes or `all`, and `deleteFiles=true` also deletes downloaded data. Browser-visible hashes, broad operations, stale confirmation and uncertain shared paths would create unacceptable risk. Sonarr and Radarr may own queue entries but remain separate systems with their own history, blocklist and search semantics.

## Decision

Download Control remains inside the Media modular monolith and uses the existing authenticated qBittorrent adapter. The public API lists safe normalized fields and process-local random opaque identifiers. An opaque identifier binds one live hash to a fingerprint of stable display and path characteristics for five minutes; raw hashes and paths never cross the backend boundary.

Removal is a preview/confirm operation. Preview refreshes the queue, revalidates identity and issues a single-use, two-minute server-side confirmation bound to the opaque target, fingerprint and explicit `deleteData` choice. Confirm re-reads and revalidates immediately before one adapter call. Concurrent reuse is rejected and completed/missing results are idempotent from the user's perspective. Audit records operation, destructive choice and safe result only.

The default operation sends exactly one hash and `deleteFiles=false`. Destructive removal is separate, never defaulted, and sends `deleteFiles=true` only after an explicit destructive preview and UI acknowledgement. It is blocked for empty or ambiguous content paths, shared content paths, root-like save-path identity, completed/import-uncertain jobs, changed identity or any other uncertain scope. Safe removal with files preserved remains available when destructive removal is blocked.

Category is a presentation warning, not authorization. Sonarr- or Radarr-categorized jobs may be removed from qBittorrent after confirmation, but BigBrain does not mutate Arr history, blocklist, searches, monitored state, media or download-client configuration. The UI explains that Arr may download the item again.

Provider errors map to stable Problem Details codes. Raw upstream bodies, hashes, SID/cookies, credentials, magnet links and paths are excluded. No general qBittorrent proxy, database, broker or distributed lock is introduced. The process-local contract is single-instance only.

## Consequences

- A restart invalidates opaque IDs and confirmations, requiring a fresh list/preview.
- Safe removal is usable broadly; destructive removal is deliberately unavailable whenever certainty is insufficient.
- The API cannot prove filesystem hardlink topology because the API process has no general filesystem capability. Completed/import-uncertain jobs are therefore blocked from destructive removal.
- Multi-instance deployment requires a separately decided durable store and distributed concurrency contract.
- Mass operations, completed-job cleanup/retention and coordinated Arr blocklist/search are deferred backlog work with separate authorization.

## Alternatives rejected

- Browser-supplied raw hashes or lists: exposes upstream identity and enables broad targeting.
- A general qBittorrent proxy: exceeds the capability boundary.
- Global delete or clear-all: cannot guarantee one intended target.
- Automatic Arr mutation: combines independent write contracts without explicit scope.
- Unconditional `deleteFiles=true`: cannot protect shared, imported or ambiguously scoped data.
