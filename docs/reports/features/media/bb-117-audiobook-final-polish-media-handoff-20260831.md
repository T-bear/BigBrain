# BB-117 — Audiobook Final Polish, Media Decision Freeze & Finance Handoff

Detta är en sanerad GitHub-version. No credentials, private addresses, owner media identifiers, raw logs or private runtime paths are published.

## Metadata

- Date: 2026-08-31
- Baseline/source of truth: `de1f256ecb1424556f05825f4d6525fd5e5675c9`
- Scope: production sleep-timer polish and durable Media pause/Finance handoff

## Status

**IMPLEMENTED / FOCUSED AUTOMATED VERIFICATION PASSED / DEPLOYMENT AND CI PENDING / PHYSICAL OWNER REVIEW PENDING.** Baseline/source of truth: `de1f256ecb1424556f05825f4d6525fd5e5675c9`. Physical owner acceptance cannot be inferred from component tests, browser QA, deployment health or CI.

## Changes

BB-116A's shared production/lab `SleepTimerControl` is refined rather than duplicated. Continue Listening renders a BigBrain vector crescent at 23 px inside a circular gold 48 px target at the card's lower right. Active state uses `aria-pressed` and a subtle selected gold surface while the independent `Stannar HH:MM · N min kvar` row remains authoritative. Desktop/tablet keep the compact anchored interaction; mobile uses a compact centered bottom sheet above the dock. Dialog focus enters its close action and returns to the trigger on explicit/Escape dismissal. Presets, custom local time, off, shared AppShell deadline, expiry pause and ordinary progress sync are unchanged.

## Evidence

The owner physically accepted shared state, timer configuration, active-state propagation and the independent deadline row before BB-117, while rejecting the former visual trigger/menu. BB-117's focused component/style/lab tests pass 34/34 and full Web passes 160/160. The Vite production build passes. Browser, deployment/runtime and CI evidence remains pending at this checkpoint and cannot grant owner UX approval.

## Security

Timer authority remains local to the existing AppShell provider. No server timer, database timer, scheduler, worker, provider credential, arbitrary URL, external metadata source, destructive media/progress path or new infrastructure is added. Finance, Family, Sentinel, auth, dock and external media services are unchanged.

## Remaining work

Complete four-viewport browser QA, publish the scoped implementation, deploy Web only, verify runtime and GitHub CI, then publish the resulting evidence. Physical owner iPhone/PWA review remains required. BB-155, BB-156 and BB-157 remain open.

## Frozen owner decisions and deferred work

The mobile Audiobook Detail Hero is owner-design-approved: back above, cover left and compact title/author/series hierarchy right. It is unchanged. Primary playback action and full player remain experimental and owner-review pending. The functional vertical library is not final; a cover-forward, likely two-column iPhone grid is deferred. Rejected designs are the tiny white clock, broad/form-like disclosure and persistent floating mini-player.

BB-155 canonical metadata, BB-156 runtime latency/reliability diagnostics and BB-157 release availability/seeder search remain planned and open. Storytel is limited to future public high-level UX reference; no third-party code, CSS, DOM, identifiers, tokens, assets, artwork, icons, branding or implementation was copied. UX/UI Lab is implemented/deployed with owner review in progress; Design System v1 is not owner approved.

After successful physical iPhone verification, Media enters **PLANNED PAUSE — FUNCTIONAL WORK STABLE / DEFERRED UX & DISCOVERY BACKLOG PRESERVED**. The next intended development activity is a read-only Finance source-of-truth assessment. No Finance code, live broker, order execution or real-money automation is introduced.

## Verification and publication checkpoint

- Focused Web: 34 tests passed across audiobook behavior, style contracts and UX/UI Lab reuse.
- Production build, browser QA, deployment/runtime and CI: pending at this checkpoint.
- Physical iPhone/PWA: pending and authoritative for crescent appearance/placement/target, menu appearance/placement/width, presets/custom/off/active state and unchanged Detail Hero.

## Resumption

If interrupted, use only `docs/operations/codex-recovery.md`, preserve valid working-tree work and resume the exact outstanding verification/publication step. Do not infer completion from this checkpoint.
