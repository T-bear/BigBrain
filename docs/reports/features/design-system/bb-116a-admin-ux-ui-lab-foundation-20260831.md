# BB-116A — Admin UX/UI Lab Foundation

Detta är en sanerad GitHub-version. Inga hemligheter, privata adresser, råloggar, privata identiteter eller proprietära tredjepartstillgångar ingår.

## Metadata

- Date: 2026-08-31
- Baseline: `fc4129cb8c6e13f2154528feb3dbb269593dbfae`
- Baseline source of truth: `fc4129cb8c6e13f2154528feb3dbb269593dbfae`

## Status

Implemented, automatically and runtime verified, Web-deployed and CI-verified; physical owner review pending.

## Changes

Admin exposes `/admin/ux-ui-lab` as a permanent mobile-first owner review surface. Each specimen has a stable human-readable identity, a state and one of `EXPERIMENTELL`, `OWNER GODKÄND` or `RATAD`. Initial components are experimental unless explicit evidence says otherwise. Design System v1 remains **OWNER REVIEW IN PROGRESS**.

The lab directly renders the shared production primitives `BBButton`, `BBInput`, `BBSelect`, `BBSurface`, `BBEmptyState`, `BBLoadingIndicator`, `StatusBadge`, `BBMediaArtwork` and `AppIcon`. The timer UI was extracted as the smallest justified shared presentation component: both production playback and the lab use `SleepTimerControl`; only their callbacks differ. Lab callbacks mutate local specimen state and issue no domain or external requests.

## Evidence

Catalogued production areas are actions and supported states, icon actions, navigation/back semantics, surfaces/cards, text/search/select, checkbox and range, loading/empty/error/warning/status, artwork/fallback, playback affordances and sleep timer. Several recurring controls remain local markup: checkbox treatment, navigation rows, confirmation/dialog implementations and domain-specific cards. BB-116A records this as consolidation debt and does not create speculative abstractions.

The previous tiny white timer icon and wide form-like disclosure are recorded as **RATAD** and are not reproduced. The candidate is a gold 23 px crescent SVG inside a 48 px target with an at-most 280 px anchored dialog, 15/30/45/60 presets, local stop time and off/active handling. Timer state and expiration architecture are unchanged.

## Security

The lab's interactive specimens use local React state. It does not invoke destructive/domain actions, alter owner data or call external services. Production timer callbacks retain the existing authorized AppShell path. No credentials or server configuration changed.

## Reference and scope

Public Storytel catalogue/category pages were reviewed only for high-level hierarchy: cover prominence, grouped collections, concise title/author copy, browse-first density and progressively disclosed secondary information. No CSS, HTML/DOM, JavaScript, source, class names, tokens, proprietary artwork, icons, assets or branding were copied. The lab-only audiobook card candidate uses BigBrain primitives and representative local content. Production audiobook library remains unchanged.

BB-157 and a discovery-card candidate were deferred. BB-155 and BB-156 were not implemented. Finance, Family, bottom dock, full player architecture and backend APIs were not changed.

## Verification

- Focused Web: 35 passed.
- Full Web: 160 passed.
- Vite production build: passed.
- Local Firefox: 390×844, 430×932, 768×1024 and 1440×900 rendered with responsive category navigation, single-column mobile specimens, desktop two-column specimens and mobile dock clearance.
- Deployment: implementation `a50c1ffa9b238bc5238b537dcbb7ecc36e476f78`; only Web rebuilt/recreated without volume removal; image `sha256:e2360112a7f11e892937f0556a67d134aa14c1b40ff866c9a458ff357db9a3aa`; container healthy.
- Runtime: `/`, `/health` and `/admin/ux-ui-lab` returned HTTP 200. The deployed route passed Firefox rendering at 390×844, 430×932, 768×1024 and 1440×900.
- CI: GitHub Actions run `33339440666` completed successfully for the implementation commit.
- Physical iPhone/PWA owner review: pending and authoritative for approval.

## Remaining work

Request physical owner review. Production audiobook grid migration, search/discovery candidate, BB-157 and broad local-pattern consolidation remain deferred.

## Resumption

Resume from the published BB-116A implementation and this report. Follow `AGENTS.md`, preserve unrelated owner work and use only `docs/operations/codex-recovery.md` if interrupted.
