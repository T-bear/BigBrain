# Sprint 3 Download Control navigation implementation report

> Detta är en sanerad GitHub-version. No credentials, private addresses, raw provider
> identities, paths, logs or media data are included.

## Metadata

- Date: 2026-08-10
- Scope: BB-040 and the remaining presentation scope of BB-033

## Status

- Status: Closed; implemented, automatically verified, production-built, Web-deployed and technically accepted
- Deployment: Web-only deployment performed after explicit product-owner approval
- Immediate product-owner smoke verification: Accepted; no blocker found
- Extended manual UX evaluation: Deferred to BB-041
- Known defect preventing closure: None

## Problem

A long completed-download history made active and problematic jobs, selection controls
and batch actions expensive to reach, especially on mobile. The Media view also needed
plain-language separation between the download client's queue and the wider lifecycle
from search to the media library.

## Changes

The existing filters and controls stay at the top. The all-status view groups jobs in
the product priority order: errors/problems, active, queued/paused and completed.
Completed history is collapsed by default with an explicit count and disclosure button.
The completed filter bypasses the collapse and shows the full completed scope directly.

Nedladdningskö now explains that it manages the download itself. Medieflöde explains
search, download, processing and arrival in the library, and explicitly notes that a
title can appear in both views during the download phase.

This solution was chosen because it preserves current workflows, keeps urgent content
at zero extra interaction cost and makes history reachable in one action. A floating
action button was rejected because the controls already remain above the compact list
and a floating element would compete with mobile bottom navigation. A separate history
page was rejected as unnecessary navigation. Pagination or a generic “show more” list
was rejected because status grouping communicates priority more clearly. No backend,
retention or cleanup capability was needed.

## Accessibility and responsive behavior

- The completed disclosure is a native button with `aria-expanded` and `aria-controls`;
  focus remains on it when opening or closing.
- Group headings include textual status and counts; priority is not communicated only
  through color.
- Filter and selection semantics, live selected count, diagnostics, dialog focus trap
  and keyboard controls remain intact.
- The new controls use at least 44 px touch height on mobile, wrap within their container
  and add no fixed or floating surface that could cover content or bottom navigation.
- Existing width constraints and single-column mobile cards remain; tablet/desktop use
  available width without rendering the completed history until requested.
- No new motion is introduced; reduced-motion behavior remains explicit.

## Evidence

- `npm ci`: passed.
- `npm test -- --run`: 19 files and 103 tests passed.
- `npm run build`: TypeScript and Vite production build passed.
- A 30-item completed dataset verifies prioritization, collapsed history, disclosure
  semantics and focus retention.
- Regression coverage verifies filters, filtered select-all scope, clearing selection,
  selected count, batch pause/resume/retry, per-item pause/resume/retry, diagnostics and
  the Manage dialog/removal boundary.
- BB-033 tests verify both plain-language explanations and unique screen-reader context.

Repository documentation, Compose validation and final diff checks are recorded in the
task handoff after all quality gates complete.

### Web-only deployment evidence

The approved deployment used `docker compose up -d --no-deps --build web`. Only the Web
container was recreated. Web became healthy and returned HTTP 200. API, Sentinel,
qBittorrent, Sonarr, Radarr, Jellyfin, Prowlarr and FlareSolverr retained their exact
pre-deployment container identities. The API's named-volume destinations were unchanged.
Read-only Calendar, theme/settings and Download Control list responses returned HTTP 200
with their expected structures before and after deployment. No API deployment, runtime
configuration change, torrent mutation or user-data write was performed.

## Security

Risk is low to medium and concentrated in frontend presentation state: hidden completed
rows could otherwise become hard to find, and grouping could disturb filtered selection.
The explicit completed filter, count, disclosure and regression tests mitigate those
risks. Selection still operates on the current filtered dataset even when completed
rows are collapsed in the all-status view.

No API, shared contract, adapter, capability, audit behavior or mutation boundary
changed. There is no batch delete, retention policy, Arr recovery, search, replacement,
real-time transport or AI access. No live torrent mutation was performed.

## Remaining work

Sprint 3 is closed. BB-040 remains **Pågår** until longer use provides its remaining
qualitative evidence for a long real list, rapid access to problem/active jobs,
completed-history access, filters, selection, batch controls, mobile, desktop, keyboard
and absence of overflow.

BB-033 remains **Pågår** until longer use confirms that the distinction and intentional
overlap are understandable on mobile and desktop. BB-041 owns this post-release UX
evaluation. It is not a blocker, known defect or failed Sprint 3 verification. Retry
retains Sprint 2's separate pending manual-verification exception and is likewise not a
Sprint 3 blocker.

## Resumption

Sprint 3 requires no further implementation or deployment. Resume BB-041 only after a
representative period of actual use. Record qualitative evidence, then assess BB-040 and
BB-033 against their own remaining DoD without reopening Sprint 3. Any new verified UX
problem receives a separate backlog item.

## Closure

- Implementation: COMPLETE
- Automated verification: PASS
- Production build: PASS
- Deployment: PASS
- Technical runtime verification: PASS
- Immediate product-owner smoke verification: ACCEPTED; no blocker found
- Extended manual UX evaluation: DEFERRED to BB-041
- Known defect preventing closure: NO
- Sprint status: CLOSED

The deferred evaluation needs normal use over time to judge whether grouping, compact
history, everyday navigation and the distinction between the two Media views feel right.
It is post-release validation and does not reduce the technical evidence or block Sprint
3 closure.
