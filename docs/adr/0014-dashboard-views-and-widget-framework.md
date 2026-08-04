# ADR 0014: Dashboard views and first-party widget framework

- Status: Proposed
- Date: 2026-08-04

## Context

BigBrain Web grew as one vertically ordered page. Modules were mounted directly by the application shell, which made navigation increasingly long and forced future features to know where they belonged in `App`. The accepted architecture already states that modules contribute dashboard widgets through a registry, but no application-wide widget contract or user layout model existed.

## Proposed decision

BigBrain Web owns four stable dashboard view identifiers: `home`, `media`, `ai` and `admin`. The application shell and bottom navigation select one view without a reload. Dashboards query a compiled first-party `ApplicationWidgetRegistry`; they do not import or position feature components directly.

Each widget declares stable identity, title, description, icon, category, default view, default and minimum size, supported views, a future permissions field and a compiled render function. Widget IDs are unique and become persistence keys. The registry rejects duplicate IDs and definitions whose default view is unsupported.

`WidgetProvider` owns a versioned local preference document containing active view plus per-view order, hidden widgets and collapsed widgets. Unknown or malformed values fall back safely. Newly compiled default widgets are appended without discarding known user choices. Hiding a widget changes presentation only; it never deletes module data. Reordering supports drag-and-drop and accessible move buttons.

Only compiled first-party React components may render. The framework is not a dynamic plugin loader and accepts no remote executable code. Existing module components are adapted incrementally rather than rewritten. Calendar, reminders and AI entries may be registered as explicit unavailable placeholders until their modules exist, so architecture does not imply implemented capabilities.

## Deferred decisions

Per-user and shared dashboards, templates, profiles, role-based layouts, user-selectable sizes, enforced widget permissions and server synchronization require separate contracts. The v2 local document is an implementation detail, not a cloud-sync API. Conflict resolution, identity ownership and cross-device migration are deliberately deferred.

## Consequences and rollback

Navigation becomes view-oriented and feature placement becomes declarative. Widget registration metadata is a long-lived frontend contract, while render functions remain internal compiled code. Local persistence is resilient but device-specific. The previous `bigbrain.dashboard.layout.v1` value is left untouched and can be restored by reverting the shell and registry changes; no backend or module data migration is involved.
