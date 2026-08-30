# BB-115R — Physical iPhone Sleep-Timer Remediation

## Metadata

- Date: 2026-08-30
- Baseline: `f8136eccb7feed7bd2f9d23f449aa9f327c51a82`
- Scope: two physical-iPhone timer fixes plus permanent interrupted-run recovery rules
- Status: implemented and automatically verified; deployment, CI and physical owner verification pending
- Sanitization: no credentials, private addresses, runtime identifiers, raw payloads or sensitive logs are published.

Detta är en sanerad GitHub-version. Raw runtime payloads, identifiers, addresses, credentials, logs and screenshots are not published.

## Status

**IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYMENT, CI AND PHYSICAL OWNER VERIFICATION PENDING.** BB-115 owner UX approval remains NO.

## Changes

Physical iPhone/PWA review established that the compact timer button was disabled whenever the AppShell provider had no live in-memory session. A disabled native button dispatches no activation, so the owner saw a small icon with no response. BB-115R keeps the disclosure operable: it shows a clear session prerequisite and an explicit playback-start action, then exposes the existing shared options in the same open panel once the session exists. Playing and paused existing sessions remain directly configurable.

The active status collision came from absolute positioning inside the narrow action cluster. Status is now a separate semantic sibling in its own in-flow full-width grid row. The timer panel is bounded to the viewport. No player, library or backend architecture changed.

## Evidence

Focused Web tests pass 32/32 and Vite production build passes. Tests exercise the visible pre-session response/start path, session transition, shared detail state, preset/custom/cancel/expiration behavior and the non-absolute status-row contract. Deployment, deployed browser QA, CI and physical owner re-verification remain pending.

## Security

The timer remains client/AppShell scoped with no server scheduler, database or duplicate state. No provider credential, media metadata, progress authority, Finance, Sentinel, auth or destructive data path changed. Storytel code/CSS/assets are not used. BB-155, BB-156 and BB-157 remain unimplemented.

## Remaining work

Deploy Web only, verify timer interaction/containment at 390×844, 430×932 and 1440×900, obtain green GitHub CI and ask the owner to repeat the checks on the physical iPhone/PWA. UX/UI Lab remains the next sprint only after owner acceptance.

## Resumption

Resume from the published BB-115R commit and this report. Follow `AGENTS.md`; if interrupted, use only `docs/operations/codex-recovery.md` and preserve unrelated owner work.
