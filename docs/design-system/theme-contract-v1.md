# BigBrain theme contract v1

## Stable public contract

Themes are CSS files selected with `data-theme="<theme-id>"` on `document.documentElement`. IDs use lowercase ASCII kebab-case. The default is `obsidian-gold`; invalid stored values fall back to that default. An explicit BigBrain choice is never replaced by the operating-system color preference.

Required color tokens are `--bb-color-bg`, `--bb-color-bg-subtle`, `--bb-color-surface`, `--bb-color-surface-raised`, `--bb-color-surface-overlay`, `--bb-color-border`, `--bb-color-border-strong`, `--bb-color-text`, `--bb-color-text-muted`, `--bb-color-text-inverse`, `--bb-color-primary`, `--bb-color-primary-hover`, `--bb-color-primary-active`, `--bb-color-primary-contrast`, `--bb-color-accent`, `--bb-color-success`, `--bb-color-success-contrast`, `--bb-color-warning`, `--bb-color-warning-contrast`, `--bb-color-danger`, `--bb-color-danger-contrast`, `--bb-color-info`, `--bb-color-info-contrast`, and `--bb-color-backdrop`.

Required effect tokens are `--bb-shadow-sm`, `--bb-shadow-md`, `--bb-shadow-lg`, and `--bb-focus-ring`. A theme may optionally override typography, spacing, radii, control heights, motion and layout tokens declared in `styles/tokens.css`; their base definitions are the fallback model.

## Accessibility and testing

Text and interactive-state combinations should meet WCAG AA contrast (4.5:1 for normal text, 3:1 for large text and meaningful UI boundaries). Focus must remain clearly visible in every state. Themes must not defeat `prefers-reduced-motion`, encode status by color alone, or reduce touch targets below the component contract.

The shared component contract uses a 44 px minimum interaction target, visible `focus-visible` treatment, semantic status text, safe-area-aware navigation and reduced-motion fallbacks. To add a theme, change only semantic tokens, add its ID to the typed frontend/API allowlists and extend the contract tests.

## First-party themes

- `obsidian-gold` uses layered graphite surfaces, warm off-white text and restrained brass-gold interaction accents.
- `arctic-wind` uses deep cold-blue surfaces, cyan interaction accents and an airy aurora treatment from the supplied visual reference.
- `forest-night` uses deep green surface hierarchy and restrained amber interaction accents.

Legacy stored IDs (`bigbrain-dark`, `bigbrain-light`, `bigbrain-obsidian-gold`) are accepted as migration aliases and normalized to the new IDs. They are not selectable themes.

ThemeProvider använder det globala, versionssatta `GET/PUT /api/v1/settings/theme` som auktoritativ familjeinställning. Värdet persisteras i Settings-modulens separata SQLite-volume. Den befintliga nyckeln `bigbrain-theme` i `localStorage` är endast cache/offline-fallback och kan seeda servern en gång när serverinställningen ännu saknas; därefter hämtar mobil och desktop samma servervärde vid start och när klienten återfår fokus. Ogiltiga värden avvisas av API:t och lokal UI-state återställs om skrivningen misslyckas.

Nya teman förblir token-only: lägg till filen under `src/BigBrain.Web/src/styles/themes/`, importera den från `styles/index.css`, registrera samma ID i både frontendens och API:ts allowlist, lägg till svensk selector-label och utöka kontraktstesterna. Komponent- eller modulspecifika palettregler hör inte hemma i ett tema. `ThemeProvider` uppdaterar även browserns `theme-color` utan omladdning.

## Component and layout system

`AppShell` owns the global background, desktop rail, mobile dock, safe areas and content width. The five primary destinations are Hem, Familj, Media, Finance and Mer. AI and Admin are secondary destinations under Mer. `DashboardWorkspace` retains widget editing, hiding, ordering and collapse behavior. Shared styles define button, input, card, status, metric, empty/error, dialog and disclosure treatments from the same semantic tokens, spacing scale and radius scale. Numeric Finance/system values use tabular figures.

Mobile is the primary composition at approximately 390 px. The floating dock adds `env(safe-area-inset-bottom)` and content reserves the dock height. At desktop widths the dock becomes the persistent rail while content is constrained instead of stretched across the viewport.

## Stability boundary and versions

The `--bb-` tokens above, `data-theme`, and general `bb-` component class meanings are v1. Exact DOM structure, module-specific classes, layout selectors, React component props and compatibility aliases are not stable public API. Breaking token changes require a new contract version and migration notes.

A BigBrain theme changes BigBrain tokens. A Jellyfin adapter is separate compiled CSS with `--bb-jf-` tokens and Jellyfin-specific selectors. Jellyfin selectors and compatibility are never part of this contract and are revalidated against each Jellyfin Web version.
