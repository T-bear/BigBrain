# BB-107 — Owner UX Remediation after BB-106

## Metadata

- Date: 2026-08-27
- Scope: BigBrain API, Web, tests and documentation; no user-data mutation
- Sanitization: This report contains no secrets, private addresses, owner data or raw service payloads.

Detta är en sanerad GitHub-version. Lokal rå runtime-evidens publiceras inte.

## Status

**IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / OWNER UX REVIEW PENDING**

## Owner review and scope

BB-106's automated and deployed status did not constitute owner approval. Real-device review found a
broken Home row composition, an ambiguous Media mutation response, inconsistent search waiting UI and
an audiobook catalogue that still dominated the Media overview. BB-107 remediates those findings without
changing Finance, scheduler, governor, Sentinel, acquisition policy or existing media data.

## Changes

### Root causes and remediation

| Area | Root cause | Remediation |
| --- | --- | --- |
| Home mobile | `BBButton` adds a content wrapper; the Home three-column grid therefore treated all children as one grid item and compressed nested text. | The shared contextual-row rule makes the wrapper participate with `display: contents`, keeps `min-width: 0`, and preserves icon/text/affordance columns. |
| Media first click | Arr could commit before a timeout/response failure. API released the preview without reconciling the write and Web generated a fresh retry key. | Web retains one idempotency key per dialog. API marks the attempted write and performs bounded exact-ID read-after-write reconciliation for ambiguous timeout/5xx responses. |
| Media search | Local CSS overrode the design-system button and a separate English loading sentence duplicated status. | The standard dimensions-stable `BBButton busy` contract is authoritative; the redundant sentence is removed. |
| Audiobook library | Bounded pagination was still rendered directly in the overview. | Default Media view renders Continue Listening plus at most four recent covers. **Alla ljudböcker** owns search, sort and pagination. Detail remains a separate surface. |
| Identity | Some navigation/search affordances retained generic glyph treatment. | Family, Media and Finance use one stroke/geometry system; search uses the shared SVG icon and missing covers retain the theme-aware BigBrain B artwork primitive. |

### First-click safety contract

The API performs exactly one provider POST. It records `AddAttempted` immediately before that POST. If
the response is ambiguous, it performs an independently bounded lookup using TVDB/TMDB identity; a match
completes the same BigBrain action. Authentication and provider 4xx failures remain failures. The Web
retry reuses its original opaque idempotency key, and the busy state blocks concurrent activation.
No live library mutation was used for QA; the exact commit-before-timeout scenario is covered by a safe
provider fixture.

## Evidence

- Focused: 26 API request tests; 21 Home/Media/Audiobook Web tests.
- Complete: 556 API, 32 Sentinel and 137 Web tests; zero failures.
- Builds: .NET Release and Vite production passed with zero .NET warnings/errors.
- Compose configuration passed. Only API and Web were rebuilt/recreated; both reached healthy state.
- Browser matrix: 390×844, 430×932 and 1440×900 across Obsidian Gold, Forest Night and Arctic Wind.
  No horizontal overflow was observed. Home text cells measured 259 px and 299 px at the two mobile
  widths; the final non-fixed Media action was reachable above the dock. Inputs retained visible borders.

## Security

No existing acquisition was cancelled or restarted. No media file was changed. No secret or provider
URL is included. Finance remains research-only and unchanged. The exact mutation-before-timeout case is
tested with an isolated provider fixture rather than the owner's library. Authentication, Problem Details,
opaque identifiers and least-privilege adapter boundaries remain intact.

## Remaining work

BB-107 is not owner UX approved. Owner review should exercise Home on the physical iPhone/PWA, one normal
Media add, search busy feedback, the compact audiobook overview, **Alla ljudböcker**, missing artwork and
all three themes. Embedded Audiobookshelf pause/seek/chapter controls remain the previously documented
BB-106 partial API-boundary limitation; this sprint does not invent unsupported playback APIs.

## Resumption

Start from the published BB-107 commit and current `docs/STATUS.md`. Owner UX review remains required on
the physical iPhone/PWA. Do not infer owner approval from automated or headless-browser evidence. The
separate embedded Audiobookshelf playback-session limitation remains documented under BB-106; Sentinel's
independent restart issue remains outside this scope. BB-099 remains **TECHNICALLY COMPLETE / OWNER UX REVIEW PENDING**.
