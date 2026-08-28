# BB-108 — Audiobook Navigation UX Experiment

## Metadata

- Date: 2026-08-28
- Scope: audiobook-local Web/PWA navigation experiment, tests, deployment evidence and documentation
- Baseline: `d068e84858d7f06c4d2cbe9c570ab3e88a856130`
- Sanitization: no private addresses, library inventory, credentials or raw service payloads are published.

Detta är en sanerad GitHub-version. Lokal rå runtime- och browser-evidens publiceras inte.

## Status

**IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / OWNER UX REVIEW PENDING**

The experiment is not accepted, is not a global BigBrain navigation standard and has no navigation ADR. Only physical-device owner feedback may change that verdict.

## Changes

The registry-composed Media overview now contributes only a compact audiobook heading, a count/chevron collection affordance and one real progress-backed Continue Listening row. It never renders `recent` as listening activity and never renders the catalogue. `/media/audiobooks` is a dedicated bounded collection/search/acquisition route. `/media/audiobooks/{id}` is a dedicated detail route that uses the existing versioned BigBrain item API and safe Audiobookshelf playback link.

The experiment uses `history.pushState`, `popstate` and the existing application shell rather than a router package, modal stack or global framework. Direct audiobook routes select Media. Browser and in-app back restore the previous route; query/sort remain in the mounted audiobook session and the previous history entry stores scroll position. Route headings receive focus. Decorative forward/back motion uses existing motion tokens and is removed by `prefers-reduced-motion`.

No FAB was added: opening a collection is navigation, not a create action, and acquisition already lives behind explicit edition confirmation inside the collection.

## Owner UX experiment history

| Hypothesis / design | Test surface | Owner observation | Result | Scope / possible lesson |
| --- | --- | --- | --- | --- |
| Large two-column covers on Media overview | BB-107 physical iPhone/PWA | Poor density and excessive vertical consumption. | **REJECTED** | Audiobook-local evidence; may inform a later global principle. |
| Bounded catalogue directly in overview with search/sort/pagination/load-more/collapse | BB-106/107 | Still turns overview into a catalogue and encourages long scrolling. | **REJECTED** | Audiobook-local evidence. |
| Recently added as primary overview information | BB-107 | Listening continuity is more relevant. | **REJECTED** | Audiobook-local evidence. |
| FAB as compensation for long pages | Historical BigBrain prototypes | No current evidence that FAB solves this hierarchy. | **UNDECIDED** | Not included in BB-108. |
| Overview → collection → detail real-route stack | BB-108 Web/PWA experiment | Automatic/browser evidence is green after Web-only deployment; physical verdict is absent. | **PENDING OWNER REVIEW** | Candidate for later review, not a standard. |

## Evidence

Focused verification passed 31 tests covering compact overview, progress semantics, bounded collection, deep-link detail, safe playback link, History API back, session state restoration, provider search and explicit acquisition confirmation. Complete regression passed 142 Web, 556 API and 32 Sentinel tests. Vite and full Release builds, Compose validation and documentation verification passed.

The deployed browser matrix passed 390×844, 430×932 and 1440×900 in Obsidian Gold, Forest Night and Arctic Wind. Every case showed zero catalogue cards on Media overview, a clear 20-item collection affordance, at most 20 of the bounded first page in collection, working detail/playback-link navigation, restored query/sort after browser back and no horizontal overflow. Mobile retained 112 px dock clearance. The runtime had no progress-backed item, so it truthfully rendered no Continue Listening tile. Reduced-motion behavior is covered by the stylesheet regression because the available Firefox automation transport cannot emulate that media feature. No acquisition was created by QA.

Runtime artwork tracing followed Audiobookshelf metadata through the BigBrain cover proxy. The reported film/music-note image returned HTTP-successful bytes from Audiobookshelf, and multiple related library records returned the same content. It is genuine upstream library artwork, not BigBrain CSS, Web fallback, broken-image UI or service-worker substitution. BigBrain must not overwrite it. Absent/failed responses still use the canonical B fallback.

## Security

No media or acquisition state is mutated. No secret, upstream credential, private provider URL or library inventory is published. Existing adapter, opaque candidate, confirmation, source-policy, qBittorrent isolation, no-overwrite and Audiobookshelf boundaries are unchanged. Finance, scheduler, governor and Sentinel are untouched.

## Remaining work

- Physical iPhone/PWA owner review must judge density, route motion, back behavior and whether the persistent dock helps or conflicts with the deeper hierarchy.
- The current reviewed contract exposes playback progress but not listening recency. Multiple recently listened items cannot be ordered truthfully without a future adapter-contract decision.
- Embedded pause/resume/seek/chapter controls remain outside BB-108 because no reviewed public API contract exists.
- Global standardization, if desired after owner acceptance, requires separate system-architect review and possibly an ADR.

## Resumption

Start from the final published BB-108 SHA and repository status. Do not infer owner acceptance from automated evidence. Collect the owner's physical-device verdict as **ACCEPTED**, **REJECTED** or **NEEDS ITERATION** and update this single report plus canonical status/backlog; do not create another experiment-log subsystem.
