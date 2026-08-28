# BB-109 — Audiobook Owner UX Remediation & Native Playback Investigation

## Metadata

- Date: 2026-08-28
- Scope: audiobook owner UX remediation, progress/playback contract investigation and stale acquisition reconciliation
- Baseline: `2937105c80869eb0bfa30b55c154d85d4f98cc04`
- Sanitization: no private identities, item IDs, addresses, tokens, stream URLs or provider payloads are published.

Detta är en sanerad GitHub-version. Lokal rå runtime- och browser-evidens publiceras inte.

## Status and scope

**IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / OWNER UX REVIEW PENDING** on 2026-08-28, based on `2937105c80869eb0bfa30b55c154d85d4f98cc04`. This report is sanitized: it contains no private identities, item IDs, addresses, tokens, stream URLs or provider payloads.

BB-108 physical review positively validated a bounded Media overview with explicit entry into the long collection. BB-109 preserves that local experiment and remediates its affordance, hierarchy, discovery placement, collection utility and acquisition-history correctness. It does not adopt the navigation model globally.

## Changes

- The overview uses the established tertiary **Bibliotek** control; inventory count is no longer the dominant action.
- A real progress-backed item remains the only source of **Fortsätt lyssna**. When the connected profile has none, BigBrain reports that profile limitation instead of claiming the owner has not started a book.
- The collection places **Hitta en ny ljudbok** near the top and labels it **Sök utanför ditt bibliotek**. Local **Sök i ditt bibliotek** remains a distinct intent.
- Collection title/count hierarchy is quieter; the catalogue remains bounded to 24 items per request.
- A 44 px scroll-to-top utility appears after 600 px, clears dock/safe area and switches from smooth to immediate motion under reduced-motion preference.
- Downloads are split into active, requires-attention and terminal history. **Dölj visad historik** writes only bounded device-local presentation IDs; audit/job/media data is not deleted.

## Proven Continue Listening root cause

The BigBrain adapter uses the restricted Audiobookshelf commissioning identity. Sanitized runtime API and read-only database aggregation showed 0 media-progress rows, 0 active progress rows and 0 listening sessions for that identity, while another Audiobookshelf identity had 3 progress rows, 3 active rows and 3 sessions. Library responses consequently omitted `userMediaProgress` before reaching BigBrain. The Web progress predicate was not the loss point.

Audiobookshelf progress and playback are user-specific. Relabeling added timestamps or borrowing another user's state would be factually and security-wise wrong.

## Playback-contract investigation

Installed Audiobookshelf 2.36.0 source/runtime and official API material establish these semantics without publishing private paths: authenticated item playback starts a user-owned session; the response carries bounded track/session data; session tracks may be served through an ephemeral session identity; authenticated sync and close update that same user's progress. Browser seeking depends on range-capable delivery, and transcoded tracks may redirect.

This is enough to show a native BigBrain player is feasible, but not enough to choose its identity safely. Starting with the current restricted identity would resume the owner's book at zero and synchronize progress to the wrong account. Passing an owner token to Web would violate the server-side credential boundary. **Native player: PARTIAL/BLOCKED** pending an owner/system-architect decision on canonical playback identity and a same-origin session/Range/progress-sync adapter contract. No player API was invented; the existing owner link is explicitly temporary.

## Proven stale acquisition root cause and fix

Sanitized runtime correlation found persisted active jobs for which current Librarr/qBittorrent state and durable import evidence were all absent. Provider status therefore returned no job. BigBrain previously interpreted that absence as “keep old state” forever, leaving `Hämtas` stale.

Reconciliation now permits a five-minute registration grace, then persists a sanitized `failed` attention state when the provider still has no job/evidence. Existing destination/import/no-overwrite semantics are unchanged. No owner acquisition is cancelled, restarted or deleted by this transition.

## Evidence

Focused Web and API regressions cover truthful Continue semantics, the two search intents, bounded collection, scroll utility/reduced motion, history presentation semantics, old missing-provider transition and new-registration grace. Full test/build/runtime evidence is recorded in `TESTING.md` and final publication status in `docs/STATUS.md`.

The verified totals are 17 focused Web, 13 focused API, 144 complete Web, 558 complete API and 32 Sentinel tests. Vite and full Release builds passed without warnings; Compose and the 190-file documentation verifier passed. Only API and Web were recreated and became healthy. Read-only reconciliation transitioned 15 stale evidence-free active rows, leaving one completed and 18 failed/attention rows without deleting records. Nine deployed viewport/theme cases had no overflow, no catalogue on overview, no dominant count, clear separate search intents, visible scroll-to-top and 112 px mobile dock clearance.

Finance, scheduler, governor, Sentinel, source policy, qBittorrent configuration, Audiobookshelf data and acquisition confirmation are unchanged. No owner media was deleted and no owner acquisition was started, cancelled or restarted.

## Security

All Audiobookshelf credentials remain server-side. No stream/session URL or token is exposed to Web or documentation. The existing provider-neutral acquisition, explicit confirmation, source policy, qBittorrent category, import evidence, path escape and no-overwrite boundaries are unchanged. The reconciliation transition preserves records and media.

## Remaining work

1. Decide which Audiobookshelf identity is authoritative for BigBrain playback/progress and approve a same-origin playback-session/Range proxy design before native playback implementation.
2. Physically verify **Bibliotek**, collection hierarchy, separate search intents, scroll-to-top, dock clearance and active/history presentation on iPhone/PWA.
3. BB-109 remains owner UX review pending; BB-108 is still not a global navigation standard.

## Resumption

Start from the final published BB-109 SHA and current repository status. Do not infer native-player authorization or owner UX approval. The next action is an explicit owner/system-architect decision on playback identity and same-origin streaming/session boundaries, followed separately by physical-device review of the deployed BB-109 UX.
