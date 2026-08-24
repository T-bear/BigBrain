# BB-103 — First Usable Audiobook Flow

## Metadata

- Date: 2026-08-24
- Status: technically ready and deployed; first real acquisition owner-gated and not started
- Scope: close the existing BB-100/101/102 acquisition lifecycle without changing provider boundaries
- Sanitization notice: no keys, private addresses, raw provider URLs, magnet links, tracker details, private library data or credentials are published.

Detta är en sanerad GitHub-version; lokal runtime-evidens med privata värden publiceras inte.

## Status

**TECHNICALLY READY / FIRST REAL ACQUISITION OWNER-GATED.** Search, explicit release confirmation, truthful lifecycle reconciliation and deployed mobile UI are verified. No request/download was started, so the first real acquisition remains unverified.

## Verified baseline

BB-100 provides the commissioned Audiobookshelf library, metadata, progress and owner playback link. BB-101 owns bounded candidates, explicit jobs and the registry-composed Media UI. BB-102 supplies the internal, authenticated Librarr provider, Prowlarr-first plus explicitly allowlisted AudioBookBay discovery, qBittorrent's dedicated audiobook path/category, and fail-closed no-overwrite import behavior. The deployed provider is healthy and read-only searches have returned real candidates. No acquisition has been requested.

## Implementation plan

1. Replace disappearance-based completion with durable Librarr import evidence and Audiobookshelf indexing confirmation. Import conflict/failure remains terminal and sanitized; missing evidence remains nonterminal.
2. Make candidate submission an explicit confirmation from the details surface, keep opaque candidate IDs and provider secrets server-side, and render localized truthful job states.
3. Refresh the Audiobookshelf overview only after confirmed indexing so a completed job naturally appears in BigBrain's library.
4. Add focused provider, API/service, UI and registry regression coverage, then run repository validation.
5. Deploy only the changed Librarr/API/Web services, execute real read-only search and mobile/theme QA, and stop before the owner-gated first Add action.

The first real acquisition lifecycle remains **NOT VERIFIED** until the owner selects a specific candidate and explicitly starts it in BigBrain.

## Changes

Candidate cards no longer expose a one-tap request. The owner opens **Välj utgåva**, reviews title/release, author, narrator when present, language/confidence, bounded edition data and sanitized Prowlarr/AudioBookBay provenance, then explicitly confirms **Lägg till vald utgåva**. The frontend submits only the opaque BigBrain candidate and renders localized job states without an invented percentage.

The previous provider considered a qBittorrent row disappearing after `importing` sufficient for `completed`. BB-103 removes that unsafe inference. Librarr image `bigbrain-librarr:1208254-bb3` preserves the immutable upstream pin and both BB-102 patches, and adds only durable sanitized import-failure evidence. BigBrain now checks, in order: a matching active qBittorrent row; the newest exact-hash Librarr import event; an exact local audiobook `source_id`; and exact title/author visibility through Librarr's Audiobookshelf-backed library endpoint. Successful import without ABS visibility is `indexing`; missing evidence remains nonterminal; conflict/failure is `failed`; only ABS visibility becomes `completed`.

The local Librarr library response is consumed server-side only. File paths, raw indexer names, download/provider URLs, magnet data and credentials never enter Web. Display HTML entities are decoded as text; absent author, narrator and language remain absent/unknown. Completion causes Web to refresh the existing BigBrain Audiobookshelf overview, and playback remains an owner-reachable Audiobookshelf deep link.

## Evidence

- Patched Librarr build: upstream `internal/organize`, `internal/search` and `internal/download` all passed; the focused API hash regression passed. The new temp-filesystem regression verifies conflict evidence, source preservation and unchanged destination.
- BigBrain API: 538 passed; focused final provider set: 23 passed.
- BigBrain Web: 129 passed across 22 files; focused audiobook view: 8 passed.
- Sentinel tests: 32 passed. This does not remediate or conceal the independent runtime socket issue.
- Vite production build and `BigBrain.slnx` Release build passed with zero warnings/errors.

## Runtime and owner gate

Only Librarr, API and Web were recreated. Librarr `bb3`, API and Web are healthy; Audiobookshelf, Prowlarr, qBittorrent, Jellyfin, Sonarr and Radarr were not recreated. Audiobookshelf overview and provider status are both `configuredHealthy`; acquisition job total remains zero and the library is currently empty.

Read-only `Harry Potter` searches returned 11–12 candidates across observations, all from Prowlarr; AudioBookBay returned zero. Results exposed sanitized provenance and bounded release/size differentiation. No authoritative language or narrator metadata was present, so all candidates remained `und/unknown` and narrator stayed absent. This is truthful even with Swedish selected.

Deployed Firefox QA at 390 × 844 and 430 × 932 passed in Obsidian Gold, Forest Night and Arctic Wind with 11 real candidates: explicit confirmation and enabled Add were visible, no acquisition activity existed, HTML entities were decoded, long release text wrapped, no horizontal overflow occurred and the modal/action remained clear of the dock. Obsidian Gold was restored.

Finance remained `RESEARCH / 0 SEK / NONE`. The scheduler is enabled, but the 2026-08-24 02:00 UTC opportunity was skipped as a non-research day with no research run; the next due time is 2026-08-25 02:00 UTC. BB-099 remains **TECHNICALLY COMPLETE / OWNER UX REVIEW PENDING**. Sentinel remains in its known `sentinel.sock` address-in-use restart loop and was not modified.

**TECHNICALLY READY: YES. FIRST REAL ACQUISITION VERIFIED: NO.** The owner must select one specific release in BigBrain and explicitly confirm Add before request, download, import and final indexing can be verified against real media.

## Security

BB-102's no-overwrite, path-containment, exact source allowlist, internal-only Librarr, opaque candidate and server-side secret boundaries are unchanged. BB-103 removes an unsafe completion inference, adds only sanitized failure evidence and strips raw indexer names from owner-facing edition metadata. The first acquisition remains owner-gated.

## Remaining work

The real request → qBittorrent → safe import → Audiobookshelf indexing → BigBrain library lifecycle cannot be marked verified until the owner chooses a specific candidate and explicitly confirms it. If that real run conflicts or fails, preserve both source and destination and investigate through the runbook.

## Resumption

Open BigBrain → Media → Ljudböcker, search for the desired title, select **Välj utgåva**, verify the release details and choose **Lägg till vald utgåva**. Resume validation from the resulting BigBrain job ID without exposing provider identifiers or credentials.
