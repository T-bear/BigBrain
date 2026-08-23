# BB-101 — Audiobook Acquisition Foundation

## Metadata

- Baseline: `caba99de5b8a7087c62f7de24271c142703985d0`
- Date: 2026-08-23
- Scope: provider-neutral acquisition workflow above the commissioned BB-100 Audiobookshelf integration
- Sanitization notice: no keys, private addresses, provider credentials, raw production payloads or audiobook-library data are published.

## Status

BB-101 is technically complete, published, CI-verified and deployed as `1ef411a99a6b57e057099597366aaa64004bc801`. It implements the smallest safe vertical slice that does not require an unapproved third party. Audiobookshelf remains solely the library, metadata, playback and progress system. BigBrain owns discovery candidates, explicit edition selection, acquisition jobs and the Web/API contract. The runtime provider remains `None / NotConfigured`; request controls are unavailable, no request is represented as successful and no download/import progress is fabricated.

A provider decision remains an explicit owner gate. A viable provider must have current maintenance evidence, a versioned authenticated API, Docker support, reliable audiobook edition/language/narrator metadata, status and safe cancellation capabilities, normal Prowlarr/qBittorrent orchestration, controlled output beneath the configured import root and a reviewed security posture. Readarr remains retired and is not selected. No Chaptarr, Librarr/Librarry, Readarr fork or native download adapter was installed in this slice.

## Changes

`IAudiobookAcquisitionProvider` exposes truthful status/capability, bounded search, explicit request, job refresh and optional cancellation. `NoAudiobookAcquisitionProvider` is the deployed implementation. `AudiobookAcquisitionService` validates input, ranks verified Swedish before verified English and unknown language, maps provider failures to safe Problem Details and creates a stable BigBrain job ID only after a real provider accepts a request.

Jobs are persisted in a dedicated SQLite file on the API's existing durable `/data` volume. The store follows current in-process module persistence conventions and introduces no new service or volume. States are explicit (`requested`, `searching`, `candidateFound`, `awaitingSelection`, `queued`, `downloading`, `importing`, `completed`, `failed`, `cancelled`) but are used only when a provider truthfully returns them.

The import contract accepts only a relative provider output beneath the server-controlled library root. Absolute paths and traversal fail, and an existing file or directory is never overwritten. Web cannot select the import root or submit an arbitrary URL. Actual file movement, duplicate reconciliation and Audiobookshelf rescan remain pending a real provider; completion is not simulated.

## API and UX

The versioned Media API adds provider status, acquisition search, create/list/detail/cancel job endpoints. Queries, author fields, paging and opaque identifiers are bounded. Errors expose only stable safe codes/messages. Neither Audiobookshelf nor future provider credentials enter responses or Web.

The actual registry-composed Media/Ljudböcker view retains BB-100 library/continue-listening behavior and adds title plus optional-author search, Swedish-default language choice, separate local/provider results, edition cards with narrator/language/source/confidence, an accessible details dialog and activity only for real jobs. When provider None is active, the UI explains that automatic acquisition is not configured, disables Add and displays no progress indicator. Provider API failure is isolated from the Audiobookshelf library surface.

## Evidence

Local automated verification on 2026-08-23 passed 20 focused audiobook API tests, 515 complete API tests, 125 Web tests and 32 Sentinel tests. The Vite production build passed; the complete Release solution build passed with zero warnings and errors. Documentation verification passed 180 Markdown files and 89 unique BB IDs; Compose and diff checks passed. GitHub Actions run `32655261525` passed backend, frontend, documentation and secrets.

Only API and Web were built/recreated. Their deployed images are `sha256:77bc7249fb62635c28ecd1ea2b67d001ca8b23a029c630868d54d195f808ea9d` and `sha256:cb17d65b96bd323a2cebc89cc26624392393c3ef21fa8d1102d3c0e032acb4f7`; both containers are healthy. Audiobookshelf was not recreated and reports `configuredHealthy` through BigBrain. Provider status is `none / notConfigured`, job count is zero, local Swedish search returns a truthful empty result and a synthetic bounded request returns controlled HTTP 409 `providerNotConfigured` without creating a job. Cover proxy missing-item behavior remains controlled 404.

Deployed normal-viewport browser QA covered Obsidian Gold, Forest Night and Arctic Wind at 390 × 844 and 430 × 932. All six runs showed connected empty-library state, Swedish default, explicit provider-unavailable explanation, no fake progress, no horizontal overflow and the last audiobook control fully above the dock. The reviewed images are `/tmp/bb101-{theme}-audiobooks-{390x844,430x932}-{normal,searched}.png`; these are local runtime evidence, not tracked artifacts. Obsidian Gold was restored. Deployed Home, Family, Media, Finance and More routes rendered without console errors or overflow; Media retained Ljudböcker, Downloads, Smart Shuffle, Jellyfin, Sonarr, Radarr and Prowlarr surfaces.

## Security

Detta är en sanerad GitHub-version. No secret, private address, raw production identifier, credential, log payload or private audiobook title is included.

BB-100's commissioned Audiobookshelf configuration, restricted service identity, server-side key, tailnet-only owner access and persistent volumes are unchanged. Existing Jellyfin/Sonarr/Radarr/Prowlarr/qBittorrent contracts are unchanged and all five services report online after deployment. Finance remains `RESEARCH / 0 SEK / NONE`; scheduler is enabled and idle, and the governor remains active with no execution authority. BB-099 remains **TECHNICALLY COMPLETE / OWNER UX REVIEW PENDING**. The independent Sentinel socket runtime problem remains visible as a restarting container and was neither caused nor remediated by BB-101.

## Remaining work

Choose whether to evaluate a provider candidate against the capability/security gate above. Only after that decision should a separate bounded step install credentials/provider runtime, implement its adapter, validate actual output/import/rescan behavior and enable Add. BB-101 does not start BB-102.

## Resumption

Resume from this report, `docs/modules/media.md`, the commissioned BB-100 report and the Audiobookshelf runbook. Verify repository and runtime truth first. Keep provider `None` until the owner explicitly selects a candidate after maintenance/API/security review; never move credentials into Git or Web.
