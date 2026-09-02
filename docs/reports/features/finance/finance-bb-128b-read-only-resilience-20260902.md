# BB-128B Finance Read-Only Resilience & Last-Known-Good UX

Detta är en sanerad GitHub-version. It contains no credential, private address, provider payload or unrestricted API response.

## Metadata and result

- Date: 2026-09-02
- Baseline: `8d374e22f5927cb4afce7c0de77ee5cca9b32d49`
- Implementation: `6eabbafed72c5df9a7484167a98acb0249c8a39d`
- GitHub Actions: `33657212798` — backend, frontend, documentation and secrets passed
- Result: **IMPLEMENTED / AUTOMATICALLY VERIFIED / CI VERIFIED / NOT DEPLOYED / OWNER UX REVIEW PENDING**
- Finance: `RESEARCH / 0 SEK / NONE`

## Status

Implementation, deterministic local verification and CI verification are complete. Deployment, runtime verification and owner iPhone/PWA UX approval are not yet complete.

## Evidence

Evidence is the scoped source diff, deterministic Web tests, production Web build and documentation/Compose/diff/secret publication gates. No runtime or deployment claim is used.

## Root cause and design

`getFinanceObservation()` was the page-level first-render gate. Its initial failure set one fatal flag and replaced the entire Finance view, while numerous technical detail requests also started when the collapsed detail section mounted. The API already provided a bounded observation projection and individual panels already degraded locally, so no backend endpoint or general offline framework was justified.

The Web client now retains version-1 last-known-good observation display state in `localStorage`. A compatible entry renders synchronously, visibly marked stale, while the observation is revalidated. Success replaces the view and cache; temporary failure keeps cached content and shows a calm degraded notice plus `Försök igen`. With no compatible cache, failure retains the existing honest unavailable state and now provides in-place retry.

Only one refresh may be in flight. Mount, a return to visible state and the browser `online` event trigger revalidation; no timer or polling loop exists. Navigation aborts are ignored rather than shown as backend failures. The retry button remains disabled during a request and never clears displayed data.

## Cache boundary

The key is `bigbrain.finance.last-known-good.v1`; schema version is `1`. It stores browser fetch time separately from API market/source timestamps and declares its one included section (`observation`). Its explicit allowlisted projection contains Finance safety/provider display state, latest market-data timestamp, up to 16 watchlist instruments with up to 400 chart points each, historical-memory summary and retention summary. Serialized size is capped at 512,000 characters.

Malformed, oversized, semantically invalid or incompatible-version state is ignored. Unexpected response properties are removed before serialization. Credentials, authorization material, headers, complete database/history, arbitrary API responses, overview/risk/autonomous/detail payloads and action authority are not stored.

This cache describes **what BigBrain last knew**, never **what BigBrain may do now**. It has no action path and cannot authorize provider activation, acquisition, entitlement, research, PAPER/LIVE/AUTO, broker/orders or capital allocation. Backend state remains authoritative and fail-closed.

## Partial failure and request reduction

The owner-facing overview, risk and autonomous-research panels retain their existing local fallback behavior. Features, datasets, backtests, robustness, backups, shadow and scheduler/governor/operations status are now requested only after `Detaljer & forskning` opens. The first layer therefore avoids nine detail requests without changing API contracts or research semantics.

## Changes

- `src/BigBrain.Web/src/finance/FinanceObservation.tsx`: cached-first rendering, revalidation, retry and detail-read deferral.
- `src/BigBrain.Web/src/finance/financeSnapshotCache.ts`: versioned, bounded and allowlisted local cache.
- Finance Web tests and scoped styling.
- Current status, backlog, module, testing and report index documentation.

## Verification

- Focused: `npm test -- --run src/finance/financeSnapshotCache.test.ts src/finance/FinanceObservation.test.tsx` — 23/23 passed.
- Full Web: `npm test` — 171/171 passed.
- Production Web build: `npm run build` — passed.
- Backend tests: not required; no backend/read-model source changed.
- Deployment/runtime: not performed; this sprint did not include separate deployment authorization.
- Owner iPhone/PWA UX: pending after deployment.

The deterministic tests cover successful persistence, cached-first render, refresh success/failure, retained content, no fatal cached failure, first-use failure/retry, repeated failure, request deduplication, visibility/online recovery, abort behavior, malformed/incompatible/oversized cache, allowlisted persistence, unexpected credential-field exclusion and lazy detail reads. Existing Finance tests continue to prove research-only safety and local panel degradation.

## Security

No secret, credential, private address, raw provider payload, authorization header or unrestricted response is persisted or published. The allowlisted cache has no backend write/action capability and cannot alter Finance safety authority.

## Manual owner verification

After a separately authorized Web deployment: load Finance once while healthy; temporarily make the API unreachable through the safe appliance procedure; return to Finance and verify cached content plus stale status; retry while unavailable and verify content remains; restore the API; retry or foreground/reconnect the PWA and verify fresh content replaces stale state without restarting the app. Confirm no stale status appears live or authoritative.

## Remaining work

Only the bounded observation snapshot is durable across browser sessions. First-layer overview, risk and autonomous-research details may show their local unavailable states during an outage rather than cached values. Cache is device/browser-profile local and disappears if site storage is cleared. No future live observation is cached. The next owner decision is to authorize Web deployment and perform the documented physical iPhone/PWA recovery test; owner UX must remain pending until explicit approval.

## Resumption

Resume from this report, `docs/STATUS.md`, `docs/BACKLOG.md`, `docs/modules/finance.md` and `TESTING.md`. The smallest safe next step is a separately authorized Web deployment followed by the documented iPhone/PWA outage/recovery test and explicit owner verdict.
