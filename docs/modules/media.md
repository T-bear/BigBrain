# Media Module

## Ljudböcker (BB-100)

Audiobookshelf owns audiobook files, metadata and listening progress. BigBrain Web calls only the versioned BigBrain API; the API key remains server-side. The adapter implements bounded overview, paged library, detail, local search and a same-origin cover proxy. Missing credentials return `notConfigured`, and upstream failure returns a controlled unavailable state without affecting the existing Media stack.

Language identifiers are normalized (`sv`, `en`, `de`, `und`). Audio-edition language is not inferred from a translated work title; unknown stays explicit. Narrator and edition IDs remain distinct metadata. Acquisition is behind `IAudiobookAcquisitionProvider`; BB-100 registers the safe None provider and does not request or download anything.

## Anskaffningsflöde (BB-101)

BB-101 extends the existing boundary without selecting a third party. `IAudiobookAcquisitionProvider` truthfully reports status and capabilities and can support bounded search, explicit edition request, provider-job status and safe cancellation. The active implementation is still provider `None / NotConfigured`; library search continues to work, while request creation returns controlled Problem Details and persists no fake job.

BigBrain owns `AudiobookAcquisitionCandidate`, `AudiobookAcquisitionJob` and stable job IDs. Candidate metadata keeps title, author, narrator, normalized audio language, edition, source and confidence distinct. Jobs use the existing API SQLite persistence volume, are bounded in list APIs and never contain credentials. The Web UI defaults to Swedish, permits English/all-language selection, shows local and provider results separately and renders activity only for actual stored jobs.

## Librarr provider continuation (BB-102)

The owner approved a minimal BigBrain-maintained patch/image after upstream overwrite-on-destination behavior blocked deployment. Commit `1208254c20b31fbf217558c0fb987f779fed1cf8` is pinned; the isolated import patch reserves a new book directory, uses exclusive file creation and fails closed on existing/partial destinations and path escapes. The latest owner decision trusts every native audiobook discovery implementation in that exact revision as a set while retaining Prowlarr first/preferred. Image `1208254-bb5` binds the trusted IDs and local source registry to the immutable revision; revision mismatch, unknown/injected/duplicate IDs and source-set drift fail startup. A future upstream revision requires a fresh inventory before its new sources can run. Web receives opaque candidate IDs plus sanitized provenance, `CanCancel=false`, and weak release-name language hints remain `Probable` while missing language remains `und`. Provider search has a bounded 45-second Librarr-specific timeout for multi-source search and propagates cancellation.

## Usable acquisition lifecycle (BB-103)

BB-103 retains the provider-neutral job contract and upgrades the patched image to `bigbrain-librarr:1208254-bb3`. A failed torrent import now leaves a durable, sanitized `torrent_import_failed` event keyed by the provider job hash. BigBrain never treats disappearance from qBittorrent as completion. It requires an exact successful Librarr import event and an exact local library `source_id`, reports `indexing` while Audiobookshelf is still scanning, and marks `completed` only when the imported title/author is visible through Librarr's authenticated Audiobookshelf library adapter. Missing or ambiguous evidence remains nonterminal; an explicit failure becomes a safe failed state.

The discovery-quality revision `bigbrain-librarr:1208254-bb4` keeps the same contracts and safety boundaries. Title-only search remains one source query. Supplying an author adds only normalized, unique title-plus-author variants (maximum three total, including a leading-English-article-free variant), so the author reaches Prowlarr and approved native discovery rather than only influencing later scoring. Partial source success is retained and redundant audiobook suffixes are suppressed.

Revision `bigbrain-librarr:1208254-bb5` changes discovery trust only. The active native set is AudioBookBay, LibriVox and The Pirate Bay audiobook; BookTracker audiobook is trusted as pinned code but inactive without its own configuration. LibriVox/BookTracker results that cannot support the existing hash-backed provider job contract remain discovery-only and are rejected before request. Explicit edition confirmation, opaque candidate lifetime, qBittorrent isolation, durable import evidence, no-overwrite placement and Audiobookshelf completion reconciliation are unchanged.

## Universal metadata search (BB-104)

`IAudiobookMetadataProvider` is a BigBrain-owned metadata-only boundary. Its first adapter calls Open Library server-side with a five-second timeout, one request per owner search, no automatic retries, at most three works and bounded one-megabyte JSON. It validates ISBN-10/13 locally and classifies probable ASIN/free text honestly. Covers use a numeric-ID-only same-origin BigBrain proxy with a two-megabyte bound. Open Library never supplies acquisition URLs and Web never calls it directly.

The planner creates at most two normalized discovery seeds and the existing Librarr bb4 author-aware behavior expands those to at most six source queries. Canonical title plus author is preferred, followed by a distinct series or alternate title; unresolved input falls back to the literal query. Results merge on opaque provider edition identity so one release returned by multiple derived queries is collapsed without merging different narrators, languages or editions. Metadata match evidence ranks before the existing Swedish/English/unknown preference; English and unknown remain visible.

Open Library can supply canonical/alternate title, authors, identifiers, series where present, publication year, language and cover. It does not supply reliable narrator metadata or a complete ASIN crosswalk. The contract reserves narrator/ASIN fields, but the runtime declares narrator search unsupported and never fabricates them. A future narrator provider requires a separate owner decision.

## AudioBookBay parser remediation (BB-105)

Image `bigbrain-librarr:1208254-bb6` keeps the same immutable upstream revision and source policy. Its only Librarr behavior change preserves separators between adjacent `.postInfo` DOM nodes before evaluating the declared language. This fixes current markup where `English` and a nested `Keywords:` label were flattened into `EnglishKeywords:`. A sanitized fixture covers two retained English rows and one rejected non-English row. Debug diagnostics expose counts only—never query text, result titles, paths, URLs or download identifiers.

The BB-105 follow-up image `bigbrain-librarr:1208254-bb7` additionally normalizes only AudioBookBay's outgoing search parameter to lowercase. Runtime proved `Pirateaba` parsed nine source rows but retained zero while `pirateaba` retained all nine; direct source evidence confirmed case-dependent responses. BigBrain still preserves the owner's literal input, and Prowlarr plus all other sources are unchanged. All three Pirateaba casings and both Wandering Inn casings now return the same nine AudioBookBay candidates; all remain honestly `und`.

For author-only metadata resolution, the planner reserves the normalized literal owner input as the first of at most two provider seeds. One metadata-derived work may supplement it but cannot replace it. Swedish and English remain ranking preferences, unknown candidates remain visible, and All Languages applies no language preference. Acquisition, source trust, opaque candidates, confirmation, no-overwrite and completion reconciliation are unchanged.

## Audiobook navigation experiment (BB-108)

BB-108 tests an audiobook-local route hierarchy: the registry-composed Media overview contributes a compact progress-backed listening surface and a collection affordance; `/media/audiobooks` owns the bounded catalogue/search/acquisition surface; `/media/audiobooks/{id}` owns item detail. The routes use the existing application shell and browser History API, support refresh/deep links and preserve in-memory collection query/sort state on detail return. This is not a global navigation contract or accepted design-system rule.

The current Audiobookshelf overview supplies progress but no reviewed listening-recency field. BigBrain therefore labels only an actual 0–100% item as **Fortsätt lyssna** and never substitutes `addedAt` recency. Detail continues to open playback through the configured owner-reachable Audiobookshelf URL; undocumented embedded playback controls remain outside the contract. A successful upstream cover response remains user/library artwork even when its motif is generic. Only absent/failed cover responses invoke `BBMediaArtwork`'s BigBrain-B fallback.

The registry-composed Media view shows sanitized source provenance, decoded display metadata and Swedish labels for job states. Candidate cards open a release-confirmation surface; only that surface can submit the opaque candidate ID. Completion refreshes the existing Audiobookshelf overview, and an indexed item's detail surface keeps playback delegated to the configured Audiobookshelf owner URL. No percentage is shown because the current bounded job contract does not expose authoritative progress.

The first real import proved that provider release metadata can differ from Audiobookshelf's canonical metadata. Completion still starts from an exact provider job hash and exact local `source_id`; after that, reconciliation accepts either an exact title or exactly one normalized canonical Audiobookshelf title contained in the imported release title. `Unknown` is treated as absent author metadata. Multiple matching library items fail closed in `indexing`. A transient provider-status request is represented as `configuredUnavailable`, never `notConfigured`.

Endpoints:

- `GET /api/v1/modules/media/audiobooks/acquisition/provider-status`
- `POST /api/v1/modules/media/audiobooks/acquisition/search`
- `POST /api/v1/modules/media/audiobooks/acquisition/jobs`
- `GET /api/v1/modules/media/audiobooks/acquisition/jobs`
- `GET /api/v1/modules/media/audiobooks/acquisition/jobs/{id}`
- `POST /api/v1/modules/media/audiobooks/acquisition/jobs/{id}/cancel`

Future provider flow: discovery → explicit edition/language confirmation → approved provider → qBittorrent under provider control → validated relative output under `/srv/media/audiobooks` → ABS scan → BigBrain library. Web cannot provide filesystem paths or arbitrary URLs. Traversal and an existing import destination fail closed; existing books are never overwritten. Import movement and ABS rescan are intentionally not simulated before a real provider is approved.

## Sprint 4 – Media Experience

**Goal:** Improve the existing BigBrain 1.0 media search, request, status and mobile
experience without adding a new platform, client type or module architecture.

**Definition of Done:**

- Film, Serie and Båda select only the intended lookup providers.
- Movie and series results share normalized status, poster fallback and request UX.
- Optional service Web UI links expose no credentials or internal adapter URLs.
- Mobile users have stable Home, Search, Queue and Services navigation.
- Queue and service status polling pauses while the page is hidden and never overlaps.
- Provider errors have safe stable categories and Swedish UI messages.
- Backend/frontend tests and Release builds pass without breaking preview/confirm.

## Responsibility and boundary

The Media module provides normalized dashboard and search reads for Jellyfin, Sonarr,
Radarr, Prowlarr and qBittorrent. It also exposes a small set of purpose-built,
explicitly authorized write workflows such as Smart Shuffle, controlled Arr requests
and Download Control. Every external service remains behind an adapter; there is no
general proxy. Sentinel remains the exclusive boundary for operating-system resources,
Docker, processes and filesystems.

Each upstream API is treated as untrusted. Typed clients translate allowlisted fields into BigBrain contracts, apply short timeouts and bounded lists, accept cancellation, and convert service-specific failures into sanitized status results. One failing service does not fail the aggregate response.

The module does not mount the Docker socket or media directories, execute commands, access host inventory, log credentials, or expose upstream authentication material. qBittorrent 5.2.0+ is accessed statelessly with its official Bearer API key mechanism; the module does not call qBittorrent's login or logout endpoints.

## Configuration

.NET configuration uses the `Media` section. Environment variables use double underscores:

| Variable | Default | Secret |
|---|---|---|
| `MEDIA__JELLYFIN__BASEURL` | `http://jellyfin:8096` | No |
| `MEDIA__JELLYFIN__APIKEY` | unset | Yes |
| `MEDIA__JELLYFIN__USERID` | unset | Yes |
| `MEDIA__SMARTSHUFFLE__ENABLED` | `false` | No |
| `MEDIA__SONARR__BASEURL` | `http://sonarr:8989` | No |
| `MEDIA__SONARR__APIKEY` | unset | Yes |
| `MEDIA__RADARR__BASEURL` | `http://radarr:7878` | No |
| `MEDIA__RADARR__APIKEY` | unset | Yes |
| `MEDIA__PROWLARR__BASEURL` | `http://prowlarr:9696` | No |
| `MEDIA__PROWLARR__APIKEY` | unset | Yes |
| `MEDIA__QBITTORRENT__BASEURL` | `http://qbittorrent:8080` | No |
| `MEDIA__QBITTORRENT__APIKEY` | unset | Yes |
| `MEDIA__TIMEOUTSECONDS` | `3` | No |
| `MEDIA__REQUESTS__ENABLED` | `true` | No |
| `MEDIA__REQUESTS__DEFAULTSEARCHAFTERADD` | `false` | No |
| `MEDIA__REQUESTS__PREVIEWTOKENLIFETIMEMINUTES` | `5` | No |
| `MEDIA__REQUESTS__MAXIMUMCONCURRENTREQUESTS` | `1` | No |

URLs must be absolute HTTP or HTTPS URLs without query strings or fragments. Timeout must be between 1 and 15 seconds. Credentials are optional so an unconfigured service becomes `notConfigured`; real values must be supplied through runtime secret injection and must never be committed.

BigBrain API shares the existing external `bigbrain_default` application network so the verified service names resolve. The network name can be overridden with `MEDIA_DOCKER_NETWORK`. Compose does not own or manage the media services or their network. See [`TESTING.md`](../../TESTING.md) for setup and verification.

## Public API

### `GET /api/v1/modules/media`

Returns one aggregate `MediaOverview`:

- `status`: `online`, `degraded`, `unavailable` or `notConfigured`.
- `collectedAtUtc`.
- `services`: normalized status for all five services.
- `qBittorrent`: bounded torrent activity and transfer rates.
- `sonarr`: counts, queue, recent history and health warnings.
- `radarr`: counts, queue, recent history and health warnings.
- `prowlarr`: indexer summary, Sonarr/Radarr connections and health warnings.
- `jellyfin`: library/content counts and privacy-limited active-session count.

Each service status contains only:

- `serviceName`
- `status`
- `version`
- `responseTimeMs`
- `checkedAtUtc`
- `sanitizedMessage`
- `isConfigured`

The endpoint always returns normalized BigBrain DTOs. It never returns raw provider payloads, request headers, cookies, API keys, passwords, user names, IP addresses, device identifiers, local paths, download URLs or detailed viewing history.

The dashboard and library-search endpoints remain read-only. Mutations use separate,
versioned endpoints with their own authorization, validation and Problem Details contracts.

### `GET /api/v1/modules/media/search?query={query}`

Searches the existing Jellyfin, Sonarr and Radarr libraries in parallel. Queries are
trimmed and must contain at least two characters. Each provider returns at most ten
normalized results with allowlisted title, year, media type, state and typed media
statistics. Provider failures remain isolated and produce a sanitized per-provider
status while successful results remain visible.

Jellyfin uses its bounded text-search endpoint. Sonarr and Radarr search only their
registered series and movies.
Library-search poster URLs remain null. External lookup maps provider `images` entries
with `coverType=poster` and a permitted public HTTPS `remoteUrl` to a signed relative
BigBrain poster URL. The browser loads the image through BigBrain, not from Radarr,
Sonarr or the public artwork host directly. The proxy allows only known TMDB, TVDB and
Fanart hosts, follows no redirects, accepts bounded JPEG/PNG/WebP responses and never
forwards provider credentials. Internal URLs, local paths and raw provider errors are
never returned.

### External lookup and controlled requests

- `GET /api/v1/modules/media/lookup?query={query}&mediaType={series|movie|all}`
- `GET /api/v1/modules/media/service-links`
- `GET /api/v1/modules/media/add-options/series`
- `GET /api/v1/modules/media/add-options/movie`
- `POST /api/v1/modules/media/requests/preview`
- `POST /api/v1/modules/media/requests/confirm`

Lookup uses Sonarr's official series lookup and Radarr's official movie lookup. Stable
TVDB/TMDB identifiers are compared with registered provider libraries before a result
can be requested. Root folders and quality profiles are returned as opaque IDs; full
provider paths never cross the API boundary.

`series` selects only Sonarr, `movie` selects only Radarr and `all` selects both
concurrently. Omitting `mediaType` retains the backward-compatible `all` behavior.
Invalid values return Problem Details with `code: invalidMediaType`.

Lookup results retain the earlier request fields and add normalized `providerId`,
`posterUrl`, `monitored`, `canRequest`, `requestState`, `errorCode` and
`errorMessage`. Provider failures use stable safe categories such as `timeout`,
`authenticationFailure`, `providerUnavailable` and `unknownError`.

The service-links endpoint returns only `id`, `displayName`, `url` and `enabled`.
Browser-facing links are configured separately under `Media:ServiceLinks`; adapter
base URLs and authentication material are never returned.

Preview is non-mutating and returns a random, five-minute opaque token. Confirm
revalidates lookup identity, duplicate state and current provider options before the
only permitted external writes: `POST /api/v3/series` or `POST /api/v3/movie`.
`searchAfterAdd` is passed only through the add payload and defaults to false.

Preview and idempotency state are held by a locked DI singleton for the current
single API instance. Restart invalidates outstanding previews safely. Multiple API
replicas require a shared durable request store before this feature can be enabled
across replicas.

### Intelligent Media Manager

- `GET /api/v1/modules/media/jobs`
- `GET /api/v1/modules/media/jobs/{opaqueJobId}`
- `GET /api/v1/modules/media/jobs/events`
- `GET /api/v1/modules/media/library-status?provider={provider}&foreignId={id}&mediaType={series|movie}`
- `GET /api/v1/modules/media/play/{jellyfinItemId}`

Media Jobs is a separate read-only application contract inside the Media module.
Sonarr, Radarr and qBittorrent records are normalized to `requested`, `searching`,
`queued`, `downloading`, `stalled`, `completed`, `importing`, `available`,
`failed` or the safe fallback `unknown`. The list supports bounded `status`,
`mediaType`, `provider`, `includeCompleted` and `limit` filters. Details use a
deterministic opaque job identifier and contain only normalized provider data.

Arr episodes are grouped by stable foreign ID and season. qBittorrent records are
attached to an Arr group only when a deterministic normalized title-and-season key
matches; this fallback affects presentation only and is never used to authorize
playback. Raw provider payloads, tracker data and magnet links are not returned.

A stable TVDB/TMDB match in Jellyfin transitions a movie or series item to
`available`. A series-level Jellyfin match deliberately does not make an
unverified season playable. Recently added Jellyfin movies and series are included
as bounded available results.

The events endpoint remains available for compatibility. Sprint 4's React client uses
simple polling: Media Jobs refreshes about every 12 seconds and media service status
about every 45 seconds while the page is visible. Polling pauses while hidden,
refreshes when visibility returns and prevents overlapping requests. Provider reads
share a three-second, process-local snapshot cache, run in parallel, propagate
cancellation and isolate provider failures.

The play endpoint returns metadata only. `playUrl` is a relative browser path and
never contains a provider hostname, port, API key or token. `artwork` remains null
until a bounded BigBrain image proxy is available. BigBrain does not redirect,
autoplay or stream media.

The normalized `IMediaLibraryCatalog` boundary is independent from dashboard
components and Arr queue payloads. A future Smart Queue can build on this catalog
without depending on the Media Jobs presentation model.

### Sprint 4 limitations

- Posters depend on a safe public HTTPS image supplied by a provider and supported by
  the bounded BigBrain proxy; otherwise the UI shows a placeholder. Poster tokens are
  process-local and old search results should be refreshed after an API restart.
- Service Web UI links are optional and disabled until explicitly configured.
- Mobile navigation targets stable sections in the existing hash view. It is not a
  TV interface or a general multi-client platform.
- Polling is intentionally used instead of adding a new real-time transport.

## Upstream read contract

| Service | Endpoints used |
|---|---|
| Jellyfin | Existing endpoints plus read-only `/Items`, `/Items/Latest` and `/Items/{id}` metadata |
| Sonarr | Existing endpoints plus read-only `/queue`; controlled `POST /series` only |
| Radarr | Existing endpoints plus read-only `/queue`; controlled `POST /movie` only |
| Prowlarr | `GET /api/v1/system/status`, `/indexer`, `/health`, `/applications` |
| qBittorrent | `GET /api/v2/app/version`, `/torrents/info`, `/transfer/info` with the existing server-side credential |

Queue and torrent lists are limited to 25 entries, health/indexer output to 25 entries, application connections to 10 and history to 10. qBittorrent summary counts in Sprint 1 describe the bounded recent list returned by the upstream query; an exact, safely bounded global-count strategy remains technical debt.

## Failure model

- Missing credentials: `notConfigured`.
- Successful normalized response: `online`.
- Rejected authentication or malformed provider response: `degraded`.
- Timeout, DNS failure, connection refusal or other transport failure: `unavailable`.
- A mixture of service outcomes: aggregate `degraded`, while successful service data remains available.
- All services unconfigured: aggregate `notConfigured`.

Messages are fixed, sanitized text and never include raw exception messages, URLs or credentials. No automatic retry loop is configured.

qBittorrent API key authentication requires qBittorrent 5.2.0+ or WebAPI 2.14.1+. The key is valid for the WebAPI endpoints above but not for static WebUI assets or the authentication endpoints. See the [official qBittorrent API key contract](https://github.com/qbittorrent/qBittorrent/wiki/API-Key-Authentication-%28%E2%89%A5v5.2.0%29).

## Deferred write scope

The following are explicitly deferred and require separate authorization, audit, confirmation and capability design:

- Search for missing episodes or movies.
- Pause, resume or delete a torrent.
- General redirect, autoplay or streaming outside the narrowly scoped Smart Shuffle
  PlayNow capability.
- AI commands for media management.

No rename, move, edit, release or general download-client command exists. The separately
bounded Download Control capability below is the sole qBittorrent delete boundary.

## Download Control MVP

The Media dashboard exposes `/api/v1/modules/media/downloads` plus opaque detail,
`remove-preview` and `remove` endpoints. qBittorrent 5.2.3/Web API 2.15.1 is normalized to
safe display fields; hashes, content/save paths, credentials and upstream bodies remain
server-side. Random process-local IDs expire after five minutes. Preview confirmations
expire after two minutes and bind exactly one live fingerprint and explicit `deleteData`
choice.

The default action sends one internally resolved hash to `POST /api/v2/torrents/delete`
with `deleteFiles=false`. Destructive `deleteFiles=true` is separately presented and
requires an active acknowledgement. It is blocked for completed/import-uncertain jobs,
empty or shared content paths, root-like path scope or changed identity. Both paths
re-read the queue immediately before mutation, serialize token use and provide idempotent
missing/completed results. Safe structured audit excludes raw identities.

Sonarr/Radarr category ownership produces a warning only. Download Control does not alter
Arr history, blocklist, searches, monitored state, media or client configuration. See
[Proposed ADR 0013](../adr/0013-safe-qbittorrent-download-removal-boundary.md) and the
[safe removal runbook](../operations/runbooks/download-control-safe-removal.md).

The MVP is deployed. The user has confirmed one UI-driven file-preserving removal from
qBittorrent. Destructive removal remains conservatively available only through the
documented risk gates and is not claimed as fully production-verified.

The Sprint 1 Web fix constrains Download Control headers, messages, progress, cards and
long names to the widget's available width. Mobile uses a single-column card layout;
tablet and desktop retain the bounded two-column action layout. This presentation fix
does not change the removal capability or provider contract and is automatically tested,
production-built, deployed and manually viewport-verified.

Sprint 3 keeps all filters, selection, partial batch commands, row commands, diagnostics
and safe removal above a status-grouped presentation. The default order is problems,
active, queued/paused and completed. Completed history is collapsed in the all-status
view and remains directly reachable through its disclosure control or the completed
filter. The control explains that it manages the download itself; the separate
**Medieflöde** explains the larger path through search, download, processing and the
library, including their intentional overlap during download. This is frontend-only,
automatically verified, production-built, Web-deployed and technically accepted without
a known blocking regression. Sprint 3 is closed. Extended qualitative UX evaluation
during real use is intentionally deferred to BB-041; BB-040 and BB-033 retain their
remaining evidence requirements independently of sprint status.

### Sprint 2 command capabilities

Download Control now exposes server-owned single-target and bounded partial-batch
capabilities for pause, resume and retry. Every opaque target is resolved and revalidated
against a fresh live queue immediately before one adapter mutation. Batch manifests
contain 1–25 explicit IDs; there is no implicit all operation. qBittorrent 5.2.3 uses
`stop`, `start` and `reannounce`. Retry never searches, removes or mutates Arr.

Each normalized download includes eligibility flags and deterministic read-only
diagnostics from allowlisted state, queue position, speed and connected peer/seeder
counts. Unknown causes remain unknown. The UI calls Download Control **Nedladdningskö**
and the cross-provider lifecycle view **Medieflöde**. Batch delete is deferred because
the destructive preview contract remains single-target. Sprint 2 was deployed and
accepted by the product owner on 2026-08-10. Pause/resume, batch handling and diagnostics
were manually approved. Retry remains implemented and automatically verified but awaits
manual verification because no naturally failing download was available; this is not a
known defect or a Sprint 2 blocker.

## Smart Shuffle MVP

Smart Shuffle is an opt-in Media capability controlled by `Media:SmartShuffle:Enabled`
and runtime-only `Media:Jellyfin:UserId`. It exposes versioned options, devices and
session actions under `/api/v1/modules/media/smart-shuffle`. A user selects at least two
series and one connected remote-control client, then explicitly starts the session.
Selection uses weighted randomness, immediate anti-repeat, least-recently-selected
weighting and a starvation threshold of twice the active candidate count.

For the configured Jellyfin user, episodes are ordered by season and episode number.
Season 0, played and non-playable episodes are excluded. Saved playback position is used
for resume. The browser receives opaque device/session identifiers; raw Jellyfin session
and user identities remain server-side. Stop ends automation only and does not stop the
episode currently playing on the TV.

State is process-local and limited to one active session. Restarting the API loses active
automation state and does not stop outstanding playback. The background coordinator
polls only active sessions and serializes transitions to prevent duplicate starts. See
[Proposed ADR 0011](../adr/0011-smart-shuffle-jellyfin-remote-playback-boundary.md).

Candidate episode checks run in parallel under a bounded Smart Shuffle-specific request
timeout. Immediately before playback, the backend revalidates the live session, configured
user and remote-control capability, then sends the server-selected episode using Jellyfin
10.11.11's `POST /Sessions/{sessionId}/Playing` contract. Accepted commands transition
through `awaitingPlaybackConfirmation`; bounded confirmation polling promotes an exact
`NowPlayingItem` match to `active` without retrying the write command. The frontend blocks
overlapping starts and maps only stable, sanitized error categories to Swedish messages.

Explicit start and user-driven skip have been end-to-end verified on the selected Samsung
Tizen TV. Jellyfin accepted each command and the expected episode became `NowPlayingItem`.
Natural completion transition remains tracked as BB-014; no terminal or automated test run
starts real playback.

## BB-106 audiobook browsing and interaction quality

The normal audiobook surface no longer exposes Librarr or metadata-pipeline explanations. It keeps
Continue Listening prominent, pages/searches/sorts the Audiobookshelf library in bounded groups and
separates active work, attention and collapsed historical acquisition evidence. History cleanup is
presentation-only: durable lifecycle evidence, provider jobs and media files are never deleted to make
the list shorter. Missing covers use the shared BigBrain media placeholder and all remote images retain
their existing same-origin proxy boundaries.

BigBrain currently owns library browsing, cover, metadata, detail, progress, continue-listening and the
play/open action. Full embedded pause/resume, seeking and chapter navigation remain **PARTIAL**: the
reviewed adapter does not yet expose an authenticated Audiobookshelf playback-session/streaming contract.
Audiobookshelf 2.36.0 has internal start/sync/close and track routes, but these are not included in its
official OpenAPI specification. Native BigBrain pause/seek/chapter playback therefore remains partial
until a dedicated slice defines a same-origin Range proxy and a tested session/progress lifecycle rather
than binding the product to undocumented routes.
The supported action therefore opens the configured owner-reachable player; no ABS key enters Web.
Adding such a contract requires a bounded server-side adapter, stream/range and progress-sync review,
not a frontend call to Audiobookshelf.

## BB-107 owner UX remediation

Media request confirmation uses one stable Web idempotency key for the complete owner action. Immediately
before the single Arr POST, API records that the write was attempted. A timeout or server-side response
failure triggers a bounded read-after-write lookup using the exact TVDB/TMDB identity; if the item exists,
the original action completes as `created`. A retry of that preview returns the same result and never
mislabels BigBrain's own successful write as a pre-existing title. Provider 4xx/auth failures remain
controlled failures and are not treated as ambiguous success.

The registry-composed Media page now follows **overview → collection → detail** for Audiobookshelf.
Continue Listening remains prominent; the default library overview renders at most four recent covers
and a count. **Alla ljudböcker** opens the existing bounded search, sort and pagination controls. Missing
artwork uses the shared theme-aware BigBrain B cover in overview, collection, search, detail and continue
listening. Audiobookshelf remains the library/playback engine and the existing owner-reachable play link
is unchanged.

## BB-109 owner UX remediation and playback boundary

BB-108's bounded `overview → collection → detail` remains audiobook-local and has positive owner evidence, but is not a global navigation standard. Media uses the standard tertiary **Bibliotek** affordance without a dominant inventory count. The collection separates new provider discovery from local library filtering, keeps its 24-item bounded page, uses a dock-safe reduced-motion scroll-to-top utility and separates active jobs, attention and terminal history. Hiding visible terminal history is device-local presentation state only; durable job/audit data and media remain intact.

Audiobookshelf progress is user-specific. Runtime evidence on 2.36.0 shows no progress/session rows for the restricted BigBrain service identity and three active rows for another library identity, so the adapter cannot truthfully show the owner's Continue Listening state with its current credential. The installed start, ephemeral session-track, sync and close routes establish that native playback is technically possible, but do not decide which identity owns playback/progress. BigBrain therefore keeps the owner link as an explicitly temporary fallback and must not expose an Audiobookshelf token or start a zero-progress session under the wrong identity. A future approved slice must choose the canonical playback identity and define a same-origin Range/session/progress-sync adapter boundary.

## BB-110 audiobook UX consolidation and playback identity gate

Audiobook-samlingen använder lokalt experimentkontraktet overview → collection → detail. Collection ordnas discovery → owned library → downloads; synlig copy är reducerad men ny discovery och lokal filterinput behåller skilda accessible names. En bokrad är en semantisk navigation target över hela raden och katalogen förblir bounded 24 poster. Terminala failed-rader kan döljas via ett max 200-ID device-local presentationstillstånd; BigBrain-jobb, audit, provider och media ändras inte.

`BBButton` secondary äger **Bibliotek**-affordancens geometri/tillstånd; den tidigare lokala guldtexten/förstorade chevronen var inte ett nytt variantkontrakt och är borttagen. Detta whole-row- och informationsdensitetsbeteende är fortsatt audiobook-lokalt och inväntar owner UX review.

Owner direction är service identity för katalog/integration och per-authenticated-user playback identity för progress/session. Någon sådan godkänd identitetsmappning eller credential lifecycle finns ännu inte i BigBrains authkontrakt. Native same-origin stream/Range/session/progress-sync, mini-player och korrekt Continue Listening är därför blockerade tills owner/systemarkitekt beslutar denna varaktiga säkerhetsgräns; servicekontot ersätts inte och token lämnar aldrig API-sidan.

Acquisition reconciliation treats provider absence as a bounded ambiguity: a missing provider job retains its active state for five minutes to allow registration, then fails closed into attention rather than remaining `downloading` forever. This does not cancel, delete or restart provider work.

## BB-111 route/detail remediation and credential result

Audiobook route focus is still programmatic for assistive continuity, but its visual treatment now follows input modality: pointer/touch route entry suppresses only the programmatic heading ring, while keyboard route focus remains visible through the shared focus contract. Focus uses `preventScroll`. A forward History entry owns `scrollY=0`; popstate restores the saved scroll on the prior collection entry. This preserves collection return without making every route globally scroll to top.

The overview's **Bibliotek** affordance is an audiobook-local semantic link/navigation row, not a button variant. Detail uses a token-based two-column summary with a 2:3, `height:auto`, `object-fit:cover` artwork contract; mobile collection/detail sizes are locally compact and no global artwork primitive was resized. Optional `und`/unknown language is omitted from normal book/detail presentation.

The existing server-side Audiobookshelf key was rechecked without publishing identity or credential material. It acts for an active restricted non-root integration user with zero progress and zero listening sessions, so it is not the owner's authoritative playback identity. Audiobookshelf's per-user session/progress semantics mean a separate server-side playback key for the correct user is required. Until configured and sanitized permission/identity verification succeeds, BigBrain must not start a session, proxy Range bytes, expose a token, borrow progress, or implement a player/mini-player. No new ADR is created for a boundary that has not yet been safely implemented.

## ADR impact

## BB-112 native audiobook playback

The separate `Media:Audiobookshelf:PlaybackApiKey` acts only for owner playback/session/progress; `ApiKey` remains catalogue/integration identity. Purpose-built availability, item start, session-bound Range, sync and close endpoints use opaque process-local sessions, expire after 120 minutes by default, bind one item/track allowlist, cap one byte range to 8 MiB and cannot accept an upstream URL. Audiobookshelf remains durable progress truth; Web receives no credential and BigBrain adds no progress database.

AppShell owns audio/session lifetime. Continue Listening uses authoritative items-in-progress; player/detail provide play/pause, seek, ±30 seconds, actual time/duration and close. Chapters/speed are deferred. Discovery language is preference, not strict exclusion: explicit language is verified, strong release tokens probable and unknown visible. Stable provider release identity/infohash/guid drives deduplication. ADR 0037 records the boundary and current single-owner auth limitation.

## BB-113 owner-review remediation

Continue Listening exposes compact direct play/pause without changing the AppShell playback lifetime; its separate identity area retains detail navigation. Current/total time is shown only when the existing authoritative Audiobookshelf duration and progress are both present. Detail explicitly verifies the playback availability endpoint: healthy native playback is primary, while the external Audiobookshelf link is labelled as a secondary reserve path; an unavailable identity produces a bounded explanation and promotes only the fallback.

The detail hero owns one explicit artwork/summary grid with `minmax(0,1fr)` and natural title wrapping. At very narrow width it stacks and gives metadata the full page width. Successful Audiobookshelf covers remain owner artwork by default. The one physical-owner-observed generic filmstrip/music-note JPEG was traced to Audiobookshelf and is recognized only by its exact verified SHA-256; that response becomes missing artwork so the established BigBrain-B placeholder renders. No external artwork source or metadata/media mutation is introduced.

The read-only dashboard and controlled Arr request decisions remain unchanged. Smart
Shuffle's new Jellyfin write boundary is documented separately in Proposed ADR 0011.
