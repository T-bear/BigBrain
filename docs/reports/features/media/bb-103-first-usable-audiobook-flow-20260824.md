# BB-103 — First Usable Audiobook Flow

## Metadata

- Date: 2026-08-24
- Status: technically complete, deployed and first real acquisition verified
- Scope: close the existing BB-100/101/102 acquisition lifecycle without changing provider boundaries
- Sanitization notice: no keys, private addresses, raw provider URLs, magnet links, tracker details, private library data or credentials are published.

Detta är en sanerad GitHub-version; lokal runtime-evidens med privata värden publiceras inte.

## Status

**TECHNICALLY COMPLETE / FIRST REAL ACQUISITION VERIFIED.** The owner-selected Narnia release completed request, download, safe import, Audiobookshelf indexing, BigBrain library visibility and owner playback/deep-link verification.

## Verified baseline

BB-100 provides the commissioned Audiobookshelf library, metadata, progress and owner playback link. BB-101 owns bounded candidates, explicit jobs and the registry-composed Media UI. BB-102 supplies the internal, authenticated Librarr provider, Prowlarr-first plus explicitly allowlisted AudioBookBay discovery, qBittorrent's dedicated audiobook path/category, and fail-closed no-overwrite import behavior. The deployed provider is healthy and read-only searches have returned real candidates. No acquisition has been requested.

## Implementation plan

1. Replace disappearance-based completion with durable Librarr import evidence and Audiobookshelf indexing confirmation. Import conflict/failure remains terminal and sanitized; missing evidence remains nonterminal.
2. Make candidate submission an explicit confirmation from the details surface, keep opaque candidate IDs and provider secrets server-side, and render localized truthful job states.
3. Refresh the Audiobookshelf overview only after confirmed indexing so a completed job naturally appears in BigBrain's library.
4. Add focused provider, API/service, UI and registry regression coverage, then run repository validation.
5. Deploy only the changed Librarr/API/Web services, execute real read-only search and mobile/theme QA, and stop before the owner-gated first Add action.

The first real acquisition lifecycle is **VERIFIED** for the owner-selected Narnia release.

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

An observed post-acquisition inconsistency had two causes. Audiobookshelf was initially still indexing, while a transient provider-status request was incorrectly mapped by Web to an invalid `unavailable` value and therefore displayed as not configured. Separately, the completed job remained `indexing` because Librarr stored the tagged release title and author `Unknown`, while Audiobookshelf exposed canonical title/author metadata. Runtime configuration was present and bound throughout the investigation.

The minimal fix maps transient provider failure to `configuredUnavailable` and withholds provider copy while status is pending. Completion still requires the exact provider hash, exact import event and exact local `source_id`; it then permits exactly one deterministic normalized canonical-title match and treats `Unknown` as absent metadata. Ambiguous matches remain `indexing`. The existing Narnia job reconciled naturally to `completed` without persisted-state manipulation. BigBrain returned one unique library item, the cover proxy and owner deep link returned HTTP 200, and 390 × 844 QA verified `Klar`, the second existing job truthfully `Hämtas`, no overflow and full dock clearance.

**TECHNICALLY COMPLETE: YES. FIRST REAL ACQUISITION VERIFIED: YES.**

## Security

BB-102's no-overwrite, path-containment, exact source allowlist, internal-only Librarr, opaque candidate and server-side secret boundaries are unchanged. BB-103 removes an unsafe completion inference, adds only sanitized failure evidence and strips raw indexer names from owner-facing edition metadata. The first acquisition remains owner-gated.

## Remaining work

No BB-103 lifecycle blocker remains. The separate Revolvermannen acquisition was observed read-only as downloading and was not modified.

## Resumption

No further BB-103 action is required for the verified Narnia lifecycle. The separate existing Revolvermannen job may be observed through its BigBrain-owned job ID without cancelling, restarting or exposing provider identifiers or credentials.

## Bounded author-aware discovery follow-up

Owner-requested discovery quality work on 2026-08-24 produced `bigbrain-librarr:1208254-bb4` without changing provider, source, indexer or acquisition architecture. The isolated fourth patch keeps title-only searches unchanged and generates at most three normalized unique audiobook source queries when an author is supplied. For the diagnostic title these are `The Wandering Inn`, `The Wandering Inn pirateaba` and `Wandering Inn pirateaba`. Each Prowlarr query retains its category `3030` variant and one generic audiobook variant; an existing equivalent audiobook term is never appended twice. Variants share the existing 30-second cancellation boundary, successful variants survive another variant failing, and original title/author scoring remains authoritative after aggregation.

The deployed read-only comparison used English preference and made no acquisition request. Before the patch the title-plus-author call completed with zero candidates while author affected only scoring. After deployment, all three author-aware variants reached sources; the six sanitized Prowlarr category/generic calls each returned zero raw rows. Librarr retained zero and BigBrain returned zero candidates in 24.26 seconds. AudioBookBay completed without yielding a retained result. The evidence therefore leaves audiobook-capable English indexer coverage—not application filtering or ranking—as the primary remaining limitation for this title.

The bb4 build passed complete upstream organizer, search and download packages plus focused API regression. BigBrain provider tests passed 33/33, the registry-composed audiobook Web suite passed 9/9, Vite production build and documentation/Compose checks passed. Deployed 390 × 844 browser smoke passed Obsidian Gold, Forest Night and Arctic Wind with provider configured, English selection and author retained, no horizontal overflow, dock clearance and no acquisition dialog or Add action.

## Pinned-revision native discovery policy

On 2026-08-24 the owner superseded individual approval of each native discovery source with trust in the complete audiobook source set shipped by the exact pinned Librarr commit. Image `bigbrain-librarr:1208254-bb5` admits only `audiobookbay`, `librivox`, `tpb_audiobook` and `booktracker_audiobook`, plus the existing `prowlarr_audiobooks` integration. The exact revision and complete set are startup invariants: an unknown/injected/duplicate/malformed source, a missing expected source or another revision fails closed. Prowlarr remains first. AudioBookBay, LibriVox and The Pirate Bay audiobook operate with the pinned public registry configuration; BookTracker is present but disabled because its separate credentials/configuration are unavailable.

This is discovery trust, not an acquisition bypass. BigBrain retains opaque candidate IDs, expiry/single use, explicit edition review and confirmation, server-side credentials, dedicated qBittorrent category, durable import evidence, no-overwrite placement and Audiobookshelf reconciliation. Native direct-download references are URL-validated and may be displayed, but a source that cannot satisfy the existing hash-backed job lifecycle is rejected before provider request.

The deployed read-only comparison searched `The Wandering Inn`, author `pirateaba`, preferred language English. The three bounded variants remained `The Wandering Inn`, `The Wandering Inn pirateaba` and `Wandering Inn pirateaba`; the author reached every active source. Sanitized source-stream evidence reported 0 rows from Prowlarr, AudioBookBay, LibriVox and The Pirate Bay audiobook. Librarr retained 0 and BigBrain returned 0 candidates, so no titles, languages or provenances were available to rank. The native set did not improve discovery for this title; additional audiobook-oriented Prowlarr/indexer coverage remains the smallest next discovery improvement.

Four-source search showed variable latency (about 1.3 seconds for AudioBookBay/TPB, 14.4 seconds for LibriVox and 24.5 seconds for Prowlarr in the diagnostic stream). The combined owner-facing endpoint crossed the old 30-second budget on one observation, so the existing Librarr-specific bounded setting was raised to its already validated hard maximum of 45 seconds. Cancellation still propagates; status and other clients retain their shorter limits. After redeployment the combined endpoint completed with provider `configuredHealthy`, and 390 × 844 browser smoke passed all three themes without overflow or dock occlusion. No acquisition endpoint was called.
