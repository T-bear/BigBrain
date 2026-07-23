# Testing BigBrain with the existing media stack

This guide connects the completed read-only Media Module Sprint 1 to the existing Jellyfin, Sonarr, Radarr, Prowlarr and qBittorrent containers. It does not add write operations and does not change or restart the media stack.

## Verified local topology

The current server was inspected using container names, image version labels and network membership only. No container environment variables, mounts or credentials were read.

| Service | Verified image version | Internal URL |
|---|---:|---|
| Jellyfin | 10.11.11 | `http://jellyfin:8096` |
| Sonarr | 4.0.19.2979 | `http://sonarr:8989` |
| Radarr | 6.3.0.10514 | `http://radarr:7878` |
| Prowlarr | 2.4.0.5397 | `http://prowlarr:9696` |
| qBittorrent | 5.2.3 | `http://qbittorrent:8080` |

All five services are connected to the existing Docker bridge network `bigbrain_default`. BigBrain API joins that network as an external network; Compose does not own, create, stop or remove it. Override `MEDIA_DOCKER_NETWORK` if the media stack is renamed later.

## 1. Create local runtime configuration

From the repository root:

```bash
cp .env.example .env
chmod 600 .env
```

`.env` is ignored by Git. Do not add it with `git add --force`, paste it into logs, or send it with bug reports.

Fill in these required credential variables:

```dotenv
MEDIA__JELLYFIN__APIKEY=<jellyfin-api-key>
MEDIA__SONARR__APIKEY=<sonarr-api-key>
MEDIA__RADARR__APIKEY=<radarr-api-key>
MEDIA__PROWLARR__APIKEY=<prowlarr-api-key>
MEDIA__QBITTORRENT__USERNAME=<qbittorrent-webui-username>
MEDIA__QBITTORRENT__PASSWORD=<qbittorrent-webui-password>
```

Keep the supplied internal URLs unless the container names or ports differ. `MEDIA__TIMEOUTSECONDS` defaults to 3 and accepts 1–15 seconds.

### Where to obtain credentials

- Jellyfin: Dashboard → Advanced → API Keys. Create a dedicated BigBrain key.
- Sonarr: Settings → General → Security → API Key.
- Radarr: Settings → General → Security → API Key.
- Prowlarr: Settings → General → Security → API Key.
- qBittorrent: use the configured Web UI credentials and keep the Web UI limited to trusted internal networks.

Use the least-privileged account available. The upstream products do not all provide read-only API scopes, so credentials must still be treated as sensitive even though BigBrain exposes only read operations.

## 2. Validate and start BigBrain

Validate the effective Compose model without printing secret values:

```bash
docker compose config --quiet
```

Build and recreate only the BigBrain services:

```bash
docker compose up -d --build api web
```

This command must not include media service names. The external `bigbrain_default` network remains owned by the existing media stack.

Check BigBrain status:

```bash
docker compose ps
curl --fail http://127.0.0.1:18080/health
curl --fail http://127.0.0.1:18080/api/v1/modules/media
```

Open the dashboard:

```text
http://<bigbrain-server-ip>:13000/#media
```

For a browser on the BigBrain server itself, use `http://127.0.0.1:13000/#media`.

## 3. Verify each integration

The Media section should show a status card for all five services:

- `online`: authentication and all required read endpoints succeeded.
- `degraded`: authentication was rejected or a provider returned malformed data.
- `unavailable`: DNS, connection or timeout failure.
- `notConfigured`: the required key or credentials were not supplied.

The API response can be inspected without exposing configured credentials:

```bash
curl --fail --silent http://127.0.0.1:18080/api/v1/modules/media
```

Verify:

1. `services` contains Jellyfin, Sonarr, Radarr, Prowlarr and qBittorrent.
2. Each configured service has `isConfigured: true`.
3. Each working service reports `status: "online"` and a version.
4. qBittorrent shows bounded torrent activity and current transfer rates.
5. Sonarr and Radarr show queue counts and bounded queue entries.
6. Sonarr, Radarr and Prowlarr health warnings are visible when reported.
7. Stopping or misconfiguring one service produces a partial/degraded response while other cards and data remain visible.

Never paste the contents of `.env`, Docker container configuration, request headers or qBittorrent cookies into diagnostics.

## 4. Upstream read endpoints

The installed versions use these documented endpoints:

- Jellyfin 10.11: `GET /System/Info`, `/Library/VirtualFolders`, `/Items/Counts`, `/Sessions`.
- Sonarr 4: `GET /api/v3/system/status`, `/series`, `/wanted/missing`, `/queue`, `/history`, `/health`.
- Radarr 6: `GET /api/v3/system/status`, `/movie`, `/wanted/missing`, `/queue`, `/history`, `/health`.
- Prowlarr 2: `GET /api/v1/system/status`, `/indexer`, `/health`, `/applications`.
- qBittorrent 5: `POST /api/v2/auth/login` only to establish the required SID session, followed by `GET /api/v2/app/version`, `/torrents/info` and `/transfer/info`.

No BigBrain POST, PUT, PATCH or DELETE Media route exists. No upstream mutation route is called.

## 5. Troubleshooting

### `notConfigured`

- Confirm all six credential variables in `.env` are non-empty.
- Run `docker compose up -d --force-recreate api` after changing `.env`.
- Use single quotes around `.env` values containing `$` or other interpolation characters; Compose removes the quotes and preserves the literal value.

### `unavailable`

- Confirm the five media containers are running.
- Confirm `MEDIA_DOCKER_NETWORK=bigbrain_default`.
- Confirm BigBrain API is attached to both networks without printing its environment:

  ```bash
  docker inspect --format '{{range $name, $_ := .NetworkSettings.Networks}}{{$name}} {{end}}' bigbrain-sprint1-api-1
  ```
- Confirm the internal service name and port match the table above.
- Increase `MEDIA__TIMEOUTSECONDS` only after ruling out DNS and connection failures.

### `degraded` or authentication rejected

- Regenerate or recopy the affected API key.
- Confirm qBittorrent Web UI credentials and host-header/CSRF configuration permit requests from `http://qbittorrent:8080`.
- Recreate only the API container after credential changes.
- Review BigBrain's sanitized status message. Do not enable request-header or body logging.

### Dashboard loads but has no media data

- Call `/api/v1/modules/media` directly to distinguish API failure from browser rendering.
- Hard-refresh the browser after rebuilding the Web container.
- Confirm the reverse proxy still forwards `/api/` to the BigBrain API.

### One service fails

This is an expected partial-failure mode. The aggregate status becomes `degraded`, but other services must remain visible. Fix the affected service without restarting the remaining media containers.
