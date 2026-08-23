# BB-099 — Whole-App UX Audit

## Scope

Family is the owner-approved golden design reference. This audit covers the seven registered application views plus the embedded Settings surface and important view states. It evaluates composition, purpose, reachability and progressive disclosure rather than token adoption alone. Normal 390 × 844 viewports are primary; stitched full-page iOS captures are not acceptance evidence.

## View inventory and migration matrix

| View/state | Purpose / primary user goal | Current problems found | Mobile reachability / hierarchy | Required change | Status |
|---|---|---|---|---|---|
| Home | Understand what matters now | Three launcher rows duplicated the persistent dock and provided no information | Reachable but mostly empty; navigation dominated | Compose real meal, calendar, shopping, Media and Finance signals before contextual navigation | implemented locally |
| Family normal | Coordinate meals, shopping and calendar | Approved composition | Audit last content, tabs, Byt, dialogs and keyboard against shared clearance | Keep composition; adopt shared dock clearance | implemented locally |
| Family Byt/settings/dialogs | Contextual Family actions | No new composition defect found | Overlays already dock/safe-area aware | Regression only | verified locally |
| Media search/request | Find, play or request media | Primary workflow still sits among several equally framed modules | Search remains first but needs section rhythm rather than dashboard framing | Retain search first; use purpose sections and shared clearance | implemented locally |
| Media downloads/removal dialog | Understand and control current activity | Dense technical actions; destructive workflow must remain explicit | Dialog is viewport-bounded; bottom content needed more guaranteed dock clearance | Preserve grouped/contextual actions; shared clearance and overlay audit | implemented locally |
| Media Smart Shuffle | Start fair playback on a selected TV | Bordered form, nested checkbox scroller and select near lower page edge | Nested scrolling and dock proximity impaired mobile use | Remove nested scroller; use calm selectable list rows and material grouping | implemented locally |
| Media jobs/integrations | Follow pipeline and reach technical services | Technical detail competed with consumer tasks and opened by default | Long but scrollable | Keep below primary workflows; collapse technical integrations by default | verified locally |
| Finance default | Understand market, research, prospective evidence and risk | Five equally boxed cards still felt like an older dashboard | Key information existed but visual rhythm fragmented it | One ordered material narrative: market, current signals, prospective, risk, autonomous research | implemented locally |
| Finance advanced disclosure | Inspect research machine and provenance | Very dense by necessity | Closed by default; all expert evidence remains reachable | Preserve one explicit Details & Research disclosure and all datasets/actions | verified locally |
| More | Reach secondary destinations | Functionally appropriate; minor generic-list risk | Short and reachable | Retain AI/Admin contextual rows and integrated Settings | verified locally |
| Settings/theme picker | Change shared theme | No IA defect found | Embedded in More; native controls reachable | Verify all three themes, persistence and focus | verified locally |
| Admin | Diagnose system/recovery/integrations | Dense but legitimate; healthy status can become visually repetitive | Long page requires reliable final clearance | Keep issue-first semantics and technical sections; shared clearance | verified locally |
| AI | Truthfully describe present capability | Five planned placeholders repeated the same grammar | Reachable but intentionally low-value | Consolidate into one quiet truthful capability boundary; do not invent functions | verified locally |

## Functional parity inventory

- Home: Family, Media, Finance and Admin navigation remain available; new summaries use existing read-only endpoints only.
- Family: meal tabs, week navigation, Byt actions, shopping, shopping mode, calendar, reminders and settings remain unchanged.
- Media: search/play/request, downloads and destructive confirmation, Media jobs, Smart Shuffle, Jellyfin, Sonarr, Radarr and Prowlarr remain available.
- Finance: observation, overview, signals, prospective evidence, autonomous research, risk, scheduler, governor, operations, datasets, backups, shadow, watchlist, charts, features, robustness, backtests, retention and entitlement remain available.
- More/Settings: AI, Admin and the three allowlisted themes remain available.
- Admin: system metrics, recovery, Docker inventory and integration status remain read-only and available.
- AI: no capability is invented; planned states remain truthful.

## Shared reachability contract

`--bb-mobile-dock-clearance` owns page-end space for every mobile view: dock height plus safe-area inset and comfortable breathing room. Family and all DashboardWorkspace routes consume the same token. This replaces per-page arithmetic and ensures the final control can scroll completely above the persistent dock. Full-screen modes that intentionally hide the dock keep their existing dedicated safe-area rules.

## Validation matrix

| View | Obsidian mobile | Forest mobile | Arctic mobile | Desktop | Bottom reachable | Overlays reachable | Key actions | Result |
|---|---|---|---|---|---|---|---|---|
| Home | pass | pass | pass | pass | pass | n/a | summaries/navigation | pass |
| Family | pass | pass | pass | pass | pass | pass | tabs/Byt/settings | pass |
| Media | pass | pass | pass | pass | pass | pass | search/download/shuffle | pass |
| Finance | pass | pass | pass | pass | pass | pass | default/advanced | pass |
| More/Settings | pass | pass | pass | pass | pass | pass | secondary nav/theme | pass |
| Admin | pass | pass | pass | pass | pass | pass | status/disclosures | pass |
| AI | pass | pass | pass | pass | pass | n/a | truthful planned states | pass |

The normal-viewport matrix rendered all seven views in all three themes at 390 × 844 (21 captures), including top and lower scroll frames for the long Family, Media, Finance and Admin compositions. Family also passed its stabilized Arctic canary capture after the theme transition; an earlier immediate capture was discarded because it sampled the intentional 200 ms theme transition rather than steady-state UI. Responsive review covered 430 × 932, 768 × 1024 and 1440 × 900 without horizontal overflow. Final deployed evidence is appended to the implementation report after publication and appliance rollout.

## Known separate limitation

**FULL-PAGE IOS SAFARI SCREENSHOT ARTIFACT: STILL REPRODUCIBLE** according to owner evidence. It is distinct from actual dock occlusion. BB-099 uses normal viewport and scroll-position evidence and does not claim an iOS stitching fix.
