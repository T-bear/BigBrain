# BB-119 — Finance Readiness & Resource-Governor Reconciliation

Detta är en sanerad GitHub-version. It contains no credential, private address, hostname, raw provider payload, raw sensitive log or owner-data path.

## Metadata

- Date: 2026-08-31
- Baseline: `5145194ba09ace91f8078c1eba1563bcfd4dbc93`
- Scope: Finance readiness semantics plus the smallest required Sentinel restart compatibility fix

## Status

Implementation and focused tests are complete; deployment/runtime/CI evidence is pending. Finance remains `RESEARCH / 0 SEK / NONE`. No research methodology, scheduler cadence, provider/data policy, strategy, experiment, broker, order, PAPER, LIVE or AUTO capability changed.

## Evidence

1. Scheduler status selected the latest opportunity date even when it was Sunday 2026-08-30, then ran an exact-session database query. Because no fabricated Sunday session exists, the result was misleading `universeIncomplete`, 0/8.
2. Operations mapped only three known failure substrings and defaulted every other latest reason to `READY`. `nonResearchDay` therefore appeared ready although it meant no current session was required.
3. Sentinel's Unix socket lives in a persistent runtime volume. A socket file survived process termination; startup did not remove it, Kestrel failed to bind with address-in-use and the service restarted continuously. Finance correctly converted unavailable metrics to governor `DEFER`.

## Changes

Scheduler and operations share a deterministic current projection that separates historical evidence, current-session requirement/date, universe completeness and feature-lineage readiness. A non-session is explicitly not applicable; an eligible session retains the exact 8/8 and lineage gates. Sentinel removes only its own configured stale socket before binding. Finance still consumes Sentinel through the existing adapter and does not substitute permissive resource defaults.

## Verification

- Focused API suite: 82 passed.
- Sentinel protocol integration: 3 passed, including startup over a pre-existing stale socket.
- Web: 160 passed; production build passed.
- Runtime and GitHub CI: pending publication/deployment.

## Security

The remediation adds no host mount, Docker socket, permissive fallback or new capability. Sentinel removes only its fixed configured protocol socket in its private runtime volume before binding. Resource evidence remains authenticated through the existing adapter and every unknown/stale state fails closed.

## Remaining work

Deploy only Sentinel, API and Web, inspect the same read-only status surfaces, verify health and publish CI/runtime evidence. Do not manually trigger a research run.

## Resumption

Start from the published BB-119 implementation SHA and recheck Sentinel/API health before interpreting governor state. If deployment has not occurred, build and recreate only Sentinel, API and Web; never remove Finance or runtime volumes. Then query scheduler, operations, governor and system overview read-only.
