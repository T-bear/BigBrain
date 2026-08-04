# BigBrain theme contract v1

## Stable public contract

Themes are CSS files selected with `data-theme="<theme-id>"` on `document.documentElement`. IDs use lowercase ASCII kebab-case and must not reuse the reserved `bigbrain-` prefix outside first-party themes. The default is `bigbrain-dark`; invalid stored values fall back to the normal default/system preference flow.

Required color tokens are `--bb-color-bg`, `--bb-color-bg-subtle`, `--bb-color-surface`, `--bb-color-surface-raised`, `--bb-color-surface-overlay`, `--bb-color-border`, `--bb-color-border-strong`, `--bb-color-text`, `--bb-color-text-muted`, `--bb-color-text-inverse`, `--bb-color-primary`, `--bb-color-primary-hover`, `--bb-color-primary-active`, `--bb-color-primary-contrast`, `--bb-color-accent`, `--bb-color-success`, `--bb-color-success-contrast`, `--bb-color-warning`, `--bb-color-warning-contrast`, `--bb-color-danger`, `--bb-color-danger-contrast`, `--bb-color-info`, `--bb-color-info-contrast`, and `--bb-color-backdrop`.

Required effect tokens are `--bb-shadow-sm`, `--bb-shadow-md`, `--bb-shadow-lg`, and `--bb-focus-ring`. A theme may optionally override typography, spacing, radii, control heights, motion and layout tokens declared in `styles/tokens.css`; their base definitions are the fallback model.

## Accessibility and testing

Text and interactive-state combinations should meet WCAG AA contrast (4.5:1 for normal text, 3:1 for large text and meaningful UI boundaries). Focus must remain clearly visible in every state. Themes must not defeat `prefers-reduced-motion`, encode status by color alone, or reduce touch targets below the component contract.

To add a theme: copy `themes/example-theme.css`, change only tokens, add its ID to the typed allowlist and Swedish selector labels, then test default/fallback/persistence, keyboard focus, 320 px layout, text enlargement, critical modules and production build in both themes. A future JSON generator may emit this token file, but JSON and generators are not part of v1.

## First-party themes

- `bigbrain-dark` (`Mörkt`) is the default dark blue-violet theme.
- `bigbrain-light` (`Ljust`) follows the light system preference when no choice is stored.
- `bigbrain-obsidian-gold` (`Obsidian Gold`) uses layered graphite surfaces, warm off-white text and restrained brass-gold interaction accents. Semantic success, warning, danger and information colors remain distinct.

The selector stores the chosen ID under the existing `bigbrain-theme` key. Reloading restores a valid choice; an invalid stored value safely returns to `bigbrain-dark`. New themes remain token-only: add a file under `src/BigBrain.Web/src/styles/themes/`, import it from `styles/index.css`, register the ID in `theme.ts`, add the Swedish selector label and extend the contract tests. Component- or module-specific palette rules do not belong in a theme.

## Stability boundary and versions

The `--bb-` tokens above, `data-theme`, and general `bb-` component class meanings are v1. Exact DOM structure, module-specific classes, layout selectors, React component props and compatibility aliases are not stable public API. Breaking token changes require a new contract version and migration notes.

A BigBrain theme changes BigBrain tokens. A Jellyfin adapter is separate compiled CSS with `--bb-jf-` tokens and Jellyfin-specific selectors. Jellyfin selectors and compatibility are never part of this contract and are revalidated against each Jellyfin Web version.
