# BB-111 — Audiobook Route/Detail UX Fixes & Playback Credential Gate

## Metadata

- Date: 2026-08-28
- Baseline: `47bdf7c33425fed862ba9e1184c94031e2629af1`
- Scope: audiobook-local route focus/scroll/navigation/detail remediation and sanitized existing-credential investigation
- Sanitization: no credential, private identity, user/item ID, address, stream URL or raw provider payload is published.

Detta är en sanerad GitHub-version. Lokal rå runtime- och browser-evidens publiceras inte.

## Status

- UX: **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / OWNER UX REVIEW PENDING**
- Native BigBrain audiobook playback: **BLOCKED**
- Mini-player: **NO**
- Owner UX approved: **NO**
- Audiobook navigation globally adopted: **NO**

## Changes

The shared route-focus utility records keyboard versus pointer modality. Route headings still receive programmatic focus with `preventScroll` for assistive continuity. Only pointer-origin programmatic heading focus suppresses the decorative ring; keyboard focus continues to use the global visible focus contract.

Forward navigation stores the collection's current scroll on its history entry and creates detail with `scrollY=0`, then performs a bounded top scroll. Popstate restores the prior entry's saved scroll. The overview now exposes **Bibliotek** as one semantic link/navigation row, not a raised action button or new design-system variant.

Detail artwork, title, author and primary metadata form one compact grid. Artwork uses the established portrait contract (`aspect-ratio: 2/3`, `height: auto`, `object-fit: cover`) and a locally compact 88 px mobile detail width. Collection remains 68 px on mobile. No global artwork token changed. Optional unknown language is omitted from collection/detail; meaningful acquisition confidence and error states remain visible. The temporary Audiobookshelf link remains secondary because native playback is blocked.

## Existing credential investigation

Sanitized read-only runtime calls prove that the configured key acts for an active restricted, non-root Audiobookshelf user/integration identity. It has zero media-progress rows and zero listening sessions. Prior BB-109 evidence established that the owner's actual progress belongs to another identity. Reusing the current key would start at zero and write progress to the wrong account.

Audiobookshelf 2.36.0 supports user-owned session, tracks, sync and close semantics, but the right per-user credential is not configured in BigBrain. This is Case B of the owner decision tree: a separate server-side playback API key for the correct user is required. Codex did not create, copy, expose or guess such a key. Because identity/permission cannot yet be verified for that key, no native player, Continue Listening replacement, same-origin stream/Range/session/progress endpoint, app-shell audio state, mini-player or ADR was implemented. Adding an unused credential option alone would have no verified responsibility and was rejected.

## Security and scope

Credentials remain server-side and absent from browser DTO/config/logs/documentation. No generic proxy exists. No playback session was started against the wrong identity. Finance, live trading, scheduler, governor, Sentinel, acquisition controls, provider data and audit history are unchanged. No media was deleted and no acquisition was started, cancelled or restarted.

## Evidence

Focused Web regression is 28/28; complete Web is 151/151; API is 558/558 and Sentinel is 32/32. Vite and full Release builds, Compose, 192-file documentation verification, staged gitleaks and diff checks pass. Implementation `686f621937ae0e376bcc55003077769fff8b4351` is published and GitHub Actions run `33187236677` passed.

Only BigBrain Web was rebuilt/recreated and became healthy; API remained healthy. Nine Firefox cases across 390×844, 430×932 and 1440×900 plus all three themes proved link semantics, no pointer focus ring while DOM focus remains on route headings, detail at top, exact Library scroll restoration, no overflow, 2:3 artwork at 88 px mobile/180 px desktop with `object-fit:cover`, and no unknown-language copy.

## Remaining work

Physical owner review must verify on iPhone/PWA: no yellow route-heading rectangle after touch navigation; detail opens at top; back returns to the prior Library position; Library reads as navigation; compact hero alignment and undistorted cover; missing language consumes no space; fallback remains secondary; no overflow in all three themes. Native playback is not expected until the correct playback credential is separately configured and verified.

## Resumption

Start from the published BB-111 SHA and canonical `docs/STATUS.md`, `docs/BACKLOG.md`, `TESTING.md` and Media module documentation. The smallest safe playback step is owner configuration of a separate server-side per-user Audiobookshelf API key, followed by sanitized identity/permission verification; do not implement or invoke playback before that gate passes.
