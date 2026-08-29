# BB-113 — Audiobook Player Affordance, Detail Layout & Artwork Remediation

## Metadata

- Date: 2026-08-29
- Baseline: `a50a52e33d95e317b0391647a2378a5e347210e5`
- Scope: small post-BB-112 physical owner-review remediation
- Status: implemented, automatically verified and deployed; publication/owner UX review pending
- Sanitization: no credential, identity, private address, item/session ID, raw payload or private path is published.

Detta är en sanerad GitHub-version. Raw runtime payloads, identifiers, addresses, credentials, logs and screenshots are not published.

## Status

**IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / CI VERIFIED / OWNER UX REVIEW PENDING**. Implementation `5d829fdcf64541e49b8cf355004bea90d2a372d3` is published.

## Owner evidence and causes

The physical iPhone/PWA view had authoritative Continue Listening progress but no direct playback control. Detail compressed “The Golden Compass Disc 1” into a narrow residual column, showed only a dominant temporary Audiobookshelf action and rendered a generic filmstrip/music-note image.

The detail layout combined an old mobile `display:contents` rule for every direct wrapper with later partial overrides. This made the hero depend on fragile cascade and intrinsic grid placement. The current BB-112 source already rendered a native Spela action and deployed availability was healthy, while the screenshot's exact temporary fallback copy is absent from current source. The discrepancy is therefore an older cached Web/PWA shell, not a hidden healthy-state condition or missing DTO. BB-113 adds explicit availability-driven presentation and prevents the HTML shell/manifest from being reused stale.

The visible generic image was traced without guessing: BigBrain's cover endpoint returned Audiobookshelf's successful 400×400 JPEG, not the canonical BigBrain fallback. Only that exact verified binary hash is now treated as a known upstream generic placeholder. All other successful artwork remains untouched; missing/recognized generic artwork falls through to the established BigBrain-B component.

## Changes

Continue Listening now separates detail navigation from a prominent 48 px accessible play/pause control. Current/total time is derived only from Audiobookshelf duration and progress. Direct start uses the unchanged BB-112 native session path and AppShell player state; mini-player structure was not redesigned.

Detail owns an explicit artwork/summary hero, `minmax(0,1fr)`, natural word wrapping, no character-like breaking, 2:3 artwork and a narrow stacked fallback. Healthy availability makes native playback primary and labels external Audiobookshelf access as a tertiary reserve path. Unavailable playback shows a bounded explanation and only then promotes the reserve path.

Focused verification passed 18 API and 23 Web tests. Full verification passed 564 API, 32 Sentinel and 154 Web tests; Vite build and Compose validation passed. Only API and Web were rebuilt/recreated and both are healthy.

## Evidence

Firefox at 390×844 verified compact direct play, authoritative `current / total`, playing/Pausa state, mini-player appearance and survival after navigation. The 390×844, 430×932 and 1440×900 matrix in Obsidian Gold, Forest Night and Arctic Wind verified direct affordance, full-width nonzero mobile hero boxes, natural Golden Compass wrapping, synthetic long Swedish wrapping with `word-break:normal`, canonical B fallback at 2:3, visible native action, tertiary external reserve path and zero horizontal overflow. The Web shell returns `no-cache, no-store, must-revalidate`. GitHub Actions run `33269194506` passed frontend, backend, documentation and secrets. No owner UX approval is inferred.

A subsequent documentation run exposed an existing async test race: the assertion queried `Filtrera` while collection loading could still expose the busy-name `Filtrera pågår`. Testfix `09976f0ab43c825f22434177ca7952f5207b746b` waits for readiness; three focused repetitions, full Web and Vite passed locally, and GitHub Actions run `33269619672` passed all jobs. Product behavior did not change.

## Security and boundaries

ADR 0037 is unchanged. Credentials remain server-side, sessions remain opaque/bounded, Audiobookshelf remains durable progress truth and AppShell retains playback lifetime. No media, progress, acquisition, Finance, Sentinel, Library navigation or search/language behavior was modified.

## Remaining work

On physical iPhone/PWA, verify fresh shell loading, direct Continue Listening play/pause and time, mini-player appearance/route survival, natural Golden Compass title wrapping, BigBrain-B fallback in place of the media note, native detail controls as the primary path, secondary Audiobookshelf reserve path, and no overflow/dock or safe-area collision.

## Resumption

Start from the published BB-113 commit and current `docs/STATUS.md`, `docs/BACKLOG.md`, `TESTING.md`, Media module documentation, this report and ADR 0037. Do not infer owner approval from automated or Firefox evidence, and do not broaden follow-up into Library, search/language or mini-player redesign without a separate owner decision.
