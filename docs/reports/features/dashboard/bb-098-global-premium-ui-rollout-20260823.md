# BB-098 — Global Premium UI Rollout

## Metadata

- Date: 2026-08-23
- Scope: frontend presentation, shared controls, routes, AppShell and themes
- Baseline: `0e60ffe970e231c9af6c594c8e23b8e1e15f2600`

## Status

- Implemented: locally
- Automatically tested: complete local repository-native validation
- Deployed: Web only; API, Sentinel and volumes preserved
- Manually verified: local route/theme matrix and deployed responsive Home/theme persistence

## Scope and reference

Baseline is `0e60ffe970e231c9af6c594c8e23b8e1e15f2600`. The owner approved the deployed BB-097 Family implementation as BigBrain's golden design direction. BB-098 applies that proven language to every current route without redesigning Family or changing backend behavior. The original Obsidian Gold, Forest Night and Arctic Wind mockups were reopened as secondary visual references; generated images were not used.

## Inventory and migration matrix

The current application exposes seven views: Home, Family, Media, Finance, More, AI and Admin. The complete post-migration source audit contains 107 JSX `button` occurrences and 31 native `input`/`select`/`textarea` occurrences; every occurrence intentionally inherits the semantic global control contract, while high-frequency shell and launcher actions use explicit React variants. One central `WidgetFrame` previously imposed `dashboard-widget` presentation on every non-Family route.

| View/component | Previous presentation | BB-098 target | Preserved capability | State |
|---|---|---|---|---|
| AppShell and dock/rail | Separate desktop controls and Family dock | One material navigation language | All destinations and current-view state | implemented locally |
| Home | Generic launcher widget | Calm contextual module entries | Family, Media and Finance navigation | implemented locally |
| Family | Approved BB-097 composition | Unchanged golden canary | All Family actions | regression pending |
| Media | Repeated framed widgets | Purpose sections plus semantic controls | Search, requests, downloads, Jellyfin/ARR and Smart Shuffle | implemented locally |
| Finance | Dashboard frame around progressive disclosure | Quieter route composition and tokenized controls | All research, risk, operations and evidence views | implemented locally |
| More/Settings | Generic widget and local controls | Integrated action list and material theme surface | AI/Admin navigation and three-theme picker | implemented locally |
| AI | Repeated planned widget frames | Quiet purpose sections | Truthful current planned states | implemented locally |
| Admin | Generic system widgets | Calm status/metric sections | System, Docker and recovery visibility | implemented locally |
| Buttons/forms | Local visual rules | Semantic global state contract and explicit shared primitives | Native semantics and handlers | implemented locally |
| Dialogs/status/empty state | Mixed module styling | Shared material, focus and status anatomy | Focus, labels, dismissal and messages | implemented locally |

## Component architecture

`BBButton` provides primary, secondary, tertiary, danger, icon and contextual intent, including native disabled and accessible busy behavior. `BBInput` and `BBSelect` retain native platform and accessibility behavior while joining the shared material contract. `BBSurface` and `BBEmptyState` cover proven grouping and concise zero-data anatomy. Existing specialized controls remain specialized but inherit the same base material, 100 ms press response, 180 ms component transition and focus-visible treatment. Components never branch on theme identity.

The central legacy `dashboard-widget` wrapper is replaced by `workspace-module`. It retains ordering, visibility, collapse and settings behavior but no longer draws a card around every module. Route-level composition suppresses redundant module headings where the screen already names itself. Family remains structurally and visually isolated as the approved canary.

## Changes

- Added semantic button, input, select, surface and empty-state React primitives.
- Unified native press, hover, focus-visible, disabled and loading states.
- Migrated AppShell, Home and More navigation controls to explicit semantic variants.
- Replaced the legacy central DashboardWidget presentation with purpose-oriented workspace modules.
- Refined shared status, dialog, theme-picker, list, metric and module presentation.
- Added the complete Arctic Wind light-material token set.

## Theme identities

Obsidian Gold and Forest Night retain the approved BB-097 tokens and Family appearance. Arctic Wind now uses a light color scheme with an icy blue canvas, white/cold raised materials, dark blue-gray text hierarchy, cold directional highlights, restrained blue selection and shadows designed for light surfaces. All three consume the same semantic component rules.

## Validation and safety status

Local browser iteration rendered all seven views in all three themes at 390 × 844 with no horizontal overflow. The mobile dock remained 358 × 64 and clear of content. Home also passed 768 × 1024 and 1440 × 900 without horizontal overflow. Local validation passed 119 frontend tests across 21 files, 495 API tests and 32 Sentinel tests. The frontend production build and Release solution build passed with zero Release warnings/errors; documentation verification passed 175 Markdown files and 89 unique backlog IDs; Compose validation and `git diff --check` passed. `gitleaks` is unavailable locally, so the CI secrets job remains authoritative. CI, deployment, deployed route/theme review and Finance runtime evidence are recorded only after they actually complete.

The owner-confirmed status remains: **FULL-PAGE IOS SAFARI SCREENSHOT ARTIFACT: STILL REPRODUCIBLE**. Normal viewport captures are the acceptance evidence. BB-098 does not claim an iOS fix.

## Evidence

Evidence is the route/control inventory, frontend primitive tests, route regression tests, production build and normal browser viewport matrix described above. Final CI and appliance identifiers are appended after publication and deployment, never inferred from local source.

## Security

Detta är en sanerad GitHub-version.

This is a frontend presentation migration. No Finance endpoint, scheduler, governor, research, Hard Risk, provider, cadence, database, authorization boundary, secret or appliance volume is changed. This report contains no private address, credential, raw production log or private data.

## Remaining work

Migrated modules require owner review; Family direction approval does not pre-approve them. The known iOS full-page capture artifact requires equivalent real-iOS evidence before its status can change.

## Publication and deployment evidence

The implementation was published in three coherent code/test commits ending at `bdfef76335e6f218899d2be8a02deb9dc105e715`. The first CI run exposed a pre-existing timing race in the Meal Planner empty-state test; the test now waits for the asynchronous state it asserts without changing product code. Final GitHub Actions run 32632208758 passed backend, frontend, documentation and secrets.

Only Web was rebuilt and recreated with `--no-deps`. Final image `sha256:5410d787fadfd368587c1113675c2801e558c41c43aced8512bd94e984d3aa8e` is healthy; the pre-existing API and Sentinel containers stayed healthy and were not recreated. Deployed Home was opened at `/tmp/bb098-deployed-final-obsidian-home-390x844.png`, its 430 × 932 counterpart and `/tmp/bb098-deployed-final-obsidian-home-1440x900.png`. The initial normal capture found the mobile settings control incorrectly stretching across the page; the final evidence confirms the corrected 44 × 44 control, clear dock, stable content width and shared desktop material rail. The deployed Settings API accepted and persisted Obsidian Gold, Forest Night and Arctic Wind selections, then was restored to Obsidian Gold. Local pre-deployment browser evidence covered all seven views in all three themes at 390 × 844 plus responsive sizes; no horizontal overflow was observed.

Post-deployment Finance remained `RESEARCH`, `BudgetSek = 0`, `ExecutionAuthority = NONE`, status `CONTINUE_RESEARCH`. Scheduler remained enabled, idle and unchanged with `finance.research.scheduler.nonResearchDay`; recovery retained clean previous shutdown, completed recovery, synchronized clock, no low-disk condition and zero interrupted jobs.

## Resumption

Resume from this report, `docs/BACKLOG.md`, `docs/STATUS.md`, `docs/design-system/theme-contract-v1.md` and the latest published commit. Do not widen scope into Family functionality or Finance backend work.
