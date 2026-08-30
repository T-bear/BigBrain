# BB-115 — Physical iPhone Artwork Remediation & Continue-Listening Sleep Timer

## Metadata

- Date: 2026-08-30
- Baseline: `ad3e39d76139d664c403ea28068d586eaddbada3`
- Scope: exactly two Web product fixes after BB-114 physical owner review
- Status: implemented, automatically/runtime verified, deployed and CI verified; physical owner verification pending
- Sanitization: no credentials, identities, private addresses, item/session IDs, raw payloads or private paths are published.

Detta är en sanerad GitHub-version. Raw runtime payloads, identifiers, addresses, credentials, logs and screenshots are not published.

## Status

**IMPLEMENTED / AUTOMATICALLY AND RUNTIME VERIFIED / DEPLOYED / CI VERIFIED / PHYSICAL OWNER VERIFICATION PENDING.** Implementation `6917ed16b1fedbebaf26b06e36352d4627959d33` is published.

## Changes

BB-114's browser measurements did not establish the physical iPhone/PWA result. The stylesheet still contained three generations of detail layout rules. In particular, an obsolete mobile `display: contents` composition and broad direct-child grid-column assignments competed with the newer nested hero. BB-115 removes those legacy detail declarations and leaves one canonical hero. Its fetched artwork and BigBrain-B selectors are identical: explicit first grid column, `min-inline-size: 0`, 2:3 aspect ratio, 40 vw on ordinary mobile and a 180 px ceiling. No global Media artwork rule changed.

Continue Listening now exposes an icon-based, 44 px sleep-timer disclosure after the current book has an active playback session. It offers the existing 15/30/45/60-minute presets, local clock deadline and off, plus discreet active stop/remaining text. The detail view reuses the same component and AppShell provider deadline. There is no second timer implementation; ordinary pause/progress sync and best-effort iOS/PWA timing remain unchanged.

## Evidence

Focused Web regression passed 31/31, full Web passed 156/156 and the Vite production build passed. The CSS regression asserts removal of the actual legacy cascade in addition to the geometry contract. Component tests cover visible/disabled quick access, activation after playback start, accessible disclosure state, preset/custom/cancel/replace, shared detail state and ordinary expiration pause. API code did not change.

Only Web was rebuilt/recreated and became healthy; `/` and the proxied `/health` returned HTTP 200. Deployed Firefox measured Induction at 156×234 px (390×844), 172×258 px (430×932) and 180×270 px (1440×900), each exactly 2:3 without horizontal overflow. Golden Compass BigBrain-B matched 156/172/180 px and the same ratio. At 390×844, Continue Listening exposed `Sovtimer för Induction`, enabled it after direct playback, started 15 minutes, showed the identical deadline on detail, replaced it with 30 minutes and cancelled it without overflow. GitHub Actions run `33328144299` passed backend, frontend, documentation and secrets.

Physical iPhone/PWA owner verification remains pending. Browser QA cannot replace it because BB-114's browser evidence failed to expose the owner-reported regression.

## Security

Full player visual design remains owner-review pending and not Design System v1 approved. No floating player, UX/UI Lab, BB-155 implementation, provider, external artwork, Library/search/language, Finance, Sentinel, auth, infrastructure or destructive media/progress change is included. BB-156 Module Runtime Latency & Reliability is registered as **PLANNED / BACKLOG ONLY**.

## Remaining work

Ask the owner to repeat the Induction detail and Continue Listening timer checks in the physical iPhone/PWA. Browser evidence does not close that physical verification.

## Resumption

Resume from the published BB-115 commit and this report. Preserve AppShell playback lifetime, shared timer state and the audiobook-local artwork scope.
