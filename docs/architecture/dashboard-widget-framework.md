# Dashboard Views and Widget Framework

## Scope

The framework exposes seven instant client-side views through one reusable shell:

- **Hem** – a calm module launcher and attention-only system signal.
- **Familj** – Matlista, Inköpslista, Calendar and the existing honest Reminders placeholder.
- **Media** – Media Search, Downloads, Smart Shuffle, active jobs and Jellyfin/integration overview.
- **Finance** – a compiled read-only RESEARCH observation widget; no provider or trading controls.
- **Mer** – theme settings and discoverable secondary destinations.
- **AI** – registered placeholders for AI Chat, Agents, Voice Assistant, suggestions and automations; no AI capability is claimed yet.
- **Admin** – server metrics, recovery, container inventory, media integrations and future update information.

The mobile dock and desktop rail expose Hem, Familj, Media, Finance and Mer. AI and Admin remain registered, bookmark-safe application states reached through Mer. Selecting a view changes React state and writes local preferences; it does not reload the document.

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

## Responsive contextual-row contract

Dashboard launch rows use the shared `BBButton` content wrapper. A row that composes leading icon,
copy and trailing affordance must account for that wrapper rather than styling component children as if
they were direct button grid items. The standard pattern keeps the button and copy at `min-width: 0`,
lets the wrapper participate through `display: contents`, and assigns remaining width with
`minmax(0, 1fr)`. Long localized text may wrap naturally; it must not create a narrow vertical text
column or horizontal page overflow. Navigation icon paths share the same 1.7 stroke, round caps,
24×24 geometry and theme-token color behavior.
