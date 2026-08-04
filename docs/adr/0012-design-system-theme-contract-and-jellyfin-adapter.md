# ADR 0012: Design system theme contract and Jellyfin adapter boundary

- Status: Proposed
- Date: 2026-08-04

## Context

BigBrain Web had one large palette-bound stylesheet and inconsistent component patterns. BigBrain and Jellyfin should share visual identity without sharing DOM, release cycle or application CSS.

## Proposed decision

BigBrain owns versioned `--bb-` design tokens, typed allowlisted `data-theme` IDs and a stable v1 component-class contract. Components consume semantic tokens. Dark is the no-JavaScript CSS default; light proves theme independence. The user choice is stored locally and applied on `documentElement`, while invalid values follow the safe default/system preference flow.

The Web team owns tokens, component behavior, accessibility and migration. Module owners retain unique layouts while consuming shared primitives. WCAG AA-oriented contrast, persistent visible focus, 44 px controls and reduced-motion support are required.

Jellyfin receives a separate, manually published adapter with `--bb-jf-` tokens and version-checked Jellyfin selectors. Exact shared CSS is rejected because the products have different DOMs, clients and release cycles. Jellyfin selectors are explicitly outside BigBrain's stable theme contract.

Theme-contract changes are versioned. A future token generator may consume JSON, but v1 adds no plugin engine, marketplace or build service.

## Alternatives

Keeping one global stylesheet preserves duplication and prevents safe themes. Sharing BigBrain component CSS with Jellyfin couples unrelated markup. Per-module palettes fragment the product. A runtime theme plugin system adds unverified code-loading and governance risk.

## Consequences, migration and rollback

The token and component layers become long-lived public UI contracts; module layouts can migrate incrementally without functional rewrites. Jellyfin upgrades require selector revalidation and Tizen remains a manual gate. Rollback removes the new imports/theme control and restores the previous stylesheet; the Jellyfin adapter has no runtime effect until separately pasted and can be removed from Custom CSS.
