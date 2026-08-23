# BB-100 — Audiobooks Platform Foundation

## Metadata

- Baseline: `c2efe5385f714de10490b2dee54db76055f66a70`
- Date: 2026-08-23
- Scope: Audiobookshelf read integration and provider-neutral acquisition foundation
- Sanitization: This is a sanitized GitHub report; no keys, private addresses, raw logs or user library data are included.

## Status

Technically implemented, published, CI-verified and foundation-deployed. Audiobookshelf is running loopback-only; its library and dedicated API identity require the documented owner first-run action, so BigBrain truthfully reports `notConfigured`. BB-099 remains technically complete with owner UX review pending.

## Decision

Official Readarr is retired because its metadata became unusable and maintainership did not continue. BigBrain does not bind its product contract to Readarr. Chaptarr and resurrected forks preserve familiar APIs but inherit fork/metadata/migration risk. Librarr and Librarry show active development and Prowlarr/qBittorrent integration, but are young and expose broad acquisition/scraping surfaces. BB-100 consequently ships `IAudiobookAcquisitionProvider` with provider `None`; automated acquisition is deferred rather than simulated.

## Changes

Audiobookshelf 2.36.0 is the library core. The adapter uses a bounded timeout, server-side Bearer API key, page/result limits, strict opaque item-ID validation and a size-limited same-origin cover endpoint. ABS HTML descriptions become bounded plain text. Authentication, transport, timeout and malformed JSON failures degrade only Ljudböcker.

Media/Ljudböcker renders continue listening when genuine progress exists, a cover-forward library, local language-filtered search, details with narrator and explicit language, truthful empty/unavailable states and provider-None messaging. Settings persists preferred/fallback identifiers (`sv` default, `en` fallback); `und` remains unknown. Discovery contracts preserve work and edition IDs plus language confidence.

## Evidence

Network-free API fixtures cover healthy, authentication failure, malformed payload and provider-None states. Web tests cover configured/not-configured rendering, language-filtered local search and first-class registration in the actual Media route. Local validation passed 124 frontend, 506 API and 32 Sentinel tests; Vite production and full Release builds, documentation verification (179 Markdown files), Compose and diff checks passed. CI runs `32646849659` and `32647803354` passed.

The first deployed browser review exposed that the component had been placed in the unused non-admin `MediaDashboard` branch while the real Media route is registry-composed. Commit `7911505187ca6c316fc050ed349f1980938d1ad6` moved it to the widget registry and added the route regression. The final deployed Web image is `sha256:e6b04941152dc4efed4fd6a7e2ddb64e7b025d8b766f5f6a122554599103ef19`; API is healthy and ABS image `sha256:180acad33d69c99ed208676465d8edcb268fa46967735579a7810859885b1a8e` is running. Normal viewport captures at `/tmp/bb100-{obsidian-gold,forest-night,arctic-wind}-audiobooks-{390x844,430x932}.png` show the truthful setup state in all themes with no horizontal overflow. Obsidian Gold was restored after theme verification.

The final appliance inventory also found the separately deployed Sentinel container restarting with `AddressInUseException` for `/run/bigbrain/sentinel.sock`. The BB-100 Web-only correction neither changed nor restarted Sentinel. Read-only inspection found its dedicated runtime volume and no visible listening socket owner; deleting the socket or recreating Sentinel was not performed because that would exceed the explicitly restricted resumption scope. Automated Sentinel tests and CI remain green, but current Sentinel runtime health must not be reported as passing until that independent appliance condition is remediated and reverified.

## Security

Detta är en sanerad GitHub-version. No secret, private address, raw production log or user library data is published.

Secrets remain server-side. Cover access accepts only bounded opaque ABS item IDs and responses are size-limited. Upstream descriptions are HTML-decoded plain text; arbitrary remote cover URLs and `dangerouslySetInnerHTML` are not used. Requests and page sizes are bounded, and provider failures cannot cascade into the rest of Media.

## Deployment model

Compose pins the official image, joins the internal media network, binds only `/srv/media/audiobooks` and exposes setup on loopback. Config and metadata use named volumes; large media is excluded from application backups. Manual creation of the ABS library and dedicated API identity is required and is not complete without runtime evidence.

BB-099 remains **TECHNICALLY COMPLETE / OWNER UX REVIEW PENDING**. Finance is outside this change. BB-101 is planning only and has not started.

## Remaining work

Manual ABS first-run, dedicated least-privilege API identity, library ID injection and configured-library runtime validation remain owner actions. BB-101 may later evaluate a provider and explicit acquisition flow; it is not implemented here.

## Resumption

Resume from this report, the Audiobookshelf setup runbook, `docs/modules/media.md`, `docs/STATUS.md` and `docs/BACKLOG.md`. Never deploy an acquisition provider without a separate maintenance/API/security gate.
