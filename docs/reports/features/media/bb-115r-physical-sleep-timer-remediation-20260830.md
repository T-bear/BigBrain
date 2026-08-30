# BB-115R — Physical iPhone Sleep-Timer Remediation

## Metadata

- Date: 2026-08-30
- Baseline: `f8136eccb7feed7bd2f9d23f449aa9f327c51a82`
- Scope: two physical-iPhone timer fixes plus permanent interrupted-run recovery rules
- Status: implemented, automatically/runtime verified, deployed and CI verified; physical owner verification pending
- Sanitization: no credentials, private addresses, runtime identifiers, raw payloads or sensitive logs are published.

Detta är en sanerad GitHub-version. Raw runtime payloads, identifiers, addresses, credentials, logs and screenshots are not published.

## Status

**IMPLEMENTED / AUTOMATICALLY AND RUNTIME VERIFIED / DEPLOYED / CI VERIFIED / PHYSICAL OWNER VERIFICATION PENDING.** BB-115R owner UX approval remains NO. Implementation `c6519e059fc87faec92243271e26d0d9bf06d37e` and native-disclosure correction `a4139e46820a46b78c628d1db976a150d26adf98` are published.

## Changes

Physical iPhone/PWA review established that the compact timer button was disabled whenever the AppShell provider had no live in-memory session. A disabled native button dispatches no activation, so the owner saw a small icon with no response. Pre-correction deployed Firefox hit-testing additionally reached the enabled JavaScript-only toggle without opening it, exposing the weakness that component tests missed. BB-115R uses native `<details>/<summary>` disclosure semantics and keeps it operable: it shows a clear session prerequisite and an explicit playback-start action, then exposes the existing shared options in the same open panel once the session exists. Playing and paused existing sessions remain directly configurable.

The active status collision came from absolute positioning inside the narrow action cluster. Status is now a separate semantic sibling in its own in-flow full-width grid row. The timer panel is bounded to the viewport. No player, library or backend architecture changed.

## Evidence

Focused Web tests pass 32/32, full Web passes 157/157 and Vite production build passes. Tests exercise the visible pre-session response/start path, session transition, shared detail state, preset/custom/cancel/expiration behavior and the non-absolute status-row contract.

Only Web was rebuilt/recreated and became healthy; `/` and `/health` returned HTTP 200. Deployed Firefox at 390×844, 430×932 and 1440×900 pointer-opened the native summary, showed visible guidance/start before a session and bounded the panel to 280 px. After start, a 60-minute timer beside a 4:33:57 audiobook rendered with static positioning in grid column `1 / -1`; rectangle comparison showed no progress/time intersection and no horizontal overflow. Detail showed the exact same deadline/status and cancellation removed it. GitHub Actions runs `33337614697` and `33337917824` passed backend, frontend, documentation and secrets.

## Security

The timer remains client/AppShell scoped with no server scheduler, database or duplicate state. No provider credential, media metadata, progress authority, Finance, Sentinel, auth or destructive data path changed. Storytel code/CSS/assets are not used. BB-155, BB-156 and BB-157 remain unimplemented.

## Remaining work

Ask the owner to repeat the timer-open, configure, shared-state and no-overlap checks on the physical iPhone/PWA. UX/UI Lab remains the next sprint only after owner acceptance.

## Resumption

Resume from the published BB-115R commit and this report. Follow `AGENTS.md`; if interrupted, use only `docs/operations/codex-recovery.md` and preserve unrelated owner work.
