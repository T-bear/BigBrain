# BB-097 — Family / Obsidian Gold premium reference

## Metadata

- Date: 2026-08-23
- Scope: Family frontend presentation and Obsidian Gold reference validation
- Related commits: `d29f0f88253752809042d0c5687e6d1b2d6b1d1a`, `cb9c9caa0efc66119616767bc3d9507d9a83562b`

## Status

- Implemented: yes
- Tested: full local suite and two GitHub Actions runs passed
- Deployed: yes, Web only; volumes and other services preserved
- Manually verified: local fixture-backed comparison and deployed PWA browser review
- Owner approved: no

## Why BB-096 was insufficient

BB-096 correctly introduced semantic themes, AppShell and navigation, but Family remained four dashboard widgets. Each widget added a large repeated identity header around a module that often added another accordion header and another card. Repeated labels, nested rectangles, prominent settings, borders and old dashboard density survived while color, radius and typography changed. That was a CSS-led reskin rather than the intended presentation redesign.

## Changes

The three 1536 × 1024 local references `239C888A-1D87-4B9F-A62C-9F26FD4EDB6F.PNG`, `23A62185-AC50-4227-A02A-63F95276CD8D.PNG` and `BF83EC0F-7E55-47CD-8F69-4854D07A1A87.PNG` were opened at original resolution. The second file is the Obsidian Gold authority. Its compact phone silhouette, warm near-black layers, restrained amber, thin separators, modest type scale and floating dock guided composition rather than only color values.

Normal Family rendering now uses `FamilyExperience`, with compact `FamilyHeader`, contextual overview and four non-widget `FamilySection` regions. Meal Planner and Shopping List use an explicit `family` presentation variant that removes their legacy collapsible shell; their standalone dashboard contract remains for tests and edit mode. Calendar retains its complete dialog/import behavior in a quiet preview, reminders remain a low-value truthful planned state, and dashboard configuration moved behind a 44 × 44 settings icon. Other modules still use the existing dashboard framework.

Calendar preview classification uses restrained CSS markers with screen-reader text instead of permanent emoji icons; emoji originating in actual event titles remains content and is not rewritten.

Obsidian Gold was refined to warmer `#090b0d` background, quieter `#2b3033` boundary, layered `#15191c`/`#1b2024` materials and `#d9a62e` accent. Gold identifies active navigation, semantic labels, today and important focus/interaction; ordinary tabs and inline actions remain neutral. Meal days share one raised material with hairline dividers, a subtle today rail and compact multi-meal anatomy. Borders no longer outline every row or section.

## Evidence

### Functional parity

| Old function | New location | Verification |
|---|---|---|
| Matsedel and week navigation | First Family section, compact tabs and integrated week header | existing component tests pass |
| Day meals, weekend lunch/dinner and Byt | Shared meal-day list, inline secondary action | existing component tests pass |
| Maträtter, Generera and Sparade | Compact meal tablist | existing component tests pass |
| Shopping quick add and shopping mode | Integrated Shopping section | existing component tests pass |
| Calendar preview, full calendar and import | Integrated Calendar section and existing dialog | existing component tests pass |
| Reminders planned state | Quiet final section | rendered truthfully |
| Theme, order, visibility and edit mode | Header settings popover | App tests pass |
| Hem, Familj, Media, Finance and Mer | Refined safe-area dock | navigation tests pass |

No Family backend, API contract, Finance behavior or mock production data changed.

### Visual loop and current evidence

Iteration 1 rendered the new structure at 390 × 844, 430 × 932 and 1440 × 900 and exposed two dominant defects: preview API errors obscured the real density, and inherited button specificity turned every meal tab gold. Iteration 2 used sanitized production-shaped Swedish fixture data, corrected the tab hierarchy and was opened beside the source mockup. At 390 × 844 the view has no horizontal overflow, a 358 × 64 dock above the bottom edge, a compact 149 px header/context lead-in and a single 346 px-wide meal material. Five weekdays plus the start of the weekend remain visible before the dock; the remainder scrolls naturally. At 430 × 932 widths and spacing remain stable rather than stretching awkwardly. At 1440 × 900 the sidebar remains and Family becomes an intentionally bounded two-column composition; horizontal overflow is absent.

Local fixture-backed comparison evidence is retained at `/tmp/bb097-family-390x844-iteration2.png`, `/tmp/bb097-family-430x932-iteration2.png` and `/tmp/bb097-family-1440x900-iteration2.png`. Deployed evidence is `/tmp/bb097-deployed-family-390x844.png`, `/tmp/bb097-deployed-family-430x932.png` and `/tmp/bb097-deployed-family-1440x900.png`. These runtime screenshots are intentionally not publication artifacts.

Known visual differences are deliberate or pending owner judgment: the production meal week is denser and more operational than the mockup's summary treatment; Family does not copy the mockup's sample alert/people data because no matching Family API contract exists; the center BigBrain AI dock emblem is not enlarged because BB-097 preserves the five agreed destinations and does not redesign global AI navigation.

### Verification state

- Frontend: 115 tests passed; production build passed.
- Backend: 495 API and 32 Sentinel tests passed; Release build passed with zero warnings/errors.
- Accessibility: semantic heading/regions, labeled settings, touch-size controls, keyboard tablist, focus-visible foundation, non-color today label and reduced-motion handling retained.
- Arctic Wind / Forest Night: shared semantic tokens remain compatible; no cross-theme redesign performed.
- CI: GitHub Actions runs 32618788138 and 32619102449 passed backend, frontend, documentation and secrets.
- Finance runtime safety: `RESEARCH / 0 SEK / NONE`, zero experiments before and after Web recreation; scheduler configuration/operation unchanged.
- Appliance deployment: final Web image `sha256:5f37b814b1ccb03dcd25353721a4ddd42e94a0f5bcea257f2b3389e59775f312` healthy; API/Sentinel/volumes/external services untouched; recovery healthy/clean.
- Deployed PWA: real Family data, Obsidian Gold, four sections, four meal tabs, no alerts or horizontal overflow at 390 × 844, 430 × 932 and 1440 × 900.
- Technical completion: **YES.**
- **OWNER VISUAL APPROVAL PENDING.**

## Security

Detta är en sanerad GitHub-version.

This report is sanitized. It contains no secrets, credentials, private runtime identity, private address, raw log, private media name or machine-specific sensitive path. Visual validation used synthetic production-shaped Family data and did not mutate product data. No backend or Finance behavior changed.

## Remaining work

Request owner visual approval. BB-097 remains the only design-reference work item; no other module rollout is authorized.

## Resumption

Authoritative context is `docs/BACKLOG.md`, `docs/STATUS.md`, `docs/architecture/family-view-family-coordination.md`, this report and the BB-096 implementation/deployment reports. The smallest safe next step is owner review of the deployed Family reference. Stop before any other module or theme rollout.
