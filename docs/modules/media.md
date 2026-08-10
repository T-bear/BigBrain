# Media Module

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

## ADR impact

The read-only dashboard and controlled Arr request decisions remain unchanged. Smart
Shuffle's new Jellyfin write boundary is documented separately in Proposed ADR 0011.
