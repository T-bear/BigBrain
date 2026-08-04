# Dashboard Views and Widget Framework Phase 1 — Implementation

> Detta är en sanerad GitHub-version. Lokala identiteter, interna adresser, råloggar och hemligheter har utelämnats.

## Metadata

- Date: 2026-08-04
- Scope: BigBrain Web
- Decision: [ADR 0014](../../../adr/0014-dashboard-views-and-widget-framework.md)

## Status

Implemented and automatically verified. The later deployment report records Web-only
deployment; product-owner visual approval was recorded after deployment.

## Evidence

Frontend dependency installation, the full test suite and the production build passed.
Tests cover view switching, registry-driven rendering, visibility, ordering, collapse,
versioned persistence and malformed-state fallback.

## Changes

Phase 1 introduced Hem, Media, AI and Admin, `DashboardRegistry`,
`ApplicationWidgetRegistry`, `WidgetProvider`, `DashboardWorkspace`, the widget library,
edit mode, touch-friendly controls and local persistence. Existing modules are wrapped
as widgets instead of rewritten. Calendar, reminders and AI entries are clearly marked
placeholders.

## Security

No backend, Compose, external-service configuration or media data changed. This report
contains no runtime identities or credentials.

## Remaining work

BB-027 tracks user profiles, shared dashboards, templates, roles, server sync and custom
widget sizing.

## Resumption

Read the [architecture](../../../architecture/dashboard-widget-framework.md),
[runbook](../../../operations/runbooks/dashboard-widget-framework-verification.md) and
[current status](../../../STATUS.md). Extend the registry rather than hardcoding a module
into a dashboard.
