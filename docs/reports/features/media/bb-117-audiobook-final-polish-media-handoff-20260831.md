# BB-117 — Audiobook Final Polish, Media Decision Freeze & Finance Handoff

Detta är en sanerad GitHub-version. No credentials, private addresses, owner media identifiers, raw logs or private runtime paths are published.

## Metadata

- Date: 2026-08-31
- Baseline/source of truth: `de1f256ecb1424556f05825f4d6525fd5e5675c9`
- Scope: production sleep-timer polish and durable Media pause/Finance handoff

## Status

**IMPLEMENTED / AUTOMATICALLY AND RUNTIME VERIFIED / DEPLOYED / CI VERIFIED / PHYSICAL OWNER REVIEW APPROVED.** Baseline/source of truth: `de1f256ecb1424556f05825f4d6525fd5e5675c9`. Implementation `601896619b29b2b487be8e840a48d3ea132891ad`; final compact-sheet fix `29cb505804c07b6413c057f134ec0175f08a7d99`. On 2026-08-31 the product owner physically reviewed iPhone/PWA and explicitly stated “I am satisfied.”

## Changes

BB-116A's shared production/lab `SleepTimerControl` is refined rather than duplicated. Continue Listening renders a BigBrain vector crescent at 23 px inside a circular gold 48 px target at the card's lower right. Active state uses `aria-pressed` and a subtle selected gold surface while the independent `Stannar HH:MM · N min kvar` row remains authoritative. Desktop/tablet keep the 280 px anchored interaction; mobile uses a compact centered 320 px bottom sheet above the dock. Dialog focus enters its close action and returns to the trigger on explicit/Escape dismissal. Presets, custom local time, off, shared AppShell deadline, expiry pause and ordinary progress sync are unchanged.

## Evidence

The owner physically accepted shared state, timer configuration, active-state propagation and the independent deadline row before BB-117, while rejecting the former visual trigger/menu. BB-117's focused component/style/lab tests pass 34/34 and full Web passes 160/160. Vite production build and documentation validation pass.

Deployed Firefox at 390×844, 430×932, 768×1024 and 1440×900 measured the crescent at 23×23 inside a 48×48 top hit-test button, gold `rgb(214, 163, 65)`, mobile sheet 320 px, desktop/tablet popover 280 px, correct focus entry/return, active `aria-pressed`, shared exact detail deadline, off, `grid-column: 1 / -1`, no status/time intersection, cover left of metadata and no horizontal overflow. Firefox BiDi pointer dispatch was inconsistent on three narrow runs even though `elementsFromPoint` resolved glyph → button with no overlay; DOM activation completed state transitions. This automation limitation is not treated as physical touch approval.

Only Web was rebuilt/recreated. Image `sha256:215921b72acbcf27c5f1fcc2bdc9f0e7c512c70e7fb5892e664b18629bed3749` is running healthy; `/` and `/health` returned HTTP 200. GitHub Actions runs `33385591024` and `33386467803` passed backend, frontend, documentation and secrets.

## Security

Timer authority remains local to the existing AppShell provider. No server timer, database timer, scheduler, worker, provider credential, arbitrary URL, external metadata source, destructive media/progress path or new infrastructure is added. Finance, Family, Sentinel, auth, dock and external media services are unchanged.

## Remaining work

Physical owner review is complete and approved. BB-155, BB-156 and BB-157 remain open. Media is in its planned pause; no further Media implementation starts automatically.

## Frozen owner decisions and deferred work

The mobile Audiobook Detail Hero is owner-design-approved: back above, cover left and compact title/author/series hierarchy right. It is unchanged. Primary playback action and full player remain experimental and owner-review pending. The functional vertical library is not final; a cover-forward, likely two-column iPhone grid is deferred. Rejected designs are the tiny white clock, broad/form-like disclosure and persistent floating mini-player.

BB-155 canonical metadata, BB-156 runtime latency/reliability diagnostics and BB-157 release availability/seeder search remain planned and open. Storytel is limited to future public high-level UX reference; no third-party code, CSS, DOM, identifiers, tokens, assets, artwork, icons, branding or implementation was copied. UX/UI Lab is implemented/deployed with owner review in progress; Design System v1 is not owner approved.

After successful physical iPhone verification, Media enters **PLANNED PAUSE — FUNCTIONAL WORK STABLE / DEFERRED UX & DISCOVERY BACKLOG PRESERVED**. The next intended development activity is a read-only Finance source-of-truth assessment. No Finance code, live broker, order execution or real-money automation is introduced.

## Verification and publication

- Focused Web: 34 tests passed across audiobook behavior, style contracts and UX/UI Lab reuse.
- Production build, four-viewport browser QA, Web-only deployment/runtime and both GitHub CI runs: passed as detailed above.
- Physical iPhone/PWA: pending and authoritative for crescent appearance/placement/target, menu appearance/placement/width, presets/custom/off/active state and unchanged Detail Hero.

## Resumption

If interrupted, use only `docs/operations/codex-recovery.md`, preserve valid working-tree work and resume the exact outstanding verification/publication step. Do not infer completion from this checkpoint.
