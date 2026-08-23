# Audiobookshelf setup

BB-100 pins `ghcr.io/advplyr/audiobookshelf:2.36.0`. The service joins the internal media network and exposes setup only on appliance loopback port `13378`; use an SSH tunnel rather than publishing it. `/config` and `/metadata` are named persistent volumes. Audiobook media is a dedicated bind mount, default `/srv/media/audiobooks`, and is intentionally excluded from normal BigBrain application backups.

## First run

1. Ensure the host library directory exists with suitable ownership.
2. Start only `audiobookshelf`, tunnel port 13378 and complete the ABS first-run administrator setup.
3. Create the audiobook library rooted at `/audiobooks`.
4. Create a dedicated least-privilege user with library/progress read access. In **Settings → Users → API Keys**, create a revocable server-to-server key for that identity.
5. Inject the key as `MEDIA__AUDIOBOOKSHELF__APIKEY` and library ID as `MEDIA__AUDIOBOOKSHELF__LIBRARYID` through the appliance environment. Never place either value in Git or Web.
6. Recreate only API and verify the audiobook overview reports `configuredHealthy`.

Until steps 2–5 are performed, `notConfigured` is expected. Do not use an admin key for routine reads. ABS configuration/metadata may be backed up separately; audiobook media follows the media-library retention policy.

## Rollback

Stop only `audiobookshelf`, remove its API environment values and recreate only API. Keep named volumes and `/srv/media/audiobooks`; rollback must not delete media or metadata.
