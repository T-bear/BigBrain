# ADR 0016: Safe Download Control command and partial batch boundary

- Status: Accepted
- Date: 2026-08-09

## Context

Download Control needs pause, resume, retry, deterministic diagnostics and bounded multi-selection without exposing qBittorrent identities or moving policy into React. The same capabilities must remain reusable by a future authorized Autonomic consumer.

## Decision

The Media module owns purpose-built `pause`, `resume`, `retry` and diagnostics capabilities. Browser clients send only process-local opaque BigBrain IDs. The service resolves each ID, re-reads the live queue, revalidates the stored fingerprint, checks server-owned eligibility and sends exactly one internally resolved hash to the qBittorrent adapter.

For verified qBittorrent 5.2.3 / Web API 2.15.1, pause uses `torrents/stop`, resume uses `torrents/start`, and retry starts a paused job when necessary and then uses `torrents/reannounce`. Retry never searches, removes, blocklists or mutates Sonarr/Radarr.

Batch requests contain an explicit manifest of 1–25 opaque IDs and one allowlisted operation. Results are partial and ordered per target. Each target repeats resolution, live read, identity revalidation, eligibility and one-target mutation. Results are normalized; raw hashes, paths and provider payloads are excluded. Process-local per-target guards reject overlapping submissions.

Diagnostics is deterministic and read-only. It uses normalized state, transfer speed, queue position and connected peer/seeder counts. It reports insufficient data instead of inferring unverified tracker, disk or network causes. Suggested actions come from the same server-computed capabilities used for mutation.

Batch deletion is excluded. Existing removal preview, confirmation and destructive risk gates remain object-specific. Sonarr/Radarr recovery remains BB-021.

## Consequences

- UI and future trusted automation can reuse the same application capabilities without direct qBittorrent access.
- Opaque identities and concurrency state remain process-local and single-instance.
- Batch is bounded and partial rather than transactional.
- No real-time transport, distributed lock, Arr recovery or general qBittorrent proxy is introduced.
