# Media Module – Sprint 1

## Responsibility and boundary

The Media module provides a read-only, normalized dashboard view of Jellyfin, Sonarr, Radarr, Prowlarr and qBittorrent. These are application services, so their documented HTTP APIs are accessed through Control Plane adapters. Sentinel remains the exclusive boundary for operating-system resources, Docker, processes and filesystems; the Media module does not use those interfaces.

Each upstream API is treated as untrusted. Typed clients translate allowlisted fields into BigBrain contracts, apply short timeouts and bounded lists, accept cancellation, and convert service-specific failures into sanitized status results. One failing service does not fail the aggregate response.

The module does not mount the Docker socket or media directories, execute commands, access host inventory, log credentials, or expose upstream authentication material. The only upstream POST is qBittorrent's required session login; it authenticates a read-only collection session and does not mutate torrent state.

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
| `MEDIA__QBITTORRENT__USERNAME` | unset | Yes |
| `MEDIA__QBITTORRENT__PASSWORD` | unset | Yes |
| `MEDIA__TIMEOUTSECONDS` | `3` | No |

URLs must be absolute HTTP or HTTPS URLs without query strings or fragments. Timeout must be between 1 and 15 seconds. Credentials are optional so an unconfigured service becomes `notConfigured`; real values must be supplied through runtime secret injection and must never be committed.

BigBrain API must share an internal application network with a service for its Docker DNS name to resolve. This sprint does not guess or modify the externally managed media stack's network. Operators should attach BigBrain API using a deployment-specific Compose override after confirming the existing network name.

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

## Upstream read contract

| Service | Endpoints used |
|---|---|
| Jellyfin | `GET /System/Info`, `/Library/VirtualFolders`, `/Items/Counts`, `/Sessions` |
| Sonarr | `GET /api/v3/system/status`, `/series`, `/wanted/missing`, `/queue`, `/history`, `/health` |
| Radarr | `GET /api/v3/system/status`, `/movie`, `/wanted/missing`, `/queue`, `/history`, `/health` |
| Prowlarr | `GET /api/v1/system/status`, `/indexer`, `/health`, `/applications` |
| qBittorrent | `POST /api/v2/auth/login` for SID authentication; then `GET /api/v2/app/version`, `/torrents/info`, `/transfer/info` |

Queue and torrent lists are limited to 25 entries, health/indexer output to 25 entries, application connections to 10 and history to 10. qBittorrent summary counts in Sprint 1 describe the bounded recent list returned by the upstream query; an exact, safely bounded global-count strategy remains technical debt.

## Failure model

- Missing credentials: `notConfigured`.
- Successful normalized response: `online`.
- Rejected authentication or malformed provider response: `degraded`.
- Timeout, DNS failure, connection refusal or other transport failure: `unavailable`.
- A mixture of service outcomes: aggregate `degraded`, while successful service data remains available.
- All services unconfigured: aggregate `notConfigured`.

Messages are fixed, sanitized text and never include raw exception messages, URLs or credentials. No automatic retry loop is configured.

## Sprint 2 backlog

The following are explicitly deferred and require separate authorization, audit, confirmation and capability design:

- Search for a movie or series.
- Add a series to Sonarr or a movie to Radarr.
- Search for missing episodes or movies.
- Pause, resume or delete a torrent.
- Open a title in Jellyfin.
- AI commands for media management.

There are no placeholder write routes, inactive write buttons or write capabilities in Sprint 1.

## ADR impact

No new ADR is required. The implementation follows the existing decision that Sentinel owns node-local privileged resources while ordinary product APIs remain behind Control Plane integration adapters. Any future write scope must receive its own architecture and security review before implementation.
