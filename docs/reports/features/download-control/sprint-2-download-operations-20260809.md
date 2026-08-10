# Sprint 2 Download Control implementation report

> Detta är en sanerad GitHub-version. Hemligheter, privata adresser, råa identiteter, paths och providerpayloads har utelämnats.

## Metadata

- Date: 2026-08-09
- Scope: BB-020 non-destructive batch, BB-023, BB-024, BB-026 and limited BB-033 UX

## Status

Sprint 2 closed and deployment accepted by the product owner on 2026-08-10. The
implemented scope is automatically verified and deployed. All listed manual checks
passed except Retry, whose manual verification remains pending under the documented
non-blocking exception below.

## Evidence

- 214 API tests and 32 Sentinel tests pass in an isolated .NET 10 SDK build.
- 99 frontend tests and the production frontend build pass.
- The API production image builds successfully.
- API, Web and Download Control returned HTTP 200 after the restricted deployment. Calendar counts, theme, integration configuration state and unrelated container identities were unchanged.

## Changes

The Media module owns reusable pause, resume, retry and deterministic diagnostic capabilities. Single and partial-batch requests accept only opaque BigBrain IDs, refresh and revalidate every target, enforce server-side eligibility and call the qBittorrent adapter with one exact target. Batch size is limited to 25. Batch deletion, Arr recovery, retention, real-time synchronization and automation are excluded.

The Web UI adds selection for the current filtered view, selected count, clear selection, non-destructive batch actions, per-row actions and normalized diagnostic explanations. Download Control is presented as **Nedladdningskö** and Media Jobs as **Medieflöde**.

## Security

No browser-visible hash, path or provider payload was added. Every provider mutation has one internally resolved target; batch manifests are bounded and partial. No Arr mutation, delete batch, live torrent mutation or external configuration change was performed.

## Remaining work

Retry remains implemented, automatically tested and deployed but not manually approved.
There was no naturally failing or problematic download available for a safe and
realistic test. This is not a known defect and does not block Sprint 2 closure. Manual
verification must be performed when such a download becomes available.

Destructive batch/delete, retention, Arr recovery and the full BB-033 information-
architecture redesign were not part of the accepted Sprint 2 scope.

## Manual verification

The product owner approved Dashboard, shared theme and theme synchronization, Calendar,
preservation of the imported work schedule, Download Control, Pause/Resume, batch
handling, diagnostics, mobile presentation and the remaining integrations. The deployed
Sprint 2 release is accepted. Retry was not exercised and must not be represented as
manually verified.

No runtime, external service configuration or user data was changed during closure.

## Resumption

When a naturally failing or problematic download becomes available, manually exercise
Retry through BigBrain and verify that only the intended job is affected. Do not create
or mutate a production download merely to manufacture this condition. Update BB-023 and
the current status with dated evidence; until then, keep manual verification pending.
