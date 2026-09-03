# BB-128C — Finance Design-System Conformance for Async & Degraded States

## Metadata

- Date: 2026-09-03
- Baseline: `2fa43170f51f434e6adb04765618d88ffb74c601`

## Status

- Status: **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYMENT PENDING / OWNER VISUAL REVIEW PENDING**
- Safety: Finance `RESEARCH / 0 SEK / NONE`

## Authority and gap

ADR 0012 and versioned `--bb-*` semantic tokens plus shared component primitives are implementation authority. UX/UI Lab specimens render the real `BBLoadingIndicator`, `BBButton busy` and warning primitive, but the relevant specimens remain labelled `EXPERIMENTELL`, not `OWNER GODKÄND`; they are reference surfaces rather than a second CSS system. Finance previously substituted raw busy text and used a tall module-specific warning card. A literal ` · ` between the stale label and time could wrap as an orphan on narrow screens.

## Changes

- Initial Finance acquisition now uses `BBLoadingIndicator` with accessible status and container `aria-busy`.
- First-use and cached-state retry use `BBButton busy`; its label footprint remains stable, activation is disabled while busy and shared dots communicate activity visually.
- Cached Finance remains visible. Its compact warning strip separately exposes stale label, original browser fetch time, background activity or failed-refresh state, and retry.
- Label and `<time>` are independent flex items with no literal separator. The time is nonbreaking and both items wrap safely at narrow widths.
- Shared CSS supplies semantic tokens, focus/control behavior and `prefers-reduced-motion`; no Finance-specific animation or UX-lab CSS was copied.

BB-128B's cache version, stored projection, background refresh, foreground/online triggers, abort handling and one-in-flight request remain unchanged. No backend, API, provider, data, entitlement, research or trading behavior changed.

## Evidence

Focused component and Finance tests passed 27/27. The full Web suite passed 173/173 and the production build completed. Tests cover standardized initial loading, standardized retry busy state, request deduplication, retained cached view on failure, recovery on success, timestamp exposure, absence of an orphanable separator and the existing research-only boundary. Shared component CSS explicitly disables loading animation under reduced motion.

Deployment/runtime evidence will be appended only after the authorized Web-only deployment. Owner visual approval remains pending.

## Owner evidence and remaining review

During a natural transient failure on the physical iPhone/PWA, BB-128B retained last-known-good Finance content, offered retry, and recovered normally after one owner retry. This establishes **BB-128B resilience behavior: OWNER UX VERIFIED**. It does not approve the visual treatment; this BB-128C correction requires a fresh owner visual check after deployment.

## Security

Detta är en sanerad GitHub-version. It contains no secret, private address, raw provider payload or sensitive runtime data. No raw data, Finance database or authority changed.

## Remaining work

Deploy only Web, verify Web/API/Finance health and the deployed bundle, then request physical iPhone/PWA visual review. Do not infer owner visual approval from automated checks.

## Resumption

Resume from this report and the BB-128C entries in Status, Backlog, Finance module documentation and Testing. Preserve BB-128B behavior and deploy only Web.
