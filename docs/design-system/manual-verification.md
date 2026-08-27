# Design system v1 manual verification

## BigBrain Web

1. Open BigBrain on a phone and confirm the dark default theme is readable.
2. Choose `Ljust` and then `Obsidian Gold` under `Tema`, reload after each choice and confirm the selection persists.
3. Verify navigation, Media, Smart Shuffle, Shopping List and Meal Planner.
4. Verify forms, primary/secondary/danger/ghost buttons, dialogs, badges and status messages.
5. Test at 320 px and a normal phone width without horizontal loss of actions.
6. Navigate by keyboard on desktop and confirm every interactive element has visible focus.
7. Enlarge text to 200% and confirm content/actions remain usable.
8. In `Obsidian Gold`, confirm layered graphite backgrounds, restrained gold actions/selection, warm readable text, distinct semantic status colors and visible hover/focus/active/disabled states.
9. At 390×844 and 430×932, wait for asynchronous content, scroll each view to its spatially last action and confirm it rests above the fixed dock. The combined `.bb-page.dashboard-workspace` rule owns dock clearance; pages must not add magic bottom margins.
10. Empty text/search/textarea controls must retain a strong material boundary. Busy actions keep their original dimensions and use the single three-dot indicator with screen-reader status; reduced motion removes its movement. Missing artwork must render `BBMediaArtwork`, never a browser broken-image glyph.

## Jellyfin (installed; manual visual approval pending)

The adapter was backed up and installed additively on 2026-08-04. Follow the
[operations runbook](../operations/runbooks/jellyfin-bigbrain-theme.md) for any future
install, update or rollback.

1. Check home/library views, a detail page, forms and dialogs.
2. Confirm playback controls remain visible and usable; stop and remove the theme if readability/function degrades.
3. Check mobile Jellyfin Web.
4. Open Jellyfin on Samsung Tizen, determine whether server CSS is actually applied, and record working/non-working selectors.
5. If anything degrades, use the documented block removal or backup restore; no database
   or client patch is involved and a server restart is normally unnecessary.

The optional `themes/jellyfin/bigbrain-obsidian-gold.css` variant is not installed automatically. If separately approved for manual installation, repeat all Jellyfin checks above and confirm the Samsung Tizen client actually loads it. Jellyfin's administration dashboard does not load server Custom CSS in the verified 10.11 baseline and remains outside adapter scope.
