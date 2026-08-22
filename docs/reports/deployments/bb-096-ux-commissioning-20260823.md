# BB-096 UX deployment / commissioning

## Metadata

- Date: 2026-08-23
- Scope: deployment-only commissioning of the accepted BB-096 frontend on the BigBrain appliance
- Repository commit: `27b67450cee61f0283a8eaaf7748a0d57274ea1c`
- Accepted UX implementation: `1293d36a04013bb65469ca2191f2f1683d52a50d`

## Status

- Implemented: previously complete under BB-096; no product code changed during commissioning.
- Tested: appliance HTTP/API smoke checks and real-browser mobile/desktop/theme checks passed.
- Deployed: Web and API were rebuilt from repository HEAD and recreated through the normal Compose procedure without removing volumes.
- Manually verified: rendered screenshots for all three mobile themes and the desktop shell were inspected. Product-owner subjective UX review remains outstanding.

## Evidence

- Web changed from the pre-BB-096 image to the repository-built image `sha256:3e2e24b77c5d…`; API was recreated as `sha256:a3ee4e0b95b4…` from the same repository state.
- The served document identifies Swedish language, `obsidian-gold`, the BB-096 hashed JavaScript/CSS assets and the theme-specific browser color.
- The repository has no service worker. PWA update behavior therefore uses a fresh application document and hashed assets; a fresh browser process loaded the deployed BB-096 client.
- At 390 x 844, Obsidian Gold, Arctic Wind and Forest Night each switched immediately, persisted through reload and a new browser process, retained readable semantic states, showed the five-item dock and had no horizontal overflow or dock overlap. A 430 x 932 composition is covered by the same responsive shell contract and was inspected during accepted BB-096 implementation; commissioning exercised the narrower limiting viewport.
- At 1440 x 900 the desktop rail replaced the mobile dock, secondary AI/Admin destinations remained reachable and content width stayed bounded.
- Read-only appliance requests for modules, recovery, Family meal/shopping/calendar, Media overview/jobs/downloads/Smart Shuffle, Finance overview/observation/research/scheduler/governor/operations/risk/backups and Settings all returned HTTP 200.
- System recovery was healthy after restart. Container logs contained no matching unhandled/API or Web error entries during the commissioning window.
- Finance remained `RESEARCH / 0 SEK / NONE`. The commissioned scheduler stayed enabled, operations stayed deferred without attention, the governor returned `ALLOW`, and maintenance pause remained false.
- Before and after deployment the scheduler journal contained one deferred opportunity while autonomous-research history contained zero runs and zero experiments. No duplicate work was created.
- Existing provider cadence, Finance volumes/evidence and backup policy were not changed.

## Changes

No source, runtime defaults, database schema or product configuration was changed. Only the API and Web containers were rebuilt/recreated from repository HEAD. This report, the report catalog, status and backlog commissioning note record the resulting evidence.

## Security

Detta är en sanerad GitHub-version. It contains no secrets, provider credentials, private addresses, raw logs, media titles or sensitive filesystem paths. No database or volume was reset, and no Finance authority or Hard Risk behavior changed.

## Remaining work

- Product-owner manual UX review is the next step.
- Subjective density, copy and icon preferences observed during commissioning were deliberately not changed.
- Installed iOS hardware behavior should receive owner confirmation; automated browser emulation verified viewport/safe-area structure but is not physical-device evidence.

## Rollback

Retag the retained pre-deployment Web/API images to the Compose service tags and recreate only those two services, preserving all volumes. If Finance authority ever differs from `RESEARCH / 0 SEK / NONE`, stop and disable the scheduler deployment override before further diagnosis. Research history must not be deleted.

## Resumption

Resume from the BB-096 feature report, this commissioning report and current `docs/STATUS.md`. Do not begin another development slice from this deployment record; obtain product-owner UX feedback first.
