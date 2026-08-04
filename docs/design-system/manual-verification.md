# Design system v1 manual verification

## BigBrain Web

1. Open BigBrain on a phone and confirm the dark default theme is readable.
2. Choose `Ljust` under `Tema`, reload and confirm the selection persists.
3. Verify navigation, Media, Smart Shuffle, Shopping List and Meal Planner.
4. Verify forms, primary/secondary/danger/ghost buttons, dialogs, badges and status messages.
5. Test at 320 px and a normal phone width without horizontal loss of actions.
6. Navigate by keyboard on desktop and confirm every interactive element has visible focus.
7. Enlarge text to 200% and confirm content/actions remain usable.

## Jellyfin (future separately approved installation only)

1. Back up current Jellyfin Custom CSS manually.
2. Open Jellyfin Web and, only after separate approval, paste `themes/jellyfin/bigbrain-jellyfin.css` into Custom CSS.
3. Check home/library views, a detail page, forms and dialogs.
4. Confirm playback controls remain visible and usable; stop and remove the theme if readability/function degrades.
5. Check mobile Jellyfin Web.
6. Open Jellyfin on Samsung Tizen, determine whether server CSS is actually applied, and record working/non-working selectors.
7. Remove the CSS to roll back; no database or client patch is involved.
