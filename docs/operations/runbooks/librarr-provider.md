# Librarr audiobook provider

## Supported build

BB-102 uses the internal-only image `bigbrain-librarr:1208254-bb1`. It is built from immutable upstream commit `1208254c20b31fbf217558c0fb987f779fed1cf8` with the single no-overwrite patch documented in `infrastructure/librarr/README.md`.

Do not replace the tag with `latest`. Re-run the patch security review for every upstream change.

## Runtime secrets

The ignored appliance `.env` must contain these values before deployment:

```dotenv
LIBRARR_API_KEY=<dedicated random revocable key>
LIBRARR_QBITTORRENT_USERNAME=<existing qBittorrent Web API user>
LIBRARR_QBITTORRENT_PASSWORD=<existing qBittorrent Web API password>
```

Never place values in Git, documentation, command output or chat. Librarr receives existing Prowlarr and Audiobookshelf credentials directly from their current server-side environment variables. BigBrain API receives only `LIBRARR_API_KEY`; Web never receives it.

Presence-only check:

```bash
awk -F= '{print $1}' .env | grep -E '^LIBRARR_(API_KEY|QBITTORRENT_USERNAME|QBITTORRENT_PASSWORD)$'
```

## Paths and networks

| Purpose | Host | qBittorrent | Librarr | Audiobookshelf |
| --- | --- | --- | --- | --- |
| Incomplete/completed acquisition | `/srv/media/audiobooks-incoming` | `/data/audiobooks-incoming` | `/data/audiobooks-incoming` | not mounted |
| Final library | `/srv/media/audiobooks` | available beneath existing `/data` mount | `/audiobooks` | `/audiobooks` |

The qBittorrent category is `audiobooks`. Existing Sonarr/Radarr categories are unchanged. Librarr has no host port and is reachable only on the existing internal media network. Its source registry is a reviewed empty local file, so audiobook discovery is exclusively through the existing Prowlarr configuration.

## Commissioning

1. Verify all three secret variable names are present without printing values.
2. Create `/srv/media/audiobooks-incoming` owned by appliance UID/GID 1000 without changing the final library.
3. Initialize the newly created `librarr-config` named volume for UID/GID 1000 before first start; Docker creates it root-owned and Librarr intentionally runs non-root.
4. Render Compose with `docker compose config --quiet`.
5. Build `bigbrain-librarr:1208254-bb1`; the build runs upstream organizer tests plus the patch regressions.
6. Start only Librarr, verify `/health`, authenticated `/api/admin/health`, Prowlarr, qBittorrent and Audiobookshelf checks.
7. Before enabling BigBrain search, verify runtime logs show only the approved discovery sources. Upstream commit `1208254` currently registers built-in audiobook sources unconditionally; an empty registry does not disable them. Stop Librarr if this remains true.
8. Recreate only API and Web after provider health and source-boundary checks succeed.
9. Search through BigBrain. Do not submit the first release until the owner explicitly selects it.

## Failure and rollback

If a dependency fails, BigBrain must report `configuredUnavailable`; do not enable Add. If an import reports a destination conflict, preserve the existing library and source download for manual review.

Rollback stops/removes only the Librarr container and restores API/Web with `LIBRARR_API_KEY` unset. Preserve the `librarr-config` volume, `/srv/media/audiobooks-incoming` and `/srv/media/audiobooks`. The BB-101 provider-neutral `NotConfigured` behavior remains the safe fallback.
