# BB-102 — Librarr Provider Security Gate

## Metadata

- Date: 2026-08-23
- Baseline: `8bc49198ef06a332d825c8498b0144e98a8bb660`
- Scope: mandatory upstream, compatibility and security gate for the owner-selected Librarr provider
- Sanitization notice: no keys, private addresses, tracker credentials, magnet links, raw runtime payloads or private library data are published.

## Status

**IMPORT AND SOURCE-POLICY GATES REMEDIATED; FIRST ACQUISITION OWNER-GATED.** The owner approved the minimal BigBrain-controlled import patch and later superseded Prowlarr-only with Prowlarr preferred plus explicitly allowlisted native sources. Both invariants are automatically and runtime verified.

## Scope and decision

The owner approved [JeremiahM37/librarr](https://github.com/JeremiahM37/librarr) as the first candidate implementation behind BigBrain's existing `IAudiobookAcquisitionProvider`. This report records the mandatory upstream and security gate. No provider implementation, Compose service, secret, runtime setting or acquisition was created.

The reviewed stable release was [v1.2.0](https://github.com/JeremiahM37/librarr/releases/tag/v1.2.0), commit `df1fc5a6951f693a492a38270c261cb863308fc4`. Upstream remained active after that tag, including a version bump and import-mode work on `main`, but those later changes did not remove destination overwrite behavior.

## Evidence

- A non-root multi-stage container image is defined upstream; Docker socket and privileged mode are not required.
- Librarr has API-key authentication and integrations for Prowlarr, qBittorrent and Audiobookshelf scanning.
- qBittorrent supports a dedicated audiobook category and separate incoming path.
- Torrent path mapping constrains reported paths beneath configured roots; later upstream code adds stronger destination containment and symlink-prefix handling.
- Runtime states exist for queued/searching/downloading/importing/completed/error-style workflows.

## Blocking finding: overwrite semantics

Stable `v1.2.0` derives the final audiobook destination as `{AUDIOBOOK_DIR}/{sanitized author}/{sanitized title}`. Its file placement first uses `os.Rename`; the fallback writes the destination with truncation. Directory imports walk into an already-created destination tree. There is no fail-closed preflight when the destination or a contained file already exists.

The current upstream `main` adds move/hardlink/copy modes and better containment, but explicitly preserves replacement behavior for existing destinations. This violates BB-101/102's server-controlled import policy: an existing audiobook must cause conflict/needs-attention, never replacement. Since Librarr itself owns the move into the mounted final library, BigBrain cannot enforce this safely from the provider API boundary.

Deployment therefore stopped before pulling a production image, mounting `/audiobooks`, creating a container or installing credentials.

## Changes

No application, Compose or runtime code was changed. Repository changes are limited to this sanitized decision record plus status, backlog, module and report-catalog references. The deployed provider remains `None / NotConfigured`.

## Security

- With default configuration Librarr loads an external source registry. BigBrain requires Prowlarr to remain the canonical indexer hub, so a future deployment must supply a reviewed empty/local registry rather than silently querying additional sources.
- The audiobook search response preserves individual releases and source details but normally lacks authoritative language, narrator and edition fields. BigBrain may retain deterministic release-title hints only as `Probable`; unknown stays `und` and must remain selectable rather than auto-acquired.
- Librarr exposes torrent/job-specific removal endpoints rather than a provider-neutral audiobook cancel contract. BigBrain must advertise `CanCancel=false` until exact partial-file and client semantics are verified.
- A valid Librarr API key receives administrative API authority. Any future instance must remain internal-only, use a dedicated revocable key, and never expose the key or raw magnet/download URLs to Web.
- Search results include sensitive provider URLs. A future adapter needs a bounded server-side candidate cache so Web receives only opaque BigBrain candidate IDs.

Detta är en sanerad GitHub-version. No credential, private endpoint, tracker identity, source URL, magnet URI or production library item is included.

## Owner decision and continuation

On 2026-08-23 the owner explicitly approved a narrowly scoped BigBrain-maintained image patch whose only purpose is fail-closed import conflict handling. Upstream is now pinned to immutable commit `1208254c20b31fbf217558c0fb987f779fed1cf8`. The patch is independently reviewable at `infrastructure/librarr/patches/0001-audiobook-import-no-overwrite.patch`; build and maintenance instructions are in `infrastructure/librarr/README.md`.

The patched organizer atomically reserves a new final book directory, uses exclusive destination-file creation, refuses existing or partially populated book destinations, rejects unsafe author/title components and source symlinks, and preserves source data when placement fails. Move deletes the source only after all destination writes and syncs succeed; copy and hardlink never replace. The image build executes all 59 organizer tests, including 11 new patch regression tests.

The focused security re-review found no Docker socket, privileged mode, host port or arbitrary Web-controlled path. At this stage the local empty registry was believed to enforce Prowlarr-only discovery; runtime commissioning later disproved that assumption. BigBrain retains opaque candidate identifiers and server-only provider payloads. Cancellation remains disabled because upstream deletion semantics are not proven safe.

BigBrain's provider adapter and Compose service were implemented and locally tested. At the original 2026-08-23 stop, runtime deployment was blocked until the appliance owner installed the three required secret values. The 2026-08-24 commissioning section below supersedes that historical credential state. No secret value was read or published and no download was requested.

## Automated verification

- Patched Librarr image build: passed; all 59 organizer tests passed, including 11 patch regressions.
- Focused BigBrain audiobook/provider tests: 21 passed.
- Complete API suite: 528 passed.
- Complete Web suite: 126 passed.
- Sentinel regression suite: 32 passed.
- Release solution build and Vite production build: passed with zero .NET warnings/errors.
- Compose render, documentation verifier (183 Markdown files / 89 unique backlog IDs) and `git diff --check`: passed.
- A redacted gitleaks scan found only the repository's pre-existing documented Finance threat-model example; no BB-102 secret was found.
- Missing-credential entrypoint test: image refused startup with exit code 78 and did not print variable values.

CI, deployment, downstream health, real provider search and browser QA are not claimed: they require publication and then the three local runtime credentials. The first real download remains explicitly prohibited until the owner selects a candidate.

## Commissioning evidence — 2026-08-24

The three required environment variables were present without their values being read or printed. `/srv/media/audiobooks-incoming` was created with appliance ownership, the pinned image rebuilt, and only Librarr started. The new Docker named volume required a one-time UID/GID 1000 ownership initialization; afterward container health and authenticated admin health were green. Librarr independently reported Prowlarr, qBittorrent and Audiobookshelf as `ok`.

BigBrain API/Web were recreated only after that health evidence. Provider status and audiobook overview both reported `configuredHealthy`, and Audiobookshelf remained healthy with an empty library. A real Swedish-preference search was then attempted. No acquisition job was created.

The search exposed a second upstream boundary issue: commit `1208254` always constructs AudioBookBay and other built-in source drivers in `cmd/librarr/main.go`. The empty local registry prevents external registry loading but does not disable those drivers; runtime logs proved attempted non-Prowlarr access. BigBrain filters returned candidates to `prowlarr_audiobooks`, but outbound requests still violate the approved Prowlarr-only architecture. The search also completed just after BigBrain's ten-second provider timeout, so no candidates were accepted or displayed.

The owner approval for the maintained patch was explicitly limited to import conflict safety. Librarr was therefore stopped rather than broadening the patch. Its volume and both media paths were preserved. A small BigBrain resilience fix was added and tested so stopped/unreachable Librarr yields `configuredUnavailable` without breaking Audiobookshelf; the deployed overview again reports Audiobookshelf `configuredHealthy`. Acquisition jobs remain zero.

Continuation now requires explicit owner approval for a second independently reviewable patch limited to making source registration configurable/Prowlarr-only, plus a bounded search-timeout adjustment. This is not authorization for other Librarr behavior changes, and the first download remains owner-gated.

The controlled-degradation fix passed 22 focused audiobook/provider tests and the complete 529-test API suite plus a zero-warning Release build. The unavailable-state Web refinement passed 6 focused tests and its production build. Deployed browser QA at both 390×844 and 430×932 passed for Obsidian Gold, Forest Night and Arctic Wind: Audiobookshelf remained connected, Swedish remained selected, local empty search worked, no fake progress or horizontal overflow appeared, and the last action cleared the dock. Obsidian Gold was restored afterward. This evidence validates the safe blocked state, not a commissioned acquisition provider or real candidate list.

## Remaining work

The acquisition pipeline is ready for its first owner-gated release. The owner must search in BigBrain, inspect language/source/edition evidence and explicitly choose **Lägg till**. Current result metadata does not verify Swedish or narrator, so the first choice requires human inspection. No automatic first acquisition is authorized.

## Owner source-policy decision and resumed commissioning — 2026-08-24

The owner revoked Prowlarr-only after reviewing the prior stop. The approved architecture is now Prowlarr preferred plus explicitly allowlisted Librarr-native audiobook sources. Native sources are not implicitly trusted: current and future upstream registrations are denied unless their exact identifier is configured.

Pinned source inventory found Anna's Archive, AudioBookBay, Gutenberg, Open Library, Standard Ebooks, Librivox, MangaDex, Nyaa Manga, Anna's Manga, Web Novels, Flibusta, Z-Library, ThePirateBay ebook/audiobook and BookTracker ebook/audiobook implementations. Only AudioBookBay was approved: it is audiobook-specific, unauthenticated, torrent-based, needs no privilege/public bind and remains behind owner-gated Add. Librivox was disabled because it is a direct-download path outside the commissioned qBittorrent flow. ThePirateBay and BookTracker audiobook modes were disabled pending separate source/security approval. Every non-audiobook implementation was disabled as out of scope.

| Identifier / display | Mechanism and auth | Metadata in pinned adapter | Policy / security result |
| --- | --- | --- | --- |
| `audiobookbay` / AudioBookBay | HTTPS scrape, torrent/magnet resolution; no credential | release title and source; no authoritative narrator/edition/language; distinguishable detail path | **ENABLED**; audiobook-specific, server-side public egress, no new privilege or bind |
| `librivox` / Librivox | public JSON feed, direct ZIP; no credential | title, author, duration-like text and cover; no mapped narrator/language/edition | **DISABLED**; safe-looking public source but bypasses the reviewed qBittorrent/import flow |
| `tpb_audiobook` / ThePirateBay | public API, torrent/magnet; no credential in adapter | release name, size, peers and hash; no narrator/language/edition | **DISABLED**; separate source/reliability review required |
| `booktracker_audiobook` / BookTracker | authenticated forum scrape, torrent; username/password and cookie session | release name, author, format, size and peers; no authoritative language/narrator/edition | **REJECTED for this slice**; would add unapproved source credentials and session handling |
| `annas` / Anna's Archive | HTML/metadata scrape plus direct download; no configured account | author, language, publisher and year for ebooks | **DISABLED**; main/ebook scope, not audiobook acquisition |
| `gutenberg` / Project Gutenberg | public JSON/direct download; no credential | title, author, language and format | **DISABLED**; ebook scope |
| `openlibrary` / Open Library | public JSON/direct link; no credential | title/author/cover; limited edition data | **DISABLED**; ebook scope |
| `standardebooks` / Standard Ebooks | public HTML/direct download; no credential | title/author/format | **DISABLED**; ebook scope |
| `mangadex` / MangaDex | public API/direct download; no credential | manga title/author/language-like feed metadata | **DISABLED**; manga scope |
| `nyaa_manga` / Nyaa | public feed, torrent/magnet; no credential | release/size/peers/source; no audiobook metadata | **DISABLED**; manga scope |
| `annas_manga` / Anna's Archive (Manga) | scrape/direct download; no configured account | manga release metadata | **DISABLED**; manga scope |
| `webnovel` / Web Novels | multi-site HTML scrape/direct download; no credential | title/author/site, no audiobook metadata | **DISABLED**; web-novel scope and broad outbound surface |
| `flibusta` / Flibusta | configured endpoint/direct download; no adapter credential | title/author/format | **DISABLED**; ebook scope and unapproved endpoint |
| `zlibrary` / Z-Library | authenticated RPC/direct download; email/password/session | title/author/file metadata | **REJECTED for this slice**; unrelated ebook scope plus new credentials |
| `tpb` / ThePirateBay | public API, torrent/magnet | ebook release/size/peers/hash | **DISABLED**; ebook scope |
| `booktracker` / BookTracker | authenticated forum scrape, torrent | ebook release/author/format/size/peers | **REJECTED for this slice**; unrelated scope plus new credentials |

Image `bigbrain-librarr:1208254-bb2` retains the unchanged import-safety patch and adds an exact source-policy patch. Empty, duplicate or unavailable identifiers fail startup; selection preserves Prowlarr-first construction order; future sources are excluded. The local registry contains only the pinned upstream AudioBookBay public mirror/tracker metadata. BigBrain accepts only Prowlarr and AudioBookBay results, stores raw release references only in its bounded server-side cache, exposes sanitized provenance, deduplicates safely by exact info hash where available and never merges merely similar titles/narrators/editions.

The previous approximately 10-second HTTP timeout was below the observed Librarr multi-source latency. A Librarr-specific bounded 30-second timeout is deployed; caller cancellation propagates. Librarr itself runs sources concurrently and returns partial useful results when another source returns no results or fails without exhausting the parent context.

Runtime commissioning loaded exactly `prowlarr_audiobooks` and `audiobookbay`. Authenticated health reported Librarr plus Prowlarr, qBittorrent and Audiobookshelf `ok`; BigBrain reported provider `configuredHealthy`, search/request enabled and cancel disabled. Read-only searches for `Harry Potter` and `The Martian` completed in roughly 17–20 seconds. The representative result set contained 11 Prowlarr candidates and zero AudioBookBay candidates; a second query returned 10 Prowlarr and zero AudioBookBay. AudioBookBay recorded successful searches but no matching parsed rows, proving partial-source behavior while providing no measured coverage improvement for these samples. Prowlarr results had release/size edition text but no authoritative narrator or language metadata: all remained `und/unknown`, and Swedish preference therefore did not falsely relabel them. No acquisition job, Add request or qBittorrent download was created.

The final local regression passed 533 API, 127 Web and 32 Sentinel tests, a zero-warning Release solution build, Vite production build, Compose validation, documentation verification (183 Markdown files / 89 unique backlog IDs), staged-diff secret scan and whitespace check. Browser QA rendered real candidate results at 390×844 and 430×932 in Obsidian Gold, Forest Night and Arctic Wind: Swedish remained selected, Prowlarr provenance and edition text were visible, Add was enabled but untouched, activity remained absent, and every state had no horizontal overflow or dock occlusion. Obsidian Gold was restored. One repeated external search temporarily exceeded the browser-QA wait window; a subsequent isolated run passed with ten real candidates while provider/source health remained green. This is a bounded upstream-latency limitation, not fabricated success.

## Preserved state

- BB-100 Audiobookshelf commissioning: unchanged.
- BB-101 provider-neutral contracts and job store: unchanged; Librarr is active behind the same boundary.
- Existing Prowlarr, qBittorrent and Audiobookshelf services: unchanged.
- Finance: no code or runtime change.
- Sentinel: no code or runtime change.
- BB-099: **TECHNICALLY COMPLETE / OWNER UX REVIEW PENDING**.

## Resumption

Owner review may now initiate the first acquisition from the deployed BigBrain candidate list. Preserve the provider-neutral BB-101 boundary and do not initiate a release without explicit owner selection.
