# Dashboard Views and Widget Framework

## Scope

The framework now exposes five instant client-side views:

- **Hem** – Matlista, Inköpslista and honest placeholders for Calendar and Reminders.
- **Media** – Media Search, Downloads, Smart Shuffle, active jobs and Jellyfin/integration overview.
- **Finance** – a compiled read-only RESEARCH observation widget; no provider or trading controls.
- **AI** – registered placeholders for AI Chat, Agents, Voice Assistant, suggestions and automations; no AI capability is claimed yet.
- **Admin** – server metrics, container inventory, media integrations and future update information.

The bottom and desktop navigation use the same `DashboardRegistry`. Selecting a view changes React state and writes local preferences; it does not reload the document.

## Widget contract

`WidgetDefinition` requires:

| Field | Responsibility |
|---|---|
| `id` | Stable unique persistence identity |
| `title`, `description`, `icon`, `category` | User-facing discovery metadata |
| `defaultView`, `supportedViews` | Default placement and valid library destinations |
| `defaultSize`, `minimumSize` | Declarative layout metadata; user sizing is deferred |
| `permissions` | Reserved metadata only; Phase 1 does not enforce roles |
| `render()` | Compiled first-party React rendering |

`ApplicationWidgetRegistry` validates definitions and answers which widgets support a view. `DashboardWorkspace` only consumes registry output. Feature modules are therefore registered once rather than manually arranged in the dashboard shell.

## Editing and persistence

Every view exposes an edit mode and widget library. Edit mode enables HTML drag ordering plus explicit up/down controls for keyboard and touch use. Widgets can be hidden and restored. Each widget can be collapsed outside edit mode as well. None of these actions mutate feature data.

Preferences use localStorage key `bigbrain.dashboard.preferences.v2`:

```text
version, activeView
views[view].order
views[view].hidden
views[view].collapsed
```

Malformed versions and unknown IDs are ignored safely. New default widgets are appended. Writes failing in privacy mode leave the current in-memory layout usable.

## Extension rule

Add a future first-party widget by registering one complete `WidgetDefinition`; do not import it into `App` or `DashboardWorkspace`. A real permissions service, server synchronization, templates, shared/per-user ownership and custom sizing require later decisions and are tracked in BB-027.

See [Proposed ADR 0014](../adr/0014-dashboard-views-and-widget-framework.md).
