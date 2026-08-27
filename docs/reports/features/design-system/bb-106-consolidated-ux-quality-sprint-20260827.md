# BB-106 — Consolidated UX / Quality Fix Sprint

## Metadata

- Date: 2026-08-27
- Scope: BigBrain Web and documentation; no backend/runtime data mutation
- Sanitization: This report contains no secrets, private addresses, owner data or raw service payloads.

Detta är en sanerad GitHub-version. Lokal rå runtime-evidens publiceras inte.

## Status

IMPLEMENTED / AUTOMATICALLY VERIFIED / WEB DEPLOYED / OWNER UX REVIEW PENDING

## Changes

### Shared root fixes

The late-loaded `.bb-page` padding shorthand overrode the mobile shell's dock clearance. The combined
`.bb-page.dashboard-workspace` contract now reapplies `--bb-mobile-dock-clearance`, including safe area,
after that shorthand. Shared controls gained visible material boundaries and hover/focus/disabled/error
states; one reduced-motion-aware three-dot loading status preserves control dimensions. `BBMediaArtwork`
turns absent and failed images into an intentional branded placeholder. Global min-width, wrapping and
media bounds prevent ordinary horizontal scroll, and shared list rows use the spacing scale.

### Traceability

| Fix | Status | Evidence / remaining scope |
| --- | --- | --- |
| FIX-01 | FIXED | Active, attention and collapsed paginated acquisition history; no provider/media deletion. |
| FIX-02 | FIXED | Shared overflow/min-width/wrapping rules; deployed narrow widths have no horizontal overflow. |
| FIX-03 | FIXED | Shell cascade root cause corrected; safe-area/dock token owns final reachability. |
| FIX-04 | FIXED | One accessible, dimension-stable loading primitive; reduced motion covered. |
| FIX-05 | FIXED | Provider and metadata-pipeline language removed from normal audiobook search/results. |
| FIX-06 | PARTIAL | BigBrain owns browse/detail/cover/progress/continue/play-open. Audiobookshelf 2.36.0 contains internal play/session/track routes, but they are absent from its official OpenAPI contract and require a reviewed Range proxy plus start/sync/close lifecycle. Depending on those undocumented routes here would be version-fragile; no owner action is required until a dedicated playback-contract slice is chosen. |
| FIX-07 | FIXED | Shared missing-media component catches absent and failed artwork. |
| FIX-08 | FIXED | Shared row/acquisition padding uses spacing tokens. |
| FIX-09 | FIXED | Local-date past/today/future presentation with restrained strike-through/accent. |
| FIX-10 | FIXED | Home uses meaningful overview labels; next event includes local date, title and time. |
| FIX-11 | FIXED | Shared textual field boundary plus hover/focus/disabled/error states; Family overrides reconciled. |
| FIX-12 | FIXED | Concise Swedish add/confirm decision; technical settings remain optional disclosure. |
| FIX-13 | FIXED | Bounded server paging, library search/sort/load-more and prominent Continue Listening. |
| FIX-14 | PARTIAL | Standard feedback, duplicate blocking, safe retry messages and existing bounded cancellation/SSE reconnect retained. A transient Media aggregate delay was reproduced while several downstream services timed out, then recovered to HTTP 200 in 5.6 s; this did not justify a global timeout increase. Next step is endpoint-specific timing instrumentation if owner failures recur. |
| FIX-15 | DOCUMENTED | Future account/strategy/risk/evaluation/execution isolation added without runtime Finance changes. |

## Evidence

Focused Web tests passed 27 before full regression; final Web total is 135. Release validation passed
555 API and 32 Sentinel tests with zero failures, plus Vite/Release builds, Compose and diff checks.
Only Web was rebuilt/recreated; the resulting container is healthy. Browser QA covers 390×844 and
430×932 in Obsidian Gold, Forest Night and Arctic Wind plus 1440×900. No acquisition was created,
cancelled or restarted; no media/volume/backend integration changed. Finance execution authority,
scheduler and governor are unchanged. BB-099 remains TECHNICALLY COMPLETE / OWNER UX REVIEW PENDING.

## Security

No credentials or provider-private identifiers enter Web or this report. Existing acquisition
confirmation, opaque identifiers, no-overwrite/import safety and server-side integration credentials
are unchanged. No active job, provider, media file or volume was mutated by QA.

## Remaining work

- Embedded Audiobookshelf pause/resume/seek/chapter support awaits a reviewed server-side playback
  session and streaming contract. The installed 2.36.0 implementation proves the capability exists,
  but the official OpenAPI specification does not publish that contract.
- If module response failures recur, collect sanitized endpoint-specific timing evidence before changing
  any timeout; no broad timeout inflation is approved.
- Owner visual review remains pending, including the pre-existing BB-099 gate.

## Resumption

Start from the published BB-106 commit and current `docs/STATUS.md`. Reproduce only a remaining item,
preserve Finance and media safety state, and do not infer owner UX approval from automated/browser QA.
