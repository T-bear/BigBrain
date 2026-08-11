# Finance read-only market observation UI foundation

> Entitlement status update: later human Twelve Data evidence on 2026-08-11 clears the
> submitted use only on a paid Personal plan; Basic is insufficient. No provider is selected
> or active, so this report's fail-closed runtime boundary remains unchanged.

## Metadata

- Date: 2026-08-11
- Scope: BB-074 early M2 research observation UI
- Runtime/deployment: API and Web deployed and technically verified 2026-08-11

## Status

Implemented, automatically verified, deployed and technically runtime-verified. Manual
product-owner UI verification has not been claimed. See the separate deployment report.

## Changes

Finance now has a navigable first-party Web view and a versioned read-only endpoint at
`GET /api/v1/modules/finance/observation`. The Finance-owned snapshot contains typed safety,
provider and entitlement states; last update; explicit none/synthetic/real classification;
canonical instrument plus ticker/display identity; optional price/change/history;
freshness, session and quality; and sanitized historical-memory revision, coverage, counts,
persistence, policy and provenance summaries.

The compiled Web widget provides a prominent RESEARCH badge and “Ingen handel med riktiga
pengar”, configured research watchlist, honest empty/error/synthetic/stale/gap/session states,
historical-memory and entitlement panels, and a responsive first-party SVG chart. The chart
breaks lines at declared gaps and includes a textual summary/fallback.

## Security

`SafeDefaultFinanceObservationReader` is the only production registration. It reports no
authorized provider, BB-071 State B, ingestion and real-data storage denied, no broker,
PAPER/LIVE false, no observations and no durable memory. The documented eight-symbol US
watchlist is configuration only and triggers no IO. Synthetic populated snapshots exist only
in deterministic tests and remain explicitly `SyntheticFixture` in API/UI state.

No account, credential, provider SDK/adapter/call, real payload, background feed, database,
container, broker, order, execution, PAPER/LIVE/AUTO capability or runtime deployment was
added. Provider DTOs, credentials, secret URLs and private correspondence are absent from
the contract. Unknown entitlement remains fail-closed.

Detta är en sanerad GitHub-version. It contains no secrets, private machine paths, raw logs,
internal identities, real market payloads or sensitive provider correspondence.

## Roadmap boundary

BB-074 is an early M2 research observation surface. It does not start or complete M8.
Portfolio, P&L, positions, orders, signals, risk controls, broker health, execution state,
emergency controls and trading workflows remain in M8 and later gated milestones.

## Evidence

Final verification on 2026-08-11:

- targeted Finance/read-model backend tests: pass;
- targeted Finance/navigation/widget frontend tests: pass;
- `dotnet restore BigBrain.slnx` — pass;
- `dotnet build BigBrain.slnx -c Release --no-restore` — pass, zero warnings/errors;
- `dotnet test BigBrain.slnx -c Release --no-build --no-restore` — pass, 351 API + 32 Sentinel tests;
- `npm test -- --run` — pass, 106/106 frontend tests;
- `npm run build` — pass;
- documentation verification, Compose validation and `git diff --check` — pass after final run.

No network/provider acceptance test was run or authorized.

## Remaining work

Real provider entitlement, owner approval, authorized ingestion, durable production memory
and all PAPER/LIVE/order work remain deferred.

## Resumption

The next real-data gate remains exactly:

```text
BB-071 WRITTEN ENTITLEMENT CONFIRMATION
→ explicit PRODUCT OWNER APPROVAL
→ FIRST AUTHORIZED FREE MARKET DATA INGESTION
```

No written entitlement appeared during this implementation. The expected external action is
sending the exact Twelve Data Basic inquiry in
`docs/architecture/finance/provider-retention-inquiry.md`. No adapter should be implemented
before adequate written evidence and separate product-owner approval.
