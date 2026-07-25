# Media Module – Sprint 2.3

## Responsibility and boundary

The Media module provides a read-only, normalized dashboard view of Jellyfin, Sonarr, Radarr, Prowlarr and qBittorrent. These are application services, so their documented HTTP APIs are accessed through Control Plane adapters. Sentinel remains the exclusive boundary for operating-system resources, Docker, processes and filesystems; the Media module does not use those interfaces.

Each upstream API is treated as untrusted. Typed clients translate allowlisted fields into BigBrain contracts, apply short timeouts and bounded lists, accept cancellation, and convert service-specific failures into sanitized status results. One failing service does not fail the aggregate response.

The module does not mount the Docker socket or media directories, execute commands, access host inventory, log credentials, or expose upstream authentication material. qBittorrent 5.2.0+ is accessed statelessly with its official Bearer API key mechanism; the module does not call qBittorrent's login or logout endpoints.

## Configuration

.NET configuration uses the `Media` section. Environment variables use double underscores:

| Variable | Default | Secret |
|---|---|---|
| `MEDIA__JELLYFIN__BASEURL` | `http://jellyfin:8096` | No |
| `MEDIA__JELLYFIN__APIKEY` | unset | Yes |
| `MEDIA__SONARR__BASEURL` | `http://sonarr:8989` | No |
| `MEDIA__SONARR__APIKEY` | unset | Yes |
| `MEDIA__RADARR__BASEURL` | `http://radarr:7878` | No |
| `MEDIA__RADARR__APIKEY` | unset | Yes |
| `MEDIA__PROWLARR__BASEURL` | `http://prowlarr:9696` | No |
| `MEDIA__PROWLARR__APIKEY` | unset | Yes |
| `MEDIA__QBITTORRENT__BASEURL` | `http://qbittorrent:8080` | No |
| `MEDIA__QBITTORRENT__APIKEY` | unset | Yes |
| `MEDIA__TIMEOUTSECONDS` | `3` | No |

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

No Media POST, PUT, PATCH or DELETE route exists.

### `GET /api/v1/modules/media/search?query={query}`

Searches the existing Jellyfin, Sonarr and Radarr libraries in parallel. Queries are
trimmed and must contain at least two characters. Each provider returns at most ten
normalized results with allowlisted title, year, media type, state and typed media
statistics. Provider failures remain isolated and produce a sanitized per-provider
status while successful results remain visible.

Jellyfin uses its bounded text-search endpoint. Sonarr and Radarr search only their
registered series and movies; external lookup and adding media remain out of scope.
Poster availability may be reported, but `posterUrl` remains `null` until BigBrain has
an authenticated image proxy that can avoid exposing credentials or internal URLs.
Local paths, upstream URLs and raw provider errors are never returned.

## Upstream read contract

| Service | Endpoints used |
|---|---|
| Jellyfin | `GET /System/Info`, `/Library/VirtualFolders`, `/Items/Counts`, `/Sessions` |
| Sonarr | `GET /api/v3/system/status`, `/series`, `/wanted/missing`, `/queue`, `/history`, `/health` |
| Radarr | `GET /api/v3/system/status`, `/movie`, `/wanted/missing`, `/queue`, `/history`, `/health` |
| Prowlarr | `GET /api/v1/system/status`, `/indexer`, `/health`, `/applications` |
| qBittorrent | `GET /api/v2/app/version`, `/torrents/info`, `/transfer/info` with `Authorization: Bearer <API_KEY>` |

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

- Add a series to Sonarr or a movie to Radarr.
- Search for missing episodes or movies.
- Pause, resume or delete a torrent.
- Open a title in Jellyfin.
- AI commands for media management.

There are no placeholder write routes, inactive write buttons or write capabilities in Sprint 1.

## ADR impact

No new ADR is required. The implementation follows the existing decision that Sentinel owns node-local privileged resources while ordinary product APIs remain behind Control Plane integration adapters. Any future write scope must receive its own architecture and security review before implementation.
