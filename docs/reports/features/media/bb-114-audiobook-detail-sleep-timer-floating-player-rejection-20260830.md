# BB-114 — Audiobook Detail Polish, Sleep Timer & Floating Player Rejection

## Metadata

- Date: 2026-08-30
- Baseline: `587c5d0b41c02440d852d65c494918a72e175bfb`
- Scope: small post-BB-113 physical owner-review remediation
- Status: implemented, automatically/runtime verified and deployed; CI and owner UX review pending
- Sanitization: no credentials, identities, private addresses, item/session IDs, raw payloads or private paths are published.

Detta är en sanerad GitHub-version. Raw runtime payloads, identifiers, addresses, credentials, logs and screenshots are not published.

## Status

**IMPLEMENTED / AUTOMATICALLY AND RUNTIME VERIFIED / DEPLOYED / CI AND OWNER UX REVIEW PENDING.**

## Changes

Audiobook detail now uses a compact local 2:3 cover rule (maximum 180 px; 40 vw on normal mobile) for real covers and the BigBrain-B fallback. The redundant LJUDBOK label is removed. Native playback remains primary; healthy detail contains no Audiobookshelf link, while unavailable playback retains a truthful **Öppna i Audiobookshelf** recovery action without “reservväg”.

The mapping audit established exact provenance. `X3M 4ever!!!` is Audiobookshelf `media.metadata.description` on Min Morbror Trollkarlen; the exact non-synopsis value is omitted in BigBrain without source mutation. `Pirateaba` is Ghostsong `media.metadata.authorName` and remains as explicitly presented author metadata. Series, narrator, language, year and useful synopsis remain.

The AppShell-scoped provider still owns the audio element, opaque session, position and sync. AppShell no longer renders player UI, so Home, Family, Finance and unrelated Media views have no overlay while audio survives route changes. Returning to the active audiobook detail restores controls from the same player state. This records a durable owner decision: persistent floating audiobook mini-player UI is **REJECTED** after physical mobile testing; navigation-surviving playback is **RETAINED**.

A lightweight client-only sleep timer supplies 15/30/45/60-minute presets, a local clock deadline and off. Active UI shows stop time and remaining minutes. Expiration pauses through the ordinary playback path, retaining normal sync and never marking completion. Changing a timer replaces the deadline; cancel and stop clear it. iOS/PWA suspension can delay JavaScript execution, so exact background timing is not guaranteed.

## Evidence

Verification passed 19 focused API, 31 focused Web, 565 full API, 156 full Web and 32 Sentinel tests. Vite/Release, 196-document/89-unique-BB-ID, Compose and diff gates passed. The first parallel Web run exhausted worker startup capacity while API/Sentinel ran; the isolated bounded-worker rerun passed 156/156.

Only API and Web were rebuilt/recreated and became healthy. Deployed Firefox verified Narnia, Ghostsong and Golden Compass Disc 3 at 390×844, 430×932 and 1440×900: artwork widths were 156/172/180 px at 2:3, fallback used identical geometry, useful metadata/description remained, LJUDBOK/X3M/reservväg/healthy external link were absent and horizontal overflow was false. The matrix caught and drove correction of a legacy mobile `grid-column:1/-1` cascade before final evidence.

An active Induction session reported Pausa, navigated to Home with no player overlay or overflow, and returned to the same detail as Pausa with static in-flow controls. The sleep timer started at exactly 15 min and cancelled cleanly. API/Web health and sanitized playback availability were green after deployment. GitHub CI awaits the published commit.

## Security

No external metadata/artwork provider, credential change, server scheduler, worker, database, chapter/speed control, global replacement player, Library/search/language redesign, UX/UI Lab, Finance/Sentinel/auth change, media deletion or destructive progress operation is introduced. BB-155 Canonical Book Metadata Resolver is registered as **PLANNED / BACKLOG ONLY** and is not implemented. Owner UX approval and BigBrain Design System v1 approval remain **NO**.

## Remaining work

Publish the scoped commit and verify GitHub CI. Physical iPhone/PWA owner approval remains separate; Web timers cannot promise exact expiry while iOS suspends the PWA.

## Resumption

Resume from the published BB-114 commit and the canonical Status, Backlog, Testing, Media module documentation, ADR 0037 and this report. Do not infer owner UX approval from automated or browser evidence.
