# Audiobookshelf setup

BB-100 pins `ghcr.io/advplyr/audiobookshelf:2.36.0`. The service joins the internal media network. Its host port defaults safely to appliance loopback, but an owner may set `AUDIOBOOKSHELF_BIND_ADDRESS` to the appliance's current Tailscale IPv4 address for direct tailnet-only access. Never use `0.0.0.0` or the LAN address. `/config` and `/metadata` are named persistent volumes. Audiobook media is a dedicated bind mount, default `/srv/media/audiobooks`, and is intentionally excluded from normal BigBrain application backups.

## Owner access over Tailscale

1. Verify runtime truth with `tailscale ip -4` and `tailscale status`; do not copy an address from old documentation.
2. Set the non-secret appliance environment value `AUDIOBOOKSHELF_BIND_ADDRESS=<tailscale-ip>` outside Git. Keep `AUDIOBOOKSHELF_PORT=13378` unless another approved port is required.
3. Recreate only Audiobookshelf: `docker compose up -d --no-deps --force-recreate audiobookshelf`.
4. Confirm Docker publishes exactly `<tailscale-ip>:13378->80/tcp`, not `0.0.0.0`, `[::]`, loopback or the LAN address.
5. From a Tailscale-connected owner device, open `http://<tailscale-ip>:13378`. Tailscale ACLs/grants remain an additional access boundary; the host bind alone does not grant a device access.

The internal BigBrain adapter continues to use `http://audiobookshelf:80` on the Docker media network and is independent of this browser-facing bind. When `AUDIOBOOKSHELF_BIND_ADDRESS` is set to the Tailscale address, `http://127.0.0.1:13378` is intentionally no longer available on the appliance host.

## First run

1. Ensure the host library directory exists with suitable ownership.
2. Start only `audiobookshelf`, open its Tailscale-only owner URL (or use an SSH tunnel while the safe loopback default is active), and complete the ABS first-run administrator setup.
3. Create the audiobook library rooted at `/audiobooks`.
4. Create a dedicated least-privilege user with library/progress read access. In **Settings → Users → API Keys**, create a revocable server-to-server key for that identity.
5. Inject the key as `MEDIA__AUDIOBOOKSHELF__APIKEY` and library ID as `MEDIA__AUDIOBOOKSHELF__LIBRARYID` through the appliance environment. Never place either value in Git or Web.
6. Recreate only API and verify the audiobook overview reports `configuredHealthy`.

## Separate playback identity

Create a revocable API key for the active restricted user that owns the owner's real progress. It needs audiobook-library and own playback/progress access, but no admin, user-management, upload or delete authority. Store it only as `MEDIA__AUDIOBOOKSHELF__PLAYBACKAPIKEY` in ignored `.env` (`0600`). Never replace `MEDIA__AUDIOBOOKSHELF__APIKEY`, print either value, or place them in Web/Git/docs. Recreate only API; sanitized availability must report `configuredHealthy`, `separateIdentity:true` and `hasProgress:true`.

Until steps 2–5 are performed, `notConfigured` is expected. Do not use an admin key for routine reads. ABS configuration/metadata may be backed up separately; audiobook media follows the media-library retention policy.

## Commissioned state

Commissioning was verified on 2026-08-23. Audiobookshelf is initialized, `Ljudböcker` is rooted at `/audiobooks`, and the server-side key acts for a dedicated non-admin `bigbrain` identity with access only to that library. BigBrain discovers the library ID through authenticated `/api/libraries` access and stores both key and ID only in the ignored appliance environment. The deployed overview must report `configuredHealthy`; an empty library is a successful state. Never copy the key into commands, logs, source, Web configuration or documentation.

## Rollback

Stop only `audiobookshelf`, remove its API environment values and recreate only API. Keep named volumes and `/srv/media/audiobooks`; rollback must not delete media or metadata.
