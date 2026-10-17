# Media — Mediekontroll, 2026-10-17

Detta är en sanerad GitHub-version. No credentials, private paths, raw provider data or runtime state.

## Metadata

- Checkpoint: `MEDIA-CONTROL`; risk MEDIUM (navigation, persisted preferences and request lifetime).
- Branch: `media/media-control`.
- Exact verified baseline: `628041b200b1ff1283e29955073db91539bac422`.
- Baseline tree: `a36bb7dcd390c51be395a3de1105cbaefa5c4a29`.
- Resolve publication commit/tree from subject `feat(media): group operational views under Mediekontroll`.
- **MERGE CANDIDATE / independent review required / NOT MERGED / NOT DEPLOYED.**
- Owner scope: one Media control entry, three separate existing functions, preserved search and data.

## Status

Implemented and automatically verified; independent review and new physical owner acceptance pending.
No merge or deployment. Earlier accepted Media deployment is unaffected.

## Changes

Media's three separate widgets become one Mediekontroll entry. Its overview at `/media/control`
contains exactly Nedladdningskö, Medieflöde, Tekniska integrationer in that order. Real navigation
links open `/media/control/downloads`, `/media/control/flow`, `/media/control/integrations`.
Back links lead to overview or `/media`; browser history, reload and direct navigation are supported.
The URL selects the active Media view even when a different dashboard was previously stored.

Only the selected existing DownloadControl, MediaJobs or administration-only MediaDashboard mounts.
Their actions, confirmations, loading/error displays, data adapters and polling implementations are
unchanged. The technical page retains the existing Administration disclosure. Overview has no data
fetch effect; leaving a page unmounts its component and uses its existing abort/interval cleanup.
No new scheduler, router library, API or generic navigation framework. Media's normal search and
Smart Shuffle remain separate. Admin's existing administration widget is retained.

Navigation uses the existing `bb:navigation` event and browser history. The shell's duplicate
Media-route exit code is consolidated in WidgetProvider's existing view-selection callback so
both desktop and mobile navigation agree. A history entry records the preceding dashboard for
return from Media; this grants no capability and stores no service data.

Cards reuse semantic surface/button classes, existing AppIcon, theme tokens and focus contract;
no palette or shared primitive is introduced. Header focus moves to the selected page. UX Lab
includes the actual overview in an **EXPERIMENTELL** inert preview, not owner-approved UI.

## Compatibility and persistence

[Canonical Media navigation contract](../../../modules/media.md#mediekontroll--frontend-review-2026-10-17)
defines supported root/Media legacy hash bookmarks. `#downloads`/`#download-control` map to
Nedladdningskö, `#queue`/`#media-jobs` to Medieflöde, and `#administration`/
`#media-administration`/`#jellyfin-overview` to integrations. No undocumented arbitrary URL aliases
are promised. Existing audiobook routes still open the approved audiobook search.

Local version-2 widget preferences map the former three Media IDs to `media-control` at their
first ordered position. Hidden/collapsed state carries over only when all three old Media choices
were hidden/collapsed, retaining access if any was visible. Other choices are preserved. Admin's
optional old technical widget maps to the new entry while its default administration stays intact.
Normalization is idempotent; no new local-storage key, server schema or module-data migration.
Per-page transient filters reset after leaving/remounting; provider-owned data/progress does not.
Modified-click links retain normal browser behavior. Ordinary API mutations remain owned by the
original components; navigation does not retry them or create new actions.

## Security

- `media-control/MediaControl.tsx`: entry, three-card overview and selected-page composition.
- `media-control/mediaNavigation.ts`: Media-only URL/legacy-anchor rules and preference normalization.
- `media-control/MediaControl.test.tsx`: navigation, history, loading/abort/polling and compatibility proof.
- `dashboard/appWidgets.tsx`: replaces the three Media registrations with the common entry.
- `dashboard/DashboardWorkspace.tsx`, `dashboard/widgetFramework.tsx`, `AppShell.tsx`,
  `MobileNavigation.tsx`: integrate the existing shell/history/persistence mechanisms.
- `App.test.tsx`: update expected widget visibility and reduced initial reads.
- `styles/media-control.css`, `styles/index.css`: scoped token-based card layout/import.
- `ux-lab/UXLab.tsx` and its test: experimental no-request/no-navigation specimen.
- Documentation: this report/catalog, Media module/status/backlog, recovery and the preceding Media
  report's acceptance reconciliation. No Finance sections are edited.

Existing `MediaDashboard`, DownloadControl, MediaJobs and Film/Serie/Ljudbok search implementations
are unchanged. No backend, API, dependency, configuration, infrastructure, migration, volume, data
or Finance change. Unrelated untracked mockups and four Sentinel ADR drafts remain untouched.
Finance remains **FROZEN / RESEARCH / 0 SEK / NONE**. No deployment, provider call or model run.

## Evidence

Commands below ran from `src/BigBrain.Web` unless stated otherwise:

| Check | Actual outcome |
| --- | --- |
| Initial focused shell/navigation run | 34 PASS / 5 FAIL. Removal of the MediaDashboard import also affected its retained Admin registration. Restored import; no broader Admin change. |
| `npm test -- src/media-control src/App.test.tsx src/dashboard src/ux-lab src/media-search src/download-control src/media-jobs src/MediaDashboard.test.tsx` | 100 PASS / 0 FAIL / 0 SKIP, 13 files. |
| `npm test -- --run` after adding UX Lab preview assertion | 231 PASS / 0 FAIL / 0 SKIP, 28 files. One broader frontend run justified by shared shell/history and preference changes. |
| `npm run build` | PASS, TypeScript + Vite production build. |
| `npm run format:check` | PASS. |
| Isolated Firefox against built local assets with fixture fetches | PASS at 390×844 and 1440×844, all three themes; no horizontal overflow. Minimum measured card height 94 px, width 349.2 px. Exact three ordered cards, visible solid focus outline, Enter opens downloads, browser Back returns to overview. No live API/provider or production browser state. |
| Repository-root `node scripts/verify-documentation.mjs` | PASS, including relative links. |
| `git diff --check`, staged scope and Gitleaks | PASS; only intended frontend and documentation files. |

Tests exercise actual existing components with deterministic/pending requests: overview stays quiet
for 90 seconds, each selected page issues only its own reads, exit aborts every pending read and
stops polling, resolved job polling stops after exit, and errors do not leak into overview. Direct
bookmarks override stored non-Media selection. Real jsdom back/forward plus remount, desktop/mobile
return navigation, registry uniqueness and preference normalization are covered. Existing Media
search/acquisition/download tests pass without production calls. Existing jsdom `scrollTo` warnings
remain non-failing. Backend/Finance suites are not rerun: their code and assumptions are unchanged.
No new GitHub CI success is claimed for this review branch; the workflow runs on PR/main pushes.

## Earlier release acceptance versus this review

[The preceding release's reconciled record](integrated-audiobook-search-20261017.md#acceptance-and-deployment-reconciliation--2026-10-17)
separates green CI and published Web-only deployment from the owner's explicit iPhone/PWA acceptance
reported on 2026-10-17. That acceptance applies to integrated audiobook search, not Mediekontroll.
No previous Finance or operational document history was merged into this implementation branch.

## Remaining work

Independent architect review must assess the exact diff, request lifetime and preference mapping.
On iPhone/PWA, verify overview cards, all three functions, back/forward, reload, saved legacy links,
keyboard/focus/scroll and transitions back to Film/Serie/Ljudbok. The new navigation has not been
physically approved or deployed. The previous catalog bound and external acquisition limitations
remain unchanged. No known failing automated check or unresolved implementation blocker remains.

## Resumption

Read this report and the Media recovery entry, fetch origin and compare exact baseline/checkpoint
identities. Preserve unrelated work and reuse unchanged evidence. Stop after publication;
merge/deployment need separate exact-SHA owner approval.
