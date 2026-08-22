# BB-096 — BigBrain UX Sprint v1 / Unified Design System

## Metadata

- Date: 2026-08-22
- Scope: frontend design system, shell, navigation, themes and functional regression
- Related commit: pending publication

## Status

- Implemented: yes
- Automatically tested: frontend suite/build and repository build passed locally
- Deployed: no
- Manually verified: temporary mobile and desktop Obsidian Gold renders inspected; broader owner review remains

## Scope and visual sources

The three locally supplied composite mockups were inspected as visual references: Obsidian Gold, Arctic Wind and Forest Night. They informed palette, surface depth, floating navigation, spacing and hierarchy. The mockups are intentionally untracked; repository behavior remains the functional source of truth.

## Changes

- One semantic CSS token contract with three token-only themes: `obsidian-gold`, `arctic-wind`, `forest-night`.
- Obsidian Gold default, safe migration of legacy IDs, persisted selection through the existing Settings API plus local offline cache, and immediate `theme-color` updates.
- Reusable `AppShell`, shared local SVG icon set and five primary destinations: Hem, Familj, Media, Finance and Mer.
- AI and Admin moved to the secondary Mer surface without deleting their routes/widgets.
- Existing meal, shopping, calendar, media, system and Finance functionality remains registered. Finance technical evidence is closed by default through native progressive disclosure, not removed.
- Mobile-first floating dock, iOS safe-area padding, bounded desktop content width, 44 px interaction targets, visible focus, reduced-motion support and tabular numeric rendering.

## Evidence

Hem now launches the main modules. Familj owns the existing meal planner, shopping list, calendar import and reminders placeholder. Media retains search/request, download control, Smart Shuffle, media jobs, Jellyfin and integration links. Finance retains observations, charts, features, backtests, robustness, datasets, backups, shadow evidence, overview, Hard Risk, risk evaluations, Autonomous Research, scheduler, governor and operations status. Admin retains recovery, metrics, containers and integrations. No backend Finance contract or authority changed.

## Intentional deviations

The implementation adapts the mockup composition to real application volume rather than replacing real evidence with mock data. Arctic Wind follows the supplied dark cold-blue reference rather than becoming a simple light-mode inversion. Existing detailed module controls keep their established component structure where wholesale migration would risk behavior; they inherit the unified tokens and surfaces.

## Security

Detta är en sanerad GitHub-version. It contains no secrets, private network addresses, machine identities, raw logs, private media names or sensitive paths. Theme settings contain only allowlisted identifiers. Finance backend, provider behavior, authority and safety logic were not changed.

## Remaining work

Final publication and GitHub Actions evidence remain pending. Manual owner review across the supplied full mockup matrix can identify later presentation-only refinements; it must not be interpreted as authorization for Family Epic or Finance methodology work.

## Resumption

Authoritative contracts are `docs/design-system/theme-contract-v1.md`, `docs/architecture/dashboard-widget-framework.md`, `docs/BACKLOG.md` and this report. The smallest safe next step is to complete full repository validation, publish BB-096, inspect CI and then stop.

## Verification

Focused frontend tests and production build passed during implementation. Temporary 390×844 and 1440×900 browser renders were visually inspected for shell, depth, content width and dock overlap. Final full-suite, container, documentation, Compose, secret and CI evidence must be appended before this report is marked complete.
