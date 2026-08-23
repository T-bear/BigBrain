# BB-097 — Family / Obsidian Gold premium reference

## Metadata

- Date: 2026-08-23
- Scope: Family frontend presentation, Obsidian Gold reference validation and Obsidian Gold/Forest Night premium art direction
- Related commits: `d29f0f88253752809042d0c5687e6d1b2d6b1d1a`, `cb9c9caa0efc66119616767bc3d9507d9a83562b`, initial deployment-evidence publication `f5e992d3db14bce61172c48d17f98b50bd70a879`, premium art-direction implementation `4da8de726963b59dfa467c1f73d1aaf876b9d9d3`

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

## Premium art-direction refinement

Owner feedback explicitly accepted the new Family composition while withholding visual approval because the deployed result still read as a competent dark web UI rather than a premium consumer application. This pass preserves `FamilyExperience`, its section ordering and every underlying interaction. It uses only the original 1536 × 1024 mockups listed above and actual browser renders; no generated or unrelated image was used as design truth.

The semantic theme contract now adds canvas/ambient, three surface levels, surface highlight/edge/shadow, muted/highlight/shadow accent roles, tertiary text and deliberate panel/control/dock radius roles. A coherent 145-degree upper-left lighting model feeds low-opacity top responses and one compact shadow scale. Components consume these roles without theme-name branches or theme-specific hardcoded colors.

Obsidian Gold moved away from black plus flat yellow toward neutral-warm charcoal, smoked stone panels and subtly warmer upper-left material response. Its gold system separates deep/muted, main and highlight tones; the strongest tone is reserved for active navigation, focus and the narrow Today marker. `Byt`, ordinary metadata and secondary navigation remain neutral.

Forest Night is no longer the Obsidian component palette recolored green. Its canvas uses three bounded, low-contrast radial illumination fields inspired by the original lake/forest atmosphere, while panels use deep pine and green-black tonal variation. The treatment uses CSS only—no downloaded, generated or extracted bitmap—and keeps functional text off distracting imagery.

Typography uses less tracking and a clearer primary/secondary/tertiary tonal ladder. Section eyebrows retain restrained uppercase semantics but use half the previous tracking; headings and meal names use natural weight and spacing. Panel, control and dock radii are differentiated instead of repeating one generic rounded rectangle. Borders are low-opacity material edges; row separation uses quieter partial edge color.

The dock now uses a theme-owned translucent material, a single centered top highlight and a local selected-item surface with a 13 px accent glint. It no longer has a theme-specific gold outline. The settings control uses the same control material, edge, icon scale and tactile press response. Tabs use an inset parent track and locally raised active material. Today uses a bounded two-tone marker and soft local illumination rather than selecting the whole row; its compact label is `Idag` and cannot wrap vertically.

The reported full-page-only geometric artifact was traced to the combination of `background-attachment: fixed` and an unbounded diagonal Family pseudo-element during screenshot stitching. The page background now scrolls normally with a bounded background size, and Family illumination uses locally bounded radial layers. Diagnostic full-page captures after the change showed no large geometric artifact; normal viewport captures remain the acceptance evidence.

**Owner-evidence correction, 2026-08-23:** the preceding desktop/headless diagnostic did not reproduce equivalent iOS Safari screenshot stitching and therefore did not prove the artifact fixed. Real-iPhone owner evidence overrides that conclusion. **FULL-PAGE IOS SAFARI SCREENSHOT ARTIFACT: STILL REPRODUCIBLE** in both Obsidian Gold and Forest Night, while absent during normal interactive use. This owner-directed pass does not redesign the normal UI around a capture-only condition. The remaining candidates are iOS Safari composition of bounded radial backgrounds, the isolated Family stacking context, pseudo-elements, the fixed dock and viewport-unit interpretation; no further source change is justified without equivalent iOS Safari reproduction and verification.

## Owner-directed premium polish pass

The owner accepted the Family architecture as the basis but continued to withhold visual approval after real-iPhone review. The reported gaps were specific: CSS-panel materiality, insufficient stone/metal character, flat gold, Forest-as-recolor, insufficient environmental atmosphere, web-like metadata hierarchy, repetitive meal density and remaining dock detail. This pass changed only existing semantic theme values and Family presentation CSS. `FamilyExperience`, its component order, meal/week/tab interactions, Shopping, Calendar, Reminders, settings placement and bottom-navigation structure are unchanged.

Before editing, the original Obsidian Gold, Forest Night and Arctic Wind 1536 × 1024 mockups were reopened. New deployed baselines were opened at `/tmp/bb097-art-owner-baseline-obsidian-gold-390x844.png` and `/tmp/bb097-art-owner-baseline-forest-night-390x844.png`. The concrete difference list was: Obsidian ambient tone disappeared at phone scale; panels depended on uniform perimeter edges; gold selection lacked local tonal response; Forest lighting read primarily as one green top field; meal metadata and separators repeated too strongly; the Shopping add action carried too much full-gold weight; and dock selection relied too heavily on one colored surface.

Iteration 1 added theme-owned directional panel-edge response, low-opacity internal illumination, closer/softer shadow scales, material selection and metallic-marker roles. Obsidian gained perceptible warm-neutral ambient variation and a smoked charcoal/stone response without brighter panels. Forest gained stronger asymmetric pine illumination and environmental interaction in panel/dock materials. Meal rows changed from 63 px minimum plus 10 px vertical padding to 59 px plus 8 px, with quieter separators, less prominent person/type metadata and primary-tone meal names. The Shopping quick-add control became a dark theme material with primary weight instead of a large gold fill. Opened evidence is `/tmp/bb097-art-owner-iteration1-obsidian-gold-390x844.png`, `/tmp/bb097-art-owner-iteration1-forest-night-390x844.png` and normal scroll frames at 620 and 1180 CSS pixels.

Iteration 2 addressed only the largest remaining differences: Forest received a restrained cool green-mist field and a more organically layered selected material; Obsidian selection reduced its bronze fill and retained gold as a narrow metallic glint; meal-type tone receded further from `Idag` and meal names. The final within-iteration optical correction widened the day column/gap so Sunday `Idag` does not collide visually with Lunch. Opened evidence is `/tmp/bb097-art-owner-iteration2-obsidian-gold-390x844.png`, `/tmp/bb097-art-owner-iteration2-forest-night-390x844.png`, the two 430 × 932 counterparts and normal 620 px scroll frames. This is exactly two controlled art-direction iterations per dark theme; no stitched full-page image is acceptance evidence.

Gold quantity did not increase: existing accents gained deep/main/champagne tonal roles only on the active dock glint, Today edge and semantic selection response. Body text, section surfaces and ordinary actions remain non-metallic. Forest remains predominantly neutral-black/deep pine, with smoked emerald, muted moss and a small cool atmospheric contribution rather than uniform green.

### Owner-directed publication and deployment evidence

Implementation commit `cdad8fcca5ed5742d97aadbb07b54b4628b0b867` passed GitHub Actions run 32626737721: backend, frontend, documentation and secrets all completed successfully. Local verification passed 115/115 frontend tests across 20 files, the frontend production build, 495 API tests, 32/32 Sentinel tests, Release build with zero warnings/errors, documentation verification over 174 Markdown files and 89 unique backlog IDs, Compose validation and `git diff --check`. Local gitleaks was unavailable; the authoritative CI secrets job passed.

Only Web was rebuilt and recreated with `--no-deps`. Deployed image `sha256:a4c89f10d3e54c97d4d4fe91118f5235e96d858a49c32ff10311f2c145ed0fa4` is healthy; API and Sentinel remained healthy and were not recreated. Recovery reported `healthy`, previous shutdown `clean`, completed recovery, synchronized clock, no low-disk condition and zero interrupted jobs. The shared theme was restored to Obsidian Gold after validation.

Opened deployed evidence is `/tmp/bb097-art-owner-deployed-obsidian-gold-390x844.png`, `/tmp/bb097-art-owner-deployed-forest-night-390x844.png` and their normal 620 CSS-pixel scroll frames. Both dark themes retained four Family sections, all four meal tabs, zero alerts and no horizontal overflow. Opened 430 × 932 counterparts retained the same composition; the dock measured 358 × 64 at 390 and 398 × 64 at 430. `/tmp/bb097-art-owner-deployed-arctic-wind-390x844.png` passed the requested readability/layout smoke test without an Arctic art-direction pass. Finance runtime remained `RESEARCH / 0 SEK / NONE`; the enabled scheduler and all Finance behavior were unchanged.

The known iOS limitation is unchanged and is not hidden by desktop evidence: **FULL-PAGE IOS SAFARI SCREENSHOT ARTIFACT: STILL REPRODUCIBLE** for Obsidian Gold and Forest Night based on owner-provided real-iPhone evidence. Equivalent iOS Safari full-page stitching was unavailable locally, so no fix is claimed. Normal interaction and normal viewport captures remain unaffected. The owner-directed pass is technically complete; **OWNER VISUAL APPROVAL PENDING.**

## Final Family craftsmanship pass

The owner accepted the Family architecture, layout, hierarchy and Obsidian Gold/Forest Night direction but requested one final component-quality and micro-interaction pass. No section, composition, data contract or Family action was removed or relocated. The original three approved mockups were reopened; generated images were not used.

The clearest defect was the expanded Byt interaction: three unrelated generic buttons occupied a rectangular web-form block and did not identify the selected meal. The replacement remains inline and meal-anchored because that gives the strongest unambiguous relationship for weekend lunch/dinner rows and avoids detaching the action into a global sheet. It is now one semantic material panel with an `Ändra måltid` context header and selected meal name, a unified action group, primary `Föreslå en annan`, secondary `Välj maträtt manuellt` and tertiary `Avbryt`. Opening focuses the primary action without scrolling; Escape and Avbryt dismiss it. Automatic and manual replacement contracts are unchanged.

Two semantic motion values were added: 100 ms press response and 180 ms component transition with the existing standard easing. The Byt panel uses opacity plus a 3 px translate and .985 scale from its meal anchor. Active tabs, week arrows, Byt, settings, Shopping actions, Calendar navigation and dock items now share immediate touch feedback. The existing `prefers-reduced-motion: reduce` rule was extended to remove every new Family transition and the Byt animation.

Tabs gained material background/elevation transition and press response. Week arrows retain small glyphs inside 44 px hit areas. The settings glyph received a half-pixel optical lift and the control now uses the theme material edge. Shopping input and add action share a quiet containing surface, theme-aware focus edge and consistent press response; handlingsläge uses a secondary material instead of an outline. Calendar navigation remains tertiary with a small chevron, event padding is aligned to 13 px and markers/separators retain existing semantics. Dock geometry is unchanged while selected material and press transitions use the shared motion values.

The primary pass opened normal, Söndag/Idag/Smashed Burgers/Byt-open and Shopping/Calendar/dock frames for both Obsidian Gold and Forest Night at 390 × 844 and 430 × 932. It found one concrete defect: the global focus halo was clipped by the action-group boundary and appeared as a thick gold bar. The single correction pass replaced it with a complete inset theme-aware focus indicator. Final local screenshots are `/tmp/bb097-craft-correction-obsidian-gold-390x844-today-byt-open.png`, `/tmp/bb097-craft-correction-forest-night-390x844-today-byt-open.png`, their 430 × 932 counterparts and both `shopping-calendar` frames. The open panel measured 218 px wide at 390 and 258 px at 430, remained above the dock, preserved all three actions and had no horizontal overflow. Actual `Spaghetti och köttfärssås` and `Köttbullar och makaroner` rows wrapped without Byt collision. Arctic Wind remained readable and overflow-free at 390 × 844; no Arctic redesign occurred.

The iOS capture status is unchanged: **FULL-PAGE IOS SAFARI SCREENSHOT ARTIFACT: STILL REPRODUCIBLE** according to real-iPhone owner evidence and is not visible in normal interaction. This craftsmanship pass neither claims nor attempts an unverified fix.

Implementation commit `d9091574fad84d156d22d719d2286a0db473f465` passed GitHub Actions run 32629227326 in backend, frontend, documentation and secrets. Local regression passed 115/115 frontend tests, 495/495 API tests and 32/32 Sentinel tests; frontend production and Release builds passed with zero Release warnings/errors. Documentation verification passed 174 Markdown files and 89 unique backlog IDs; Compose and diff checks passed. Local gitleaks was unavailable and CI secrets passed.

Only Web was rebuilt and recreated with `--no-deps`. Image `sha256:3538f738ef44690aacc61d70e6752c008a7ce4ee60403403be65477db18f2ad1` is healthy; API and Sentinel remained healthy and were not recreated. Deployed required-state evidence was opened at `/tmp/bb097-craft-deployed-obsidian-gold-390x844-today-byt-open.png` and `/tmp/bb097-craft-deployed-forest-night-390x844-today-byt-open.png`, plus their 430 × 932 counterparts and both deployed `shopping-calendar` frames. At 390 the panel measured 218 × 199 px and dock 358 × 64; at 430 it measured 258 × 199 px and dock 398 × 64. All three actions remained present, `Föreslå en annan` owned focus, horizontal overflow was absent and the action panel remained clear of the dock. Deployed Arctic Wind retained all sections/tabs with zero alerts or overflow at 390 × 844. The theme was restored to Obsidian Gold. Finance remained `RESEARCH / 0 SEK / NONE` with its enabled scheduler unchanged.

The final craftsmanship pass is technically complete. **OWNER VISUAL APPROVAL PENDING.**

### Premium refinement visual and deployment evidence

The art-direction comparison ran two opened implementation iterations for each dark theme at both 390 × 844 and 430 × 932. Obsidian evidence is `/tmp/bb097-art-iteration1-obsidian-gold-390x844.png`, `/tmp/bb097-art-iteration2-obsidian-gold-390x844.png` and their 430 × 932 counterparts. Forest evidence follows the same `iteration1`/`iteration2` naming with `forest-night`. Additional normal 390 × 844 scroll frames at 620 and 1180 CSS pixels verified Today, weekend multi-meal rows, Shopping, Calendar and Reminders. The diagnostic, non-acceptance full-page capture is `/tmp/bb097-art-iteration2-forest-night-fullpage.png`.

The final deployed PWA was opened at `/tmp/bb097-art-deployed-obsidian-gold-390x844.png`, `/tmp/bb097-art-deployed-forest-night-390x844.png` and the corresponding 430 × 932 paths. All four Family sections and four meal tabs were present, `Idag` remained unbroken, alerts were zero and horizontal overflow was absent. The dock measured 358 × 64 at 390 and 398 × 64 at 430. `/tmp/bb097-art-deployed-arctic-wind-390x844.png` verified shared-token readability and layout compatibility. Temporary screenshots are local runtime evidence and are intentionally not publication artifacts.

Final frontend image `sha256:bc92125c7b5613dcc9db9b9661bd97f04e01db5119feeb2394463b37f7beff52` was deployed by recreating only Web with `--no-deps`; databases, volumes, API, Sentinel and external services were not recreated. Web, API and Sentinel were healthy; recovery reported healthy/clean. The shared theme setting was returned to Obsidian Gold. Finance remained `RESEARCH / 0 SEK / NONE` with scheduler enabled unchanged.

GitHub Actions run 32624929631 passed backend, frontend, documentation and secrets for the art-direction implementation. Local evidence included 115/115 frontend tests before the final label adjustment, 12/12 targeted Meal Planner tests after it, frontend production build, Release build with zero warnings/errors, 32/32 Sentinel tests, documentation verification over 174 Markdown files and 89 unique backlog IDs, Compose validation and `git diff --check`. The authoritative CI backend job passed the complete API/Sentinel suite; CI frontend passed the complete 115-test suite and production build. Local gitleaks was unavailable, while the authoritative CI secrets job passed.

Remaining visual differences require owner judgment rather than silent scope growth: the production Family week remains denser than the mockup summary, no mockup people/conflict content is invented, and the global center AI emblem/navigation silhouette remains outside this Family-only pass. Technical completion is **YES**. **OWNER VISUAL APPROVAL PENDING.**

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
