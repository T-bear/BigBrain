# Media — Integrated audiobook search and simplified navigation

Detta är en sanerad GitHub-version. No secrets, private addresses, credentials or raw provider data are included.

## Metadata

- Date: 2026-10-17.
- Checkpoint: `MEDIA-INTEGRATED-AUDIOBOOK-SEARCH`.
- Branch: `media/integrated-audiobook-search`.
- Accepted baseline: `70b659f772b7b7883a5716786859f1e7833a5634`.
- Baseline tree: `6433c956ff87bc8434a9a8fff6c5294fe45b258d`.
- Publication: resolve the commit with subject `feat(media): integrate audiobook search and hide legacy navigation` on this branch; its Git metadata supplies the exact SHA/tree and file list.
## Status

**REVIEW CHECKPOINT — implementation tested locally; not merged or deployed; physical owner UX review pending.**

## Changes

### Product behavior and ownership

The owner requested Film / Serie / Ljudbok in Mediesökning, with Film as default, and removal of the separate audiobook section. No screenshot was available in this session; implementation uses the explicit written decision and existing production search geometry. The previous BB-117 Media pause is superseded only for this bounded owner-authorized change. Earlier dated design approvals remain historical evidence, not approval of this new composition.

`MediaSearch` retains the shared search form and mode/type selectors. Both modes expose all three types; the visible Båda option is removed without deleting backend support for existing clients. Library Film filters movie results; Serie retains series/season/episode results. The existing movie/series lookup and preview/confirm request flow are retained.

`AudiobookSearch` composes existing versioned BigBrain API calls:

- Hitta nytt calls the existing universal audiobook search with an explicit Swedish/English/all-language preference. Metadata describes a work; it is not proof of availability. Local items and provider editions remain separate. Provider identity, edition identity, narrator, language confidence, cover and source are retained.
- Choosing an edition opens an inline, keyboard-focusable confirmation. Only explicit Begär hämtning posts the unchanged opaque candidate through the existing acquisition API. Capability gates remain visible and server validation remains authoritative. Concurrent clicks cannot submit twice, a failed POST is never automatically retried, and uncertain completion is described honestly. The latest 25 existing jobs can be read on demand in Hitta nytt, including after reopening the page. A returned durable job can be refreshed explicitly through the existing read API; it is not called completed until that API says so.
- Mina bibliotek checks the existing catalog overview status, then queries the existing Audiobookshelf library page through BigBrain. It never requests an acquisition or native playback session.
- Playback links use the API's existing configured `playbackUrl` and open Audiobookshelf. An absent link yields an explicit unavailable-link message. No URL/provider authority is invented.
- Clearing or changing media type, mode or audiobook language cancels pending search reads and removes stale results. Aborted late successes and errors cannot repopulate another search context. Switching controls retains keyboard focus. No automatic search is triggered by selecting a type.

### Existing catalog limitation, explicitly displayed

The accepted `AudiobookshelfClient.GetLibraryAsync` filters title/author within a page fetched by `GetLibraryCoreAsync`; the upstream request does not perform whole-catalog search and the response total is a filtered page count. The new library UI therefore states that it searches **the 50 most recently added books**, not the entire library. Existing universal discovery searches at most the first 25 local books; its UI also states that bound. Narrator search is explicitly unsupported. A no-match message is scoped to that sample, never proof that a title does not exist elsewhere in Audiobookshelf. A full-catalog implementation is outside this UI checkpoint and is not silently fabricated.

## Navigation and preservation

The audiobook widget is unregistered from normal dashboard composition and its widget picker. The AppShell no longer mounts the native playback provider. Existing `/media/audiobooks` and item bookmarks open integrated Media search with Ljudbok selected, so they do not become blank routes or launch the old player. A bookmarked item is not automatically opened or claimed to be selected.

Legacy `Audiobooks`, playback, sleep timer and backend APIs remain in source. The only change inside the legacy component is extraction of its existing Swedish job labels into a shared helper for the new UI. No file, database, playback credential, progress record or acquisition record is deleted or migrated. Re-enabling the old UI is a source-level registration/composition change, without data migration. Existing per-device acquisition-history display preferences are preserved. Administrative language configuration remains available; it is not a native-player entry point.

## Design and accessibility

The existing BBInput/BBButton/BBSelect/BBSurface/BBEmptyState/BBMediaArtwork and Media styles are reused; no new provider, framework, palette or component system is introduced. Selected search controls use the theme's primary-contrast token. Touch targets and focus styling follow the existing contract. UX/UI Lab renders the real type selector and the real local-book result with a missing playback link, marked EXPERIMENTELL. These are fixture-only specimens without API calls. Obsidian-gold, forest-night and arctic-wind remain supported.

## Evidence

All network-facing tests use fixtures; no actual Media provider or deployed API is contacted.

- PASS: `npm test -- src/media-search src/App.test.tsx src/dashboard src/ThemeControl.test.tsx src/components.test.tsx src/ux-lab src/audiobooks src/UXPolishStyles.test.js src/ThemeAssets.test.js` from `src/BigBrain.Web`: **104 passed, 0 failed, 0 skipped, 14 files**. Covers movie/series lookup and library filtering, audiobook discovery/explicit request/job history, catalog-only reads, unavailable/empty/error states, rejected late success/error, clear/type/mode/language changes, focus, old bookmarks, hidden registry/player, preserved display preferences, shared themes, and retained legacy audiobook behavior.
- PASS: `npm run build` from `src/BigBrain.Web`: TypeScript and Vite production build.
- PASS: `npm run format:check` from `src/BigBrain.Web`.
- PASS: local isolated Firefox with a disposable profile, built Web assets and in-browser fixture fetches. At **390×844 and 1440×844**, in obsidian-gold/forest-night/arctic-wind: Film/Serie/Ljudbok present, selected audiobook deep link, minimum measured search-control height **44 px**, no horizontal document overflow, no audio element. This verifies the shared controls/layout, not real iOS rendering or provider health. The subsequently added on-demand job-history read does not change measured controls or CSS.
- PASS: `node scripts/verify-documentation.mjs`: **269 Markdown files, 91 unique backlog IDs**, including relative link validation. `git diff --check` and staged scope verification.
- PASS: staged Gitleaks secret scan (redacted output); only intended Media UI/tests and relevant documentation staged.
- Preserved: API, Finance, migrations, infrastructure and theme files have no delta against accepted main; the separate Finance freeze branch is unchanged. No production storage, credentials or runtime inspected/changed.

The first expanded regression run reported **92 passed / 1 failed**: the old test required the standalone audiobook registry entry. It was updated to the owner's new hiding requirement; the final run above passes. Existing legacy tests emit jsdom's `scrollTo` unsupported warning, not an assertion failure. The initial local browser harness needed Node20's existing `--experimental-websocket` flag; rerunning with it produced the measured PASS above. No application fix or deployment was used to mask these outcomes.

Backend/Finance suites are NOT RUN: no backend, scientific, storage or Finance code changed. Broader unrelated Web suites and live provider/acquisition checks are NOT RUN; focused shell/navigation plus Media/design-system checks cover the changed surface. Prior physical BB-117 approval is not reused as approval of this UI. GitHub CI and owner physical iPhone/PWA review are separate from these local checks.

## Security

Implementation is limited to Media search/navigation, the shared shell's audiobook mount, Media-focused tests and UX/UI Lab. No API implementation, storage, migrations, service configuration, Docker or other infrastructure changes. No deployment, real acquisition, playback, provider call or model run.

Finance remains frozen by the owner's decision; no Finance implementation, tests, documents or runtime are changed. Its independently published [freeze handoff](https://github.com/T-bear/BigBrain/blob/378d43e9a33b217fef30de5d4f74ef58a0a01926/docs/operations/finance-freeze-20261016.md) remains on the separate review branch, not imported into this Media branch. Shared STATUS/BACKLOG/recovery/catalog additions are Media-only. Main's older Finance paragraphs are left untouched rather than reconciled during unrelated work.

Untracked design mockups and Sentinel ADR drafts were present before this task and remain unpublished and untouched. No private host paths, credentials, raw provider payloads, personal library metadata or external review material are included in this report.

## Remaining work

Independent architect review should assess the API reuse, bounded catalog disclosure, cancellation and navigation removal. Physical iPhone/PWA review remains required for layout, focus/scroll, keyboard and handoff to the owner's configured Audiobookshelf. This checkpoint does not assert live provider health or acquisition success. Owner UX acceptance, merge and deployment require separate decisions. No new provider or full-library search approval is inferred.

## Resumption

Start with this report, `docs/modules/media.md` and the Media entry in `docs/operations/codex-recovery.md`. The smallest next step is review of the published checkpoint; no automatic deployment or additional sprint.


## Media CI heading correction — 2026-10-17

**REVIEW CHECKPOINT — test-only correction; not merged or deployed.**
Baseline main `08e1bcea6ae190026d4d60a543067c06b71351b1`, tree
`ad178e419746b9f3d3b3976af429366a3d814bac`, was fetched and verified unchanged.
The original Media implementation is now merged; the original checkpoint status above is
historical. [Merge/deployment handoff](https://github.com/T-bear/BigBrain/blob/97f49985b2fe5df49dc36fa74d4aae98b7ac87f6/docs/reports/deployments/media-integrated-search-deployment-20261017.md)
records the stopped deployment and [CI run 256](https://github.com/T-bear/BigBrain/actions/runs/39023635027).

Branch `media/fix-dashboard-heading-test` starts from that exact main. Resolve publication
SHA/tree from commit subject `test(media): align dashboard heading with approved search UI`.
Only `src/BigBrain.Web/src/MediaDashboard.test.tsx:189` changes in executable source: expected
heading `Hitta film och serier` becomes `Hitta film, serier och ljudböcker`, matching accepted
`MediaSearch.tsx`. Administration visibility assertions remain unchanged. No production fix,
API, configuration, dependency, database, container or Finance change is needed or included.

Verification from `src/BigBrain.Web`:

- Before correction, `npm test -- src/MediaDashboard.test.tsx`: **13 PASS / 1 FAIL / 0 SKIP**;
  the reported missing-heading failure reproduced at line 189. The test log confirmed the
  failure; a subsequent log-filter command could not find `rg` in that command environment,
  so the saved log was read directly. This did not affect test execution.
- After correction, same command: **14 PASS / 0 FAIL / 0 SKIP**.
- Full frontend `npm test -- --run`: **215 PASS / 0 FAIL / 0 SKIP**, 27 files passed. This
  single broader run checks the exact suite that blocked CI. Existing jsdom `scrollTo`
  unsupported warnings remain non-failing; no other test failure was found.
- `./node_modules/.bin/prettier --check --config .prettierrc.json --no-editorconfig src/MediaDashboard.test.tsx`: **PASS**.

Unchanged production build evidence from the original approved candidate and backend CI PASS
from the exact merge are reused; no production build or backend/Finance tests are repeated for
this text-only assertion correction. Local results are not claimed as new GitHub CI success.
No merge, deployment, service inspection, provider request or user-data mutation in this task.
Finance remains FROZEN. Unrelated untracked mockups and four Sentinel ADR drafts are preserved.
Next: independent architect review of this exact checkpoint; no automatic merge/deployment.

Publication checks: `node scripts/verify-documentation.mjs` (including relative links),
`git diff --check`, exact staged scope and staged Gitleaks secret scan **PASS**.
Only the one-line test correction, this existing report and the Media recovery entry are changed.


## Acceptance and deployment reconciliation — 2026-10-17

The historical review-only status above is superseded for the integrated audiobook release:
implementation merge `08e1bcea6ae190026d4d60a543067c06b71351b1`, followed by approved
heading-test correction merge `628041b200b1ff1283e29955073db91539bac422` (tree
`a36bb7dcd390c51be395a3de1105cbaefa5c4a29`). [Exact merge CI](https://github.com/T-bear/BigBrain/actions/runs/39025570041)
passed all four jobs. [Published Web-only deployment evidence](https://github.com/T-bear/BigBrain/blob/9c7483caa2d48067db848b9bf82c8b72a1c83cb4/docs/reports/deployments/media-integrated-search-deployment-20261017.md)
records healthy Web, matching assets and unchanged other containers.

The product owner explicitly reports iPhone/PWA approval dated 2026-10-17 in the Mediekontroll
assignment. This is owner manual acceptance, not an additional Codex device test or new provider
verification. It applies to the preceding integrated search release, not the new Mediekontroll
checkpoint. Earlier test failures and dated pending-review statements remain historical evidence.
