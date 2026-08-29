# BB-112 — Native audiobook playback and owner search remediation

## Metadata

- Date: 2026-08-29
- Baseline: `f14fbc642a081d0298e96e096feead8816a40cbb`
- Scope: native audiobook playback, Continue Listening, mini-player and owner search/result remediation
- Related commit: pending publication
- Sanitization: no credentials, identities, item/session IDs, private addresses or raw payloads are published.

Detta är en sanerad GitHub-version. Lokal rå runtime- och browser-evidens publiceras inte.

## Status

**IMPLEMENTED / AUTOMATICALLY AND RUNTIME VERIFIED / DEPLOYED / OWNER UX REVIEW PENDING**. Physical iPhone/PWA owner approval is not inferred from automated evidence.

## Evidence

A separately supplied server-side playback key was accepted as an active restricted identity, differed from the integration key and exposed real progress rows. The integration key remains unchanged. A controlled existing in-progress item started a session; a bounded 1 KiB request returned `206`, a one-second progress sync succeeded, the original position was restored on close and the closed session returned `404`. Firefox subsequently received `206` audio, reported playing state, exposed the real slider position and retained a compact player across navigation.

Focused verification passed 16 API and 21 Web tests. Full regression passed 562 API, 32 Sentinel and 151 Web tests; Release and Vite builds completed without warnings/errors. Compose, 194-file documentation verification and the nine-case 390×844/430×932/1440×900 theme matrix passed without horizontal overflow while retaining BB-111 focus, scroll and 2:3 artwork behavior.

## Changes

BigBrain exposes purpose-built availability/start, bound track, sync and close responsibilities. Random process-local IDs expire after 15–240 minutes (120 default), bind one item and track allowlist, and accept one server-capped 8 MiB byte range. No browser field accepts an upstream URL and neither credential appears in DTOs. Audiobookshelf remains authoritative; no BigBrain progress database was added. AppShell owns audio/session state. Detail/player provide play/pause, actual position/duration, seek and ±30 seconds. Continue Listening uses the playback identity's `items-in-progress`, not `addedAt`.

## Owner search evidence

The physical observations remain observations, not assumed causes: one iPhone card forced “The Lion, the Witch and the Wardrobe” into a near-vertical column; one Swedish search missed an edition later visible under All Languages; a later Swedish search returned Swedish and unknown related Prowlarr rows; three physical users independently found BB-111 Library awkward.

The card had an insufficiently defended nested min-content boundary. Candidate surface/copy/title now own full width, `min-width:0`, normal word breaking and safe wrapping while artwork stays compact. Language selection is a ranking preference, not strict exclusion: explicit language is verified, strong release tokens remain probable, and absent evidence remains unknown but visible. Duplicate “Språk okänt” confidence is removed; probable language is “Troligt …”. Dedup remains stable provider release identity/infohash/guid, never fuzzy title/size deletion.

Six read-only searches took 30–34 seconds. Swedish varied from two unknown non-Prowlarr results to four results containing one Swedish, three unknown and two Prowlarr; all three All Languages runs returned the latter shape. Provider/indexer variation is proven; BigBrain does not claim live provider determinism.

Overview hierarchy is **Lyssna/Ljudböcker → Continue Listening → secondary Bibliotek navigation row**. This addresses the isolated affordance without a primary CTA or global navigation adoption. Library remains owner UX review pending.

## Security

The appliance has no general multi-user BigBrain authentication mapping. Session ownership is bounded to the established single-owner same-origin deployment; multi-user use requires an authentication extension. Chapters and speed are deferred. No media was deleted, progress destructively cleared, acquisition changed or audit history removed. Finance, trading, scheduler, governor and Sentinel are unchanged.

Credentials, private identities, private addresses, item/session identifiers, raw logs and raw provider payloads are absent from this report. The Web receives neither Audiobookshelf credential and no generic proxy or browser-supplied upstream URL exists.

## Remaining work

Physical owner review must verify playback, mini-player, Library hierarchy, long-title cards and language presentation on iPhone/PWA. Chapters/speed remain deferred. Multi-user deployment remains gated on a future authenticated BigBrain-user mapping.

## Resumption

Start from the published BB-112 commit and canonical `docs/STATUS.md`, `docs/BACKLOG.md`, `TESTING.md`, Media module documentation and ADR 0037. The smallest next step is physical owner UX review; do not infer approval from this report.
