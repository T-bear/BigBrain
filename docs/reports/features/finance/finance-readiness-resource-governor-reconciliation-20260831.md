# BB-119 — Finance Readiness & Resource-Governor Reconciliation

Detta är en sanerad GitHub-version. It contains no credential, private address, hostname, raw provider payload, raw sensitive log or owner-data path.

## Metadata

- Date: 2026-08-31
- Baseline: `5145194ba09ace91f8078c1eba1563bcfd4dbc93`
- Scope: Finance readiness semantics plus the smallest required Sentinel restart compatibility fix

## Status

Implementation `a0699d841f118d2eccb247e2a9f58c3e522e1da8` is deployed, runtime verified and CI verified by GitHub Actions `33393820120`. Finance remains `RESEARCH / 0 SEK / NONE`. No research methodology, scheduler cadence, provider/data policy, strategy, experiment, broker, order, PAPER, LIVE or AUTO capability changed.

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
- Release solution build, documentation verification, Compose validation and diff check passed.
- Only Sentinel, API and Web were rebuilt/recreated; all three are healthy and API/Web return HTTP 200.
- Read-only runtime at 2026-08-31 13:00 UTC: scheduler enabled/not running; historical evidence true; current session `NOT_REQUIRED_NON_RESEARCH_DAY`; feature lineage `NOT_REQUIRED`; instrument count not applicable; operations agrees and has no active run/attention; governor `ALLOW / resource.ready` with healthy current CPU, memory and one configured disk evidence source.
- Scheduler opportunities remain 10; autonomous runs/experiments remain 0/0. No run was manually triggered.
- GitHub Actions run `33393820120`: passed.

## Security

The remediation adds no host mount, Docker socket, permissive fallback or new capability. Sentinel removes only its fixed configured protocol socket in its private runtime volume before binding. Resource evidence remains authenticated through the existing adapter and every unknown/stale state fails closed.

## Remaining work

No BB-119 remediation remains. Runtime evidence is dated and must be refreshed before a future implementation decision; governor may truthfully return `DEFER` or `BLOCK` when later resource evidence warrants it.

## Resumption

Start from the published BB-119 evidence SHA and recheck Sentinel/API health before interpreting governor state. Query scheduler, operations, governor and system overview read-only; never infer current eligibility from this dated snapshot or manually trigger research for verification.
