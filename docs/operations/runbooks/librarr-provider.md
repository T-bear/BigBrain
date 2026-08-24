# Librarr audiobook provider

## Supported build

BB-103 uses the internal-only image `bigbrain-librarr:1208254-bb5`. It is built from immutable upstream commit `1208254c20b31fbf217558c0fb987f779fed1cf8` with isolated no-overwrite, revision-bound source-policy, durable-import-outcome and bounded author-aware discovery patches documented in `infrastructure/librarr/README.md`.

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

The qBittorrent category is `audiobooks`. Existing Sonarr/Radarr categories are unchanged. Librarr has no host port and is reachable only on the existing internal media network. `LIBRARR_TRUSTED_SOURCE_REVISION` must exactly match the image's immutable upstream commit. The local pinned registry enables public configuration for AudioBookBay, LibriVox and The Pirate Bay audiobook. BookTracker audiobook belongs to the reviewed revision set but stays disabled without its separate server-side configuration. Prowlarr remains first/preferred; unknown, injected or future-revision sources stop startup.

## Commissioning

1. Verify all three secret variable names are present without printing values.
2. Create `/srv/media/audiobooks-incoming` owned by appliance UID/GID 1000 without changing the final library.
3. Initialize the newly created `librarr-config` named volume for UID/GID 1000 before first start; Docker creates it root-owned and Librarr intentionally runs non-root.
4. Render Compose with `docker compose config --quiet`.
5. Build `bigbrain-librarr:1208254-bb5`; the build runs complete organizer/search/download tests plus focused API, revision-policy and discovery-query regressions.
6. Start only Librarr, verify `/health`, authenticated `/api/admin/health`, Prowlarr, qBittorrent and Audiobookshelf checks.
7. Before enabling BigBrain search, verify source metadata matches the exact pinned set: Prowlarr first, AudioBookBay, LibriVox and The Pirate Bay active, and BookTracker present but disabled unless separately configured. Stop Librarr on any unknown or duplicate audiobook source.
8. Recreate only API and Web after provider health and source-boundary checks succeed.
9. Search through BigBrain. The provider-specific timeout is bounded at 45 seconds for the reviewed multi-source latency; cancellation still propagates and one source may return zero results without invalidating useful results from another. Acquisition still requires the owner's explicit edition review and confirmation.

## Acquisition lifecycle evidence

After the owner confirms a release, BigBrain follows the opaque provider job. qBittorrent states map to queued/downloading/importing. A missing qBittorrent row is never completion evidence. Librarr must record an exact successful import and local audiobook `source_id`; a durable `torrent_import_failed` event yields a safe failed state. After import, BigBrain reports indexing until the same title and author appear through Librarr's Audiobookshelf-backed library endpoint, then reports completed and refreshes its own Audiobookshelf overview.

On conflict, do not clear the torrent or destination. Inspect the sanitized activity state and filesystem ownership/path mapping without deleting either side. An asynchronous or failed Audiobookshelf scan leaves the job in indexing; use Audiobookshelf's supported library scan control and never edit its database directly.

## Failure and rollback

If a dependency fails, BigBrain must report `configuredUnavailable`; do not enable Add. If an import reports a destination conflict, preserve the existing library and source download for manual review.

Rollback stops/removes only the Librarr container and restores API/Web with `LIBRARR_API_KEY` unset. Preserve the `librarr-config` volume, `/srv/media/audiobooks-incoming` and `/srv/media/audiobooks`. The BB-101 provider-neutral `NotConfigured` behavior remains the safe fallback.
