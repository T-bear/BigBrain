# BB-099 — Whole-App UX Audit & Premium Composition Pass

## Metadata

- Date: 2026-08-23
- Baseline: `f6ffd92645f4d0d0b96dafa52a7336991c32abb6`
- Scope: frontend composition and reachability only
- Implemented: locally
- Automatically verified: complete local repository-native validation
- Deployed: pending CI
- Owner status: **OWNER UX REVIEW PENDING**

## Status

Implemented and fully verified locally. CI, deployment and appliance evidence remain pending and will not be inferred.

## Audit and decisions

Repository truth exposes seven application views—Home, Family, Media, Finance, More, AI and Admin—with Settings embedded in More. The detailed state and parity matrix is published in `docs/BB-099-UX-AUDIT.md`. Every major view was rendered at 390 × 844 in Obsidian Gold, Forest Night and Arctic Wind. Long views were reviewed at top and lower scroll positions, and representative overlays, disclosures, forms and navigation were exercised. Family remains the unchanged golden design reference.

## Changes

Home previously duplicated the persistent dock with three launcher rows. It now makes one read-only, non-polling request per existing Family meal, calendar, shopping, Media overview and Finance overview endpoint, then presents actual current signals before contextual navigation. Missing endpoint data remains honestly unavailable; no reminder, continue-watching or fabricated performance data is synthesized.

Media's obstruction came from insufficient shared page-end clearance combined with Smart Shuffle's nested checkbox scroller and a lower select, followed by technical integration content expanded by default. A single semantic dock-clearance token now reserves dock height, safe area and breathing room for every view. Smart Shuffle uses one continuous selectable list without nested scrolling, Media search is composed as its primary purpose section, and the Jellyfin/ARR technical surface remains available but starts collapsed.

Finance keeps the existing data and complete advanced research disclosure. Its default five-panel dashboard rhythm is recomposed as one ordered material narrative—market, current signals, prospective evidence, risk and autonomous research—with quiet separators. No endpoint, calculation, research, risk, scheduler, governor, provider, cadence, database or execution behavior changed.

AI's five repetitive future placeholders are consolidated into one truthful capability-boundary view. More/Settings and Admin required reachability audit rather than structural change. Family adopts only the shared dock-clearance contract and otherwise preserves the approved composition and behavior.

## Reachability, responsive and accessibility evidence

The shared `--bb-mobile-dock-clearance` is `112px + safe-area-inset-bottom` and is consumed by Family, DashboardWorkspace and the shared mobile main layout. This replaces per-page arithmetic. Dialogs remain viewport-bounded, contextual Family panels retain their focus/Escape behavior, and full-screen modes that hide the dock retain their dedicated rules. The browser matrix found no horizontal overflow at 390 × 844, 768 × 1024 or 1440 × 900; 430 × 932 is the phone smoke size. Existing semantic labels, focus-visible, minimum touch targets and reduced-motion rules remain intact.

The owner-confirmed, separate status is unchanged: **FULL-PAGE IOS SAFARI SCREENSHOT ARTIFACT: STILL REPRODUCIBLE**. Normal viewport evidence is used here; no iOS stitching fix is claimed.

## Evidence

Evidence consists of the checked-in audit matrix, behavioral tests, production build, 21 normal 390 × 844 route/theme captures, long-view scroll frames, the stabilized Arctic Family canary and responsive overflow measurements. Deployment evidence is appended only after deployment.

## Validation and deployment evidence

Local validation passes 121 frontend tests across 21 files, 495 API tests and 32 Sentinel tests. The Vite production build and complete Release solution build pass with zero warnings/errors. Documentation verification passes 177 Markdown files and 89 unique backlog IDs; Compose and `git diff --check` pass. `gitleaks` is not installed locally, so the GitHub Actions secrets job remains authoritative. CI, deployment and runtime Finance evidence are appended only after they actually pass. The first API run exhausted the 3.9 GiB tmpfs while creating isolated SQLite fixtures; the unchanged suite passed 495/495 when rerun with a task-specific temporary directory on the repository disk.

## Security and scope

Detta är en sanerad GitHub-version. The new Home composition consumes only existing authorized read-only APIs and performs no decorative polling. No secret, private address, raw production log or user data is published. Only Web is eligible for deployment; API, Sentinel, databases, volumes, settings and Finance state must be preserved.

## Remaining work

The implementation can become technically complete only after green CI, Web-only deployment and post-deploy appliance QA. Home, Media, Finance, More/Settings, Admin and AI then require owner UX review. They are not self-approved by this report.

## Resumption

Resume from this report, `docs/BB-099-UX-AUDIT.md`, `docs/STATUS.md`, `docs/BACKLOG.md`, `TESTING.md` and the published BB-099 commit. Do not widen scope into backend, Family functionality, PAPER/broker work or security testing.
